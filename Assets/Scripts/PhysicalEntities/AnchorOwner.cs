using UnityEngine;

public class AnchorOwner : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("OnCollisionENter on Anchor Owner");
        if (!collision.gameObject.GetComponent<PlayerController>()) return;

        PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
        if (pc == null || pc.platformAnchor != null) return;

        // Create anchor as child of boat at player's current position
        GameObject anchor = new GameObject("PlayerAnchor");
        anchor.transform.SetParent(this.transform);
        anchor.transform.position = collision.transform.position;

        pc.AttachToPlatform(anchor.transform);
    }

    private void OnCollisionExit(Collision collision)
    {
        Debug.Log("OnCollisionExit on Anchor Owner");
        if (!collision.gameObject.GetComponent<PlayerController>()) return;

        PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
        if (pc == null || pc.platformAnchor == null) return;

        Destroy(pc.platformAnchor.gameObject);
        pc.DetachFromPlatform();
    }
}