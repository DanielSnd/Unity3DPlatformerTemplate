using UnityEngine;

public enum AnimatorSetType {
    SetTrigger,
    SetFloat,
    SetBool
}
public class AnimatorTriggerAction : TriggerAction
{
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private string parameterName;
    [SerializeField][Conditional("isBoolSetType", true)] private bool statusToSet;
    [SerializeField][Conditional("isFloatSetType", true)] private float floatToSet;
    [SerializeField] private AnimatorSetType setAnimatorParameter = AnimatorSetType.SetBool;
    private bool isFloatSetType {get => setAnimatorParameter == AnimatorSetType.SetFloat;}
    private bool isBoolSetType {get => setAnimatorParameter == AnimatorSetType.SetBool;}

    protected override void OnExecute()
    {
        if (targetAnimator != null) {
            if (setAnimatorParameter == AnimatorSetType.SetTrigger) {
                targetAnimator.SetTrigger(parameterName);
            }else if(setAnimatorParameter == AnimatorSetType.SetFloat) {
                targetAnimator.SetFloat(parameterName, floatToSet);
            } else {
                targetAnimator.SetBool(parameterName, statusToSet);
            }
        }
        Complete();
    }
} 