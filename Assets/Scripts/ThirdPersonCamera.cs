using UnityEngine;

/// <summary>
/// Controls the camera's movement and rotation to follow a target while handling collision avoidance.
/// Provides smooth third-person camera behavior with orbital movement and wall detection.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour 
{
	[Header("Target Settings")]
	[Tooltip("The transform to follow and orbit around (usually the player)")]
	[SerializeField] private Transform followTarget;
	
	[Tooltip("The relative position of the camera from the target (x: right/left, y: up/down, z: forward/back)")]
	[SerializeField] private Vector3 cameraOffset = new Vector3(0f, 3.5f, 7f);
	
	[Header("Movement Settings")]
	[Tooltip("How quickly the camera moves to its intended position (higher = more responsive, lower = smoother)")]
	[SerializeField] private float positionLerpSpeed = 6f;
	
	[Tooltip("How fast the camera orbits around the target when receiving input")]
	[SerializeField] private float orbitSpeed = 100f;
	
	[Tooltip("How quickly the camera rotates to look at the target (0 = instant)")]
	[SerializeField] private float lookAtSpeed = 100f;
	
	[Tooltip("Minimum distance the camera can be pushed in when avoiding walls")]
	[SerializeField] private float minimumDistance = 5f;
	
	/// <summary>
	/// The pivot point used for orbital movement around the target
	/// </summary>
	private Transform orbitPivot;
	
	/// <summary>
	/// The original offset value, used to restore camera position after collision
	/// </summary>
	private Vector3 defaultOffset;
	
	/// <summary>
	/// Whether the camera is currently being blocked by an obstacle
	/// </summary>
	private bool isObstructed;
	
	/// <summary>
	/// Input value for orbital movement (-1 to 1, where -1 is left, 1 is right)
	/// </summary>
	public float OrbitInput { get; set; }

	private void Awake()
	{
		InitializeOrbitPivot();
		defaultOffset = cameraOffset;
		ValidateComponents();
	}
	
	/// <summary>
	/// Creates and initializes the orbital pivot point GameObject
	/// </summary>
	private void InitializeOrbitPivot()
	{
		orbitPivot = new GameObject("Camera Orbit Pivot").transform;
		DontDestroyOnLoad(orbitPivot);
	}

    private void OnDestroy()
    {
        if (orbitPivot != null) {
			Destroy(orbitPivot.gameObject);
			orbitPivot = null;
		}
    }

    /// <summary>
    /// Ensures all required components and references are properly set up
    /// </summary>
    private void ValidateComponents()
	{
		if (!followTarget)
		{
			Debug.LogError($"Missing target reference in {nameof(ThirdPersonCamera)} component", this);
		}
	}
	
	private void LateUpdate()
	{
		if (!followTarget || !enabled) return;
		
		HandleCollisionAvoidance();
		UpdateCameraPosition();
		UpdateCameraRotation();
	}
	
	/// <summary>
	/// Adjusts camera distance when colliding with obstacles
	/// Gradually moves camera in when obstructed and back out when clear
	/// </summary>
	private void HandleCollisionAvoidance()
	{
		if (isObstructed)
		{
			if (cameraOffset.magnitude > minimumDistance)
			{
				cameraOffset *= 0.99f;
			}
		}
		else
		{
			cameraOffset = Vector3.MoveTowards(cameraOffset, defaultOffset, Time.deltaTime);
		}
	}
	
	/// <summary>
	/// Updates the camera's position based on target movement and orbital input.
	/// Handles both following the target and orbiting around it.
	/// </summary>
	private void UpdateCameraPosition()
	{
		// Update orbit pivot position
		orbitPivot.position = followTarget.transform.position + orbitPivot.transform.TransformDirection(cameraOffset);
		//orbitPivot.Translate(cameraOffset, Space.Self);
		
		// Orbit around target based on input
		float rotationAmount = OrbitInput * orbitSpeed * Time.deltaTime;
		orbitPivot.RotateAround(followTarget.position, Vector3.up, rotationAmount);
		
		// Smoothly move camera to pivot position
		transform.position = Vector3.Lerp(
			transform.position, 
			orbitPivot.position, 
			positionLerpSpeed * Time.deltaTime
		);
	}
	
	/// <summary>
	/// Updates the camera's rotation to look at the target.
	/// Provides smooth or instant rotation based on lookAtSpeed setting.
	/// </summary>
	private void UpdateCameraRotation()
	{
		if (lookAtSpeed <= 0)
		{
			transform.LookAt(followTarget.position);
			return;
		}

		Quaternion targetRotation = Quaternion.LookRotation(followTarget.position - transform.position);
		transform.rotation = Quaternion.Slerp(
			transform.rotation,
			targetRotation,
			lookAtSpeed * Time.deltaTime
		);
	}
	
	/// <summary>
	/// Handles collision detection with obstacles.
	/// Ignores trigger colliders and water surfaces.
	/// </summary>
	private void OnTriggerEnter(Collider other)
	{
		if (!other.isTrigger && other.CompareTag("Water") == false)
		{
			isObstructed = true;
		}
	}
	
	/// <summary>
	/// Handles collision exit with obstacles.
	/// Ignores trigger colliders and water surfaces.
	/// </summary>
	private void OnTriggerExit(Collider other)
	{
		if (!other.isTrigger && other.CompareTag("Water") == false)
		{
			isObstructed = false;
		}
	}
}