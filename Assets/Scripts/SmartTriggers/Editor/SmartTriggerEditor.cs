using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;

[CustomEditor(typeof(SmartTrigger))]
public class SmartTriggerEditor : Editor
{
    private SerializedProperty optionsProperty;
    private SerializedProperty triggerLayersProperty;
    private SerializedProperty triggerTagsProperty;
    private SerializedProperty cooldownProperty;
    private SerializedProperty requiredWeightProperty;

    ReorderableList triggerList, untriggerList;

    private void OnEnable()
    {
        optionsProperty = serializedObject.FindProperty("triggerOptions");
        triggerLayersProperty = serializedObject.FindProperty("triggerLayers");
        triggerTagsProperty = serializedObject.FindProperty("triggerTags");
        cooldownProperty = serializedObject.FindProperty("cooldownBeforeReactivation");
        requiredWeightProperty = serializedObject.FindProperty("requiredWeight");
        SerializedProperty triggerListProperty = serializedObject.FindProperty("onTriggerActions");

        triggerList = new ReorderableList(serializedObject,
                    triggerListProperty,
                    true,
                    true,
                    true,
                    true);

        triggerList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Trigger Actions");
        };

        triggerList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = triggerList.serializedProperty.GetArrayElementAtIndex(index);

            SerializedObject elementSerializedObject = element.serializedObject;

            elementSerializedObject.Update();
            
            EditorGUI.BeginProperty(rect, GUIContent.none, element);

            EditorGUI.PropertyField(rect, element, new GUIContent(element.managedReferenceValue != null ? element.managedReferenceValue.ToString() : "Null"), true);
            
            //// Apply the changes to the SerializedObject
            elementSerializedObject.ApplyModifiedProperties();

            //// End the drawing of the element
            EditorGUI.EndProperty();
        };

        triggerList.elementHeightCallback = (int index) => {
            var element = triggerList.serializedProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true);
        };

        triggerList.onCanRemoveCallback = (ReorderableList l) => {
            return l.count > 0;
        };
        triggerList.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) => {
            var menu = new GenericMenu();
            List<Type> inheritingTypes = GetListOfTypesInheritingTriggerAction<TriggerAction>();
            foreach (var inhType in inheritingTypes)
            {
                menu.AddItem(new GUIContent(inhType.Name), false, addClickHandler, inhType);
            }
            menu.ShowAsContext();
        };

        untriggerList = new ReorderableList(serializedObject,
                    serializedObject.FindProperty("onUntriggerActions"),
                    true,
                    true,
                    true,
                    true);

        untriggerList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Untrigger Actions");
        };

        untriggerList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = untriggerList.serializedProperty.GetArrayElementAtIndex(index);

            SerializedObject elementSerializedObject = element.serializedObject;

            elementSerializedObject.Update();

            EditorGUI.BeginProperty(rect, GUIContent.none, element);

            EditorGUI.PropertyField(rect, element, new GUIContent(element.managedReferenceValue != null ? element.managedReferenceValue.ToString() : "Null"), true);

            //// Apply the changes to the SerializedObject
            elementSerializedObject.ApplyModifiedProperties();

            //// End the drawing of the element
            EditorGUI.EndProperty();
        };

        untriggerList.elementHeightCallback = (int index) => {
            var element = untriggerList.serializedProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true);
        };

        untriggerList.onCanRemoveCallback = (ReorderableList l) => {
            return l.count > 0;
        };
        untriggerList.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) => {
            var menu = new GenericMenu();
            List<Type> inheritingTypes = GetListOfTypesInheritingTriggerAction<TriggerAction>();
            foreach (var inhType in inheritingTypes)
            {
                menu.AddItem(new GUIContent(inhType.Name), false, addClickHandlerUntrigger, inhType);
            }
            menu.ShowAsContext();
        };
    }

    private void addClickHandler(object t)
    {
        var addType = (Type)t;
        var newInstance = System.Activator.CreateInstance(addType) as TriggerAction;
        ((SmartTrigger)target).SetTriggerListElement(triggerList.serializedProperty.arraySize, newInstance);

        serializedObject.Update();
    }

    private void addClickHandlerUntrigger(object t)
    {
        var addType = (Type)t;
        var newInstance = System.Activator.CreateInstance(addType) as TriggerAction;
        ((SmartTrigger)target).SetUnTriggerListElement(untriggerList.serializedProperty.arraySize, newInstance);
        serializedObject.Update();
    }

    public List<Type> GetListOfTypesInheritingTriggerAction<T>()
    {
        List<Type> objects = new List<Type>();
        foreach (Type type in
            Assembly.GetAssembly(typeof(T)).GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T))))
        {
            //objects.Add(typeof(T)Activator.CreateInstance(type, constructorArgs));
            objects.Add(type);
        }
        return objects;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Trigger Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        // Options field
        optionsProperty.intValue = EditorGUI.MaskField(
            EditorGUILayout.GetControlRect(), 
            new GUIContent("Options", "Configure how and when the trigger should activate"), 
            optionsProperty.intValue, 
            optionsProperty.enumNames
        );

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Trigger Conditions", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        // Basic conditions
        EditorGUILayout.PropertyField(triggerLayersProperty);
        EditorGUILayout.PropertyField(triggerTagsProperty);

        // Get current flags
        TriggerOptions flags = (TriggerOptions)optionsProperty.intValue;

        // Conditional properties based on flags
        if (flags.HasFlag(TriggerOptions.HasCooldown))
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Cooldown Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            
            EditorGUILayout.PropertyField(cooldownProperty, 
                new GUIContent("Cooldown Duration", "Time in seconds before the trigger can activate again"));
        }

        if (flags.HasFlag(TriggerOptions.RequiresMinWeight))
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Weight Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            
            EditorGUILayout.PropertyField(requiredWeightProperty, 
                new GUIContent("Required Weight", "Minimum total mass of objects required to activate the trigger"));
        }

        // Trigger Actions
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Trigger Actions", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
        if (triggerList != null) 
        {
            triggerList.DoLayoutList();
        }

        // Untrigger Actions (conditional)
        if (flags.HasFlag(TriggerOptions.UntriggerOtherwise))
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Untrigger Actions", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            untriggerList.DoLayoutList();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
