using UnityEngine;
using System.Reflection;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class ConditionalAttribute : CustomModifierPropertyAttribute
{
    public readonly string conditionalProperty;
    public readonly object compareValue;

    public ConditionalAttribute(string conditionalProperty, object compareValue = null)
    {
        this.conditionalProperty = conditionalProperty;
        this.compareValue = compareValue;
    }

#if UNITY_EDITOR

    bool hasCachedProperty = false;
    object cachedTarget = null;
    public override float GetHeight(SerializedProperty property, GUIContent label, float height)
    {
        return GetConditionalValue(this,property) ? 16f : 0f;
    }

    public override bool BeforeGUI(ref Rect position, SerializedProperty property, GUIContent label, bool visible) { return GetConditionalValue(this, property); }

    private bool GetConditionalValue(ConditionalAttribute conditional, SerializedProperty property)
    {
        string propertyPath = conditional.conditionalProperty;
        // Debug.Log(obj.targetObject.GetType().Name);
        // First try to find a property
        SerializedProperty conditionProperty = property.serializedObject.FindProperty(propertyPath);
        if (conditionProperty != null)
        {
            return EvaluatePropertyValue(conditionProperty, conditional.compareValue);
        }

        // If no property found, try to find a method or field using reflection
        object target = property.serializedObject.targetObject;
        // if (hasCachedProperty && cachedTarget != null) {
        //     return EvaluateValue(cachedTarget, conditional.compareValue);
        // }
        // Debug.Log($"{target.GetType().Name} field name {propertyPath} is editing multiple objects?{property.serializedObject.isEditingMultipleObjects}");
        bool resultF = AttemptFindCondition(conditional, propertyPath, target, out bool foundProp);
        if (!foundProp) {
            int index = property.propertyPath.LastIndexOf('.');
            if (index >= 0) {
                propertyPath = $"{property.propertyPath.Substring(0, index)}.{propertyPath}";
                bool resultFound = AttemptFindCondition(conditional, propertyPath, target, out foundProp);
                if (foundProp)
                    return resultFound;
            }
        }
        else return resultF;

        // Debug.LogError($"Conditional property, field, or method '{propertyPath}' not found");
        return true;

        bool AttemptFindCondition(ConditionalAttribute conditional, string propertyPath, object target, out bool foundProperty)
        {
            foundProperty = false;
            // Split path by periods and handle each segment
            if (propertyPath.Contains("Array.data"))
                propertyPath = propertyPath.Replace("Array.data","Array");
            string[] segments = propertyPath.Split('.');
            object currentObject = target;
            // Debug.Log($"Object {currentObject} type {currentObject.GetType().Name} propertypath {propertyPath} Segments {segments}");
            foreach (string segment in segments)
            {
                if (segment.StartsWith("Array")) {
                    foreach (PropertyInfo propertyInfo in currentObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        // Debug.Log($"Array properties... {propertyInfo.Name} is array? {propertyInfo.PropertyType.IsArray}");
                        if (propertyInfo.PropertyType.IsArray) {
                            // currentObject.GetType().GetMethod("get_Item").Invoke(currentObject, new object[] { 0 } );
                        }
                    }
                }
                // Debug.Log($"Going through segment {segment}  Object {currentObject} type {currentObject.GetType().Name} propertypath {propertyPath} Segments {segments}");
                FieldInfo field = currentObject.GetType().GetField(segment, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    currentObject = field.GetValue(currentObject);
                    continue;
                }
                
                PropertyInfo prop = currentObject.GetType().GetProperty(segment, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null)
                {
                    currentObject = prop.GetValue(currentObject);
                    continue;
                }

                // If the segment is not a field or property, it might be an array index (e.g., data[0])
                int bracketIndex = segment.IndexOf('[');
                if (bracketIndex >= 0)
                {
                    string arrayName = segment.Substring(0, bracketIndex);
                    string indexString = segment.Substring(bracketIndex).Trim('[', ']');
                    // Debug.Log($"Array name {arrayName} indexstring {indexString}");
                    if (int.TryParse(indexString, out int index))
                    {
                        FieldInfo arrayField = currentObject.GetType().GetField(arrayName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        MethodInfo minfo = currentObject.GetType().GetMethod("get_Item");
                        // Debug.Log($"found get item? {minfo != null}");
                        if (minfo != null){
                            currentObject = minfo.Invoke(currentObject, new object[] { index });
                            if (currentObject != null)
                                continue;
                        }
                        //currentObject.GetType().GetMethod("get_Item").Invoke(currentObject, new object[] { 0 } );
                        if (arrayField != null && arrayField.FieldType.IsArray)
                        {
                            Array array = (Array)arrayField.GetValue(currentObject);
                            currentObject = array.GetValue(index);
                            continue;
                        }
                    }
                }

                foundProperty = false;
                return false;
            }
            hasCachedProperty = true;
            cachedTarget = currentObject;
            foundProperty = true;
            return EvaluateValue(currentObject, conditional.compareValue);
        }

        // bool AttemptFindCondition(ConditionalAttribute conditional, string propertyPath, object target, out bool foundProperty)
        // {
        //     foundProperty = false;
        //     FieldInfo field = target.GetType().GetField(propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        //     if (field != null)
        //     {
        //         foundProperty=true;
        //         return EvaluateValue(field.GetValue(target), conditional.compareValue);
        //     }

        //     PropertyInfo prop = target.GetType().GetProperty(propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        //     if (prop != null)
        //     {
        //         foundProperty=true;
        //         return EvaluateValue(prop.GetValue(target), conditional.compareValue);
        //     }

        //     MethodInfo method = target.GetType().GetMethod(propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        //     if (method != null && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
        //     {
        //         foundProperty=true;
        //         return (bool)method.Invoke(target, null);
        //     }
        //     return false;
        // }
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
    #endif
} 