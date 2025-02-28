using UnityEditor;
using UnityEngine;


/// <summary>
/// A custom property drawer to draw a text field (on the editor GUI) in order to display some infos about the LayerOutlineRendererFeature's Settings 
/// </summary>
[CustomPropertyDrawer(typeof(CustomTextAttribute))]
public class CustomTextDrawer : DecoratorDrawer
{

    #region Functions

    /// <summary>
    /// Override this method to make your own IMGUI based GUI for the property
    /// With this function we can create a text field (on the editor GUI) in order to display some infos about the LayerOutlineRendererFeature's Settings 
    /// </summary>
    /// <param name="position">Rectangle on the screen to use for the decorator GUI</param>
    public override void OnGUI(Rect position)
    {
        /// Get the PropertyAttribute the custom GUI is created for
        CustomTextAttribute ct = attribute as CustomTextAttribute;


        /// Drawing a text field (on the editor GUI) in order to display some infos about the LayerOutlineRendererFeature's Settings
        EditorGUI.LabelField(position, new GUIContent(ct.text));
    }


    /// <summary>
    /// Specify how tall the text is in pixels
    /// </summary>
    /// <returns>The height in pixels of the text decorator</returns>
    public override float GetHeight()
    {
        /// Get the PropertyAttribute the custom GUI is created for
        CustomTextAttribute ct = attribute as CustomTextAttribute;


        /// returns the height used for a single Editor control times the number of text lines
        return EditorGUIUtility.singleLineHeight * ct.lines;
    }

    #endregion

}