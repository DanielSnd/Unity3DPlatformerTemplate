using UnityEngine;
using System;
using DG.Tweening;

/// <summary>
/// Trigger action that creates and executes DOTween animations.
/// Supports movement, rotation, scaling, and fade animations with various modes and settings.
/// </summary>
[Serializable]
public class DOTweenAction : TriggerAction
{
    /// <summary>
    /// Defines the type of property to animate
    /// </summary>
    public enum TweenType
    {
        Move,       // Animate position
        Rotate,     // Animate rotation
        Scale,      // Animate scale
        Fade        // Animate transparency (requires CanvasGroup for UI elements)
    }
    
    /// <summary>
    /// Defines how the animation should behave
    /// </summary>
    public enum TweenMode
    {
        To,         // Animate to a target value
        From,       // Animate from a starting value
        Punch,      // Create a punch effect that returns to original
        Shake       // Create a shake effect that returns to original
    }
    
    [Tooltip("The object to animate")]
    [SerializeField] private Transform target;

    [Tooltip("Optional target Transform to move/rotate towards (takes priority over value if set)")]
    [SerializeField] private Transform targetTransform;

    [Tooltip("What property of the object to animate")]
    [SerializeField] private TweenType tweenType = TweenType.Move;

    [Tooltip("How the animation should behave")]
    [SerializeField] private TweenMode tweenMode = TweenMode.To;

    [Tooltip("Target value for the animation (interpretation depends on TweenType)")]
    [SerializeField] private Vector3 value;

    [Tooltip("Whether to use local space for movement (only applies to Move TweenType)")]
    [SerializeField] private bool useLocalSpace = false;

    [Tooltip("How long the animation should take")]
    [SerializeField] private float duration = 1f;

    [Tooltip("The easing function to use")]
    [SerializeField] private Ease easeType = Ease.OutQuad;

    [SerializeField] private bool snapping = false;
    [SerializeField] private int vibrato = 10;
    [SerializeField] private float elasticity = 1f;
    [SerializeField] private bool fadeCanvasGroup = false;
    
    private Tween currentTween;
    
    /// <summary>
    /// Creates and starts the appropriate tween based on the configured settings
    /// </summary>
    protected override void OnExecute()
    {
        if (target == null) 
        {
            Complete();
            return;
        }

        currentTween = CreateTween()
            .SetEase(easeType)
            .OnComplete(() => Complete());
    }
    
    private Tween CreateTween()
    {
        switch (tweenType)
        {
            case TweenType.Move:
                return CreateMoveTween();
            case TweenType.Rotate:
                return CreateRotateTween();
            case TweenType.Scale:
                return CreateScaleTween();
            case TweenType.Fade:
                return CreateFadeTween();
            default:
                return null;
        }
    }
    
    private Tween CreateMoveTween()
    {
        // Get the target position either from targetTransform or value
        Vector3 targetPosition = targetTransform != null ? targetTransform.position : value;

        switch (tweenMode)
        {
            case TweenMode.To:
                return useLocalSpace ? 
                    target.DOLocalMove(targetPosition, duration, snapping) :
                    target.DOMove(targetPosition, duration, snapping);
            case TweenMode.From:
                return useLocalSpace ? 
                    target.DOLocalMove(targetPosition, duration, snapping).From() :
                    target.DOMove(targetPosition, duration, snapping).From();
            case TweenMode.Punch:
                return useLocalSpace ?
                    target.DOPunchPosition(targetPosition, duration, vibrato, elasticity, snapping) :
                    target.DOPunchPosition(targetPosition, duration, vibrato, elasticity, snapping);
            case TweenMode.Shake:
                return useLocalSpace ?
                    target.DOShakePosition(duration, targetPosition, vibrato, elasticity, snapping) :
                    target.DOShakePosition(duration, targetPosition, vibrato, elasticity, snapping);
            default:
                return null;
        }
    }
    
    private Tween CreateRotateTween()
    {
        switch (tweenMode)
        {
            case TweenMode.To:
                return target.DORotate(value, duration, RotateMode.FastBeyond360);
            case TweenMode.From:
                return target.DORotate(value, duration, RotateMode.FastBeyond360).From();
            case TweenMode.Punch:
                return target.DOPunchRotation(value, duration, vibrato, elasticity);
            case TweenMode.Shake:
                return target.DOShakeRotation(duration, value, vibrato, elasticity);
            default:
                return null;
        }
    }
    
    private Tween CreateScaleTween()
    {
        switch (tweenMode)
        {
            case TweenMode.To:
                return target.DOScale(value, duration);
            case TweenMode.From:
                return target.DOScale(value, duration).From();
            case TweenMode.Punch:
                return target.DOPunchScale(value, duration, vibrato, elasticity);
            case TweenMode.Shake:
                return target.DOShakeScale(duration, value, vibrato, elasticity);
            default:
                return null;
        }
    }
    
    private Tween CreateFadeTween()
    {
        if (fadeCanvasGroup)
        {
            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                switch (tweenMode)
                {
                    case TweenMode.To:
                        return canvasGroup.DOFade(value.x, duration);
                    case TweenMode.From:
                        return canvasGroup.DOFade(value.x, duration).From();
                    default:
                        return null;
                }
            }
        }
        
        return null;
    }
} 