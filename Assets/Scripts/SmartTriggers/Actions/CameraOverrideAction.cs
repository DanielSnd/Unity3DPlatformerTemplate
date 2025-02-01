using UnityEngine;

[System.Serializable]
public class CameraOverrideAction : TriggerAction
{
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 2, -5);
    [SerializeField] private float duration = 3f;
    [SerializeField] private float transitionSpeed = 2f;
    
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalTarget;
    private Camera mainCamera;
    private float timer;
    private bool transitioning = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    protected override void OnExecute()
    {
        mainCamera = Camera.main;
        if (mainCamera == null || lookAtTarget == null) 
        {
            Complete();
            return;
        }

        // Store original camera settings
        originalPosition = mainCamera.transform.position;
        originalRotation = mainCamera.transform.rotation;
        originalTarget = mainCamera.transform.parent; // Assuming camera is parented to follow target
        
        // Calculate target position and rotation
        targetPosition = lookAtTarget.position + cameraOffset;
        targetRotation = Quaternion.LookRotation(lookAtTarget.position - targetPosition);
        
        // Start the camera override
        SmartTrigger trigger = GameObject.FindObjectOfType<SmartTrigger>();
        if (trigger != null)
        {
            trigger.StartCoroutine(CameraOverrideRoutine());
        }
    }

    private System.Collections.IEnumerator CameraOverrideRoutine()
    {
        // Transition to new position
        float transitionTimer = 0;
        while (transitionTimer < 1)
        {
            transitionTimer += Time.deltaTime * transitionSpeed;
            mainCamera.transform.position = Vector3.Lerp(originalPosition, targetPosition, transitionTimer);
            mainCamera.transform.rotation = Quaternion.Lerp(originalRotation, targetRotation, transitionTimer);
            yield return null;
        }

        // Hold for duration
        yield return new WaitForSeconds(duration);

        // Transition back
        transitionTimer = 0;
        Vector3 currentPos = mainCamera.transform.position;
        Quaternion currentRot = mainCamera.transform.rotation;
        
        while (transitionTimer < 1)
        {
            transitionTimer += Time.deltaTime * transitionSpeed;
            mainCamera.transform.position = Vector3.Lerp(currentPos, originalPosition, transitionTimer);
            mainCamera.transform.rotation = Quaternion.Lerp(currentRot, originalRotation, transitionTimer);
            yield return null;
        }

        // Restore original position exactly
        mainCamera.transform.position = originalPosition;
        mainCamera.transform.rotation = originalRotation;
        
        Complete();
    }
} 