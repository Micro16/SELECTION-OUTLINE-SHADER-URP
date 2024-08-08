using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Attach this Behaviour to a GameObject to move it along the Z axis using the mouse wheel
/// </summary>
public class ScrollMove : MonoBehaviour
{
    
    /// <summary>
    /// Z coordinate at which the GameObject starts
    /// </summary>
    public float zStart;
    

    /// <summary>
    /// Minimum Z coordinate
    /// </summary>
    public float zMin;
    

    /// <summary>
    /// Maximum Z coordinate 
    /// </summary>
    public float zMax;


    /// <summary>
    /// Speed ​​along the Z axis
    /// </summary>
    public float speed;
    
    
    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        Vector3 position = transform.position;
        position.z = zStart;
    }


    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        /// Move GameObject along Z axis according to mouse wheel input
        Vector3 position = transform.position;
        position.z += Input.mouseScrollDelta.y * speed;
        position.z = Mathf.Clamp(position.z, zMin, zMax);
        transform.position = position;
    }

}