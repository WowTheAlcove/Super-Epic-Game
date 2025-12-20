using UnityEngine;

public class DrunkCatController : MonoBehaviour, IInteractable
{
    [SerializeField] private string knotName = "DrunkCatRoot";
    public void Interact(PlayerController playerInteracting)
    {
        GameEventsManager.Instance.dialogueEvents.InvokeOnEnterDialogueRequested(this, knotName);
    }
}
