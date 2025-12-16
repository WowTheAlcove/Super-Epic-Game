using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.Animations;
using UnityEngine;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject slotPrefab;
    private PlayerInputActions myPlayerInputActions;

    //inventory controllers for displays to 
    private InventoryStateStorer playerInventoryController;
    private InventoryStateStorer hotbarInventoryController;

    private BingoBongoStorer bingoBongoStorer;

    [Header("Displayer panels that UIController will control")]
    [SerializeField] private InventoryDisplayer menuInventoryUisDisplayer;
    [SerializeField] private HotbarDisplayer hotbarUisDisplayer;
    [SerializeField] private BingoBongoDisplayer bingoBongoDisplayer;

    [SerializeField] private ChestInteractionUIController chestInteractionUIController;

    public event EventHandler OnAllInventoryPanelsRefreshRequested;
    public event EventHandler OnAllInventoryPanelsHideRequested;
    public event EventHandler OnInputFreezingMenuEnabled;
    public event EventHandler<OnControlFreezingMenuDisabledEventArgs> OnInputFreezingMenuDisabled;

    public class OnControlFreezingMenuDisabledEventArgs : EventArgs {
        public bool thereAreOtherInputFreezingMenus;
    }


    #region ininitialization on Awake(), Start() and OnClientConnectedCallback, and deinitializing on Destroy()
    private void Awake() {
        if (Instance != null) {
            Debug.LogError("MenuController tried to create an instance, but there already was one");
        }
        Instance = this;

        myPlayerInputActions = new PlayerInputActions();
        myPlayerInputActions.Player.Enable();
        myPlayerInputActions.Player.ToggleMenu.performed += ToggleMenu_performed;

    }

    // Start is called before the first frame update
    void Start()
    {
        menuCanvas.SetActive(false);

        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;

        RefreshAllActiveInventoryPanels();
    }
    private void OnDestroy() {
        if(NetworkManager.Singleton != null) {
            NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
        }

        myPlayerInputActions.Player.ToggleMenu.performed -= ToggleMenu_performed;
        myPlayerInputActions.Dispose();
    }

    //when a client connects, check to see if it's the local player and if so, get the InventoryController and HotbarController components from it
    private void NetworkManager_OnClientConnectedCallback(ulong obj) {
        GameObject localPlayer = FindLocalPlayerObject();
        if (localPlayer != null) {
            playerInventoryController = localPlayer.GetComponentInChildren<InventoryStateStorer>();
            hotbarInventoryController = localPlayer.GetComponentInChildren<HotbarStateStorer>();
            bingoBongoStorer = localPlayer.GetComponentInChildren<BingoBongoStorer>();

            hotbarUisDisplayer.SetCurrentInventoryStateStorer(hotbarInventoryController);
            menuInventoryUisDisplayer.SetCurrentInventoryStateStorer(playerInventoryController);
            bingoBongoDisplayer.SetBingoBongoStorerConnection(bingoBongoStorer);

            RefreshAllActiveInventoryPanels();
        }
    }

    //finding the local player for references to the ICs you should always have
    private GameObject FindLocalPlayerObject()
    {
        // Find all Player GameObjects in the scene
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController player in allPlayers)
        {
            // Check if the player is owned by the local client
            if (player.IsOwner)
            {
                return player.gameObject;
            }
        }

        Debug.LogWarning("A UIController could not find a local Player GameObject in the scene.");
        return null;
    }

    #endregion
    private void ToggleMenu_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        if (IsDisplayingSomethingThatShouldBeHiddenOnMenuToggle()) {
            //if we are displaying a inventory panel that isn't the menu one, hide all inventory panels
            HideAllInventoryPanels();
        } else {
            //if there is no non-menu inventory panel displayed, toggle the menu canvas
            menuCanvas.SetActive(!menuCanvas.activeSelf);
        }
    }

    #region Panel Display Functions
    public void DisplayMenuInventoryPanel() {
        menuInventoryUisDisplayer.SetCurrentInventoryStateStorer(playerInventoryController);
        
        menuInventoryUisDisplayer.RefreshVisual();

        //no need to show inventory panel for this one because it's handled by scuffed TabController
    }

    public void DisplayChestInteractionUI(InventoryStateStorer openChest, InventoryStateStorer playerInventory) {
        chestInteractionUIController.SetChestSS(openChest);
        chestInteractionUIController.SetUserInventorySS(playerInventory);

        RefreshAllActiveInventoryPanels();
        chestInteractionUIController.Show();
    }

    public void UpdateBingoBongoCounterUI(BingoBongoStorer bingoBongoStorer) {

        bingoBongoDisplayer.SetBingoBongoStorerConnection(bingoBongoStorer);
        bingoBongoDisplayer.RefreshVisual();
    }

    #endregion

    #region Housekeeping
    public void RefreshAllActiveInventoryPanels() {
        OnAllInventoryPanelsRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    public void HideAllInventoryPanels() {
        OnAllInventoryPanelsHideRequested?.Invoke(this, EventArgs.Empty);
    }

    public void InputFreezingMenuEnabled(){
        OnInputFreezingMenuEnabled?.Invoke(this, EventArgs.Empty);
    }

    public void InputFreezingMenuDisabled() {
        OnInputFreezingMenuDisabled?.Invoke(this, new OnControlFreezingMenuDisabledEventArgs() {
            thereAreOtherInputFreezingMenus = IsDisplayingAnInputFreezingMenu()
        });
    }
    //returns a list of RectTransforms for all active inventory panels, created for InventoryItemMovementHandler to check if the mouse is over any of them
    public List<RectTransform> GetAllActiveInventoryPanelRectTransforms() {
        List<RectTransform> activeInventoryPanelRects = new List<RectTransform>();

        InventoryDisplayer[] allInventoryDisplayers = GetComponentsInChildren<InventoryDisplayer>();
        foreach (InventoryDisplayer invDisplayer in allInventoryDisplayers) {
            //for each inventory displayer, if it's active, add its rect transform to the list
            if (invDisplayer.gameObject.activeSelf) {
                RectTransform rectTransform = invDisplayer.GetComponent<RectTransform>();
                if (rectTransform != null) {
                    activeInventoryPanelRects.Add(rectTransform);
                } else {
                    Debug.LogError("An InventoryDisplayer had no RectTransform component");
                }
            }
        }

        return activeInventoryPanelRects;
    }

    #endregion

    public void HighlightHotbarSelectedSlot(int indexToHighlight) {
        hotbarUisDisplayer.ChangeSelectedSlot(indexToHighlight);
    }


    private bool IsDisplayingSomethingThatShouldBeHiddenOnMenuToggle() {
        IShouldHideOnMenuToggle[] allPanelsThatShouldBeHiddenOnMenuToggle = GetComponentsInChildren<IShouldHideOnMenuToggle>();
        foreach (IShouldHideOnMenuToggle panel in allPanelsThatShouldBeHiddenOnMenuToggle) {
            if (panel.IsActive()) {
                return true;
            }
        }
        return false;
    }

    private bool IsDisplayingAnInputFreezingMenu() {
        PlayerControlFreezingMenuMarker[] menuThatShouldFreezeInput = GetComponentsInChildren<PlayerControlFreezingMenuMarker>();
        
        foreach (PlayerControlFreezingMenuMarker menu in menuThatShouldFreezeInput) {
            if (menu.gameObject.activeSelf) {
                return true;
            }
        }

        return false;
    }
}
