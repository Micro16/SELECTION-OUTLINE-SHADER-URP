using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


/// <summary>
/// Attach this Behaviour to a GameObject to move it back and forth along the Y axis and rotate it around Y axis
/// </summary>
public class MoveAndRotate : MonoBehaviour
{

    /// <summary>
    /// The speed at which the object rotates
    /// </summary>
    public float rotateSpeed;


    /// <summary>
    /// The amplitude of the movement
    /// </summary>
    public float movementAmplitude;
    
    
    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        /// Zero rotation at start-up
        transform.rotation = Quaternion.identity;
    }


    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        /// Move and ...
        float y =  Mathf.Sin(Time.realtimeSinceStartup) * movementAmplitude;
        Vector3 position = transform.position;
        position.y = y;
        transform.position = position;

        
        /// ... rotate
        transform.rotation = Quaternion.Euler(0.0f, rotateSpeed * Time.deltaTime, 0.0f) * transform.rotation;
    }

}