using UnityEngine;

[System.Serializable]
public class DisplayScreenTextAction : TriggerAction
{
    [SerializeField] private string textToDisplay = "";
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private Color textColor = Color.white;
    
    private float timer;

    protected override void OnExecute()
    {
        GameManager.menuHelper.Label.text = textToDisplay;
        GameManager.menuHelper.Label.color = textColor;
        timer = displayDuration;
        
        // Start a coroutine to clear the text after duration
        MonoBehaviour runner = GameObject.FindObjectOfType<SmartTrigger>();
        if (runner != null)
        {
            runner.StartCoroutine(ClearTextAfterDelay());
        }
    }

    private System.Collections.IEnumerator ClearTextAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        GameManager.menuHelper.Label.text = "";
        Complete();
    }
} 