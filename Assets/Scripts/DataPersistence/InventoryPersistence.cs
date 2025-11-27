using Unity.Netcode;
using UnityEngine;

public class InventoryPersistence : NetworkBehaviour, IDataPersistence {

    private string id;

    private InventoryStateStorer inventoryStateStorer;

    protected void Awake() {
        inventoryStateStorer = GetComponent<InventoryStateStorer>();
        if (inventoryStateStorer == null) {
            Debug.LogError($"InventoryPersistence script cannot find InventoryStateStorer component on GameObject: {gameObject}");
        }
    }

    private void Start() {
        if (TryGetComponent<SaveID>(out SaveID saveID)) { // Get the ID from the SaveID component from somewhere on the same GameObject
            id = saveID.Id;
        } else {
            //if there's no SaveID component
            Debug.LogError($"PositionPersistence script cannot find SaveID component on GameObject: {gameObject}");
        }
    }

    public void LoadData(GameData gameData) {
        if (TryGetComponent<SaveID>(out SaveID saveID)) { // Get the ID from the SaveID component from somewhere on the same GameObject
            id = saveID.Id;
        } else {
            //if there's no SaveID component
            Debug.LogError($"PositionPersistence script cannot find SaveID component on GameObject: {gameObject}");
        }

        //(re)populating the current inventory list
        if (gameData.allInventoriesData.TryGetValue(id, out var savedInventoryData)) {
            //if there was inventory data saved for the Player Inventory

            for (int i = 0; i < savedInventoryData.items.Count; i++) {
                inventoryStateStorer.AddItemToSlotServerRpc(savedInventoryData.items[i].itemID, i);
            }
        } else {
            inventoryStateStorer.SetInventoryToEmptyServerRpc();
        }
    }

    public void SaveData(ref GameData gameData) {
        InventorySaveData inventoryData = new InventorySaveData();

        for(int i = 0; i < inventoryStateStorer.GetNumOfSlots(); i++) {
            ItemSO itemSOToGetID = inventoryStateStorer.GetNullableItemSOAtInventoryIndex(i);
            int itemIDToSave;
            if (itemSOToGetID == null) {
                itemIDToSave = -1;
            } else {
                itemIDToSave = itemSOToGetID.itemID;
            }

            inventoryData.items.Add(new InventorySlotSaveData {
                itemID = itemIDToSave,
            });
        }

        //actual save
        gameData.allInventoriesData[id] = inventoryData;
    }
}
