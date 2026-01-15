using System;
using NUnit.Framework.Constraints;
using Unity.Netcode;
using UnityEngine;

public enum PlayersSurface
{
    NONE,
    BOAT,
    LAND,
    WATER,
}
public class PlayerStateStorer : NetworkBehaviour, IDataPersistence {
    [Header("References to State Storers")]
    [SerializeField] private HotbarStateStorer hotbarStateStorer;
    [SerializeField] private InventoryStateStorer inventoryStateStorer;
    [SerializeField] private BingoBongoStorer bingoBongoStorer;

    private Vector3 defaultPosition;
    public PlayersSurface myPlayersSurface { get; private set; }
    
    private ItemSO currentEquippedItemSO = null;
    private BehaviourItem currentEquippedItemBehaviour;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        
        // Store the default position of the player (assigned in editor by Network Manager)
        defaultPosition = transform.position;
    }

    #region Accessors for State Storers

    public HotbarStateStorer GetHotbarStateStorer() {
        return hotbarStateStorer;
    }

    public InventoryStateStorer GetInventoryStateStorer() {
        return inventoryStateStorer;
    }

    public BingoBongoStorer GetBingoBongoStorer() {
        return bingoBongoStorer;
    }

    #endregion

    #region Equipped Item State

    public ItemSO GetCurrentEquippedItemSO() {
        return currentEquippedItemSO;
    }

    public BehaviourItem GetCurrentEquippedItemBehaviour() {
        return currentEquippedItemBehaviour;
    }

    public void SetEquippedItem(ItemSO itemSO, BehaviourItem behaviourItem) {
        currentEquippedItemSO = itemSO;
        currentEquippedItemBehaviour = behaviourItem;
    }

    #endregion

    #region ==== DATA PERSISTENCE ====

    public void LoadData(GameData gameData) {
        // Debug.Log("LoadData called for playerStateStorer with clientid: " + OwnerClientId + " and index: " + PlayerIndexManager.Instance.GetPlayerIndexForClientId(OwnerClientId));
        if (!IsServer) return; // Only load data for the server

        if (gameData.allPlayerData.TryGetValue(PlayerIndexManager.Instance.GetPlayerIndexForClientId(OwnerClientId).ToString(), out PlayerData playerData)) {
            // Load player position
            LoadPlayerPositionRpc(playerData.playerPositionData);

            myPlayersSurface = playerData.playersSurfaceData;
            bingoBongoStorer.LoadPlayerData(playerData.bingoBongoCountData);

            // Load item data to hotbar and inventory
            hotbarStateStorer.LoadPlayerData(playerData.hotbarData);
            inventoryStateStorer.LoadPlayerData(playerData.inventoryData);

        } else {
            // If the game data had no saved data for this player
            SetDefaultValues();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void LoadPlayerPositionRpc(Vector3 newPosition) {
        if (IsOwner) {
            transform.position = newPosition;

            if (DataPersistenceManager.Instance.IsSpawned) {
                DataPersistenceManager.Instance.ClientHasLoadedPositionRpc(OwnerClientId, transform.position);
            }
        }
    }

    public void SaveData(ref GameData gameData) {
        if (!IsServer) return; // Only save data for the server

        PlayerData playerData = new PlayerData();

        playerData.playerPositionData = transform.position;
        playerData.playersSurfaceData = myPlayersSurface;
        
        playerData.bingoBongoCountData = bingoBongoStorer.GetBingoBongoCount();
        
        playerData.hotbarData = hotbarStateStorer.GetInventorySaveData();
        playerData.inventoryData = inventoryStateStorer.GetInventorySaveData();

        gameData.allPlayerData[PlayerIndexManager.Instance.GetPlayerIndexForClientId(OwnerClientId).ToString()] = playerData;
    }

    private void SetDefaultValues() {
        // Set default values for the player
        LoadPlayerPositionRpc(defaultPosition);

        bingoBongoStorer.ResetBingoBongoCount();
        hotbarStateStorer.SetInventoryToEmptyServerRpc();
        inventoryStateStorer.SetInventoryToEmptyServerRpc();
    }
    
    #endregion
    
   
    public void SetPlayersSurface(PlayersSurface newPlayersSurface)
    {
        this.myPlayersSurface = newPlayersSurface;
    }
}