using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// A custom property attribute for the LayerMask attribute of the LayerOutlineRendererFeature's LayerSettings
/// </summary>
public class CustomLayerMaskAttribute : PropertyAttribute
{

    #region Attributes

    /// <summary>
    /// The list of user layer masks  
    /// </summary>
    public List<int> masks;


    /// <summary>
    /// The list of user layer names
    /// </summary>
    public List<string> names;

    #endregion


    #region Constructor

    /// <summary>
    /// The class constructor
    /// </summary>
    public CustomLayerMaskAttribute()
    {
        /// Creating masks and names lists
        masks = new List<int>();
        names = new List<string>();

        /// We add a mask and a name to indicate that no layer is selected
        masks.Add(0);
        names.Add("Nothing");


        /// For each of the user layers
        for (int i = 6; i < 32;  i++)
        {
            /// If the layer is named in the list then we add its mask and its name to the relevant lists
            string layerName = LayerMask.LayerToName(i);
            if (layerName.Length > 0)
            {
                masks.Add(1 << i);
                names.Add(layerName);
            }
        }
    }

    #endregion

}