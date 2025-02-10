using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[System.Serializable]
public class CameraOverrideAction : TriggerAction
{
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 2, -5);
    [SerializeField] private float duration = 2f;
    [SerializeField] private float transitionSpeed = 1.0f;
    [SerializeField] private bool stopPlayerInputWhileOverriding;
    
    
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Camera mainCamera;
    private float timer;
    private bool transitioning = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private List<MonoBehaviour> disabledMonobehaviours = new List<MonoBehaviour>();
    protected override void OnExecute()
    {
        
        // Start the camera override
        if (owner != null)
        {
            if (owner is SmartTrigger) {
                PlayerController playerController = (owner as SmartTrigger).FindLastPlayerInteractor();
                if (playerController != null && playerController.CameraFollower != null) {
                        if (stopPlayerInputWhileOverriding) {
                            playerController.enabled = false;
                            disabledMonobehaviours.Add(playerController);
                        }
                        disabledMonobehaviours.Add(playerController.CameraFollower);
                        playerController.CameraFollower.enabled = false;
                        mainCamera = playerController.CameraFollower.GetComponent<Camera>();
                    }
                
            }
            
            if (mainCamera == null)
                mainCamera = Camera.main;
            // mainCamera = Camera.main;
            if (mainCamera == null || lookAtTarget == null) 
            {
                Complete();
                return;
            }
            
            // Store original camera settings
            originalPosition = mainCamera.transform.position;
            originalRotation = mainCamera.transform.rotation;
            
            // Calculate target position and rotation
            targetPosition = lookAtTarget.position + cameraOffset;
            targetRotation = Quaternion.LookRotation(lookAtTarget.position - targetPosition);
        
            var sequence = DOTween.Sequence();
            sequence.Append(mainCamera.transform.DOMove(targetPosition, transitionSpeed));
            sequence.Join(mainCamera.transform.DORotateQuaternion(targetRotation, transitionSpeed));
            sequence.AppendInterval(duration);
            sequence.Append(mainCamera.transform.DOMove(originalPosition, transitionSpeed));
            sequence.Join(mainCamera.transform.DORotateQuaternion(originalRotation, transitionSpeed));
            sequence.OnComplete(Complete);
        } else {
            Complete();
        }
    }


    protected override void OnComplete() {
        foreach (var item in disabledMonobehaviours)
        {
            item.enabled = true;
        }
        disabledMonobehaviours.Clear();
    }
} 