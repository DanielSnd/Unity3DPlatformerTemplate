using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;

/// <summary>
/// A flexible trigger system that can execute a sequence of actions based on various trigger conditions.
/// </summary>
[System.Flags]
public enum TriggerOptions
{
    TriggerOnlyOnce = 1,        // The trigger will only execute its actions once
    TriggerWhenEnter = 2,       // Execute when objects enter the trigger
    TriggerWhenExit = 4,        // Execute when objects exit the trigger
    TriggerWhileInside = 8,     // Continuously execute while objects are inside
    UntriggerOtherwise = 16,     // Execute untrigger actions when no valid objects are inside
    RequiresMinWeight = 32,
    HasCooldown = 64
}

/// <summary>
/// Main component for the Smart Trigger system. Handles collision detection and orchestrates the execution of trigger actions.
/// Place this component on a GameObject with a Collider (set to "Is Trigger").
/// </summary>
public class SmartTrigger : MonoBehaviour
{
    [Tooltip("Configure how and when the trigger should activate")]
    [SerializeField] private TriggerOptions triggerOptions;

    [Tooltip("Which layers can activate this trigger")]
    [SerializeField] private LayerMask triggerLayers;

    [Tooltip("Cooldown between activations of this trigger")]
    [SerializeField] private float cooldownBeforeReactivation = 0.0f;

    [Tooltip("Weight required to activate this trigger")]
    [SerializeField] private float requiredWeight = 0.0f;

    [Tooltip("Which tags can activate this trigger (leave empty to accept any tag)")]
    [SerializeField][TagDropdown] private string[] triggerTags;

    [Tooltip("Actions to execute when the trigger activates")]
    [SerializeReference] public List<TriggerAction> onTriggerActions = new List<TriggerAction>();

    [Tooltip("Actions to execute when the trigger deactivates (only used with UntriggerOtherwise option)")]
    [SerializeReference] public List<TriggerAction> onUntriggerActions = new List<TriggerAction>();

    private bool hasAlreadyTriggered;
    private HashSet<Collider> triggeredColliders = new HashSet<Collider>();
    private List<Rigidbody> triggeredRigidbodies = new List<Rigidbody>();

    private bool isInCooldown = false;
    private float currentWeight = 0f;

    protected bool hasAnyTriggerActions { get => onTriggerActions.Count > 0; }
    protected bool hasAnyUntriggerActions { get => onUntriggerActions.Count > 0; }

    public int GetTriggerListCount()
    {
        return onTriggerActions.Count;
    }
    public void SetTriggerListElement(int indx, TriggerAction newAction)
    {
        if (indx >= onTriggerActions.Count)
        {
            onTriggerActions.Add(newAction);
        }
        onTriggerActions[indx] = newAction;
    }

    public void SetUnTriggerListElement(int indx, TriggerAction newAction)
    {
        if (indx >= onUntriggerActions.Count)
        {
            onUntriggerActions.Add(newAction);
        }
        onUntriggerActions[indx] = newAction;
    }

    public TriggerAction GetTriggerListElement(int indx)
    {
        return onTriggerActions[indx];
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidTrigger(other)) return;
        
        triggeredColliders.Add(other);
        
        // Track rigidbody for weight calculation
        if (other.attachedRigidbody != null && !triggeredRigidbodies.Contains(other.attachedRigidbody))
        {
            triggeredRigidbodies.Add(other.attachedRigidbody);
            UpdateTotalWeight();
        }

        if (triggerOptions.HasFlag(TriggerOptions.TriggerWhenEnter))
        {
            if (triggerOptions.HasFlag(TriggerOptions.TriggerOnlyOnce) && hasAlreadyTriggered) return;
            if (!CanTrigger()) return;
            ExecuteTriggerActions();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsValidTrigger(other)) return;

        triggeredColliders.Remove(other);
        
        // Remove rigidbody and update weight
        if (other.attachedRigidbody != null)
        {
            triggeredRigidbodies.Remove(other.attachedRigidbody);
            UpdateTotalWeight();
        }

        if (triggerOptions.HasFlag(TriggerOptions.TriggerWhenExit))
        {
            if (triggerOptions.HasFlag(TriggerOptions.TriggerOnlyOnce) && hasAlreadyTriggered) return;
            ExecuteTriggerActions();
        }

        if (triggerOptions.HasFlag(TriggerOptions.UntriggerOtherwise) && triggeredColliders.Count == 0)
        {
            ExecuteUntriggerActions();
        }
    }

    private void Update()
    {
        if (triggerOptions.HasFlag(TriggerOptions.TriggerWhileInside) && triggeredColliders.Count > 0)
        {
            if (triggerOptions.HasFlag(TriggerOptions.TriggerOnlyOnce) && hasAlreadyTriggered) return;
            if (!CanTrigger()) return;
            ExecuteTriggerActions();
        }
    }

    private void UpdateTotalWeight()
    {
        currentWeight = triggeredRigidbodies.Sum(rb => rb.mass);
    }

    private bool CanTrigger()
    {
        // Check cooldown
        if (triggerOptions.HasFlag(TriggerOptions.HasCooldown) && isInCooldown)
        {
            return false;
        }

        // Check weight requirement
        if (triggerOptions.HasFlag(TriggerOptions.RequiresMinWeight) && currentWeight < requiredWeight)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a collider meets the layer and tag requirements to activate this trigger.
    /// </summary>
    protected bool IsValidTrigger(Collider other)
    {
        // Check layer
        if (!(triggerLayers.isLayerInLayerMask(other.gameObject.layer)))
            return false;

        // Check tags
        if (triggerTags != null && triggerTags.Length > 0)
        {
            bool hasValidTag = false;
            foreach (string tag in triggerTags)
            {
                if (other.CompareTag(tag))
                {
                    hasValidTag = true;
                    break;
                }
            }
            if (!hasValidTag) return false;
        }

        return true;
    }

    protected void ExecuteTriggerActions()
    {
        StartCoroutine(ExecuteActionsRoutine(onTriggerActions));
        hasAlreadyTriggered = true;

        // Start cooldown if enabled
        if (triggerOptions.HasFlag(TriggerOptions.HasCooldown))
        {
            StartCoroutine(CooldownRoutine());
        }
    }

    protected void ExecuteUntriggerActions()
    {
        StartCoroutine(ExecuteActionsRoutine(onUntriggerActions));
    }

    /// <summary>
    /// Executes the trigger actions in sequence, respecting parallel execution flags.
    /// </summary>
    private IEnumerator ExecuteActionsRoutine(List<TriggerAction> actions)
    {
        List<TriggerAction> runningActions = new List<TriggerAction>();
        
        foreach (var action in actions)
        {
            if (action == null) continue;

            // If previous actions aren't running in parallel, wait for them to complete
            if (runningActions.Count > 0 && !action.RunInParallel)
            {
                while (runningActions.Any(a => !a.IsComplete))
                {
                    yield return null;
                }
                runningActions.Clear();
            }
            
            action.Execute();
            runningActions.Add(action);
            
            // If this action runs in parallel, continue immediately to the next action
            if (action.RunInParallel)
            {
                continue;
            }
        }
        
        // Wait for any remaining actions to complete
        while (runningActions.Any(a => !a.IsComplete))
        {
            yield return null;
        }
    }

    private IEnumerator CooldownRoutine()
    {
        isInCooldown = true;
        yield return new WaitForSeconds(cooldownBeforeReactivation);
        isInCooldown = false;
    }
}
