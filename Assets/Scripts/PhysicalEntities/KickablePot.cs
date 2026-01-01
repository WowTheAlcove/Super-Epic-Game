using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KickablePot : NetworkBehaviour, IInteractable
{
    public void Interact(PlayerController playerInteracting)
    {
        InvokePotBrokenEventServerRpc();
        RemoveSaveIdServerRpc();
        DespawnSelfServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RemoveSaveIdServerRpc()
    {
        DataPersistenceManager.Instance.RemoveSaveId(GetComponent<SaveID>().Id);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DespawnSelfServerRpc()
    {
        this.NetworkObject.Despawn(true);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void InvokePotBrokenEventServerRpc()
    {
        GameEventsManager.Instance.miscEvents.InvokeOnPotBroken(this);
    }
}