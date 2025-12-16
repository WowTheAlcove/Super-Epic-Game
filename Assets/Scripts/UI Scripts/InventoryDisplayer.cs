using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryDisplayer : MonoBehaviour
{
    private protected InventoryStateStorer myCurrentInventoryStateStorer;
    private protected List<Slot> myCurrentSlots;

    [SerializeField] private protected GameObject slotPrefab;

    private void Start() {
        UIController.Instance.OnAllInventoryPanelsRefreshRequested += UIController_OnAllActiveInventoryPanelsRefreshRequested;
        RefreshVisual();
    }

    private void OnDestroy() {
        UIController.Instance.OnAllInventoryPanelsRefreshRequested -= UIController_OnAllActiveInventoryPanelsRefreshRequested;
    }

    private void UIController_OnAllActiveInventoryPanelsRefreshRequested(object sender, System.EventArgs e) {
        if (this.gameObject.activeSelf) {
            RefreshVisual();
        }
    }

    public virtual void RefreshVisual() {
        if (myCurrentInventoryStateStorer == null) {
            return;
        }
        if (myCurrentSlots == null) {
            myCurrentSlots = new List<Slot>();
        }

        if (myCurrentSlots.Count == myCurrentInventoryStateStorer.CurrentInventory.Count) {
            //if the number of slots to display hasn't changed since the last visual refresh
            for (int i = 0; i < myCurrentInventoryStateStorer.CurrentInventory.Count; i++) {
                ItemSO itemSOToGiveSlot = myCurrentInventoryStateStorer.GetItemSOAtInventoryIndex(i);
                myCurrentSlots[i].SetCurrentSlotItemSO(itemSOToGiveSlot); 
                myCurrentSlots[i].RefreshVisual();
            }
        } else {
            //if the number of slots to display has changed since the last visual refresh
            DestroyAndRespawnSlots();
        }
    }

    private void DestroyAndRespawnSlots() {
        //clear all current slots
        foreach (Transform child in transform) {
            if (child.TryGetComponent<Slot>(out Slot slot)) {
                Destroy(child.gameObject);
            }
        }
        myCurrentSlots.Clear();

        //instantiate a slot prefab for each item in the current inventory
        for (int i = 0; i < myCurrentInventoryStateStorer.CurrentInventory.Count; i++) {
            Slot newSlot = Instantiate(slotPrefab, transform).GetComponent<Slot>();
            myCurrentSlots.Add(newSlot);
            //give the slots index and item, and then call it's refresh
            ItemSO itemSOToGiveSlot = myCurrentInventoryStateStorer.GetItemSOAtInventoryIndex(i);
            newSlot.Initialize(i);
            newSlot.SetCurrentSlotItemSO(itemSOToGiveSlot);
            newSlot.RefreshVisual();
        }
    }

    public void AddItemToInvStateStorer(int itemToAddID, int slotInInventoryToAddTo) {
        myCurrentInventoryStateStorer.AddItemToSlotServerRpc(itemToAddID, slotInInventoryToAddTo);
    }
    public void EmptySlotInInvStateStorer(int slotInInventoryToEmpty) {
        myCurrentInventoryStateStorer.EmptySlotServerRpc(slotInInventoryToEmpty);
    }
    public InventoryStateStorer GetCurrentInventoryStateStorer() {
        return myCurrentInventoryStateStorer;
    }

    public void SetCurrentInventoryStateStorer(InventoryStateStorer inventoryToDisplay) {
        myCurrentInventoryStateStorer = inventoryToDisplay;
    }

    public void Show() {
        this.gameObject.SetActive(true);
    }

    public void Hide() {
        this.gameObject.SetActive(false);
    }
}
