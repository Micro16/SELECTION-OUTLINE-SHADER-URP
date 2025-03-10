using UnityEngine;


/// <summary>
/// Attach this Behaviour to any GameObject that has to be outlined after a mouse click on his collider
/// </summary>
public class Selectable : MonoBehaviour
{

    #region Editor Properties

    /// <summary>
    /// This object has to have this LayerMask to be outlined 
    /// </summary>
    public LayerMask outlineMask;

    #endregion


    #region Attributes

    /// <summary>
    /// The default layer (when this object is not outlined)
    /// </summary>
    private int m_DefaultLayer;
    

    /// <summary>
    /// This object must belong to this layer to be outlined
    /// </summary>
    private int m_OutlineLayer;

    #endregion


    #region Functions

    /// <summary>
    /// Change recursively the layer of a specified Transform and all his children
    /// </summary>
    /// <param name="root">The root transform of the GameObject</param>
    /// <param name="layer">The layer we want the object belong to</param>
    void ChangeLayerRecursively(Transform root, int layer)
    {
        /// Change layer...
        root.gameObject.layer = layer;
        

        /// ... and repeat recursively for all childrens
        foreach (Transform child in root)
        {
            ChangeLayerRecursively(child, layer);
        }
    }

    #endregion


    #region GameObject Events

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        /// Set default mask to zero
        m_DefaultLayer = 0;


        /// If the selected mask is different from the default one
        if (outlineMask > 0)
        {
            /// Successive bit shifts to zero, we increment the layer number at each iteration
            while (outlineMask > 1)
            {
                outlineMask = outlineMask >> 1;
                m_OutlineLayer++;
            }
        }
    }


    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        
    }


    /// <summary>
    /// Called when the user has pressed the mouse button while over the Collider
    /// </summary>
    private void OnMouseDown()
    {
        /// Change the layer based on its current value
        if (gameObject.layer == m_DefaultLayer)
        {
            ChangeLayerRecursively(transform, m_OutlineLayer);
        }
        else if (gameObject.layer == m_OutlineLayer)
        {
            ChangeLayerRecursively(transform, m_DefaultLayer);
        }
    }

    #endregion

}