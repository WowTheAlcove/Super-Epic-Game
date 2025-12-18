using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FollowTransform : MonoBehaviour
{
    public Transform targetTransform { get; private set; }
    [SerializeField] private Vector3 offset;
    [SerializeField] private float speed;

    private Rigidbody2D myRigidBody2d;
    
    private void Awake()
    {
        this.enabled = false;
        myRigidBody2d = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (!targetTransform) return;
        Vector3 desired = targetTransform.position + offset;
        Vector3 next = Vector3.MoveTowards(myRigidBody2d.position, desired, speed * Time.fixedDeltaTime);
        myRigidBody2d.MovePosition(next);
    }

    public void SetFollowTransform(Transform newTargetTransform)
    {
        this.targetTransform = newTargetTransform;
    }
}
