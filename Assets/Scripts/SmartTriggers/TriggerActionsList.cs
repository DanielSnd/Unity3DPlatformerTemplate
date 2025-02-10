using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

/// <summary>
/// Base class for all trigger actions. Inherit from this class to create new action types.
/// Actions can be configured to run in parallel or sequence and report their completion status.
/// </summary>
[System.Serializable]
public class TriggerActionsList
{
    [Tooltip("Actions to execute when the trigger activates")]
    [SerializeReference] public List<TriggerAction> list = new List<TriggerAction>();

    private TriggerInterruptorHolder triggerInterruptor = new TriggerInterruptorHolder();
    public bool hasAlreadyExecuted { get; set; } = false;
    public bool isExecutingList { get; set; } = false;
    public MonoBehaviour ListOwner { get; private set; }

    public TriggerAction this [int index] {
        get { return list[index]; }
        set { list[index] = value; }
    }
    public void Add(params TriggerAction[] adding) {
        list.AddRange(adding);
    }
    public void Clear() => list.Clear();
    public int Count => list.Count;
    public void RemoveAt(int indx) => list.RemoveAt(indx);
    public void Remove(TriggerAction t) => list.Remove(t);

    public void Interrupt() { triggerInterruptor.interrupt = true; }
    public void ClearInterruption() { triggerInterruptor.interrupt = false; }


    public bool ExecuteTriggerActions(MonoBehaviour listOwner, Action executeAfterwards = null, bool canBeInterrupted = false) {
        if (isExecutingList) return false;
        isExecutingList = true;
        ListOwner = listOwner;

        listOwner.StartCoroutine(ExecuteActionsRoutine(list, canBeInterrupted ? triggerInterruptor : null, () =>
        {
            isExecutingList = false;
            triggerInterruptor.interrupt = false;
            if (executeAfterwards != null)
                executeAfterwards.Invoke();
        }));

        hasAlreadyExecuted = true;

        return true;
    }

    /// <summary>
    /// Executes the trigger actions in sequence, respecting parallel execution flags.
    /// </summary>
    private IEnumerator ExecuteActionsRoutine(List<TriggerAction> actions, TriggerInterruptorHolder interruptorHolder = null, Action afterwards = null)
    {
        List<TriggerAction> runningActions = new List<TriggerAction>();
        bool beenInterrupted = false;
        foreach (var action in actions)
        {
            if (action == null) continue;

            // If previous actions aren't running in parallel, wait for them to complete
            if (runningActions.Count > 0 && !action.RunInParallel)
            {
                while (runningActions.Any(a => !a.IsComplete) && !InterruptCheck(interruptorHolder))
                    yield return null;

                runningActions.Clear();
            }

            if (InterruptCheck(interruptorHolder)) {
                beenInterrupted = true;
                break;
            }
            action.Execute(ListOwner);
            runningActions.Add(action);

            // If this action runs in parallel, continue immediately to the next action
            if (action.RunInParallel)
                continue;
        }
        if (!beenInterrupted) {
            // Wait for any remaining actions to complete
            while (runningActions.Any(a => !a.IsComplete) && !InterruptCheck(interruptorHolder))
                yield return null;

        }

        if (beenInterrupted || InterruptCheck(interruptorHolder)) {
            for (int i = runningActions.Count - (1); i >= 0; i--)
                runningActions[i].Interrupt();

        }

        if (afterwards != null)
            afterwards.Invoke();
        
        triggerInterruptor.interrupt  = false;

        bool InterruptCheck(TriggerInterruptorHolder interruptorHolder) {
            return interruptorHolder != null && interruptorHolder.interrupt;
        }
    }

}



public class TriggerInterruptorHolder
{
    public bool interrupt = false;
}
