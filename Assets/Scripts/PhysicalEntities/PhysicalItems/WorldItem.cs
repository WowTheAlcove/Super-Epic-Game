using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class WorldItem : NetworkBehaviour {
    [SerializeField] private ItemSO myItemSO;

    public ItemSO MyItemSO => myItemSO; // read only property

    private void Start() {
        if (!IsSpawned) {
            SpawnRpc();
        }
    }

    public virtual void PickUp() {

        this.NetworkObject.Despawn(true);
    }

    [Rpc(SendTo.Server)]
    private void SpawnRpc() {
        GetComponent<NetworkObject>().Spawn();
    }
}
