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
        // Debug.Log("About to call bind function");
        DialogueManager.Instance.BindInkExternalFunction("StartFollowingRecentPlayer", () => StartFollowingRecentlyInteractedPlayer());
        DialogueManager.Instance.BindInkExternalFunction("StopFollowing", () => StopFollowing());
    }

    private void OnDestroy()
    {
        DialogueManager.Instance.UnbindInkExternalFunction("StartFollowingRecentPlayer");
        DialogueManager.Instance.UnbindInkExternalFunction("StopFollowing");
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
        GameEventsManager.Instance.dialogueEvents.InvokeOnUpdateInkVariable(this, "IsFollowingPlayer", true);
    }

    private void StopFollowing()
    {
        myFollowTransformScript.enabled = false;
        GameEventsManager.Instance.dialogueEvents.InvokeOnUpdateInkVariable(this, "IsFollowingPlayer", false);
    }
}