using UnityEngine;


/// <summary>
/// A custom property attribute to display some infos on LayerOutlineRendererFeature's Settings
/// </summary>
public class CustomTextAttribute : PropertyAttribute
{

    #region Attributes

    /// <summary>
    /// The text which will be displayed on editor GUI
    /// </summary>
    public string text;
    
    
    /// <summary>
    /// The number of text lines
    /// </summary>
    public int lines;

    #endregion


    #region Constructor

    /// <summary>
    /// The class constructor
    /// </summary>
    /// <param name="content">The text which will be displayed on editor GUI</param>
    /// <param name="linesCount">The number of text lines</param>
    public CustomTextAttribute(string content, int linesCount)
    {
        /// Assigning the text and its number of lines
        text = content;
        lines = linesCount;
    }

    #endregion

}