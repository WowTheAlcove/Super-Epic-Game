using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Netcode;
using System.Runtime.CompilerServices;
using UnityEngine.Profiling;

public class DataPersistenceManager : NetworkBehaviour {
    public static DataPersistenceManager Instance { get; private set; }

    [Header("File Storage Config")]
    [SerializeField] private string manualSaveFileName;
    [SerializeField] private string autoSaveFileName;

    [Header("Folder to store save data (Do not edit)")]
    [SerializeField] private string persistentDataPath;
    [Space]

    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private PrefabDatabase dynamicPrefabDatabase;

    // Loading state management
    private bool isLoading = false;
    private HashSet<ulong> clientsDoneWithLoading = new HashSet<ulong>();
    private float originalTimeScale = 1f;
    private SimulationMode originalPhysicsMode;
    private SimulationMode2D originalPhysics2DMode;


    private GameData currentGameData; //this needs to be stored outside of methods so that players can join and get their data loaded
    private FileDataHandler dataHandler;

    private List<IDataPersistence> dataPersistenceObjects;
    private List<DynamicPrefabStorer> dynamicPrefabStorers;


    private void Awake() {
        if (Instance != null) {
            Debug.LogError("DataPersistenceManager tried to create an instance, but there already was one");
        }
        Instance = this;

        persistentDataPath = Application.persistentDataPath;

        // Store original physics settings
        originalPhysicsMode = Physics.simulationMode;
        originalPhysics2DMode = Physics2D.simulationMode;
    }

    private void Start() {
        this.dataHandler = new FileDataHandler(persistentDataPath, manualSaveFileName, autoSaveFileName); //Application.persistentDataPath is default unity place for storing persistent data
        this.dataPersistenceObjects = FindAllDataPersistenceObjects();

    }

