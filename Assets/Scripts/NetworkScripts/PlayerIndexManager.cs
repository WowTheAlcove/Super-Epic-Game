using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerIndexManager : NetworkBehaviour
{
    public static PlayerIndexManager Instance { get; private set; }

    [Header("Local Player Data")]
    [SerializeField] private int localPlayerIndex = -1; // -1 means unassigned
    
    // NetworkList to store player index/clientId mappings
    private NetworkList<PlayerIndexEntry> indexToClientMapping;
    
    public event Action<int, ulong> OnPlayerIndexAssigned; // fires when any index is assigned
    public event Action<int> OnPlayerIndexRemoved; // fires when a player disconnects
    public event Action<int> OnLocalPlayerIndexAssigned;
    

    #region ==== INITIALIZATION/CLEANUP ====
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        indexToClientMapping = new NetworkList<PlayerIndexEntry>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            // Host automatically gets index 0
            AssignPlayerIndexServerSide(0, NetworkManager.Singleton.LocalClientId);
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        
        // Subscribe to list changes on all clients
        indexToClientMapping.OnListChanged += OnIndexMappingChanged;
        
        // If we're the host, set our local index
        if (IsHost)
        {
            localPlayerIndex = 0;
            OnLocalPlayerIndexAssigned?.Invoke(localPlayerIndex);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        
        indexToClientMapping.OnListChanged -= OnIndexMappingChanged;
    }
    #endregion

    #region ==== Public API ====
    
    /// Returns the local player's assigned index (-1 if unassigned)
    public int GetLocalPlayerIndex()
    {
        if(localPlayerIndex == -1){
            Debug.LogError("Tried to get local player index when it wasn't set");
        }
        
        return localPlayerIndex;
    }
    
    /// Get clientId for a given player index (returns null if not found)
    public ulong? GetClientIdForIndex(int playerIndex)
    {
        foreach (PlayerIndexEntry entry in indexToClientMapping)
        {
            if (entry.playerIndex == playerIndex)
            {
                return entry.clientId;
            }
        }
        return null;
    }
    
    /// Get player index for a given clientId (returns -1 if not found)
    public int GetPlayerIndexForClientId(ulong clientId)
    {
        foreach (var entry in indexToClientMapping)
        {
            if (entry.clientId == clientId)
            {
                return entry.playerIndex;
            }
        }
        return -1;
    }
    
    /// Check if a player index is available
    public bool IsIndexAvailable(int playerIndex)
    {
        foreach (var entry in indexToClientMapping)
        {
            if (entry.playerIndex == playerIndex)
            {
                return false;
            }
        }
        return true;
    }
    
    /// Get list of available indices (1-3, excluding taken ones)
    public List<int> GetAvailableIndices()
    {
        List<int> available = new List<int>();
        for (int i = 1; i <= 3; i++)
        {
            if (IsIndexAvailable(i))
            {
                available.Add(i);
            }
        }
        return available;
    }
    
    #endregion
    
    #region ==== Client Requesting Player Index ====
    
    /// Client calls this to request a specific player index
    public void RequestPlayerIndex(int requestedIndex)
    {
        if (IsServer)
        {
            Debug.LogWarning("Server should not call RequestPlayerIndex");
            return;
        }
        
        if (localPlayerIndex != -1)
        {
            Debug.LogWarning($"Already assigned index {localPlayerIndex}");
            return;
        }
        
        RequestPlayerIndexServerRpc(requestedIndex);
    }
    
    [Rpc(SendTo.Server)]
    private void RequestPlayerIndexServerRpc(int requestedIndex, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        
        // Validate: index must be 1-3 for clients
        if (requestedIndex < 1 || requestedIndex > 3)
        {
            Debug.LogWarning($"Client {clientId} requested invalid index {requestedIndex}");
            NotifyIndexRequestResultClientRpc(false, -1, RpcTarget.Single(clientId, RpcTargetUse.Temp));
            return;
        }
        
        // Check if index is available
        if (!IsIndexAvailable(requestedIndex))
        {
            Debug.LogWarning($"Client {clientId} requested taken index {requestedIndex}");
            NotifyIndexRequestResultClientRpc(false, -1, RpcTarget.Single(clientId, RpcTargetUse.Temp));
            return;
        }
        
        // Check if client already has an index
        if (GetPlayerIndexForClientId(clientId) != -1)
        {
            Debug.LogWarning($"Client {clientId} already has an index");
            NotifyIndexRequestResultClientRpc(false, -1, RpcTarget.Single(clientId, RpcTargetUse.Temp));
            return;
        }
        
        // Assign the index
        AssignPlayerIndexServerSide(requestedIndex, clientId);
        NotifyIndexRequestResultClientRpc(true, requestedIndex, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }
    
    [Rpc(SendTo.SpecifiedInParams)]
    private void NotifyIndexRequestResultClientRpc(bool success, int assignedIndex, RpcParams rpcParams = default)
    {
        if (success)
        {
            localPlayerIndex = assignedIndex;
            OnLocalPlayerIndexAssigned?.Invoke(localPlayerIndex);
            // Debug.Log($"Successfully assigned player index: {assignedIndex}");
        }
        else
        {
            Debug.LogError("Failed to assign player index. Try to choose another Player Index");
        }
    }
    
    #endregion
    
    #region ==== Server Management ====
    
    /// Server-side: Maps an index to a client in the NetworkList
    private void AssignPlayerIndexServerSide(int playerIndex, ulong clientId)
    {
        if (!IsServer)
        {
            Debug.LogError("AssignPlayerIndexServerSide can only be called on server");
            return;
        }
        
        indexToClientMapping.Add(new PlayerIndexEntry
        {
            playerIndex = playerIndex,
            clientId = clientId
        });
        
        // Debug.Log($"Assigned player index {playerIndex} to client {clientId}");
    }
    
    /// Server-side: Handle client disconnect
    /// Removes ClientId/PlayerIndex mapping from NetworkLIst
    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        
        // Find and remove the entry for this client
        for (int i = 0; i < indexToClientMapping.Count; i++)
        {
            if (indexToClientMapping[i].clientId == clientId)
            {
                int removedIndex = indexToClientMapping[i].playerIndex;
                indexToClientMapping.RemoveAt(i);
                Debug.Log($"Removed player index {removedIndex} for disconnected client {clientId}");
                break;
            }
        }
    }
    
    /// Called when the NetworkList changes on any client
    /// Invokes an event either representing a mapping being added or removed
    private void OnIndexMappingChanged(NetworkListEvent<PlayerIndexEntry> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<PlayerIndexEntry>.EventType.Add:
                OnPlayerIndexAssigned?.Invoke(changeEvent.Value.playerIndex, changeEvent.Value.clientId);
                break;
            case NetworkListEvent<PlayerIndexEntry>.EventType.Remove:
                OnPlayerIndexRemoved?.Invoke(changeEvent.Value.playerIndex);
                break;
        }
    }
    
    #endregion
}


#region ==== PLAYER INDEX ENTRY STRUCT ====
/// Struct to represent a player index mapping
/// Must implement INetworkSerializable and IEquatable for NetworkList
public struct PlayerIndexEntry : INetworkSerializable, IEquatable<PlayerIndexEntry>
{
    public int playerIndex;
    public ulong clientId;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref playerIndex);
        serializer.SerializeValue(ref clientId);
    }
    
    //IEquatable implementation (Chat Generated, just for NetworkList<> requirements)
    public bool Equals(PlayerIndexEntry other)
    {
        return playerIndex == other.playerIndex && clientId == other.clientId;
    }
    
    public override bool Equals(object obj)
    {
        return obj is PlayerIndexEntry other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + playerIndex.GetHashCode();
            hash = hash * 31 + clientId.GetHashCode();
            return hash;
        }
    }
}
#endregion