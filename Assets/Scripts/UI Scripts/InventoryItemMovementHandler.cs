using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryItemMovementHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Slot originalSlot;
    CanvasGroup canvasGroup;

    // Start is called before the first frame update
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData) {
        //store data pertaining to the original slot
        originalSlot = transform.parent.GetComponent<Slot>();

        transform.SetParent(transform.root); // Move the item back to canvas root, or else we risk clipping other UI elements above it in the hierarchy
        canvasGroup.blocksRaycasts = false; // Disable raycast blocking to allow interaction with other UI elements
        canvasGroup.alpha = 0.6f; // Make the item semi-transparent while dragging
    }

    public void OnDrag(PointerEventData eventData) {
        transform.position = eventData.position; // Update the item's position to follow the mouse cursor
    }

    public void OnEndDrag(PointerEventData eventData) {
        canvasGroup.blocksRaycasts = true; // Re-enable raycast blocking
        canvasGroup.alpha = 1f; // Restore the item's opacity
        
        Slot dropSlot = eventData.pointerEnter?.GetComponent<Slot>(); // the slot the item was dropped on (nullable)

        if (dropSlot == null) {
            GameObject item = eventData.pointerEnter;
            if (item != null) {
                //this means that the item was dropped on another item, and the raycast hit the item instead of the slot
                dropSlot = item.GetComponentInParent<Slot>();
            }
        }

        if (dropSlot != null && originalSlot != null) {
            //if the item was dropped on a slot

            int originalItemID = originalSlot.currentSlotItemSO.itemID;
            InventoryDisplayer originalSlotInvDisplayer = originalSlot.GetInventoryDisplayer();
            InventoryDisplayer dropSlotInvDisplayer = dropSlot.GetInventoryDisplayer();

            //give original slot the item the drop slot had
            if (dropSlot.IsEmpty()) {
                //if the drop slot is empty
                originalSlotInvDisplayer.EmptySlotInInvStateStorer(originalSlot.slotIndex);
            } else {
                //if the drop slot has an item
                originalSlotInvDisplayer.AddItemToInvStateStorer(dropSlot.currentSlotItemSO.itemID, originalSlot.slotIndex);
            }

            //give the drop slot the item the original slot had
            dropSlotInvDisplayer.AddItemToInvStateStorer(originalItemID, dropSlot.slotIndex);
        } else {
            // If the item was dropped outside of a valid slot
            if (CursorIsWithinAnActiveInventoryPanel(eventData.position)) {
                //do nothing
                UIController.Instance.RefreshAllActiveInventoryPanels();
            } else {
                //drop item on ground
                InventoryDisplayer originalSlotInvDisplayer = originalSlot.GetInventoryDisplayer();
                originalSlotInvDisplayer.GetCurrentInventoryStateStorer().DropItemAtSlotRpc(originalSlot.slotIndex);
            }
        }
        Destroy(this.gameObject);
    }
    private bool CursorIsWithinAnActiveInventoryPanel(Vector2 cursorPosition) {
        List<RectTransform> activeInventoryPanelRectTransforms = UIController.Instance.GetAllActiveInventoryPanelRectTransforms();

        foreach (RectTransform inventoryRect in activeInventoryPanelRectTransforms) {
            if(RectTransformUtility.RectangleContainsScreenPoint(inventoryRect, cursorPosition)) {
                return true;
            }
        }

        return false;
    }
}
