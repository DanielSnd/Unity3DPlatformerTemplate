using UnityEngine;
using System;

/// <summary>
/// Base class for all trigger actions. Inherit from this class to create new action types.
/// Actions can be configured to run in parallel or sequence and report their completion status.
/// </summary>
[System.Serializable]
public class TriggerAction
{
    [Tooltip("If true, this action will run alongside the previous action instead of waiting for it to complete")]
    [SerializeField][HideInInspector] private bool runInParallel = false;
    
    public string GetTriggerTypeName() { return GetType().Name;  }
    /// <summary>
    /// Indicates whether this action should execute in parallel with the previous action
    /// </summary>
    public bool RunInParallel => runInParallel;
    
    /// <summary>
    /// Tracks whether the action has completed its execution
    /// </summary>
    protected bool isComplete = true;
    public bool IsComplete => isComplete;
    
    protected MonoBehaviour owner;

    /// <summary>
    /// Initiates the execution of this action. Sets completion status and calls OnExecute.
    /// </summary>
    public virtual void Execute(MonoBehaviour _executor)
    {
        owner = _executor;
        isComplete = false;
        OnExecute();
    }

    /// <summary>
    /// Initiates the interruption of this action. Sets completion status and calls OnInterrupt.
    /// </summary>
    public virtual void Interrupt()
    {
        isComplete = true;
        OnInterrupt();
    }
    /// <summary>
    /// Override this method to implement the action's specific behavior.
    /// Remember to call Complete() when the action is finished.
    /// </summary>
    protected virtual void OnExecute() { }

    /// <summary>
    /// Override this method to implement what should happen when an action is interrupted.
    /// </summary>
    protected virtual void OnInterrupt() { }

    /// <summary>
    /// Marks the action as complete. Call this when your action has finished executing.
    /// </summary>
    protected void Complete()
    {
        isComplete = true;
        OnComplete();
    }

    /// <summary>
    /// Override this method to implement what should happen when an action is completed without interruption.
    /// </summary>
    protected virtual void OnComplete() { }
} 