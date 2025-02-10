using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CustomPropertyAttribute), true)]
public class CustomizedPropertyDrawer : PropertyDrawer
{
    protected bool visible = false;
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var modifiable = (CustomPropertyAttribute)attribute;
        if (modifiable != null && modifiable.modifiers == null)
            modifiable.modifiers = fieldInfo.GetCustomAttributes(typeof(CustomModifierPropertyAttribute), false)
            .Cast<CustomModifierPropertyAttribute>().OrderBy(s => s.order).ToList();

        if (modifiable != null && modifiable.modifiers != null) {
            float height = modifiable.GetPropertyHeight(property, label);
            foreach (var attr in modifiable.modifiers)
                height = attr.GetHeight(property, label, height);
            return height;
        }
        return GetPropertyHeight(property, label);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var modifiable = (CustomPropertyAttribute)attribute;

        visible = true;
        if (modifiable != null && modifiable.modifiers != null) {
            foreach (var attr in modifiable.modifiers.AsEnumerable().Reverse())
                visible = attr.BeforeGUI(ref position, property, label, visible);
        }

        if (visible)
            ActualOnGUI(position, property, label);

        if (modifiable != null && modifiable.modifiers != null) {
                foreach (var attr in modifiable.modifiers)
                    attr.AfterGUI(position, property, label);
        }
    }

    public virtual void ActualOnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var modifiable = (CustomPropertyAttribute)attribute;
        var fieldValue = fieldInfo.Name;

        SerializedProperty fieldproperty = property.serializedObject.FindProperty(fieldValue);
        if (fieldproperty != null) {
            if (!modifiable.OverrideOnGUI(position, fieldproperty, label))
            {
                EditorGUI.BeginProperty(position, label, fieldproperty);
                EditorGUILayout.PropertyField(fieldproperty);
                EditorGUI.EndProperty();
            }
            //if (modifiable.FieldOnGUI(position, fieldproperty, label))
            //    return;
            return;
        }
        modifiable.AttributeOnGUI(position, property, label);
    }
}