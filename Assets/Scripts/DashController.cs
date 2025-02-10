using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DashController : MonoBehaviour
{
    [Header("Dash Settings")]
    [Tooltip("How fast the player moves during dash")]
    public float dashSpeed = 20f;
    [Tooltip("How long the dash lasts")]
    public float dashDuration = 0.3f;
    [Tooltip("Cooldown before player can dash again")]
    public float dashCooldown = 1f;
    [Tooltip("How much to keep the player up in the air during dash")]
    [Range(0, 1)]
    public float airDashUpwardsForce = 0.5f;
    [Tooltip("Sound played when dashing")]
    public AudioClip dashAudio;
    [Tooltip("Particle system for dash effect")]
    public ParticleSystem dashParticles;
    [Tooltip("Whether player is invulnerable while dashing")]
    public bool dashInvulnerability = true;

    [Tooltip("Curve to control dash force along the dash")]
    public AnimationCurve dashCurve = new AnimationCurve();

    [Tooltip("Material to be used in dash after-images")]
    public Material afterImageMaterial;
    [Tooltip("Curve to control dash force along the dash")]
    public AnimationCurve afterImageOpacityCurve = new AnimationCurve();
    [Tooltip("Curve to control dash force along the dash")]
    public Color dashTrailColor;

    // Dash state
    private bool isDashing;
    private float dashEndTime, dashStartTime;
    private float nextDashTime;
    private Vector3 dashDirection;
    float nextAfterImageSpawn = 0.0f;

    public static List<Mesh> dashMeshPool = new List<Mesh>();

    private MeshFilter[] meshFilters;
    private SkinnedMeshRenderer[] skinnedMeshRenderers;
    private List<AfterImageData> afterImageDataList = new List<AfterImageData>();
    public struct AfterImageData
    {
        public Matrix4x4 transform;
        public Mesh sharedMesh;
        public float time;
        public bool pooledMesh;
    }

    private Animator animator;
    private MovementController moveController;
    private Rigidbody rb;
    private HealthController healthComponent;
    private TrailRenderer trail;

    private int originalLayer;

    public static int InvulnerableLayer = -1;
    private void Start()
    {
        if (InvulnerableLayer == -1)
            InvulnerableLayer = LayerMask.NameToLayer("Invulnerable");
        originalLayer = gameObject.layer;
        meshFilters = GetComponentsInChildren<MeshFilter>();
        skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        TryGetComponent(out rb);
        TryGetComponent(out healthComponent);
        TryGetComponent(out moveController);
        trail = GetComponentInChildren<TrailRenderer>();
        animator = GetComponentInChildren<Animator>();
    }

    private void LateUpdate()
    {
        if (afterImageDataList.Count > 0)
            RenderAfterImages();
    }
    void FixedUpdate()
    {
        // Handle dash movement
        if (!isDashing) return;
        UpdateDash();
    }

    private void CreateAfterImage()
    {
        foreach (var mf in meshFilters)
        {
            AfterImageData data = new AfterImageData()
            {
                transform = mf.transform.localToWorldMatrix,
                sharedMesh = mf.sharedMesh,
                time = Time.time,
                pooledMesh = false
            };
            afterImageDataList.Add(data);
        }
        foreach (var smr in skinnedMeshRenderers)
        {
            Mesh pooledMesh = null;
            if (dashMeshPool.Count > 0) {
                pooledMesh = dashMeshPool[dashMeshPool.Count - 1];
                dashMeshPool.RemoveAt(dashMeshPool.Count - 1);
            }
            if (pooledMesh == null) {
                pooledMesh = new Mesh();
            }
            smr.BakeMesh(pooledMesh, true);
            AfterImageData data = new AfterImageData()
            {
                transform = smr.transform.localToWorldMatrix,
                sharedMesh = pooledMesh,
                time = Time.time,
                pooledMesh = true
            };
            afterImageDataList.Add(data);
        }

    }

    private void RenderAfterImages()
    {
        if (afterImageMaterial == null) return;

        float currentTime = Time.time;
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        // Set a random color in the MaterialPropertyBlock
        
        for (int i = afterImageDataList.Count - 1; i >= 0; i--)
        {
            AfterImageData data = afterImageDataList[i];
            float timeElapsed = currentTime - data.time;
            float alpha = 1.2f - Mathf.Clamp(timeElapsed * 3.42f, 0.0f, 1.22f);
            if (alpha <= 0)
            {
                if (afterImageDataList[i].pooledMesh && afterImageDataList[i].sharedMesh != null)
                {
                    dashMeshPool.Add(afterImageDataList[i].sharedMesh);
                    afterImageDataList[i] = new AfterImageData();
                }
                afterImageDataList.RemoveAt(i);
                continue;
            }

            propertyBlock.SetFloat("_Opacity", (afterImageOpacityCurve != null? afterImageOpacityCurve.Evaluate(alpha) : alpha) * 0.4f);

            Graphics.DrawMesh(data.sharedMesh, data.transform, afterImageMaterial, 0, null, 0, propertyBlock);
        }
    }

    /// <summary>
    /// Attempts to start a dash if conditions are met
    /// </summary>
    public void TryStartDash(Vector3 _dashDirection)
    {
        if (isDashing || Time.time < nextDashTime) return;
        gameObject.layer = InvulnerableLayer;
        // Get dash direction based on input or facing direction
        dashDirection = _dashDirection.normalized;
        if (dashDirection == Vector3.zero)
        {
            dashDirection = animator ? animator.transform.forward : transform.forward;
        }

        // Start dash
        isDashing = true;
        dashStartTime = Time.time;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = dashEndTime + dashCooldown;

        if (trail)
        {
            trail.startColor = new Color(dashTrailColor.r, dashTrailColor.g, dashTrailColor.b, trail.startColor.a);
            trail.endColor = new Color(dashTrailColor.r, dashTrailColor.g, dashTrailColor.b, trail.endColor.a);
        }
        moveController.ApplyCustomSquashEffect(new Vector3(0.5f, 1.0f, 2.0f));
        moveController.enabled = false;

        CreateAfterImage();

        nextAfterImageSpawn = 0.05f;

        if (animator)
        {
            animator.SetTrigger(MovementController.AnimationID_Dash);
        }

        if (dashAudio)
            dashAudio.PlaySound(transform.position);

        if (dashParticles)
            dashParticles.Play();

        // Enable invulnerability
        if (dashInvulnerability && healthComponent != null)
            healthComponent.SetInvulnerable(true);
    }

    /// <summary>
    /// Update dash state and handle dash movement
    /// </summary>
    private void UpdateDash()
    {
        if (!isDashing) return;

        // Check if dash should end
        if (Time.time >= dashEndTime)
        {
            EndDash();
            return;
        }

        float dashProgress = Mathf.InverseLerp(dashStartTime, dashEndTime, Time.time);
        if (dashProgress > nextAfterImageSpawn)
        {
            CreateAfterImage();
            nextAfterImageSpawn = dashProgress + 0.19f;
        }
        // Apply dash movement
        rb.velocity = dashDirection * dashSpeed * dashCurve.Evaluate(dashProgress);
        if (dashProgress > 0.85f && moveController != null)
            moveController.enabled = true;

        if (moveController != null && !moveController.enabled)
        {
            moveController.Update();
            moveController.AlignWithDirection(dashDirection.normalized, 8.5f, true, true);
        }

        // Reduce gravity if dashing in air
        if ((moveController is AdvancedMoveController))
        {
            if (!((AdvancedMoveController)moveController).isGrounded)
            {
                rb.AddForce(new Vector3(0.0f, airDashUpwardsForce, 0.0f), ForceMode.Force);
                moveController.ApplyVelocityControl(((AdvancedMoveController)moveController).airFriction * 0.25f, moveController.maxVelocity * 1.5f, true);
            }
            else
            {
                moveController.ApplyVelocityControl(moveController.Friction * 0.6f, moveController.maxVelocity * 1.5f, true);
            }
        }
    }

    /// <summary>
    /// End dash and restore normal movement
    /// </summary>
    private void EndDash()
    {
        isDashing = false;
        gameObject.layer = originalLayer;
        if (moveController) moveController.enabled = true;
        // Disable invulnerability
        if (dashInvulnerability && healthComponent != null)
        {
            healthComponent.SetInvulnerable(false);
        }

        if (animator) {
            animator.ResetTrigger(MovementController.AnimationID_Dash);
        }

        if (dashParticles)
        {
            dashParticles.Stop();
        }

        if (trail)
        {
            trail.startColor = new Color(0.8f, 0.8f, 0.8f, trail.startColor.a);
            trail.endColor = new Color(0.8f, 0.8f, 0.8f, trail.endColor.a);
        }
    }
}
