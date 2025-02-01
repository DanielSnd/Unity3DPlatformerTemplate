using UnityEngine;

public class ConditionalAttribute : PropertyAttribute
{
    public readonly string conditionalProperty;
    public readonly object compareValue;

    public ConditionalAttribute(string conditionalProperty, object compareValue = null)
    {
        this.conditionalProperty = conditionalProperty;
        this.compareValue = compareValue;
    }
} 