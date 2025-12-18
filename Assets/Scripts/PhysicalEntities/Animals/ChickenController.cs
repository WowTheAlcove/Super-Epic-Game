using System;
using Ink.Runtime;
using UnityEngine;

public class ChickenController : MonoBehaviour, IInteractable
{
    [SerializeField] private FollowTransform myFollowTransformScript;
    
    private string knot;
    private PlayerController recentlyInteractedPlayer;

    private void Awake()
    {
        knot = "ChickenDefault";
    }

    private void Start()
    {
        GameEventsManager.Instance.dialogueEvents.InvokeOnBindInkExternalFunction(
            this, "StartFollowingRecentPlayer", () => StartFollowingRecentlyInteractedPlayer()
        );
        GameEventsManager.Instance.dialogueEvents.InvokeOnBindInkExternalFunction(
            this, "StopFollowing", () => StopFollowing()
        );
        
    }

    private void OnDestroy()
    {
        GameEventsManager.Instance.dialogueEvents.InvokeOnUnbindInkExternalFunction(this, "startFollowingRecentPlayer");
        GameEventsManager.Instance.dialogueEvents.InvokeOnUnbindInkExternalFunction(this, "stopFollowing");
    }

    public void Interact(PlayerController playerInteracting)
    {
        recentlyInteractedPlayer = playerInteracting;
        GameEventsManager.Instance.dialogueEvents.InvokeOnEnterDialogueRequested(this, knot);
    }

    private void StartFollowing(Transform targetTransform)
    {
        myFollowTransformScript.SetFollowTransform( targetTransform);
        myFollowTransformScript.enabled = true;
    }

    private void StartFollowingRecentlyInteractedPlayer()
    {
        if (!recentlyInteractedPlayer)
        {
            Debug.LogWarning("StartFollowingRecentlyInteractedPlayer on Chicken, but no player");
            return;
        }
        
        StartFollowing(recentlyInteractedPlayer.transform);
    }

    private void StopFollowing()
    {
        myFollowTransformScript.enabled = false;
    }
}