    #region OnNetworkSpawn() ========== auto save and initialization ==========
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer) {
            StartCoroutine(WaitForAllNetworkObjectsToSpawn());
            NetworkManager.Singleton.OnConnectionEvent += NetworkManager_OnConnectionEvent;
        }
    }


    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer)
        {
            NetworkManager.Singleton.OnConnectionEvent -= NetworkManager_OnConnectionEvent;
        }
    }


    private IEnumerator WaitForAllNetworkObjectsToSpawn() {
        float timeout = 30f; // Prevent infinite waiting
        float elapsed = 0f;

        while (elapsed < timeout) {
            // Find all NetworkObjects in the scene
            NetworkObject[] allNetworkObjects = FindObjectsByType<NetworkObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            // Check if all NetworkObjects are spawned
            bool allSpawned = true;
            foreach (NetworkObject netObj in allNetworkObjects) {
                if (!netObj.IsSpawned) {
                    allSpawned = false;
                    break;
                }
            }

            if (allSpawned) {
                //Debug.Log($"All {allNetworkObjects.Length} NetworkObjects are spawned. Performing initial save.");
                SaveGame(SaveFileType.Auto);
                yield break;
            }

            // Wait a bit before checking again
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        // Timeout fallback
        Debug.LogWarning("Timeout waiting for all NetworkObjects to spawn. Saving anyway.");
        SaveGame(SaveFileType.Auto);
    }

    #endregion

    #region =========== Load Game ==========
    //pauses game, clears and respawns dynamic prefabs, pushes loaded data to all scripts using IDataPersistence
    public void LoadGame(SaveFileType saveFileType) {
        if (!IsServer) {
            //this command can only be run on the server
            Debug.LogWarning("LoadGame() didn't go through: can only be run on the server");
            return;
        }

        // Start loading state - this pauses the game
        SetLoadingStateRpc(true);
        clientsDoneWithLoading.Clear();

        //load any saved data from file using data handler
        this.currentGameData = dataHandler.Load(saveFileType);

        //if no data can be loaded, make a new game
        if (this.currentGameData == null) {
            Debug.LogWarning("No save data found, creating new game data");
            this.currentGameData = new GameData();
        }

        ClearExistingDynamicPrefabs();
        RespawnSaveStateOfDynamicPrefabs(currentGameData);

        //push loaded data to all scripts in the game that use that data
        this.dataPersistenceObjects = FindAllDataPersistenceObjects();

        foreach (IDataPersistence persistentObject in dataPersistenceObjects) {
            persistentObject.LoadData(currentGameData);
        }
    }

    //method to be called by clients player controllers when they have loaded their position
    [Rpc(SendTo.Server)]
    public void ClientLoadedPositionRpc(ulong clientId, Vector3 clientReportedPosition) {
        if (!isLoading) {
            //we only care about this if this player position is being set during a loading state
            return;
        }
        //Debug.Log($"Client {clientId} reported position loaded: {clientReportedPosition}");

        // Find the player with this client ID and verify position
        StartCoroutine(VerifyClientPosition(clientId, clientReportedPosition));
    }

    //Pre: A client claims to have loaded its position on their end
    //Server waits until the position the client said they loaded to matches the server's position for that player, then adds them to the ready client list
    private IEnumerator VerifyClientPosition(ulong clientId, Vector3 clientReportedPosition) {
        //Debug.Log("Verifying position for client " + clientId);
        PlayerController playerControllerOfClient = FindPlayerControllerByClientId(clientId);

        if (playerControllerOfClient == null) {
            Debug.LogError($"Could not find PlayerController for client {clientId}");
            yield break;
        }

        float maxWaitTime = 100f;
        float waitTime = 0f;
        float positionTolerance = 0.1f;

        while (waitTime < maxWaitTime) {
            Vector3 serverPosition = playerControllerOfClient.transform.position;
            float distance = Vector3.Distance(serverPosition, clientReportedPosition);

            //Debug.Log($"Verifying client {clientId}: Server pos {serverPosition}, Client pos {clientReportedPosition}, Distance {distance}");

            if (distance <= positionTolerance) {
                // Positions match! Client is ready
                clientsDoneWithLoading.Add(clientId);
                //Debug.Log($"Client {clientId} position verified and ready. {clientsReadyForGameplay.Count}/{NetworkManager.Singleton.ConnectedClients.Count} clients ready.");

                CheckAllClientsDoneLoading();
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.1f); // Use realtime so this works during timeScale = 0
            waitTime += 0.1f;
        }

        // Timeout - position never synced properly
        Debug.LogError($"Client {clientId} position verification timed out. Accepting anyway.");
        clientsDoneWithLoading.Add(clientId);
        CheckAllClientsDoneLoading();
    }
    
    //Pre: A client has been added to the list of clients done loading
    //unpauses if all clients are ready
    private void CheckAllClientsDoneLoading() {
        if (isLoading && clientsDoneWithLoading.Count >= NetworkManager.Singleton.ConnectedClients.Count) {
            //Debug.Log("All clients ready - resuming gameplay");
            clientsDoneWithLoading.Clear();
            SetLoadingStateRpc(false);
        }
    }
    
    private void ClearExistingDynamicPrefabs() {
        dynamicPrefabStorers = FindAllDynamicPrefabStorers();
        foreach (DynamicPrefabStorer oldPrefab in dynamicPrefabStorers) {
            NetworkObject oldPrefabNORef = oldPrefab.GetComponent<NetworkObject>();
            if (oldPrefabNORef == null) {
                //if the prefab instance did not have a NetworkObject component
                Debug.LogError($"There existed a dynamic prefab: " + oldPrefab.name + " with no Network Object component");
            } else {
                //if the prefab instance did have a NetworkObject component
                oldPrefabNORef.Despawn(true);
            }
        }
    }

    private void RespawnSaveStateOfDynamicPrefabs(GameData gameDataToRespawnFrom) {

        //respawn each prefab, and give it whatever GUID it was dynamically assigned in the previous save state
        foreach (KeyValuePair<string, string> prefabAndID in currentGameData.allDynamicPrefabs) {
            GameObject prefabToReinstantiate = dynamicPrefabDatabase.GetPrefabFromID(prefabAndID.Value);

            if (prefabToReinstantiate == null) {
                //if it can't find the prefab associated with the prefab ID
                Debug.LogError("Could not find id: " + prefabAndID.Value + " in prefabDatabase");
                continue;
            }
            DynamicPrefabStorer reinstantiatedObject = Instantiate(prefabToReinstantiate).GetComponent<DynamicPrefabStorer>();
            if (reinstantiatedObject == null) {
                //if it can't find the dynamic prefab storer on the instantiated dynamic prefab
                Debug.LogError($"The id: {prefabAndID.Value} instantiated an object with no DynamicPrefabStorer");
                continue;
            }
            reinstantiatedObject.TryGetComponent<NetworkObject>(out NetworkObject reinstantiatedObjectNORef);
            if (reinstantiatedObjectNORef == null) {
                //if the dynamic prefab does not have a NO component
                Debug.LogError($"The id: {prefabAndID.Value} instantiated an object with no NetworkObject component");
                continue;
            }

            reinstantiatedObjectNORef.Spawn();
            reinstantiatedObject.SetUniqueID(prefabAndID.Key);
        }
    }
    #endregion

    #region ====== Save Game =======
    public void SaveGame(SaveFileType saveFileType) {
        if (!IsServer) {
            //this command can only be run on the server

            //Debug.LogWarning("SaveGame() didn't go through: can only be run on the server");

            return;
        }

        if (isLoading)
        {
            Debug.LogWarning("SaveGame() was called while still in a previous loading state. Cancelling...");
            return;
        }
        
        currentGameData = new GameData();
        
        //find all dynamic prefab storers and save their prefabID and GUID
        this.dynamicPrefabStorers = FindAllDynamicPrefabStorers();
        foreach (DynamicPrefabStorer dynamicPrefabStorer in this.dynamicPrefabStorers) {
            currentGameData.allDynamicPrefabs.Add(dynamicPrefabStorer.GetUniqueID(), dynamicPrefabStorer.GetPrefabID());
        }
        
        //take data from all scripts using IDataPersistent and save it
        this.dataPersistenceObjects = FindAllDataPersistenceObjects();
        foreach (IDataPersistence persistentObject in dataPersistenceObjects) {
            persistentObject.SaveData(ref currentGameData);
        }
        
        //Wait until dialogue manager is done saving before saving
        DialogueManager dialogueManager = FindAnyObjectByType<DialogueManager>();
        if (dialogueManager != null)
        {
            StartCoroutine(SaveGameAfterDialogueSaved(saveFileType, dialogueManager));
        }
        else
        {
            //takes the gamedata object and saves it, serializing it in the filedatahandler's method
            dataHandler.Save(currentGameData, saveFileType);
        }

    }
    
    //Wait until Dialogue Manager has saved
    //Save game data with dataHandler
    private IEnumerator SaveGameAfterDialogueSaved(SaveFileType saveFileType, DialogueManager dialogueManager)
    {
        //Register callback so DialogueManager can notify us when done
        // bool dialogueSaveComplete = false;
        // dialogueManager.RegisterSaveCompleteCallback(() => { dialogueSaveComplete = true; });
    
        //Wait for DialogueManager to finish (it will set the flag when done)
        float timeout = 15f;
        float elapsed = 0f;
    
        while (dialogueManager.IsSaving() && elapsed < timeout)
        {
            Debug.Log("Waiting for dialogue to finish saving so DPM can save");
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
    
        if (dialogueManager.IsSaving())
        {
            Debug.LogWarning("Timeout waiting for DialogueManager save to complete. Saving anyway.");
        }

        //takes the gamedata object and saves it, serializing it in the filedatahandler's method
        dataHandler.Save(currentGameData, saveFileType);
    }

    // Method to set loading state and pause/resume gameplay
    [Rpc(SendTo.ClientsAndHost)]
    private void SetLoadingStateRpc(bool loading) {
        if (isLoading == loading) return; // Prevent redundant calls

        isLoading = loading;

        if (loading) {
            originalTimeScale = Time.timeScale;

            Time.timeScale = 0f;
            Physics.simulationMode = SimulationMode.Script;
            Physics2D.simulationMode = SimulationMode2D.Script;

            //Debug.Log("Game paused for loading...");
        } else {
            // Resume gameplay systems
            Time.timeScale = originalTimeScale;
            Physics.simulationMode = originalPhysicsMode;
            Physics2D.simulationMode = originalPhysics2DMode;

            //Debug.Log("Game resumed after loading.");
        }
    }
    #endregion

    //finds the player controller belonging to given client ID
    private PlayerController FindPlayerControllerByClientId(ulong clientId) {
        // Find all player controllers and return the one with matching OwnerClientId
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (PlayerController player in allPlayers) {
            if (player.OwnerClientId == clientId) {
                return player;
            }
        }

        return null;
    }

    //Calls a PlayerController script's LoadData()
    public void LoadPlayerData(PlayerController player) {
        if (currentGameData == null) {
            Debug.LogWarning("No game data available to load player data from");
            return;
        }

        // Load the player's specific data
        player.LoadData(currentGameData);
    }

    private List<IDataPersistence> FindAllDataPersistenceObjects() {
        IEnumerable<IDataPersistence> dataPersistenceObjects = FindObjectsByType<MonoBehaviour>
            (FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IDataPersistence>();

        return new List<IDataPersistence>(dataPersistenceObjects);
    }

    private List<DynamicPrefabStorer> FindAllDynamicPrefabStorers() {
        IEnumerable<DynamicPrefabStorer> dataPersistenceObjects = FindObjectsByType<DynamicPrefabStorer>
            (FindObjectsInactive.Include, FindObjectsSortMode.None);

        return new List<DynamicPrefabStorer>(dataPersistenceObjects);
    }

    public ItemDatabase GetItemDatabase() {
        return itemDatabase;
    }

    public ItemSO GetItemSOFromItemID(int itemID) {
        ItemSO matchingItem = null;
        foreach (ItemSO item in itemDatabase.allItems) {
            if (item.itemID == itemID) {
                matchingItem = item;
                break; // Exit the loop once a match is found
            }
        }
        return matchingItem;
    }

    // returns whether or not the DPM has any game data loaded or saved
    public bool HasAnUpdatedGameData() {
        return currentGameData != null;
    }
    
    //When a client loads, load all the data it needs 
    //Automatically only runs on server
    private void NetworkManager_OnConnectionEvent(NetworkManager NetworkManager, ConnectionEventData connectionEventData)
    {
        if (connectionEventData.EventType == ConnectionEvent.ClientDisconnected ||
                connectionEventData.EventType == ConnectionEvent.PeerDisconnected)//If this event was from someone disconnecting
        {
            return;
        }
        if (currentGameData == null) //If the game hasn't even loaded or anything yet
        {
            return;
        }
        ulong connectedClientId = connectionEventData.ClientId;
        FindPlayerControllerByClientId(connectedClientId).LoadData(currentGameData); //Load the PlayerController's data
        
        DialogueManager dialogueManager = FindAnyObjectByType<DialogueManager>();
        dialogueManager.LoadDataForClient(connectedClientId, currentGameData);
    }
    
}