using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using System.Collections;

[CustomEditor(typeof(SmartTrigger))]
public class SmartTriggerEditor : Editor
{
    private SerializedProperty optionsProperty;
    private SerializedProperty triggerLayersProperty;
    private SerializedProperty triggerTagsProperty;
    private SerializedProperty cooldownProperty;
    private SerializedProperty requiredWeightProperty;
    private SerializedProperty toggleWithInteractProperty;
    private SerializedProperty triggerList, untriggerList;
    private Action _clearCacheForTriggerList, _clearCacheForUntriggerList;
    private static readonly MethodInfo _clearCacheMethod = typeof(ReorderableList)
    .GetMethod("InvalidateForGUI", BindingFlags.Instance | BindingFlags.NonPublic);

    private void OnEnable()
    {
        optionsProperty = serializedObject.FindProperty("triggerOptions");
        triggerLayersProperty = serializedObject.FindProperty("triggerLayers");
        triggerTagsProperty = serializedObject.FindProperty("triggerTags");
        cooldownProperty = serializedObject.FindProperty("cooldownBeforeReactivation");
        requiredWeightProperty = serializedObject.FindProperty("requiredWeight");
        toggleWithInteractProperty = serializedObject.FindProperty("togglableByInteraction");
        triggerList = serializedObject.FindProperty("onTriggerActions");
        untriggerList = serializedObject.FindProperty("onUntriggerActions");
    }

    public override void OnInspectorGUI()
    {
        SmartTrigger smartTriggerTarget = (SmartTrigger)target; 
        serializedObject.Update();
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(new GUIContent("How it triggers", "Configure how and when the trigger should activate"), EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
        optionsProperty.intValue = EditorGUI.MaskField(EditorGUILayout.GetControlRect(), GUIContent.none, optionsProperty.intValue, optionsProperty.enumNames);
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();

        serializedObject.Update();

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Who can Trigger it", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        // Basic conditions
        EditorGUILayout.PropertyField(triggerLayersProperty, GUIContent.none);

        serializedObject.ApplyModifiedProperties();

        serializedObject.Update();
        EditorGUILayout.PropertyField(triggerTagsProperty, new GUIContent("Only with Tags:", "Tags allowed to trigger this, if none are set all tags will be allowed."));

        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();


        // Get current flags
        TriggerOptions flags = (TriggerOptions)optionsProperty.intValue;


        // Trigger Actions
        EditorGUILayout.Space(2);
        if (triggerList != null && (int)flags != 0)  {
            EditorGUI.indentLevel ++;
            EditorGUILayout.PropertyField(triggerList, new GUIContent("Trigger"));
            EditorGUI.indentLevel --;
        }

        // Untrigger Actions (conditional)
        if (flags.HasFlag(TriggerOptions.UntriggerOtherwise) || (flags.HasFlag(TriggerOptions.TriggerWhenPlayerInteract) && toggleWithInteractProperty.boolValue))
        {
            EditorGUILayout.Space(2);
            EditorGUI.indentLevel ++;
            EditorGUILayout.PropertyField(untriggerList, new GUIContent("Untrigger"));
            EditorGUI.indentLevel --;
        }


        EditorGUILayout.BeginHorizontal();
        // Conditional properties based on flags
        if (flags.HasFlag(TriggerOptions.HasCooldown))
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(new GUIContent("Cooldown Duration", "Time in seconds before the trigger can activate again"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(cooldownProperty, GUIContent.none);
            EditorGUILayout.EndVertical();
        }

        if (flags.HasFlag(TriggerOptions.RequiresMinWeight))
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(new GUIContent("Required Weight", "Minimum total mass of objects required to activate the trigger"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(requiredWeightProperty, GUIContent.none);
            EditorGUILayout.EndVertical();
        }

        if (flags.HasFlag(TriggerOptions.TriggerWhenPlayerInteract))
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(new GUIContent("Toggles Triggered/Untriggered", "Player interaction will toggle between triggered and untriggered status."), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(toggleWithInteractProperty, GUIContent.none);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        serializedObject.ApplyModifiedProperties();
    }
}
