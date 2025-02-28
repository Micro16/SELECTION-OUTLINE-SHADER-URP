using UnityEditor;
using UnityEngine;


/// <summary>
/// A custom property drawer to draw a dropdown list (on the editor GUI) that will contain only user layers and an option to select no layers
/// </summary>
[CustomPropertyDrawer(typeof(CustomLayerMaskAttribute))]
public class CustomLayerMaskDrawer : PropertyDrawer
{

    #region Functions

    /// <summary>
    /// Override this method to make your own IMGUI based GUI for the property
    /// With this function we can create a dropdown list (on the editor GUI) that will contain only user layers and an option to select no layers
    /// </summary>
    /// <param name="position">Rectangle on the screen to use for the property GUI</param>
    /// <param name="property">The SerializedProperty to make the custom GUI for</param>
    /// <param name="label">The label of this property</param>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        /// Get the PropertyAttribute the custom GUI is created for
        CustomLayerMaskAttribute clm = attribute as CustomLayerMaskAttribute;


        /// We check that the property corresponds to a LayerMask
        if (property.propertyType == SerializedPropertyType.LayerMask)
        {
            /// If the property value is not defined in the PropertyAttribute then it is assigned a default value
            if (!clm.masks.Exists(x => x.Equals(property.intValue)))
                property.intValue = 0;


            /// Drawing a dropdown list (on the editor GUI) that will contain only user layers and an option to select no layers
            property.intValue = EditorGUI.IntPopup(position,"Layer", property.intValue, clm.names.ToArray(), clm.masks.ToArray());
        }
        else
        {
            /// Drawing a warning message if the property does not corresponds to a LayerMask 
            EditorGUI.LabelField(position, "Use CustomLayerMask with LayerMask type.");
        }
    }

    #endregion

}