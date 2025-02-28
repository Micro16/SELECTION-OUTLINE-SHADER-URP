using UnityEngine;

public class CustomLabelIntAttribute : PropertyAttribute
{
    string labelText;
    int linesCount;
    
    public CustomLabelIntAttribute(string text, int lines)
    {
        labelText = text;
        linesCount = lines;
    }
}
