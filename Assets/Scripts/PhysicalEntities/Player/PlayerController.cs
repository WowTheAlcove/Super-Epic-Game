using Cinemachine;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour, IDataPersistence {
    [Header("Player stats")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float interactionRange = 4f;

    [Header("References to other components on this GO")]
    [SerializeField] private HotbarStateStorer hotbarStateStorer;
    [SerializeField] private InventoryStateStorer inventoryStateStorer;
    [SerializeField] private BingoBongoStorer bingoBongoStorer;

    private Vector3 defaultPosition;
    private bool isMoving;
    public bool IsMoving => isMoving; // Public property to access isMoving state
    public NetworkVariable<int> playerIndex = new NetworkVariable<int>();
    public int PlayerIndex => playerIndex.Value;

    private Rigidbody2D myRigidBody;
    private Camera mainCamera;
    private PlayerInputActions myPlayerInputActions;
    private ItemSO currentEquippedItemSO = null;
    private BehaviourItem currentEquippedItemBehaviour;
    public event EventHandler<OnEquippedItemEventArgs> OnEquippedItem;
    public class OnEquippedItemEventArgs : EventArgs {
        public ItemSO newEquippedItemSO;
    }

    private int interactableLayerMask = 1 << 8; // Layer 8 is the "Interactable" layer


    private void Awake() {
        myRigidBody = GetComponent<Rigidbody2D>();
    }

    private void Start() {
        mainCamera = Camera.main; //for raycasting

        //set the cinemachine camera to follow this player
        Cinemachine.CinemachineVirtualCamera cmCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
        cmCamera.Follow = this.transform;

        UIController.Instance.OnInputFreezingMenuEnabled += UIController_OnInputFreezingMenuEnabled;
        UIController.Instance.OnInputFreezingMenuDisabled += UIController_OnInputFreezingMenuDisabled;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        // Only set up input for the owner of this player
        if (IsOwner) {
            SetupInput();
            InitializeHotbar();
            SubscribeEquipRefreshToInventoryChanges();

        }
        

        //stores the default position of the player (asssigned in editor I think by Network Manager)
        defaultPosition = transform.position;

        //server checks if the DPM has initialized game data
        if (IsServer) {
            if (DataPersistenceManager.Instance.HasAnUpdatedGameData()) {
                //if DPM has an updated state of game data
                InitializeFromDataPersistenceManager();
            } else {
                //if DPM doesn't have an updated state of game data
                SetDefaultValues();
            }
            playerIndex.Value = (int)OwnerClientId;
        }

        //all users subscribe to selected hotbar slot changes to refresh the equipped item
        hotbarStateStorer.SelectedSlotIndex.OnValueChanged += (oldValue, newValue) => {
            EquipItemInSelectedHotbarSlot();
        };
    }


    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        DeregisterInput();

        UnsubscribeEquipRefreshToInventoryChanges();
    }

    #region methods for initializing/deinitializing input, subscribing to inventory changes, and loading player data from DPM

    //set up the input actions using the input system
    private void SetupInput() {
        myPlayerInputActions = new PlayerInputActions();
        myPlayerInputActions.Player.Enable();

        myPlayerInputActions.Player.HotbarSlot1Pressed.performed += HotbarSlot1Pressed_performed;
        myPlayerInputActions.Player.HotbarSlot2Pressed.performed += HotbarSlot2Pressed_performed;
        myPlayerInputActions.Player.HotbarSlot3Pressed.performed += HotbarSlot3Pressed_performed;
        myPlayerInputActions.Player.HotbarSlot4Pressed.performed += HotbarSlot4Pressed_performed;
        myPlayerInputActions.Player.UseEquippedItem.performed += UseEquippedItem_performed;
        myPlayerInputActions.Player.Interact.performed += Interact_performed;

        //oh my god
        myPlayerInputActions.Player.BingoBongo.performed += BingoBongo_performed;
    }

    private void DeregisterInput() {
        if (myPlayerInputActions != null) {//if the player input actions are still around
            myPlayerInputActions.Player.HotbarSlot1Pressed.performed -= HotbarSlot1Pressed_performed;
            myPlayerInputActions.Player.HotbarSlot2Pressed.performed -= HotbarSlot2Pressed_performed;
            myPlayerInputActions.Player.HotbarSlot3Pressed.performed -= HotbarSlot3Pressed_performed;
            myPlayerInputActions.Player.HotbarSlot4Pressed.performed -= HotbarSlot4Pressed_performed;

            myPlayerInputActions.Player.UseEquippedItem.performed -= UseEquippedItem_performed;
            myPlayerInputActions.Player.Interact.performed -= Interact_performed;

            myPlayerInputActions.Player.BingoBongo.performed -= BingoBongo_performed;

            myPlayerInputActions.Dispose();
        }
    }

    //Asks DPM to load call LoadData() on player, while passsing the saved GameData reference
    private void InitializeFromDataPersistenceManager() {
        // Request player data from DPM so that we can load it
        if (IsServer && DataPersistenceManager.Instance != null) {
            DataPersistenceManager.Instance.LoadPlayerData(this);
        }
    }

    private void SubscribeEquipRefreshToInventoryChanges() {
        inventoryStateStorer.CurrentInventory.OnListChanged += CurrentInventory_OnListChanged;
        hotbarStateStorer.CurrentInventory.OnListChanged += CurrentHotbar_OnListChanged;
    }
    private void UnsubscribeEquipRefreshToInventoryChanges() {
        inventoryStateStorer.CurrentInventory.OnListChanged -= CurrentInventory_OnListChanged;
        hotbarStateStorer.CurrentInventory.OnListChanged -= CurrentHotbar_OnListChanged;
    }
    private void CurrentHotbar_OnListChanged(NetworkListEvent<int> changeEvent) {
        EquipItemInSelectedHotbarSlot();
    }
    private void CurrentInventory_OnListChanged(NetworkListEvent<int> changeEvent) {
        EquipItemInSelectedHotbarSlot();
    }
    private void UIController_OnInputFreezingMenuDisabled(object sender, UIController.OnControlFreezingMenuDisabledEventArgs e) {
        if (!e.thereAreOtherInputFreezingMenus && IsOwner) {
            myPlayerInputActions.Player.Enable();
        }
    }

    private void UIController_OnInputFreezingMenuEnabled(object sender, EventArgs e) {
        if (IsOwner) {
            myPlayerInputActions.Player.Disable();
        }
    }

    #endregion

    #region Performing Input Actions

    public void DisableInput()
    {
        myPlayerInputActions.Player.Disable();
    }

    public void EnableInput()
    {
        myPlayerInputActions.Player.Enable();
    }

    private void BingoBongo_performed(InputAction.CallbackContext obj) {
        bingoBongoStorer.IncrementBingoBongoCount();
    }

    private void Interact_performed(InputAction.CallbackContext obj) {

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D[] hits2D = Physics2D.GetRayIntersectionAll(ray, 1000f, interactableLayerMask);
        foreach(RaycastHit2D hit in hits2D) {
            if (hit.collider.TryGetComponent<IInteractable>(out IInteractable iInteractable)) { // if the hit object has an IInteractable component

                //calculate if it's in range
                float tempInteractionRange = interactionRange;
                if (hit.collider.TryGetComponent<ICustomInteractRange>(out ICustomInteractRange iCustomInteractRange)) {
                    tempInteractionRange += iCustomInteractRange.AdditionalInteractRange;
                }
                if (Vector2.Distance(hit.transform.position, this.transform.position) > (tempInteractionRange)) {
                    //if the interactable object is out of range, do nothing
                    continue;
                }   


                iInteractable.Interact(this);

                break; //only interact with the first interactable object hit by the ray
            }
        }   
    }

    //if the equipped item is not null and is usable, call its Use() method
    private void UseEquippedItem_performed(InputAction.CallbackContext obj) {
        if (currentEquippedItemSO != null) {
            if (currentEquippedItemBehaviour is IUsableItem) {
                //if the equipped item is usable
                IUsableItem currentEquippedItemUsage = currentEquippedItemBehaviour as IUsableItem;
                currentEquippedItemUsage.Use();
            } else {
                //if the equipped item is not usable, do nothing (for now(?))
            }
        } else {
            //if there is no equipped item, do nothing (for now(?))
        }
    }


    //methods setting equippedItem and instantiating it
    private void HotbarSlot1Pressed_performed(InputAction.CallbackContext obj) {
        hotbarStateStorer.SelectSlot(0);
    }
    private void HotbarSlot2Pressed_performed(InputAction.CallbackContext obj) {
        hotbarStateStorer.SelectSlot(1);
    }
    private void HotbarSlot3Pressed_performed(InputAction.CallbackContext obj) {
        hotbarStateStorer.SelectSlot(2);
    }
    private void HotbarSlot4Pressed_performed(InputAction.CallbackContext obj) {
        hotbarStateStorer.SelectSlot(3);
    }

    #endregion

    //helper method: select first slot of the hotbar and instantiate the item in it
    private void InitializeHotbar() {
        if (hotbarStateStorer != null) {
            hotbarStateStorer.SelectSlot(0);
        }
    }

    //Sets equipped item to a given ItemSO, stores it's BehaviourItem script and tells player visual to instantiate it's prefab
    private void EquipItem(ItemSO itemSOToEquip){
        currentEquippedItemSO = itemSOToEquip;

        //set the behaviour item script accordingly
        if (currentEquippedItemSO != null) {
            //if we equipped an item
            currentEquippedItemBehaviour = currentEquippedItemSO.behaviourItemPrefab.GetComponent<BehaviourItem>();
            if (currentEquippedItemBehaviour == null) {
                Debug.LogError("Player equipped item: " + itemSOToEquip.itemName + ", which doesn't have a behaviour item script on its prefab");
            }
        } else {
            //if we equipped an empty slot
            currentEquippedItemBehaviour = null;
        }

        OnEquippedItem?.Invoke(this, new OnEquippedItemEventArgs() { newEquippedItemSO = itemSOToEquip });
    }

    //helper method: refresh the equipped item based on the current hotbar selection
    private void EquipItemInSelectedHotbarSlot() {
        if (hotbarStateStorer.CurrentInventory.Count < hotbarStateStorer.GetNumOfSlots()) {
            //if the hotbar is uninitialized or not fully initialized, maybe because player data is still loading
            return;
        }
        EquipItem(hotbarStateStorer.GetSelectedIndexItemSO());
    }

    //helper method: detect interactable objects on mouse position using raycasting
    private void DetectInteractableObject() {

    }

    //on FixedUpdate we read from the input system and move the player accordingly
    private void FixedUpdate() {
        if (IsOwner) {
            Vector2 inputMoveVector = myPlayerInputActions.Player.Movement.ReadValue<Vector2>();
            isMoving = inputMoveVector.magnitude > 0;//set isMoving

            if (isMoving) {
                myRigidBody.AddForce(inputMoveVector * moveSpeed * Time.fixedDeltaTime, ForceMode2D.Force);
            }
        }
    }

    public Vector2 GetInputMoveVector() {
        return myPlayerInputActions.Player.Movement.ReadValue<Vector2>();
    }

    #region loading and saving player data
    public void LoadData(GameData gameData) {
        if (!IsServer) return; // Only load data for the server

        if (gameData.allPlayerData.TryGetValue(OwnerClientId.ToString(), out PlayerData playerData)) { //if there is saved data for this player
            //Debug.Log("Player controller is loading player data for player with client ID: " + OwnerClientId);
            // Load player position
            LoadPlayerPositionRpc(playerData.playerPositionData);

            bingoBongoStorer.LoadPlayerData(playerData.bingoBongoCountData);

            // Load item data to hotbar and inventory
            hotbarStateStorer.LoadPlayerData(playerData.hotbarData);
            inventoryStateStorer.LoadPlayerData(playerData.inventoryData);

        } else {
            //if the game data had no saved data for this player
            SetDefaultValues();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void LoadPlayerPositionRpc(Vector3 newPosition) {
        if (IsOwner){
            transform.position = newPosition;

            if (DataPersistenceManager.Instance.IsSpawned) {
                //Debug.Log("Telling DPM that player with client ID " + OwnerClientId + " has loaded at position: " + newPosition);
                DataPersistenceManager.Instance.ClientLoadedPositionRpc(OwnerClientId, transform.position);
            }
        }
    }
    public void SaveData(ref GameData gameData) {
        if (!IsServer) return; // Only save data for the server

        PlayerData playerData = new PlayerData();

        playerData.playerPositionData = transform.position;
        playerData.bingoBongoCountData = bingoBongoStorer.GetBingoBongoCount();

        playerData.hotbarData = hotbarStateStorer.GetInventorySaveData();
        playerData.inventoryData = inventoryStateStorer.GetInventorySaveData();

        gameData.allPlayerData[OwnerClientId.ToString()] = playerData;
    }

    private void SetDefaultValues() {
        // Set default values for the player
        LoadPlayerPositionRpc(defaultPosition);

        bingoBongoStorer.ResetBingoBongoCount();
        hotbarStateStorer.SetInventoryToEmptyServerRpc();
        inventoryStateStorer.SetInventoryToEmptyServerRpc();
    }

    public InventoryStateStorer GetInventoryISS() {
        return inventoryStateStorer;
    }

    public HotbarStateStorer GetHotbarISS() {
        return hotbarStateStorer;
    }

    #endregion
}