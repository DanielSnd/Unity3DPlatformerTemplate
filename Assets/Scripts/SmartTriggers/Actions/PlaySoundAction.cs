using UnityEngine;
using System;
using System.Dynamic;
using System.Collections;

[Serializable]
public class PlaySoundAction : TriggerAction
{
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private Transform playLocation;
    
    protected override void OnExecute()
    {
        audioClip.PlaySound(playLocation != null ? playLocation.position : owner.transform.position);
        Complete();
    }

}