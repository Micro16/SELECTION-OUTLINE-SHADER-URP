using UnityEngine;


/// <summary>
/// Attach this Behaviour to any GameObject that has to be outlined after a mouse click on his collider
/// </summary>
public class Selectable : MonoBehaviour
{

    #region Editor 

    public LayerMask outlineMask;

    #endregion


    #region Attributes

    private LayerMask m_DefaultMask;
    private LayerMask m_OutlineMask;

    #endregion


    #region Unity Events

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        m_DefaultMask = 0;
        m_OutlineMask = outlineMask;
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
        if (gameObject.layer == m_DefaultMask)
        {
            gameObject.layer = m_OutlineMask;
        }
        else if (gameObject.layer == m_OutlineMask)
        {
            gameObject.layer = m_DefaultMask;
        }
    }

    #endregion

}