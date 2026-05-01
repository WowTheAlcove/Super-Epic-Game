using Unity.Cinemachine;
using System;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour {
    [Header("Player stats")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float interactionRange = 4f;

    [SerializeField] private PlayerStateStorer myPlayerStateStorer;

    private bool isMoving;
    public bool IsMoving => isMoving; // Public property to access isMoving state

    private Rigidbody myRigidBody;
    private Camera mainCamera;
    private PlayerInputActions myPlayerInputActions;
    public Transform platformAnchor { get; private set; }
    private Vector3 lastAnchorPosition;
    private Quaternion lastAnchorRotation;

    public event EventHandler<OnEquippedItemEventArgs> OnEquippedItem;
    public class OnEquippedItemEventArgs : EventArgs {
        public ItemSO newEquippedItemSO;
    }

    private int interactableLayerMask = 1 << 8; // Layer 8 is the "Interactable" layer

    #region ============ INIITIALIZATION ============
    private void Awake() {
        myRigidBody = GetComponent<Rigidbody>();
        // Debug.Log($"Rigidbody found: {myRigidBody != null}, on object: {myRigidBody?.gameObject.name}");
    }

    private void Start() {
        mainCamera = Camera.main; //for raycasting

        if (IsLocalPlayer)
        {
            //set the cinemachine camera to follow this player
            CinemachineCamera cmCamera = FindFirstObjectByType<CinemachineCamera>();
            cmCamera.Follow = this.transform;
        }

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
        myPlayerStateStorer.GetHotbarStateStorer().SelectedSlotIndex.OnValueChanged += (oldValue, newValue) => {
            EquipItemInSelectedHotbarSlot();
        };
    }


    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        DeregisterInput();

        UnsubscribeEquipRefreshToInventoryChanges();
    }

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
        myPlayerStateStorer.GetInventoryStateStorer().CurrentInventory.OnListChanged += CurrentInventory_OnListChanged;
        myPlayerStateStorer.GetHotbarStateStorer().CurrentInventory.OnListChanged += CurrentHotbar_OnListChanged;
    }
    private void UnsubscribeEquipRefreshToInventoryChanges() {
        myPlayerStateStorer.GetInventoryStateStorer().CurrentInventory.OnListChanged -= CurrentInventory_OnListChanged;
        myPlayerStateStorer.GetHotbarStateStorer().CurrentInventory.OnListChanged -= CurrentHotbar_OnListChanged;
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
    //helper method: select first slot of the hotbar and instantiate the item in it
    private void InitializeHotbar() {
        if (myPlayerStateStorer.GetHotbarStateStorer() != null) {
            myPlayerStateStorer.GetHotbarStateStorer().SelectSlot(0);
        }
    }

    #endregion

    #region ========== Performing Input Actions ===========

    public void DisableInput()
    {
        myPlayerInputActions.Player.Disable();
    }

    public void EnableInput()
    {
        myPlayerInputActions.Player.Enable();
    }

    private void BingoBongo_performed(InputAction.CallbackContext obj) {
        myPlayerStateStorer.GetBingoBongoStorer().IncrementBingoBongoCount();
        ObjectVisibilityManager.Instance.PlayerEnteredLowerDeck();
    }

    private void Interact_performed(InputAction.CallbackContext obj) {

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, interactableLayerMask);
        foreach(RaycastHit hit in hits) {
            if (hit.collider.TryGetComponent<IInteractable>(out IInteractable iInteractable)) { // if the hit object has an IInteractable component

                //calculate if it's in range
                float tempInteractionRange = interactionRange;
                if (hit.collider.TryGetComponent<ICustomInteractRange>(out ICustomInteractRange iCustomInteractRange)) {
                    tempInteractionRange += iCustomInteractRange.AdditionalInteractRange;
                }
                if (Vector3.Distance(hit.transform.position, this.transform.position) > (tempInteractionRange)) {
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
        ItemSO currentEquippedItemSo = myPlayerStateStorer.GetCurrentEquippedItemSO();
        BehaviourItem currentEquippedItemBehaviour = myPlayerStateStorer.GetCurrentEquippedItemBehaviour();
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
        myPlayerStateStorer.GetHotbarStateStorer().SelectSlot(0);
    }
    private void HotbarSlot2Pressed_performed(InputAction.CallbackContext obj) {
        myPlayerStateStorer.GetHotbarStateStorer().SelectSlot(1);
    }
    private void HotbarSlot3Pressed_performed(InputAction.CallbackContext obj) {
        myPlayerStateStorer.GetHotbarStateStorer().SelectSlot(2);
    }
    private void HotbarSlot4Pressed_performed(InputAction.CallbackContext obj) {
        myPlayerStateStorer.GetHotbarStateStorer().SelectSlot(3);
    }

    #endregion

    #region ============ ITEM EQUIPPING =============
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
        myPlayerStateStorer.SetEquippedItem(itemSOToEquip, behaviourItem);

        OnEquippedItem?.Invoke(this, new OnEquippedItemEventArgs() { newEquippedItemSO = itemSOToEquip });
    }

    //helper method: refresh the equipped item based on the current hotbar selection
    private void EquipItemInSelectedHotbarSlot() {
        if (myPlayerStateStorer.GetHotbarStateStorer().CurrentInventory.Count < myPlayerStateStorer.GetHotbarStateStorer().GetNumOfSlots()) {
            //if the hotbar is uninitialized or not fully initialized, maybe because player data is still loading
            return;
        }
        EquipItem(myPlayerStateStorer.GetHotbarStateStorer().GetSelectedIndexItemSO());
    }
    
    #endregion
    
    //on FixedUpdate we read from the input system and move the player accordingly
    private void FixedUpdate() {
        if (!IsOwner)
        {
            return;
        }
        
        Vector2 inputMoveVector = myPlayerInputActions.Player.Movement.ReadValue<Vector2>();
        Vector3 worldMoveVector = new Vector3(inputMoveVector.x, 0, inputMoveVector.y);
        isMoving = worldMoveVector.magnitude > 0;
        
        // Apply platform movement delta first
        if (platformAnchor != null)
        {
            Vector3 delta = platformAnchor.position - lastAnchorPosition;
            myRigidBody.MovePosition(myRigidBody.position + delta);
            
            //weird rotation stuff to simulate boat rotation
            Quaternion rotationDelta = platformAnchor.rotation * Quaternion.Inverse(lastAnchorRotation);
            myRigidBody.MoveRotation(rotationDelta * myRigidBody.rotation);
            
            // Debug.Log("New Position: "  + platformAnchor.position);
        }
        
        //calculating movement to do from input
        Vector3 velocityVector =  new Vector3(
            worldMoveVector.x * moveSpeed,
            myRigidBody.linearVelocity.y, // preserve y, so gravity still works
            worldMoveVector.z * moveSpeed
        );
        
        //calculating rotation to do from input
        Vector3 lookDir = new Vector3(inputMoveVector.x, 0, inputMoveVector.y);
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            myRigidBody.MoveRotation(targetRot);
        }
        
        
        // Debug.Log($"Adding linearVelocity: {velocityVector}");
        myRigidBody.linearVelocity = velocityVector;
        
        //Update the platform anchor's position
        if (platformAnchor != null)
        {
            platformAnchor.position = myRigidBody.position;
            lastAnchorPosition = platformAnchor.position;
            lastAnchorRotation = platformAnchor.rotation;
        }
    }
    
    #region ========== PUBLIC GETTERS ===============
    public Vector2 GetInputMoveVector() {
        return myPlayerInputActions.Player.Movement.ReadValue<Vector2>();
    }

    public InventoryStateStorer GetInventoryISS() {
        return myPlayerStateStorer.GetInventoryStateStorer();
    }

    public HotbarStateStorer GetHotbarISS() {
        return myPlayerStateStorer.GetHotbarStateStorer();
    }
    #endregion

    //Method for DPM to load PSS data for late joiners
    public void LoadData(GameData gameData)
    {
        myPlayerStateStorer.LoadData(gameData);
    }

    public void AttachToPlatform(Transform anchor)
    {
        platformAnchor = anchor;
        lastAnchorPosition = anchor.position;
        lastAnchorRotation = anchor.rotation;
    }

    public void DetachFromPlatform()
    {
        platformAnchor = null;
    }
}