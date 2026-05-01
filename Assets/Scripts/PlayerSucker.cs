using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSucker : NetworkBehaviour
{
    [SerializeField] private NetworkVariable<bool> isSucking;
    
    void Update()
    {
        if (isSucking.Value)
        {
            SuckTimeRpc(this.transform.position);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SuckTimeRpc(Vector3 pos)
    {
        NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        localPlayer.gameObject.transform.position = this.transform.position;
    }
}