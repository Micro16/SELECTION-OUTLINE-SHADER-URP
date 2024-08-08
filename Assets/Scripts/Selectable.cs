using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Attach this Behaviour to any GameObject that has to be outlined after a mouse click on his collider
/// </summary>
public class Selectable : MonoBehaviour
{
    
    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        
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
        if (gameObject.layer == LayerMask.NameToLayer("Default"))
        {
            gameObject.layer = LayerMask.NameToLayer("Outline");
        }
        else if (gameObject.layer == LayerMask.NameToLayer("Outline"))
        {
            gameObject.layer = LayerMask.NameToLayer("Default");
        }
    }

}