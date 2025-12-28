using System;
using Unity.Netcode;
using UnityEngine;

public class ArthurController : NetworkBehaviour, IInteractable, ICustomInteractRange
{
    [SerializeField] private GameObject kickablePotPrefab;

    public float AdditionalInteractRange => 3f;

    private void Start()
    {
        GameEventsManager.Instance.dialogueEvents.InvokeOnBindInkExternalFunction(this, "SpawnPot", SpawnKickablePotServerRpc);        
    }

    public void Interact(PlayerController playerInteracting)
    {
        GameEventsManager.Instance.dialogueEvents.InvokeOnEnterDialogueRequested(playerInteracting.gameObject,
            "ArthurRoot");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnKickablePotServerRpc() {
        GameObject spawnedKickablePot = Instantiate(kickablePotPrefab, transform.position + Vector3.right * .5f, Quaternion.identity);
        spawnedKickablePot.GetComponent<NetworkObject>().Spawn();
    }
}