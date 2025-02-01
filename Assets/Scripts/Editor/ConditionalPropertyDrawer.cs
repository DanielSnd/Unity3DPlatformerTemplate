using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomPropertyDrawer(typeof(ConditionalAttribute))]
public class ConditionalPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ConditionalAttribute conditional = attribute as ConditionalAttribute;
        bool enabled = GetConditionalValue(conditional, property);

        if (enabled)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ConditionalAttribute conditional = attribute as ConditionalAttribute;
        bool enabled = GetConditionalValue(conditional, property);

        if (!enabled)
            return 0f;

        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    private bool GetConditionalValue(ConditionalAttribute conditional, SerializedProperty property)
    {
        SerializedObject obj = property.serializedObject;
        string propertyPath = conditional.conditionalProperty;

        // First try to find a property
        SerializedProperty conditionProperty = obj.FindProperty(propertyPath);
        if (conditionProperty != null)
        {
            return EvaluatePropertyValue(conditionProperty, conditional.compareValue);
        }

        // If no property found, try to find a method or field using reflection
        object target = obj.targetObject;
        FieldInfo field = target.GetType().GetField(propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            return EvaluateValue(field.GetValue(target), conditional.compareValue);
        }

        PropertyInfo prop = target.GetType().GetProperty(propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null)
        {
            return EvaluateValue(prop.GetValue(target), conditional.compareValue);
        }

        MethodInfo method = target.GetType().GetMethod(propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
        {
            return (bool)method.Invoke(target, null);
        }

        Debug.LogError($"Conditional property, field, or method '{propertyPath}' not found");
        return true;
    }

    private bool EvaluatePropertyValue(SerializedProperty property, object compareValue)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Boolean:
                return compareValue != null ? property.boolValue == (bool)compareValue : property.boolValue;
            case SerializedPropertyType.Enum:
                return compareValue != null ? property.enumValueFlag == (int)compareValue : property.enumValueFlag != 0;
            case SerializedPropertyType.Integer:
                return compareValue != null ? property.intValue == (int)compareValue : property.intValue != 0;
            case SerializedPropertyType.Float:
                return compareValue != null ? Mathf.Approximately(property.floatValue, (float)compareValue) : property.floatValue != 0;
            case SerializedPropertyType.String:
                return compareValue != null ? property.stringValue == (string)compareValue : !string.IsNullOrEmpty(property.stringValue);
            default:
                return true;
        }
    }

    private bool EvaluateValue(object value, object compareValue)
    {
        if (compareValue != null)
            return value.Equals(compareValue);

        if (value is bool boolValue)
            return boolValue;
        
        return value != null;
    }
} 