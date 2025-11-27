using Unity.Netcode;
using UnityEngine;

public class ArthurController : NetworkBehaviour, IInteractable, ICustomInteractRange
{
    [SerializeField] private GameObject kickablePotPrefab;

    public float AdditionalInteractRange => 3f;

    public void Interact(PlayerController playerInteracting) {
        SpawnKickablePotServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnKickablePotServerRpc() {
        GameObject spawnedKickablePot = Instantiate(kickablePotPrefab, transform.position + Vector3.right * .5f, Quaternion.identity);
        spawnedKickablePot.GetComponent<NetworkObject>().Spawn();
    }
}
