using UnityEditor;
using UnityEngine;


/// <summary>
/// A custom property drawer to draw a custom editor GUI for the LayerOutlineRendererFeature's LayerSettings
/// </summary>
[CustomPropertyDrawer(typeof(LayerOutlineRendererFeature.LayerSettings))]
public class CustomLayerSettingsDrawer : PropertyDrawer
{

    #region Functions

    /// <summary>
    /// This function updates the position's Rect in order to move to the next line
    /// </summary>
    /// <param name="position">Rectangle on the screen to use for the property GUI</param>
    private void MoveToNextLine(ref Rect position)
    {
        position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    }


    /// <summary>
    /// Override this method to make your own IMGUI based GUI for the property
    /// With this function we can draw a custom editor GUI for the LayerOutlineRendererFeature's LayerSettings
    /// </summary>
    /// <param name="position">Rectangle on the screen to use for the property GUI</param>
    /// <param name="property">The SerializedProperty to make the custom GUI for</param>
    /// <param name="label">The label of this property</param>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        /// Start drawing LayerSettings
        EditorGUI.BeginProperty(position, GUIContent.none, property);


        /// Set up height to a single Editor control height as the property can be expanded or not
        /// This is mandatory in order to display everything correctly when the property is expanded 
        position.height = EditorGUIUtility.singleLineHeight;


        /// Draw a label as the first line of LayerSettings and check if the property has children and is expanded 
        if (EditorGUI.PropertyField(position, property, new GUIContent("Outline Layer " + property.displayName.Split(' ')[1])))
        {
            /// Move to next line and draw LayerMask property field
            MoveToNextLine(ref position);
            EditorGUI.PropertyField(position, property.FindPropertyRelative("layerMask"));


            /// Move to next line and draw outline color property field
            MoveToNextLine(ref position);
            EditorGUI.PropertyField(position, property.FindPropertyRelative("color"));


            /// Move to next line and draw outline thickness property field
            MoveToNextLine(ref position);
            EditorGUI.PropertyField(position, property.FindPropertyRelative("thickness"));

            /*MoveToNextLine(ref position);
            EditorGUI.PropertyField(position, property.FindPropertyRelative("zTest"));

            MoveToNextLine(ref position);
            EditorGUI.PropertyField(position, property.FindPropertyRelative("scale"));

            if (property.FindPropertyRelative("scale").boolValue)
            {
                MoveToNextLine(ref position);
                EditorGUI.PropertyField(position, property.FindPropertyRelative("scalingCurve"));

                MoveToNextLine(ref position);
                EditorGUI.PropertyField(position, property.FindPropertyRelative("minimumThickness"));
            }*/
        }


        /// End drawing LayerSettings
        EditorGUI.EndProperty();
    }


    /// <summary>
    /// Specify how tall the LayerSettings property is in pixels
    /// </summary>
    /// <param name="property">The SerializedProperty to make the custom GUI for</param>
    /// <param name="label">The label of this property</param>
    /// <returns>The height in pixels of the LayerSettings</returns>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        /*if (!property.FindPropertyRelative("scale").boolValue && property.isExpanded)
        {
            SerializedProperty scale = property.FindPropertyRelative("scale");
            int count = 0;

            while (scale.NextVisible(false))
            {
                if (scale.depth == 3)
                {
                    count++;
                }
            }

            return EditorGUI.GetPropertyHeight(property) - count * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }
        else
        {*/
            return EditorGUI.GetPropertyHeight(property);
        /*}*/
    }

    #endregion

}