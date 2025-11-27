using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotbarDisplayer : InventoryDisplayer
{
    private int currentSelectedSlotIndex;
    public void ChangeSelectedSlot(int slotIndex)
    {
        currentSelectedSlotIndex = slotIndex;
        HighlightSelectedSlot();
    }

    private void HighlightSelectedSlot() {
        //set all slots to default grey
        foreach (Transform child in transform) {
            if(child.TryGetComponent<Slot>(out Slot slot)) {
                //if the child has a Slot component
                if (slot.slotIndex != currentSelectedSlotIndex) {
                    //if that slot is not the one we want to highlight
                    child.GetComponent<Image>().color = new Color(147f / 255f, 147 / 255f, 147 / 255f, 255 / 255f);
                } else {
                    //if that slot is the one we want to highlight
                    child.GetComponent<Image>().color = Color.white;
                }
            }
        }
    }

    public override void RefreshVisual()
    {
        base.RefreshVisual();
        HighlightSelectedSlot();
    }
}
