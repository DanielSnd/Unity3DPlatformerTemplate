using UnityEngine;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif
/// <summary>
/// Attribute that creates a button in the inspector which executes the marked method when clicked.
/// Can only be used on methods with no parameters.
/// </summary>
public class ButtonAttribute : CustomPropertyAttribute
{
    public string ButtonText { get; private set; }

    public ButtonAttribute(string buttonText = null)
    {
        ButtonText = buttonText;
    }

#if UNITY_EDITOR
    public override bool OverrideOnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get the method info from the property
        string methodName = property.name.Substring(1);
        object target = property.serializedObject.targetObject;
        MethodInfo methodInfo = target.GetType().GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (methodInfo == null)
        {
            EditorGUI.HelpBox(position, $"Method {methodName} not found.", MessageType.Error);
            return true;
        }

        // Get button text (use method name if no text specified)
        string buttonText = string.IsNullOrEmpty(ButtonText)
            ? ObjectNames.NicifyVariableName(methodName)
            : ButtonText;

        // Draw the button
        if (GUI.Button(position, buttonText))
        {
            // Check if method has parameters
            ParameterInfo[] parameters = methodInfo.GetParameters();
            if (parameters.Length > 0)
            {
                Debug.LogError($"Method {methodName} cannot have parameters to be used with ButtonAttribute");
                return true;
            }

            // Invoke the method
            methodInfo.Invoke(target, null);
        }
        return true;
    }


#endif
} 