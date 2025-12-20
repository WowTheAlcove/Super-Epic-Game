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
        knot = "ChickenRoot";
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
        GameEventsManager.Instance.dialogueEvents.InvokeOnUnbindInkExternalFunction(this, "StartFollowingRecentPlayer");
        GameEventsManager.Instance.dialogueEvents.InvokeOnUnbindInkExternalFunction(this, "StopFollowing");
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
        GameEventsManager.Instance.dialogueEvents.InvokeOnUpdateInkVariable(this, "IsFollowingPlayer", new BoolValue(true));
    }

    private void StopFollowing()
    {
        myFollowTransformScript.enabled = false;
        GameEventsManager.Instance.dialogueEvents.InvokeOnUpdateInkVariable(this, "IsFollowingPlayer", new BoolValue(false));
    }
}
