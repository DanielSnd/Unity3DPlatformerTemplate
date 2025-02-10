using DG.Tweening;
using UnityEngine;

[System.Serializable]
public class DisplayScreenTextAction : TriggerAction
{
    [SerializeField] private string textToDisplay = "";
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private Color textColor = Color.white;
    
    protected override void OnExecute()
    {
        GameManager.Instance.MenuHelper.m_MiddleScreenLabel.text = textToDisplay;
        GameManager.Instance.MenuHelper.m_MiddleScreenLabel.style.color = textColor;
        GameManager.Instance.MenuHelper.m_MiddleScreenLabel.style.opacity = 0.0f;
        var newSequence = DOTween.Sequence();
        newSequence.Append(GameManager.Instance.MenuHelper.m_MiddleScreenLabel.DoFade(1.0f,0.45f));
        newSequence.AppendInterval(displayDuration);
        newSequence.Append(GameManager.Instance.MenuHelper.m_MiddleScreenLabel.DoFade(0.0f,0.45f));
        newSequence.OnComplete(Complete);
    }

    protected override void OnComplete()
    {
        base.OnComplete();
        GameManager.Instance.MenuHelper.m_MiddleScreenLabel.text = "";
        GameManager.Instance.MenuHelper.m_MiddleScreenLabel.style.color = Color.white;
        GameManager.Instance.MenuHelper.m_MiddleScreenLabel.style.opacity = 1.0f;
    }
} 