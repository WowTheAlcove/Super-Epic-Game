using UnityEngine;
using Unity.Netcode;

public class PlayerVisual : NetworkBehaviour
{
    private Animator myAnimator;

    private GameObject currentInstantiatedItemGO = null;
    private bool shouldDisplayCurrentObjectGO = false;
    [SerializeField] private GameObject heldItemLocationAxle;
    private float heldItemOffset;
    private SpriteRenderer heldItemSpriteRenderer;
    private int playerVisualSortingOrderIndex;

    private NetworkVariable<Vector2> inputMoveVectorNV = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private PlayerController playerController;

    private void Awake() {
        myAnimator = GetComponent<Animator>();
        playerVisualSortingOrderIndex = GetComponent<SpriteRenderer>().sortingOrder;
    }

    private void Start() {
        playerController.OnEquippedItem += PlayerController_OnEquippedItem;
    }

    private void PlayerController_OnEquippedItem(object sender, PlayerController.OnEquippedItemEventArgs e) {
        InstantiateCurrentItemSOPrefab(e.newEquippedItemSO);
    }

    private void Update() {
        if (IsOwner) {
            //if you're the owner, set the animation values
            inputMoveVectorNV.Value = playerController.GetInputMoveVector();

            myAnimator.SetBool("isMoving", playerController.IsMoving);

            if(inputMoveVectorNV.Value.magnitude > 0) {
                myAnimator.SetFloat("moveX", inputMoveVectorNV.Value.x);//sorry codemonkey im using strings
                myAnimator.SetFloat("moveY", inputMoveVectorNV.Value.y);
            }
        }

        //for all clients, if the player has a behaviour item, move it to the correct position
        if (shouldDisplayCurrentObjectGO == true) {
            currentInstantiatedItemGO.transform.localPosition = new Vector3(myAnimator.GetFloat("moveX"), myAnimator.GetFloat("moveY"), 0) * heldItemOffset;
            if (myAnimator.GetFloat("moveY") > 0) {
                //if the player is moving up, put the item on the same sorting layer as the player
                heldItemSpriteRenderer.sortingOrder = playerVisualSortingOrderIndex - 1;
            } else {
                //if the player is moving down or sideways, put the item on the layer above the player
                heldItemSpriteRenderer.sortingOrder = playerVisualSortingOrderIndex + 1;
            }
        }
    }

    //if the item the player just equipped isn't null, instantiate its itemBehaviour for visual purposes
    public void InstantiateCurrentItemSOPrefab(ItemSO itemToInstantiate) {
        if (itemToInstantiate != null) {
            //we didn't select an empty slot
            InstantiateNewBehaviourItemGORpc(itemToInstantiate.itemID);
        } else {
            // if we selected an empty slot
            DestroyCurrentBehaviourItemGORpc();
        }
    }

    //helper method: all clients destroy the current behaviour item GO and reinstantiate the equipped itemSO
    [Rpc(SendTo.ClientsAndHost)]
    private void InstantiateNewBehaviourItemGORpc(int itemToInstantiateID) {
        if (currentInstantiatedItemGO != null) {
            Destroy(currentInstantiatedItemGO);
        }
        currentInstantiatedItemGO = Instantiate(DataPersistenceManager.Instance.GetItemSOFromItemID(itemToInstantiateID).behaviourItemPrefab, heldItemLocationAxle.transform);

        if (currentInstantiatedItemGO.GetComponent<BehaviourItem>() is IVisibleHeldItem) {
            shouldDisplayCurrentObjectGO = true;
            heldItemOffset = ((IVisibleHeldItem)currentInstantiatedItemGO.GetComponent<BehaviourItem>()).HeldOffset;
            heldItemSpriteRenderer = currentInstantiatedItemGO.GetComponent<SpriteRenderer>();
        } else {
            shouldDisplayCurrentObjectGO = false;
        }
    }

    //helper method: all clients destroy the current behaviour item GO
    [Rpc(SendTo.ClientsAndHost)]
    private void DestroyCurrentBehaviourItemGORpc() {
        if (currentInstantiatedItemGO != null) {
            Destroy(currentInstantiatedItemGO);
        }
        currentInstantiatedItemGO = null;
        shouldDisplayCurrentObjectGO = false;
    }
}
