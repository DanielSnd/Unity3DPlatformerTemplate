using UnityEngine;

/// <summary>
/// Represents an object that can be grabbed and pushed/pulled by the player.
/// The player will be locked facing the object while interacting with it.
/// </summary>
public class PushableInteractable : PhysicsInteractable
{
    [Tooltip("Force required to break the connection with the player")]
    [SerializeField] private float jointBreakForce = 200f;
    [Tooltip("Rotational force required to break the connection with the player")]
    [SerializeField] private float jointBreakTorque = 200f;

    private MovementController movementController;
    private float defaultPlayerRotationSpeed;
    private void OnJointBreak(float breakForce)
    {
        //Debug.Log($"JOINT BROKE! FORCE {breakForce}");
        if (currentInteractor != null)
            currentInteractor.EndCurrentInteraction();
    }

    public override bool OnInteract(InteractionController controller)
    {
        if (!base.OnInteract(controller)) return false;

        currentInteractor = controller;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        // Lock player rotation while pushing/pulling
        movementController = controller.GetComponent<MovementController>();
        if (movementController)
        {
            movementController.overrideCanJump = true;
            if (movementController is AdvancedMoveController) {
                (movementController as AdvancedMoveController).onJumpPerformed.AddListener(PusherJumped);
            }
            defaultPlayerRotationSpeed = movementController.rotationSpeed;
            movementController.rotationSpeed = 0;
        }

        AttachToController<FixedJoint>(controller);
        return true;
    }
    public override void OnInteractedAlreadyInteracting(InteractionController controller)
    {
        base.OnInteractedAlreadyInteracting(controller);
        PusherJumped();
    }

    protected void PusherJumped() {
        if (currentInteractor) OnInteractionEnd(currentInteractor);
    }
    /// <summary>
    /// Overrides the base attachment to add break force limits to the joint.
    /// </summary>
    protected override void AttachToController<T>(InteractionController controller)
    {
        base.AttachToController<T>(controller);
        if (physicsJoint != null)
        {
            physicsJoint.breakForce = jointBreakForce;
            physicsJoint.breakTorque = jointBreakTorque;
            physicsJoint.massScale = 3.0f;
            physicsJoint.connectedMassScale = 3.0f;
        }
    }

    public override void OnInteractionEnd(InteractionController controller)
    {
        if (currentInteractor == null) return;

        DetachFromController();
        rb.interpolation = defaultInterpolation;
        
        // Restore player's original rotation speed
        if (movementController != null) {
            movementController.overrideCanJump = false;
            if (movementController is AdvancedMoveController) {
                (movementController as AdvancedMoveController).onJumpPerformed.RemoveListener(PusherJumped);
            }
            movementController.rotationSpeed = defaultPlayerRotationSpeed;
            movementController = null;
        }
        base.OnInteractionEnd(controller);
    }
} 