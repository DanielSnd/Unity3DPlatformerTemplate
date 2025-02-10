using UnityEngine;

public class DebugTriggerAction : TriggerAction
{
    [SerializeField] private string message = "Triggered!";

    protected override void OnExecute()
    {
        Debug.Log(message);
        Complete();
    }
} 