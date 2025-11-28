using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Netcode;
using System.Runtime.CompilerServices;

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
    private HashSet<ulong> clientsReadyForGameplay = new HashSet<ulong>();
    private float originalTimeScale = 1f;
    private SimulationMode originalPhysicsMode;
    private SimulationMode2D originalPhysics2DMode;


    private GameData mostRecentlyUpdatedGameData; //this needs to be stored outside of methods so that players can join and get their data loaded
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

    #region OnNetworkSpawn() auto save
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer) {
            StartCoroutine(WaitForAllNetworkObjectsToSpawn());
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


    private void OnApplicationQuit() {
        //No auto save for now as I think it causes a save state w/o player prefabs
        //SaveGame();
    }

    //deletes dynamic prefabs, and pushes default values for GameData() (mostly empty lists)
    public void ResetGame() {
        if (!IsServer) {
            //this command can only be run on the server
            Debug.LogWarning("ResetGame() didn't go through: can only be run on the server");
            return;
        }

        LoadGame(SaveFileType.Auto);
    }

    //pauses game, clears and respawns dynamic prefabs, pushes loaded data to all scripts using IDataPersistence
    public void LoadGame(SaveFileType saveFileType) {
        if (!IsServer) {
            //this command can only be run on the server
            Debug.LogWarning("LoadGame() didn't go through: can only be run on the server");
            return;
        }

        // Start loading state - this pauses the game
        SetLoadingStateRpc(true);
        clientsReadyForGameplay.Clear();

        //load any saved data from file using data handler
        this.mostRecentlyUpdatedGameData = dataHandler.Load(saveFileType);

        //if no data can be loaded, make a new game
        if (this.mostRecentlyUpdatedGameData == null) {
            Debug.LogWarning("No save data found, creating new game data");
            this.mostRecentlyUpdatedGameData = new GameData();
        }

        ClearExistingDynamicPrefabs();
        RespawnSaveStateOfDynamicPrefabs(mostRecentlyUpdatedGameData);

        //push loaded data to all scripts in the game that use that data
        this.dataPersistenceObjects = FindAllDataPersistenceObjects();

        foreach (IDataPersistence persistentObject in dataPersistenceObjects) {
            persistentObject.LoadData(mostRecentlyUpdatedGameData);
        }
    }

    [Rpc(SendTo.Server)]
    //method to be called by clients player controllers when they have loaded their position
    public void ClientLoadedPositionRpc(ulong clientId, Vector3 clientReportedPosition) {
        if (!isLoading) {
            //we only care about this if this player position is being set during a loading state
            return;
        }
        //Debug.Log($"Client {clientId} reported position loaded: {clientReportedPosition}");

        // Find the player with this client ID and verify position
        StartCoroutine(VerifyClientPosition(clientId, clientReportedPosition));
    }

    //ran on server, waits until the position the client said they loaded to matches the server's position for that player, then adds them to the ready client list
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
                clientsReadyForGameplay.Add(clientId);
                //Debug.Log($"Client {clientId} position verified and ready. {clientsReadyForGameplay.Count}/{NetworkManager.Singleton.ConnectedClients.Count} clients ready.");

                CheckAllClientsReady();
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.1f); // Use realtime so this works during timeScale = 0
            waitTime += 0.1f;
        }

        // Timeout - position never synced properly
        Debug.LogError($"Client {clientId} position verification timed out. Accepting anyway.");
        clientsReadyForGameplay.Add(clientId);
        CheckAllClientsReady();
    }

    public void SaveGame(SaveFileType saveFileType) {

        if (!IsServer) {
            //this command can only be run on the server

            //Debug.LogWarning("SaveGame() didn't go through: can only be run on the server");

            return;
        }

        mostRecentlyUpdatedGameData = new GameData();


        //find all dynamic prefab storers and save their prefabID and GUID
        this.dynamicPrefabStorers = FindAllDynamicPrefabStorers();
        foreach (DynamicPrefabStorer dynamicPrefabStorer in this.dynamicPrefabStorers) {
            mostRecentlyUpdatedGameData.allDynamicPrefabs.Add(dynamicPrefabStorer.GetUniqueID(), dynamicPrefabStorer.GetPrefabID());
        }

        //take data from all scripts using IDataPersistent and save it
        this.dataPersistenceObjects = FindAllDataPersistenceObjects();
        foreach (IDataPersistence persistentObject in dataPersistenceObjects) {
            persistentObject.SaveData(ref mostRecentlyUpdatedGameData);
        }

        //takes the gamedata object and saves it, serializing it in the filedatahandler's method
        dataHandler.Save(mostRecentlyUpdatedGameData, saveFileType);
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

    //unpauses if all clients are ready
    private void CheckAllClientsReady() {
        if (isLoading && clientsReadyForGameplay.Count >= NetworkManager.Singleton.ConnectedClients.Count) {
            //Debug.Log("All clients ready - resuming gameplay");
            clientsReadyForGameplay.Clear();
            SetLoadingStateRpc(false);
        }
    }

    public void LoadPlayerData(PlayerController player) {
        if (mostRecentlyUpdatedGameData == null) {
            Debug.LogWarning("No game data available to load player data from");
            return;
        }

        // Load the player's specific data
        player.LoadData(mostRecentlyUpdatedGameData);
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
        return mostRecentlyUpdatedGameData != null;
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
        foreach (KeyValuePair<string, string> prefabAndID in mostRecentlyUpdatedGameData.allDynamicPrefabs) {
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
}
