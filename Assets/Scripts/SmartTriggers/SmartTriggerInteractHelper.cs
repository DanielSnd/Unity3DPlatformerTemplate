using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class SmartTriggerInteractHelper : Interactable
{
    SmartTrigger smartTrigger;
    public void SetSmartTrigger(SmartTrigger owner)
    {
        smartTrigger = owner;
    }

    public override bool CanInteract(InteractionController controller)
    {
        return base.CanInteract(controller) && smartTrigger != null && smartTrigger.CanTrigger() && !smartTrigger.IsExecuting && smartTrigger.IsValidTrigger(controller.PlayerCollider);
    }

    public override bool OnInteract(InteractionController controller)
    {
        if (base.OnInteract(controller)) {
            return smartTrigger.TriggeringFromInteractHelper(controller);
        }
        return false;
    }
}
