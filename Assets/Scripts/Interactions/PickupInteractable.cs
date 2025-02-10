using UnityEngine;

/// <summary>
/// Represents an object that can be picked up and thrown by the player.
/// The object will be held above the player's head and can be thrown in the direction they're facing.
/// </summary>
public class PickupInteractable : PhysicsInteractable
{
    [Tooltip("How much lighter the object becomes when held (0.3 means 30% of original mass)")]
    [SerializeField] private float weightMultiplierWhenHeld = 0.3f;
    
    [Tooltip("Additional height above the player's head to hold the object")]
    [SerializeField] private float holdHeight = 0.5f;

    [Space(3)]
    [Tooltip("If set to true pressing the interact button while holding the object wil toss it.")]
    [SerializeField, ToggleLeft]private bool InteractHoldingTossesObject = true;

    [Tooltip("Actions to execute when using interaction button while holding")]
    [SerializeReference]
    [Conditional("InteractHoldingTossesObject",false)]
    public TriggerActionsList onTriggerActions = new TriggerActionsList();
    
    public static int PickedUpObjectLayer {get; private set;} = -1;
    int originalLayer = 0;
    private void OnJointBreak(float breakForce)
    {
        Debug.Log("JOINT BROKE!");
        if (currentInteractor != null)
            currentInteractor.EndCurrentInteraction();
    }

    protected override void AttachToController<T>(InteractionController controller)
    {
        if (gameObject.layer != PickedUpObjectLayer)
            originalLayer = gameObject.layer;
        gameObject.layer = PickedUpObjectLayer;
        base.AttachToController<T>(controller);
    }

    protected override void DetachFromController()
    {
        base.DetachFromController();
        gameObject.layer = originalLayer;
    }

    public override bool OnInteract(InteractionController controller)
    {
        if (!base.OnInteract(controller)) return false;
        if (PickedUpObjectLayer == -1) PickedUpObjectLayer = LayerMask.NameToLayer("PickedUpObject");
        endWithToss = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.mass *= weightMultiplierWhenHeld;
        
        Vector3 holdPosition = CalculateHoldPosition(controller);
        Vector3 previousPos = transform.position;
        transform.position = holdPosition;
        transform.rotation = controller.transform.rotation;
        
        AttachToController<FixedJoint>(controller);
        transform.position = previousPos;
        
        return true;
    }

    public override void OnInteractedAlreadyInteracting(InteractionController controller)
    {
        base.OnInteractedAlreadyInteracting(controller);
        if (InteractHoldingTossesObject) {
            endWithToss = true;
            OnInteractionEnd(controller);
        }
        else {
            if (!onTriggerActions.isExecutingList) {
                onTriggerActions.ExecuteTriggerActions(this);
            }
        }
    }

    bool endWithToss = false;

    public override void OnInteractionEnd(InteractionController controller)
    {
        if (currentInteractor == null) return;

        DetachFromController();
        rb.interpolation = defaultInterpolation;
        rb.mass = originalMass;
        
        if (endWithToss)
            rb.AddRelativeForce(controller.ThrowForce, ForceMode.VelocityChange);
        else {
            rb.AddRelativeForce(new Vector3(0, 1, 2), ForceMode.VelocityChange);
        }
        base.OnInteractionEnd(controller);
    }

    /// <summary>
    /// Calculates the position where the object should be held above the player's head.
    /// Takes into account the player's height and the object's size.
    /// </summary>
    /// <param name="controller">The player's interaction controller.</param>
    /// <returns>The world position where the object should be held.</returns>
    private Vector3 CalculateHoldPosition(InteractionController controller)
    {
        Vector3 holdPosition = controller.transform.position;
        if (TryGetComponent<MeshFilter>(out MeshFilter meshFilter) && meshFilter.sharedMesh != null)
        {
            var playerCollider = controller.GetComponent<Collider>();
            holdPosition.y += playerCollider.bounds.extents.y + meshFilter.sharedMesh.bounds.extents.y + holdHeight;
        }
        return holdPosition;
    }
} 