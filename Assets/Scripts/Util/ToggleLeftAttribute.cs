
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;
#endif

public class ToggleLeftAttribute : CustomPropertyAttribute
{

#if UNITY_EDITOR

    public override bool OverrideOnGUI (Rect position, SerializedProperty property, GUIContent label)
    {
        property.boolValue = EditorGUI.ToggleLeft(position, property.displayName, property.boolValue);
        return true;
    }

#endif
}