using UnityEngine;

/// <summary>
/// Attribute that creates a button in the inspector which executes the marked method when clicked.
/// Can only be used on methods with no parameters.
/// </summary>
public class ButtonAttribute : PropertyAttribute
{
    public string ButtonText { get; private set; }

    public ButtonAttribute(string buttonText = null)
    {
        ButtonText = buttonText;
    }
} 