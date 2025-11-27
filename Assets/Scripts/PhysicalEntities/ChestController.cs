using UnityEngine;
using UnityEngine.Serialization;

public class ChestController : MonoBehaviour, IInteractable
{
    [SerializeField] private InventoryStateStorer chestInventoryStateStorer;

    public void Interact(PlayerController playerInteracting) {
        UIController.Instance.DisplayChestInteractionUI(chestInventoryStateStorer, playerInteracting.GetInventoryISS());
    }

}
