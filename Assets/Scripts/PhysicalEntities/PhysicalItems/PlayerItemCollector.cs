using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using Unity.Netcode;

public class PlayerItemCollector : NetworkBehaviour
{
    [SerializeField] private InventoryStateStorer myInventoryController;
    [SerializeField] private HotbarStateStorer myHotbarController;

    private void OnTriggerEnter2D(Collider2D collision) {
        if (IsServer) {
            if (collision.gameObject.TryGetComponent<WorldItem>(out WorldItem worldItem)) {//if the thing entering has WorldItem script
                if (worldItem.MyItemSO == null) {
                    //error check
                    Debug.LogError("WorldItem has no InventoryItemSO assigned! Cannot collect item.");
                }

                if (myHotbarController.HasRoomFor(worldItem.MyItemSO)) {
                    ShowItemPopClientRpc(worldItem.MyItemSO.itemID);
                    myHotbarController.AddItemToFirstSlotInInventoryServerRpc(worldItem.MyItemSO.itemID);
                    worldItem.PickUp();
                } else if (myInventoryController.HasRoomFor(worldItem.MyItemSO)) {
                    ShowItemPopClientRpc(worldItem.MyItemSO.itemID);
                    myInventoryController.AddItemToFirstSlotInInventoryServerRpc(worldItem.MyItemSO.itemID);
                    worldItem.PickUp();
                }
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ShowItemPopClientRpc(int itemID) {
        if (!IsOwner) {
            return;
        }
        ItemSO itemSOToDisplayOnPopup = DataPersistenceManager.Instance.GetItemSOFromItemID(itemID);

        ItemPopupUI.Instance.ShowItemPopup(itemSOToDisplayOnPopup.itemName, itemSOToDisplayOnPopup.icon);
    }
}
