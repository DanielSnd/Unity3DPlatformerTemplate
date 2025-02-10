using UnityEngine;

/// <summary>
/// Base class for all objects that can be interacted with by the player.
/// Provides common functionality for interactions.
/// </summary>
public abstract class Interactable : MonoBehaviour
{
    protected Collider col;
    protected InteractionController currentInteractor;

    protected virtual void Awake()
    {
        TryGetComponent(out col);
    }

    public float GetInteractableHeight()
    {
        return col != null ? col.bounds.extents.y : 1.0f; 
    }

    public virtual bool CanInteract(InteractionController controller)
    {
        return true;
    }

    /// <summary>
    /// Called when the player initiates an interaction with this object.
    /// </summary>
    /// <param name="controller">The player's interaction controller.</param>
    public virtual bool OnInteract(InteractionController controller)
    {
        if (currentInteractor == null)
            currentInteractor = controller;
        else {
            OnInteractedAlreadyInteracting(controller);
            return false;
        }
        return true;
    }
    
    public virtual void OnInteractedAlreadyInteracting(InteractionController controller)
    {

    }

    /// <summary>
    /// Called when the player ends their interaction with this object.
    /// </summary>
    /// <param name="controller">The player's interaction controller.</param>
    public virtual void OnInteractionEnd(InteractionController controller)
    {
        if (controller != null && controller.IsCurrentInteractingWithThis(this)) {
            controller.EndCurrentInteraction();
        }    
        currentInteractor = null;
    }
    
} 