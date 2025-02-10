using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;

/// <summary>
/// Trigger action that invokes a UnityEvent, allowing for flexible integration with any public methods.
/// Provides multiple completion modes to accommodate different timing needs.
/// </summary>
[Serializable]
public class UnityEventAction : TriggerAction
{
    /// <summary>
    /// Defines when the action should be considered complete
    /// </summary>
    public enum CompletionType
    {
        Immediate,      // Complete immediately after invoking the event
        AfterDelay,     // Complete after a specified time delay
        Manual          // Complete only when Complete() is called externally
    }

    [Tooltip("The UnityEvent to invoke when this action executes")]
    [SerializeField] private UnityEvent onExecute;

    [Tooltip("When should this action be considered complete")]
    [SerializeField] private CompletionType completionType = CompletionType.Immediate;

    [Tooltip("How long to wait before completing (only used with AfterDelay)")]
    [SerializeField] private float completionDelay = 0f;
    
    private Coroutine completionCoroutine;

    protected override void OnExecute()
    {
        onExecute?.Invoke();

        switch (completionType)
        {
            case CompletionType.Immediate:
                Complete();
                break;
            case CompletionType.AfterDelay:
                owner.StartCoroutine(CompleteAfterDelay());
                break;
            case CompletionType.Manual:
                // Will be completed by external call to Complete()
                break;
        }
    }

    /// <summary>
    /// Coroutine that waits for the specified delay before completing the action
    /// </summary>
    private IEnumerator CompleteAfterDelay()
    {
        yield return new WaitForSeconds(completionDelay);
        Complete();
    }

    /// <summary>
    /// Public method that can be called by other scripts to manually complete the action.
    /// Useful when the completion depends on external events or systems.
    /// </summary>
    public void SetComplete()
    {
        Complete();
    }
} 