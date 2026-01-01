using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class WorldItem : NetworkBehaviour {
    [SerializeField] private ItemSO myItemSO;

    public ItemSO MyItemSO => myItemSO; // read only property

    private void Start() {
        if (!IsSpawned && NetworkManager.Singleton.IsListening) { //Spawn the item if it needs to be. Do not spawn if the NM isn't started
            SpawnRpc();
        }
    }

    public virtual void PickUp() {
        RemoveSaveIdServerRpc();
        this.NetworkObject.Despawn(true);
    }

    [Rpc(SendTo.Server)]
    private void SpawnRpc() {
        this.NetworkObject.Spawn();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RemoveSaveIdServerRpc()
    {
        DataPersistenceManager.Instance.RemoveSaveId(GetComponent<SaveID>().Id);
        // Debug.Log("Removing SaveID: " + GetComponent<SaveID>().Id);
    }
}