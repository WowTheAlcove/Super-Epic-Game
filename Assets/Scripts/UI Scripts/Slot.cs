using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using UnityEngine;

public class Slot : MonoBehaviour
{
    //the item currently held in this slot
    //null by default so save data can populate blank spaces(?)
    public ItemSO currentSlotItemSO { get; private set; } = null;

    public int slotIndex { get; private set; } = -1;

    public void Initialize(int index) {
        slotIndex = index;
    }

    public void RefreshVisual() {
        //destroy any existing item visuals in the slot
        foreach (Transform child in transform) {
            if (child.gameObject != this.gameObject) {
                Destroy(child.gameObject);
            }
        }

        //if the slot has an item, instantiate the item's prefab in the slot
        if (currentSlotItemSO != null) {
            GameObject newItemVisual = Instantiate(currentSlotItemSO.inventoryItemPrefab, this.transform);
            newItemVisual.GetComponent<RectTransform>().anchoredPosition = Vector3.zero; // Reset position to slot center
        } else {
            //do nothing, the slot is empty and so nothing will appear in it
        }
    }
    public InventoryDisplayer GetInventoryDisplayer() {
        return transform.parent.GetComponent<InventoryDisplayer>();
    }

    public void SetCurrentSlotItemSO(ItemSO newItem) {
        currentSlotItemSO = newItem;
    }

    public bool IsEmpty() {
        return currentSlotItemSO == null;
    }
}
