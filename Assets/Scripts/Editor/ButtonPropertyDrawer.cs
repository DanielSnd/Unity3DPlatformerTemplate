using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomPropertyDrawer(typeof(ButtonAttribute))]
public class ButtonPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get the attribute
        ButtonAttribute buttonAttribute = attribute as ButtonAttribute;
        
        // Get the method info from the property
        string methodName = property.name;
        object target = property.serializedObject.targetObject;
        MethodInfo methodInfo = target.GetType().GetMethod(methodName, 
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        if (methodInfo == null)
        {
            EditorGUI.HelpBox(position, $"Method {methodName} not found.", MessageType.Error);
            return;
        }

        // Get button text (use method name if no text specified)
        string buttonText = string.IsNullOrEmpty(buttonAttribute.ButtonText) 
            ? ObjectNames.NicifyVariableName(methodName) 
            : buttonAttribute.ButtonText;

        // Draw the button
        if (GUI.Button(position, buttonText))
        {
            // Check if method has parameters
            ParameterInfo[] parameters = methodInfo.GetParameters();
            if (parameters.Length > 0)
            {
                Debug.LogError($"Method {methodName} cannot have parameters to be used with ButtonAttribute");
                return;
            }

            // Invoke the method
            methodInfo.Invoke(target, null);
        }
    }
} 