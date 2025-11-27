using UnityEngine;
using Unity.Netcode;
using NUnit.Framework;
using System.Collections.Generic;

public class ISSOnSpawnItemAdder : NetworkBehaviour
{
    [SerializeField] private List<ItemSO> itemsToAdd;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            InventoryStateStorer inventoryStateStorer = GetComponent<InventoryStateStorer>();
            if (inventoryStateStorer != null)
            {
                foreach (ItemSO itemSO in itemsToAdd)
                {
                    int itemIDToAdd = itemSO.itemID;
                    inventoryStateStorer.AddItemToFirstSlotInInventoryServerRpc(itemIDToAdd);
                }
            }
            else
            {
                Debug.LogError("No InventoryStateStorerItemAdder could not find an InventoryStateStorer component found its the GameObject: " + this.gameObject.name);
            }
        }
    }
}
