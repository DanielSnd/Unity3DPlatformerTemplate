using UnityEngine;

public class SideBySideAttribute : PropertyAttribute
{
    public readonly string groupId;
    
    // Optional parameters for fine-tuning the layout
    public readonly float widthWeight = 1f;    // Relative width compared to other fields in the group
    public readonly float minWidth = 0f;       // Minimum width in pixels
    
    public SideBySideAttribute(string groupId, float widthWeight = 1f, float minWidth = 0f)
    {
        this.groupId = groupId;
        this.widthWeight = widthWeight;
        this.minWidth = minWidth;
    }
} 