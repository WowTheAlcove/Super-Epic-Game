using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreenPotionBehaviour : BehaviourItem, IUsableItem, IVisibleHeldItem
{
    [SerializeField] private float heldOffset = 0.5f;
    public float HeldOffset => heldOffset;

    public void Use()
    {
        Debug.Log("Green potion used!");
        GameEventsManager.Instance.miscEvents.InvokeOnPotionDrank(this);
    }
}