using Unity.Cinemachine;
using UnityEngine;

public class CameraBoundary : MonoBehaviour
{

    private CinemachineConfiner2D localCmCamera;
    private Collider2D boundaryCollider;

    private void Start() {
        boundaryCollider = GetComponent<Collider2D>();
        if(boundaryCollider == null)
            Debug.LogError("CameraBoundary requires a Collider2D component.");

        localCmCamera = FindFirstObjectByType<CinemachineConfiner2D>();
        if(localCmCamera == null)
            Debug.LogError("No CinemachineConfiner found in the scene.");
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        PlayerController enteringPlayer = collision.GetComponent<PlayerController>();

        if (enteringPlayer != null && enteringPlayer.IsOwner){ //if what entered was the local player
            localCmCamera.BoundingShape2D = boundaryCollider;
        }
    }
}