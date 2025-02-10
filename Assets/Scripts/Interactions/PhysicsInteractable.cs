using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extended class for objects that can be interacted with by the player with physics.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PhysicsInteractable : Interactable
{
    protected Rigidbody rb;
    protected RigidbodyInterpolation defaultInterpolation;
    protected Joint physicsJoint;
    protected float originalMass;
    protected override void Awake()
    {
        base.Awake();
        TryGetComponent(out rb);
        originalMass = rb.mass;
        defaultInterpolation = rb.interpolation;
    }

    /// <summary>
    /// Creates a physics joint between this object and the player controller.
    /// </summary>
    /// <param name="controller">The player's interaction controller to attach to.</param>
    protected virtual void AttachToController<T>(InteractionController controller) where T : Joint
    {
        physicsJoint = gameObject.AddComponent<T>();
        physicsJoint.connectedBody = controller.GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Removes the physics joint between this object and the player.
    /// </summary>
    protected virtual void DetachFromController()
    {
        if (physicsJoint != null)
        {
            Destroy(physicsJoint);
            physicsJoint = null;
        }
    }
}
