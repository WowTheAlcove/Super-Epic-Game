using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HotbarStateStorer : InventoryStateStorer
{
    private NetworkVariable<int> selectedSlotIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public void SelectSlot(int index) {
        //visually highlight the selected slot
        selectedSlotIndex.Value = index;
        UIController.Instance.HighlightHotbarSelectedSlot(selectedSlotIndex.Value);
    }

    public ItemSO GetSelectedIndexItemSO() {
        return GetItemSOAtInventoryIndex(selectedSlotIndex.Value);
    }

    public NetworkVariable<int> SelectedSlotIndex => selectedSlotIndex;
}
