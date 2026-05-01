using System.Collections;
using UnityEngine;

public class AnchorOwner : MonoBehaviour
{
    private bool playerOnBoard = false;
    private PlayerController boardedPlayer = null;

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.GetComponent<PlayerController>()) return;

        PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
        if (pc == null) return;

        boardedPlayer = pc;
        playerOnBoard = true;

        if (pc.platformAnchor == null && pc.IsLocalPlayer)
        {
            GameObject anchor = new GameObject("PlayerAnchor");
            anchor.transform.SetParent(this.transform);
            anchor.transform.position = collision.transform.position;
            pc.AttachToPlatform(anchor.transform);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.GetComponent<PlayerController>()) return;
        playerOnBoard = true; // keep refreshing as long as contact exists
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!collision.gameObject.GetComponent<PlayerController>()) return;
        playerOnBoard = false;
        StartCoroutine(CheckDetach());
    }

    private IEnumerator CheckDetach()
    {
        // Wait a few physics frames — if OnCollisionStay refreshes playerOnBoard,
        // the player never truly left and we cancel the detach
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        if (!playerOnBoard && boardedPlayer != null)
        {
            if (boardedPlayer.platformAnchor != null)
                Destroy(boardedPlayer.platformAnchor.gameObject);

            boardedPlayer.DetachFromPlatform();
            boardedPlayer = null;
        }
    }
}