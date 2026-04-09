using UnityEngine;

public class MoveTest : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] private bool posX;
    [SerializeField] private bool negX;
    [SerializeField] private bool posY;
    [SerializeField] private bool negY;
    [SerializeField] private bool posZ;
    [SerializeField] private bool negZ;

    [Header("Rotation")]
    [SerializeField] private bool rotPosX;
    [SerializeField] private bool rotNegX;
    [SerializeField] private bool rotPosY;
    [SerializeField] private bool rotNegY;
    [SerializeField] private bool rotPosZ;
    [SerializeField] private bool rotNegZ;

    [Header("Speed")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float rotateSpeed = 90f;

    private void FixedUpdate()
    {
        // Position
        Vector3 currentPosition = transform.position;
        float chillMoveSpeed = moveSpeed * 0.1f * Time.deltaTime;

        if (posX) currentPosition.x += chillMoveSpeed;
        if (negX) currentPosition.x -= chillMoveSpeed;
        if (posY) currentPosition.y += chillMoveSpeed;
        if (negY) currentPosition.y -= chillMoveSpeed;
        if (posZ) currentPosition.z += chillMoveSpeed;
        if (negZ) currentPosition.z -= chillMoveSpeed;

        transform.position = currentPosition;

        // Rotation
        Vector3 rotationDelta = Vector3.zero;
        float chillRotateSpeed = rotateSpeed * Time.deltaTime;

        if (rotPosX) rotationDelta.x += chillRotateSpeed;
        if (rotNegX) rotationDelta.x -= chillRotateSpeed;
        if (rotPosY) rotationDelta.y += chillRotateSpeed;
        if (rotNegY) rotationDelta.y -= chillRotateSpeed;
        if (rotPosZ) rotationDelta.z += chillRotateSpeed;
        if (rotNegZ) rotationDelta.z -= chillRotateSpeed;

        transform.Rotate(rotationDelta, Space.Self);
    }
}