using Unity.Cinemachine;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour {
    [Header("Player stats")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float interactionRange = 4f;

    [SerializeField] private PlayerStateStorer playerStateStorer;

    private bool isMoving;
    public bool IsMoving => isMoving; // Public property to access isMoving state

    private Rigidbody2D myRigidBody;
    private Camera mainCamera;
    private PlayerInputActions myPlayerInputActions;
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
        CinemachineCamera cmCamera = FindFirstObjectByType<CinemachineCamera>();
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
        
        //all users subscribe to selected hotbar slot changes to refresh equipped item
        playerStateStorer.GetHotbarStateStorer().SelectedSlotIndex.OnValueChanged += (oldValue, newValue) => {
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

    private void SubscribeEquipRefreshToInventoryChanges() {
        playerStateStorer.GetInventoryStateStorer().CurrentInventory.OnListChanged += CurrentInventory_OnListChanged;
        playerStateStorer.GetHotbarStateStorer().CurrentInventory.OnListChanged += CurrentHotbar_OnListChanged;
    }
    private void UnsubscribeEquipRefreshToInventoryChanges() {
        playerStateStorer.GetInventoryStateStorer().CurrentInventory.OnListChanged -= CurrentInventory_OnListChanged;
        playerStateStorer.GetHotbarStateStorer().CurrentInventory.OnListChanged -= CurrentHotbar_OnListChanged;
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
        playerStateStorer.GetBingoBongoStorer().IncrementBingoBongoCount();
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
    private void UseEquippedItem_performed(InputAction.CallbackContext obj)
    {
        ItemSO currentEquippedItemSo = playerStateStorer.GetCurrentEquippedItemSO();
        BehaviourItem currentEquippedItemBehaviour = playerStateStorer.GetCurrentEquippedItemBehaviour();
        if (currentEquippedItemSo != null) {
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
        playerStateStorer.GetHotbarStateStorer().SelectSlot(0);
    }
    private void HotbarSlot2Pressed_performed(InputAction.CallbackContext obj) {
        playerStateStorer.GetHotbarStateStorer().SelectSlot(1);
    }
    private void HotbarSlot3Pressed_performed(InputAction.CallbackContext obj) {
        playerStateStorer.GetHotbarStateStorer().SelectSlot(2);
    }
    private void HotbarSlot4Pressed_performed(InputAction.CallbackContext obj) {
        playerStateStorer.GetHotbarStateStorer().SelectSlot(3);
    }

    #endregion

    //helper method: select first slot of the hotbar and instantiate the item in it
    private void InitializeHotbar() {
        if (playerStateStorer.GetHotbarStateStorer() != null) {
            playerStateStorer.GetHotbarStateStorer().SelectSlot(0);
        }
    }

    //Sets equipped item to a given ItemSO, stores it's BehaviourItem script and tells player visual to instantiate it's prefab
    private void EquipItem(ItemSO itemSOToEquip)
    {
        BehaviourItem behaviourItem = null;
        
        // Set the behaviour item script accordingly
        if (itemSOToEquip != null) {
            behaviourItem = itemSOToEquip.behaviourItemPrefab.GetComponent<BehaviourItem>();
            if (behaviourItem == null) {
                Debug.LogError("Player equipped item: " + itemSOToEquip.itemName + ", which doesn't have a behaviour item script on its prefab");
            }
        }

        // Store in PlayerStateStorer
        playerStateStorer.SetEquippedItem(itemSOToEquip, behaviourItem);

        OnEquippedItem?.Invoke(this, new OnEquippedItemEventArgs() { newEquippedItemSO = itemSOToEquip });
    }

    //helper method: refresh the equipped item based on the current hotbar selection
    private void EquipItemInSelectedHotbarSlot() {
        if (playerStateStorer.GetHotbarStateStorer().CurrentInventory.Count < playerStateStorer.GetHotbarStateStorer().GetNumOfSlots()) {
            //if the hotbar is uninitialized or not fully initialized, maybe because player data is still loading
            return;
        }
        EquipItem(playerStateStorer.GetHotbarStateStorer().GetSelectedIndexItemSO());
    }

    //on FixedUpdate we read from the input system and move the player accordingly
    private void FixedUpdate() {
        if (IsOwner) {
            Vector2 inputMoveVector = myPlayerInputActions.Player.Movement.ReadValue<Vector2>();
            isMoving = inputMoveVector.magnitude > 0;//set isMoving

            if (isMoving) {
                myRigidBody.AddForce(inputMoveVector * (moveSpeed * Time.fixedDeltaTime), ForceMode2D.Force);
            }
        }
    }

    public Vector2 GetInputMoveVector() {
        return myPlayerInputActions.Player.Movement.ReadValue<Vector2>();
    }


    public InventoryStateStorer GetInventoryISS() {
        return playerStateStorer.GetInventoryStateStorer();
    }

    public HotbarStateStorer GetHotbarISS() {
        return playerStateStorer.GetHotbarStateStorer();
    }

    public void LoadData(GameData gameData)
    {
        playerStateStorer.LoadData(gameData);
    }
}