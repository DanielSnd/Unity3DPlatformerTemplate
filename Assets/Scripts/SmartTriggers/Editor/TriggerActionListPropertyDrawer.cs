using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using UnityObject = UnityEngine.Object;


[CustomPropertyDrawer(typeof(TriggerActionsList))]
public class TriggerActionListPropertyDrawer : PropertyDrawer
{
    private TriggerActionsList _TriggersActionList;
    //private bool _Foldout;

    string drawName = "";
    ReorderableList triggerList;
    private Action _clearCacheForTriggerList, _clearCacheForUntriggerList;
    private static readonly MethodInfo _clearCacheMethod = typeof(ReorderableList)
    .GetMethod("InvalidateForGUI", BindingFlags.Instance | BindingFlags.NonPublic);

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // var propertyHeight = base.GetPropertyHeight(property, label);
        // if (visible) {
        CheckInitialize(property, label);
            //if (_Foldout)
        if (property.isExpanded)
        {
            return triggerList.GetHeight() * 0.07f;
        }

        return EditorGUIUtility.singleLineHeight;
        // }
        // return propertyHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        CheckInitialize(property, label);

        //position.height = 12f;
        drawName = label.text;
        
        if (triggerList != null)
        {
            //triggerList.displayAdd = (property.isExpanded);
            //triggerList.displayRemove = (property.isExpanded) && _TriggersActionList.list.Count > 0;
            //triggerList.draggable = triggerList.displayRemove;
            triggerList.DoLayoutList();
        }
    }

    private void CheckInitialize(SerializedProperty property, GUIContent label)
    {
        if (_TriggersActionList == null)
        {
            _TriggersActionList = (TriggerActionsList)property.managedReferenceValue;
            //_Foldout = EditorPrefs.GetBool(label.text);

            triggerList = GetListWithFoldout(property.serializedObject, property.FindPropertyRelative("list"), true, true, true, true, ref _clearCacheForTriggerList);
        }
    }


    public ReorderableList GetListWithFoldout(SerializedObject serializedObject, SerializedProperty property, bool draggable, bool displayHeader, bool displayAddButton, bool displayRemoveButton, ref Action clearCacheRef)
    {
        var list = new ReorderableList(serializedObject, property, draggable, displayHeader, displayAddButton, displayRemoveButton);
        var newActionClearCache = (Action)Delegate.CreateDelegate(typeof(Action), list, _clearCacheMethod);
        clearCacheRef = newActionClearCache;

        list.drawHeaderCallback = (Rect rect) => {
            var newRect = new Rect(rect.x + 2, rect.y, rect.width - 10, rect.height);
            bool newValue = EditorGUI.Foldout(newRect, property.isExpanded, $"{drawName} Actions: {(triggerList.count > 0 ? ($"{triggerList.count} Action{(triggerList.count > 1 ? "s" : "")}") : ("Empty"))}");
            if (property.isExpanded == newValue)
                return;
            property.isExpanded = newValue;
            triggerList.displayAdd = newValue;
            triggerList.displayRemove = newValue;
            triggerList.draggable = newValue;
            newActionClearCache.Invoke();
        };
        list.drawElementCallback =
            (Rect rect, int index, bool isActive, bool isFocused) => {
                if (!property.isExpanded)
                {
                    //GUI.enabled = index == list.count;
                    return;
                }

                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;

                SerializedObject elementSerializedObject = element.serializedObject;

                elementSerializedObject.Update();

                //EditorGUI.BeginProperty(rect, GUIContent.none, element);
                SerializedProperty runInParallelProperty = element.FindPropertyRelative("runInParallel");

                if (element.isExpanded)
                {
                    var rectSpot = new Rect(EditorGUIUtility.currentViewWidth - 100.0f, rect.y, 100.0f, EditorGUIUtility.singleLineHeight);
                    runInParallelProperty.boolValue = EditorGUI.ToggleLeft(rectSpot, new GUIContent("Run Parallel"), runInParallelProperty.boolValue);
                }
                //EditorGUI.PropertyField(rect, runInParallelProperty);
                //EditorGUI.bar
                EditorGUI.PropertyField(rect, element, new GUIContent(element.managedReferenceValue != null ? element.managedReferenceValue.ToString() : "Null"), true);

                //// Apply the changes to the SerializedObject
                elementSerializedObject.ApplyModifiedProperties();

                //// End the drawing of the element
                //EditorGUI.EndProperty();
            };

        list.elementHeightCallback = (int indexer) => {
            if (!property.isExpanded)
                return 0;
            else
            {
                SerializedProperty elementProp = list.serializedProperty.GetArrayElementAtIndex(indexer);
                return EditorGUI.GetPropertyHeight(elementProp);
            }
        };

        list.onCanRemoveCallback = (ReorderableList l) => {
            return l.count > 0;
        };
        list.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) => {
            var menu = new GenericMenu();
            List<Type> inheritingTypes = GetListOfTypesInheritingTriggerAction<TriggerAction>();
            foreach (var inhType in inheritingTypes)
            {
                menu.AddItem(new GUIContent(inhType.Name), false, (object t) =>
                {
                    var addType = (Type)t;
                    var newInstance = System.Activator.CreateInstance(addType) as TriggerAction;
                    _TriggersActionList.Add(newInstance);
                    serializedObject.Update();
                }, inhType);
            }
            menu.ShowAsContext();
        };

        list.displayAdd = false;
        list.displayRemove = false;
        list.draggable = false;

        return list;
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

}
