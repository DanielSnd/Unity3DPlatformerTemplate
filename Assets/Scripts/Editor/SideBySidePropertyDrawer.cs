using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomPropertyDrawer(typeof(SideBySideAttribute))]
public class SideBySidePropertyDrawer : PropertyDrawer
{
    // Static dictionary to track properties in the same group across multiple drawers
    private static Dictionary<string, List<PropertyDrawerGroup>> groupedProperties = new Dictionary<string, List<PropertyDrawerGroup>>();
    
    private class PropertyDrawerGroup
    {
        public SerializedProperty property;
        public string propertyPath;
        public float widthWeight;
        public float minWidth;
        public bool drawn;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var attr = attribute as SideBySideAttribute;
        
        // Only the first property in the group should contribute to height
        if (!IsFirstPropertyInGroup(property, attr.groupId))
            return 0f;
            
        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = attribute as SideBySideAttribute;
        
        // Register this property in the group if not already registered
        RegisterProperty(property, attr);
        
        // Only draw the group once (when we hit the first property)
        if (!IsFirstPropertyInGroup(property, attr.groupId))
            return;
            
        DrawGroup(position, attr.groupId);
    }
    
    private void RegisterProperty(SerializedProperty property, SideBySideAttribute attr)
    {
        if (!groupedProperties.ContainsKey(attr.groupId))
        {
            groupedProperties[attr.groupId] = new List<PropertyDrawerGroup>();
        }
        
        var group = groupedProperties[attr.groupId];
        
        // Check if this property is already registered
        if (!group.Any(p => p.propertyPath == property.propertyPath))
        {
            group.Add(new PropertyDrawerGroup
            {
                property = property.Copy(),
                propertyPath = property.propertyPath,
                widthWeight = attr.widthWeight,
                minWidth = attr.minWidth,
                drawn = false
            });
        }
    }
    
    private bool IsFirstPropertyInGroup(SerializedProperty property, string groupId)
    {
        if (!groupedProperties.ContainsKey(groupId))
            return true;
            
        var group = groupedProperties[groupId];
        return group.First().propertyPath == property.propertyPath;
    }
    
    private void DrawGroup(Rect position, string groupId)
    {
        if (!groupedProperties.ContainsKey(groupId))
            return;
            
        var group = groupedProperties[groupId];
        
        // Calculate total weight and validate properties
        float totalWeight = group.Sum(p => p.widthWeight);
        
        // Begin horizontal group
        EditorGUILayout.BeginHorizontal();
        
        float currentX = position.x;
        float remainingWidth = position.width;
        
        // Draw each property
        for (int i = 0; i < group.Count; i++)
        {
            var prop = group[i];
            
            // Calculate width for this property
            float propWidth = (prop.widthWeight / totalWeight) * position.width;
            if (propWidth < prop.minWidth)
                propWidth = prop.minWidth;
                
            // Adjust remaining width
            if (i == group.Count - 1)
                propWidth = remainingWidth; // Use all remaining width for last property
                
            Rect propRect = new Rect(currentX, position.y, propWidth, position.height);
            
            // Draw the property
            EditorGUI.PropertyField(propRect, prop.property, GUIContent.none, true);
            
            currentX += propWidth;
            remainingWidth -= propWidth;
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Clear the group if all properties have been drawn
        if (Event.current.type == EventType.Repaint)
        {
            groupedProperties.Remove(groupId);
        }
    }
} 