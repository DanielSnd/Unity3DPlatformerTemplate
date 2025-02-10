using System;
using UnityEngine;

/// <summary>
/// Core movement functionality for physics-based characters.
/// Provides precise velocity-based movement with custom friction handling.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MovementController : MonoBehaviour 
{
    [Header("Movement Settings")]
    [Tooltip("Ground rotation speed")]
    public float rotationSpeed = 1.8f;
    [Tooltip("Maximum movement velocity")]
    public float maxVelocity = 9f;
    public float Acceleration = 70f;
    public float Friction = 7.6f;



	private Vector3 currentVelocity;
	public float distanceToDestination {get; set;}
	
	protected Rigidbody rb;
    protected Collider mainCollider;
	protected Animator animator;
    protected Vector3 baseModelScale;
    public bool overrideCanJump { get; set; } = false;

    public Vector3 CurrentVelocity { get => rb != null ? rb.velocity : Vector3.zero; }
    public static int AnimationID_DistanceToTarget = Animator.StringToHash("DistanceToTarget");
    public static int AnimationID_IsGrounded = Animator.StringToHash("Grounded");
    public static int AnimationID_YVelocity = Animator.StringToHash("YVelocity");
    public static int AnimationID_Death = Animator.StringToHash("Death");

    public static int AnimationID_Hurt = Animator.StringToHash("Hurt");
    public static int AnimationID_Attack = Animator.StringToHash("Attack");
    public static int AnimationID_Dash = Animator.StringToHash("Dash");

    protected static PhysicMaterial frictionlessMaterial = null;

	[Header("Squash and Stretch")]
	public Vector3 SquashEffect = new Vector3(1.2f, 0.8f, 1.2f);
    public Vector3 StretchEffect = new Vector3(0.7f, 1.2f, 0.7f);
	public float SquashAndStretchRecoverSpeed = 12f;

	private TrailRenderer trail;

	private bool hasTrail = false;
    private Vector2 overridingMultiplyForceAndFriction = Vector2.zero;
    protected virtual void Awake()
	{
		trail = GetComponentInChildren<TrailRenderer>();
		hasTrail = trail != null;
		// Cache rigidbody reference
		rb = GetComponent<Rigidbody>();
		mainCollider = GetComponent<Collider>();
		animator = GetComponentInChildren<Animator>();
		
		// Set up rigidbody constraints
		ConfigureRigidbody();
		ApplyFrictionlessMaterial();

        // Store original scale for animation effects
        if (animator)
            baseModelScale = animator.transform.localScale;
    }

	public virtual void Update() {
        if (animator)
        {
            // Smoothly return to original scale
            animator.transform.localScale = Vector3.Lerp(
                animator.transform.localScale,
                baseModelScale,
                Time.fixedDeltaTime * SquashAndStretchRecoverSpeed
            );
        }
    }
	private void ConfigureRigidbody()
	{
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		rb.constraints = RigidbodyConstraints.FreezeRotation;
	}

	private void ApplyFrictionlessMaterial()
	{
		if (frictionlessMaterial == null) {
			frictionlessMaterial = CreateFrictionlessMaterial();
		}
		mainCollider.material = frictionlessMaterial;
	}

	private PhysicMaterial CreateFrictionlessMaterial()
	{
		var material = new PhysicMaterial {
			name = "Frictionless",
			frictionCombine = PhysicMaterialCombine.Minimum,
			bounceCombine = PhysicMaterialCombine.Minimum,
			dynamicFriction = 0f,
			staticFriction = 0f
		};
		return material;
	}

	/// <summary>
	/// Applies force to move towards a target position
	/// </summary>
	/// <param name="targetPosition">Target world position to move towards</param>
	/// <param name="moveForce">Force applied to move towards target</param>
	/// <param name="arrivalThreshold">Minimum distance to target before stopping (should be > 0 to prevent jittering)</param>
	/// <param name="lockVertical">If true, ignores vertical movement, useful for ground-based movement. Disable if flying.</param>
	/// <returns>True when within arrivalThreshold of target</returns>
	public bool ApplyMovementForceTowards(Vector3 targetPosition, float moveForce, float arrivalThreshold, bool lockVertical = true)
    {
        Vector3 moveDirection = (targetPosition - transform.position);

		// Handle slipperiness possibility with override multiplier.
        if (overridingMultiplyForceAndFriction.magnitude > 0.0001f) {
			if (overridingMultiplyForceAndFriction.y < 0.6f) {
				var useVelocity = rb.velocity.magnitude > 0.001f ? rb.velocity : moveDirection;
				var directionAlignCheck = useVelocity.dotProduct(moveDirection);
				moveForce *= (1.2f - overridingMultiplyForceAndFriction.x) * Mathf.Clamp(1.0f - ((directionAlignCheck + 1.0f) * 0.5f), 0.38f, 1.0f);
			} else
				moveForce *= overridingMultiplyForceAndFriction.x;
		}

		if(lockVertical)
			moveDirection.y = 0;
		
		distanceToDestination = moveDirection.magnitude;

        if (distanceToDestination <= arrivalThreshold)
			return true;

		rb.AddForce(moveDirection.normalized * moveForce * Time.deltaTime, ForceMode.VelocityChange);
		return false;
	}

    /// <summary>
    /// Rotates entity to match its velocity direction
    /// </summary>
    /// <param name="rotationSpeed">How quickly to rotate towards the movement direction</param>
    /// <param name="lockVertical">If true, only considers horizontal velocity for rotation</param>
    public void AlignWithVelocity(bool lockVertical)
	{	
		Vector3 velocityDirection = lockVertical 
			? new Vector3(rb.velocity.x, 0f, rb.velocity.z)
			: rb.velocity;
		
		ApplyRotation(velocityDirection, rotationSpeed);
	}
	
	/// <summary>
	/// Rotates entity to face a specific direction
	/// </summary>
	/// <param name="targetDirection">Direction to face towards</param>
	/// <param name="rotationSpeed">How quickly to rotate towards the target direction</param>
	/// <param name="lockVertical">If true, only considers horizontal rotation</param>
	public void AlignWithDirection(Vector3 targetDirection, float rotationSpeed, bool lockVertical, bool force = false)
	{
		if(lockVertical) {
			targetDirection.y = 0;
		}
		
		ApplyRotation(targetDirection, rotationSpeed, force);
	}
	
	private void ApplyRotation(Vector3 direction, float rotationSpeed, bool force = false)
	{
        if (!enabled && !force)
            return;
        if (direction.magnitude > 0.1f && Mathf.Abs(rotationSpeed) > 0.01f)
		{
			Quaternion targetRotation = Quaternion.LookRotation(direction);
			Quaternion newRotation = Quaternion.Slerp(
				transform.rotation, 
				targetRotation, 
				direction.magnitude * (Mathf.Clamp(rotationSpeed - rb.angularDrag * 5f, rotationSpeed*0.4f,rotationSpeed)) * Time.deltaTime
			);
			rb.MoveRotation(newRotation);
		}
	}

    /// <summary>
    /// Applies velocity-based friction and enforces speed limits
    /// </summary>
    public void ApplyVelocityControl()
    {
        ApplyVelocityControl(Friction, maxVelocity, false);
    }

    /// <summary>
    /// Applies velocity-based friction and enforces speed limits
    /// </summary>
    /// <param name="friction">How quickly to slow down when no force is applied</param>
    /// <param name="speedLimit">Maximum allowed velocity magnitude</param>
    /// <param name="lockVertical">If true, only affects horizontal velocity</param>
    public void ApplyVelocityControl(float friction, float speedLimit, bool lockVertical)
	{
		if (overridingMultiplyForceAndFriction.magnitude > 0.0001f)
			friction *= overridingMultiplyForceAndFriction.y;
		currentVelocity = rb.velocity.SetY(lockVertical ? 0 : rb.velocity.y);
		if (trail != null)
			trail.emitting = currentVelocity.magnitude > 0.5f;
		if (currentVelocity.magnitude > 0) {
            ApplyFrictionForce(friction + (rb.drag * 10.0f));
            currentVelocity = rb.velocity.SetY(lockVertical ? 0 : rb.velocity.y);

			if (currentVelocity.magnitude > speedLimit) {
				ApplyFrictionForce(friction + (rb.drag * 10.0f));
			}
        }
    }

    private void ApplyFrictionForce(float friction)
    {
        rb.AddForce((currentVelocity * -1) * friction * Time.deltaTime, ForceMode.VelocityChange);
    }


    public void ApplyCustomSquashEffect(Vector3 customSquash)
    {
        if (!animator) return;
        animator.transform.localScale = new Vector3(
            customSquash.x * baseModelScale.x,  // Horizontal squeeze
            customSquash.y * baseModelScale.y,   // Vertical stretch
            customSquash.z * baseModelScale.z   // Horizontal squeeze
        );
    }

    public void ApplyJumpSquashEffect()
    {
        if (!animator) return;

        animator.transform.localScale = new Vector3(
            StretchEffect.x * baseModelScale.x,  // Horizontal squeeze
            StretchEffect.y * baseModelScale.y,   // Vertical stretch
            StretchEffect.z * baseModelScale.z   // Horizontal squeeze
        );
    }

    public void ApplyLandingSquashEffect()
    {
        if (!animator) return;

        animator.transform.localScale = new Vector3(
            SquashEffect.x * baseModelScale.x,  // Horizontal stretch
            SquashEffect.y * baseModelScale.y,   // Vertical squash
            SquashEffect.z * baseModelScale.z   // Horizontal stretch
        );
    }

	public void SetOverridingForceAndFriction(Vector2 _overridingValues)
	{
		overridingMultiplyForceAndFriction = _overridingValues;
	}
}
