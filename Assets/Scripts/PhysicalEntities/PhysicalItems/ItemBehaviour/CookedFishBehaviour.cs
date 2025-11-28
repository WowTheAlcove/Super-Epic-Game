using UnityEngine;

public class CookedFishBehaviour : BehaviourItem, IUsableItem, IVisibleHeldItem {
    public float HeldOffset => .5f;

    public void Use() {
        Debug.Log("Eating disgusting fish!");
    }
}
