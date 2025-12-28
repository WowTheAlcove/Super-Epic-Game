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

    [ServerRpc(RequireOwnership = false)]
    private void RemoveSaveIdServerRpc()
    {
        DataPersistenceManager.Instance.RemoveSaveId(GetComponent<SaveID>().Id);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void DespawnSelfServerRpc()
    {
        this.NetworkObject.Despawn(true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void InvokePotBrokenEventServerRpc()
    {
        GameEventsManager.Instance.miscEvents.InvokeOnPotBroken(this);
    }
}