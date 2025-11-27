using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class InventoryController : NetworkBehaviour, IDataPersistence {

    [SerializeField] private int numOfSlots;
    [SerializeField] private string inventorySaveID;

    private protected NetworkList<int> currentInventory = new NetworkList<int>();
    public NetworkList<int> CurrentInventory => currentInventory; // public getter for currentInventory

    private void Awake() {
    }

    // Start is called before the first frame update
    void Start() {
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsClient) {
            // Subscribe to the OnListChanged event to handle updates on the client side
            currentInventory.OnListChanged += OnInventoryChanged;
        }
    }

    public override void OnNetworkDespawn() {
        if (IsClient) {
            // Unsubscribe from the event when the object is despawned
            currentInventory.OnListChanged -= OnInventoryChanged;
        }

        base.OnNetworkDespawn();
    }

    private void OnInventoryChanged(NetworkListEvent<int> changeEvent) {
        // Handle inventory changes (e.g., update UI)

        UIController.Instance.RefreshAllActiveInventoryPanels();
    }

    public void LoadData(GameData gameData) {
        
        //(re)populating the current inventory list
        if (gameData.allInventoriesData.TryGetValue(inventorySaveID, out var savedInventoryData)) {
            //if there was inventory data saved for the Player Inventory

            if (currentInventory != null) {// If there is already an inventory, clear the current inventory slot visuals
                currentInventory.Clear();
            } else {
                currentInventory = new NetworkList<int>();
            }

            foreach (InventorySlotSaveData savedItem in savedInventoryData.items) {
                currentInventory.Add(savedItem.itemID);
            }
        } else {
            SetInventoryToEmptyServerRpc();
        }
    }

    //create a new inventory with empty slots
    [ServerRpc(RequireOwnership = false)]
    public void SetInventoryToEmptyServerRpc() {
        if (currentInventory != null) {// If there is already an inventory, clear the current inventory slot visuals
            currentInventory.Clear();
        } else {
            currentInventory = new NetworkList<int>();
        }

        for (int i = 0; i < numOfSlots; i++) {
            currentInventory.Add(-1);
        }
    }

    public void SaveData(ref GameData gameData) {
        InventorySaveData inventoryData = new InventorySaveData();
        foreach (int itemToSave in currentInventory) {
            inventoryData.items.Add(new InventorySlotSaveData {
                itemID = itemToSave,
            });
        }

        //actual save
        gameData.allInventoriesData[inventorySaveID] = inventoryData;
    }

    //method to be called by the player, for loading the specific save class 'PlayerData'
    public void LoadPlayerData(InventorySaveData inventoryData) {
        //make sure the currentInventory is initialized and empty
        if (currentInventory == null) {
            currentInventory = new NetworkList<int>();
        }
        currentInventory.Clear();

        //populating the current inventory list with items from the loaded inventory data
        foreach (InventorySlotSaveData savedItem in inventoryData.items) {
            currentInventory.Add(savedItem.itemID);
        }
    }

    public InventorySaveData GetInventorySaveData() {
        InventorySaveData data = new InventorySaveData();

        if (currentInventory != null) {
            foreach (int item in currentInventory) {
                data.items.Add(new InventorySlotSaveData { itemID = item });
            }
        }

        return data;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddItemToSlotServerRpc(int itemToAdd, int index) {
        if(index < 0 || index >= numOfSlots) {
            Debug.LogError("Index out of bounds when trying to add item to inventory slot: " + index);
            return;
        }
        currentInventory[index] = itemToAdd;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddItemToFirstSlotInInventoryServerRpc(int itemToAdd) {
        for(int i = 0; i < numOfSlots; i++) {
            if (currentInventory[i] == -1) {
                //if the slot is empty, add the item to it
                currentInventory[i] = itemToAdd;
                break;
            }
        }
    }

    public ItemSO GetItemSOAtInventoryIndex(int index) {
        if (index < 0 || index >= numOfSlots) {
            Debug.LogError("Index out of bounds when trying to get item from inventory slot: " + index);
            return null;
        }
        int itemID = currentInventory[index];
        return DataPersistenceManager.Instance.GetItemSOFromItemID(itemID);
    }

    public bool HasRoomFor(ItemSO itemCandidate) {
        bool hasRoom = false;
        foreach (int itemID in currentInventory) {
            if (itemID == -1) {
                hasRoom = true;
                break;
            }
        }
        return hasRoom;
    }
}
