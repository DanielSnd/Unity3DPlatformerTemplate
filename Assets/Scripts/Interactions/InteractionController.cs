using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerController))]
/// <summary>
/// Manages player interactions with Interactable objects in the game world.
/// Handles detection of interactable objects and mediates the interaction process.
/// </summary>
public class InteractionController : MonoBehaviour 
{
    [Header("Interaction Settings")]
    [SerializeField] private GameObject interactionZone;
    [SerializeField] private float interactionRadius = 0.5f;
    [SerializeField] private Texture2D interactableButtonTexture;

    [Header("Throwing Settings")]
    [SerializeField] private Vector3 throwForce = new Vector3(0, 5, 7);
    
    [Header("Audio")]
    [SerializeField] private AudioClip interactSound;
    [SerializeField] private AudioClip releaseSound;
    
    [Header("Animation")]
    [SerializeField] private int armsAnimationLayer;
    
    private Animator animator;
    private PlayerController playerController;
    private MovementController movementController;
    private Collider playerCollider;
    private AudioSource audioSource;
    private float defaultRotationSpeed;
    
    private Interactable currentInteractable;
    private FixedJoint physicsJoint;
    private float lastInteractionTime;
    
    private List<Interactable> interactablesInRange = new List<Interactable>();
    public Collider PlayerCollider { get => playerCollider; }
    public Vector3 ThrowForce => throwForce;


    public bool IsCurrentInteractingWithThis(Interactable otherInteractable)
    {
        return currentInteractable == otherInteractable;
    }

    private void Awake()
    {
        SetupComponents();
        SetupInteractionZone();
    }

    Vector3 lastInteractablePosition = Vector3.zero;
    float interactableSize = 0f;
    private void LateUpdate()
    {
        RemoveInvalidEntriesFromInteractablesList();

        DrawButtonHintForInteractables();
    }

    private void DrawButtonHintForInteractables()
    {
        if (interactablesInRange.Count > 0 && interactableButtonTexture != null && currentInteractable == null)
        {
            interactableSize = Mathf.Clamp(interactableSize + Time.deltaTime * 2.6f, 0.0f, 1.0f);
            lastInteractablePosition = interactablesInRange[0].transform.position;
        }
        else
        {
            interactableSize = Mathf.Clamp(interactableSize - Time.deltaTime * 4.2f, 0.0f, 1.0f);
        }

        if (interactableSize > 0.01f)
        {
            DrawQuadSprite.DrawSprite(interactableButtonTexture, lastInteractablePosition + Vector3.up * Mathf.Sin(Time.time * 4.25f) * 0.125f, Vector3.one * interactableSize);
        }
    }

    private void RemoveInvalidEntriesFromInteractablesList()
    {
        // Remove any invalid entries in our list.
        for (int i = interactablesInRange.Count - 1; i >= 0; i--)
        {
            if (interactablesInRange[i] == null) interactablesInRange.RemoveAt(i);
        }
    }

    private void SetupComponents()
    {
        TryGetComponent(out playerCollider);
        TryGetComponent(out playerController);
        TryGetComponent(out audioSource);
        TryGetComponent(out movementController);
        animator = GetComponentInChildren<Animator>();
        
        if (animator)
            animator.SetLayerWeight(armsAnimationLayer, 1);
            
        if (movementController)
            defaultRotationSpeed = movementController.rotationSpeed;
        
        if (TryGetComponent(out HealthController health))
            health.OnDeath.AddListener(EndCurrentInteraction);
    }

    private void SetupInteractionZone()
    {
        if (!interactionZone)
        {
            interactionZone = new GameObject("InteractionZone");
            var boxCollider = interactionZone.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            interactionZone.transform.SetParent(transform);
            interactionZone.transform.localPosition = new Vector3(0f, 0f, 0.5f);
        }
    }

    /// <summary>
    /// Called when the player presses the interact button.
    /// Either begins a new interaction or ends the current one.
    /// </summary>
    public void OnInteract()
    {
        if (currentInteractable != null && Time.time > lastInteractionTime + 0.1f)
        {
            if (currentInteractable.CanInteract(this))
                currentInteractable.OnInteract(this);
        }
        else if (interactablesInRange.Count > 0 && Time.time > lastInteractionTime + 0.2f)
        {
            BeginInteraction(interactablesInRange[0]);
        }
    }

    void OnCancel()
    {
        if (currentInteractable != null && Time.time > lastInteractionTime + 0.1f) {
            EndCurrentInteraction();
        }
    }

    private void BeginInteraction(Interactable interactable)
    {
        if (!CanInteract(interactable)) return;
        
        currentInteractable = interactable;
        PlaySound(interactSound);
        interactable.OnInteract(this);
        lastInteractionTime = Time.time;
        
        UpdateAnimator();
    }

    public void EndCurrentInteraction()
    {
        if (currentInteractable == null) return;

        var endingInteraction = currentInteractable;
        currentInteractable = null;
        
        PlaySound(releaseSound);
        endingInteraction.OnInteractionEnd(this);
        lastInteractionTime = Time.time;
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (!animator) return;
        
        animator.SetBool("HoldingPickup", currentInteractable is PickupInteractable);
        animator.SetBool("HoldingPushable", currentInteractable is PushableInteractable);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Interactable>(out Interactable interactable) 
            && !interactablesInRange.Contains(interactable))
        {
            interactablesInRange.Add(interactable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Interactable>(out Interactable interactable))
        {
            interactablesInRange.Remove(interactable);
        }
    }

    /// <summary>
    /// Checks if an interaction can be started with the given interactable.
    /// For pickups, checks if there's enough space above the player's head.
    /// </summary>
    private bool CanInteract(Interactable interactable)
    {
        if (interactable is PickupInteractable)
        {
            Vector3 holdPosition = transform.position.SetY(transform.position.y + interactable.GetInteractableHeight() + playerCollider.bounds.extents.y);
            return !Physics.CheckSphere(holdPosition, interactionRadius, GameManager.Instance.groundMask);
        }
        return true;
    }
} 