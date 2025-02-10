using System;
using System.Collections.Generic;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem.Interactions;


[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public class CustomPropertyAttribute : PropertyAttribute
{
    
    public List<CustomModifierPropertyAttribute> modifiers = null;

#if UNITY_EDITOR

    public static object GetTargetObjectOfProperty(SerializedProperty prop)
    {
        if (prop == null) return null;
        Debug.Log(prop.propertyPath);
        
        var path = prop.propertyPath.Replace(".Array.data[", "[");
        object obj = prop.serializedObject.targetObject;
        var elements = path.Split('.');
        foreach (var element in elements) {
                obj = element.Contains("[") ? GetValue_Imp(obj, element.Substring(0, element.IndexOf("[")), System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""))) : GetValue_Imp(obj, element);
        }
        return obj;
    }
    
    public static object GetTargetObjectWithProperty(SerializedProperty prop) {
        var path = prop.propertyPath.Replace(".Array.data[", "[");
        object obj = prop.serializedObject.targetObject;
        var elements = path.Split('.');
        foreach (var element in elements) {
                obj = element.Contains("[") ? GetValue_Imp(obj, element.Substring(0, element.IndexOf("[")), System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""))) : GetValue_Imp(obj, element);
        }
        return obj;
    }
    
    private static object GetValue_Imp(object source, string name)
    {
        if (source == null) return null;
        var type = source.GetType();
        while (type != null) {
            var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (f != null) return f.GetValue(source);
            var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (p != null) return p.GetValue(source, null);
            type = type.BaseType;
        }
        return null;
    }

    private static object GetValue_Imp(object source, string name, int index)
    {
        var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
        if (enumerable == null) return null;
        var enm = enumerable.GetEnumerator();
        for (int i = 0; i <= index; i++) {
            if (!enm.MoveNext()) return null;
        }
        return enm.Current;
    }

    public virtual void AttributeOnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, label);
    }

    public virtual bool OverrideOnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        return false;
    }

    public virtual bool FieldOnGUI(Rect position, SerializedProperty property, GUIContent label) {
        return false;
    }

    public virtual float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label);
    }
#endif
}


[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
public abstract class CustomModifierPropertyAttribute : CustomPropertyAttribute
{
    public int order { get; set; }

#if UNITY_EDITOR
    public virtual float GetHeight(SerializedProperty property, GUIContent label, float height) {
        return height;
    }

    public virtual bool BeforeGUI(ref Rect position, SerializedProperty property, GUIContent label, bool visible) { return true; }
    public virtual void AfterGUI(Rect position, SerializedProperty property, GUIContent label) { }
#endif
}
