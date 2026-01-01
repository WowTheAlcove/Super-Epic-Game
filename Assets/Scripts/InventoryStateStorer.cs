using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;


public class InventoryStateStorer : NetworkBehaviour {
    [SerializeField] private int numOfSlots;

    [SerializeField] private float minDropDistance = 2f;
    [SerializeField] private float maxDropDistance = 3f;

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

        if (IsServer) {
            SetInventoryToEmptyServerRpc();
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

    //create a new inventory with empty slots
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
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

    public InventorySaveData GetInventorySaveData() {
        InventorySaveData data = new InventorySaveData();

        if (currentInventory != null) {
            foreach (int item in currentInventory) {
                data.items.Add(new InventorySlotSaveData { itemID = item });
            }
        }

        return data;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddItemToSlotServerRpc(int itemToAdd, int index) {
        if(index < 0 || index >= numOfSlots) {
            Debug.LogError("Index out of bounds when trying to add item to inventory slot: " + index);
            return;
        }
        currentInventory[index] = itemToAdd;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void EmptySlotServerRpc(int index) {
        if (index < 0 || index >= numOfSlots) {
            Debug.LogError("Index out of bounds when trying to add item to inventory slot: " + index);
            return;
        }
        currentInventory[index] = -1;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddItemToFirstSlotInInventoryServerRpc(int itemToAdd) {
        for(int i = 0; i < numOfSlots; i++) {
            if (currentInventory[i] == -1) {
                //if the slot is empty, add the item to it
                currentInventory[i] = itemToAdd;
                break;
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void DropItemAtSlotRpc(int slotIndex) {
        //random drop position around us
        Vector2 dropOffset = Random.insideUnitCircle.normalized * Random.Range(minDropDistance, maxDropDistance);
        Vector2 dropPosition = (Vector2)transform.position + dropOffset;

        NetworkObject droppedWorldItemNORef = Instantiate(GetItemSOAtInventoryIndex(slotIndex).worldItemPrefab, dropPosition, Quaternion.identity).GetComponent<NetworkObject>();
        droppedWorldItemNORef.Spawn(); // Spawn the item in the world

        EmptySlotServerRpc(slotIndex);
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

        public int GetNumOfSlots() {
            return numOfSlots;
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
}