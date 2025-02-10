using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Represents a checkpoint in the game world that can save the player's progress.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Whether this is the initial checkpoint of the level")]
    [SerializeField] private bool isInitialCheckpoint;
    public bool IsInitialCheckpoint { get => isInitialCheckpoint; }

    [Header("Visual Feedback")]
    [SerializeField] private Material inactiveMaterial;
    [SerializeField] private Material activeMaterial;
    [SerializeField] private ParticleSystem activationEffect;
    
    [Header("Audio")]
    [SerializeField] private AudioClip activationSound;
    
    [Header("Events")]
    public UnityEvent onCheckpointActivated;
    
    private static Checkpoint currentActiveCheckpoint;
    private new Renderer renderer;
    private new Collider collider;
    private AudioSource audioSource;
    
    private void Awake()
    {
        CheckpointManager.checkpoints.Add(this);
        SetupComponents();
        ValidateConfiguration();
        
        if (isInitialCheckpoint)
        {
            ActivateCheckpoint(false);
        }
    }

    private void OnDestroy()
    {
        CheckpointManager.checkpoints.Remove(this);
    }

    private void SetupComponents()
    {
        renderer = GetComponent<Renderer>();
        collider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        
        collider.isTrigger = true;
    }
    
    private void ValidateConfiguration()
    {
        if (!renderer || !inactiveMaterial || !activeMaterial)
        {
            Debug.LogError($"Missing required components on {gameObject.name}", this);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        ActivateCheckpoint(true);
    }
    
    private void ActivateCheckpoint(bool playEffects)
    {
        if (currentActiveCheckpoint == this) return;
        
        // Deactivate previous checkpoint
        if (currentActiveCheckpoint != null)
        {
            currentActiveCheckpoint.Deactivate();
        }
        
        // Activate this checkpoint
        currentActiveCheckpoint = this;
        renderer.material = activeMaterial;
        
        // Save checkpoint data
        CheckpointManager.SetCheckpoint(
            transform.position,
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
        
        if (playEffects)
        {
            PlayActivationEffects();
        }
        
        onCheckpointActivated?.Invoke();
    }
    
    private void Deactivate()
    {
        renderer.material = inactiveMaterial;
    }
    
    private void PlayActivationEffects()
    {
        if (activationEffect != null)
        {
            activationEffect.Play();
        }
        
        if (audioSource != null && activationSound != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
    }
} 