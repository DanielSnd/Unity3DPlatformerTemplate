using UnityEngine;
using System;

[Serializable]
public class GameObjectChangeMaterialAction : TriggerAction
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private Material newMaterial;
    
    protected override void OnExecute()
    {
        if (targetObject != null && newMaterial != null)
        {
            Renderer renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = newMaterial;
            }
        }
        Complete();
    }
}
