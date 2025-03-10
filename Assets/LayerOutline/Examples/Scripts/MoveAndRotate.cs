using UnityEngine;


/// <summary>
/// Attach this Behaviour to a GameObject to move it back and forth along the Y axis and rotate it around Y axis
/// </summary>
public class MoveAndRotate : MonoBehaviour
{

    #region Editor Properties

    /// <summary>
    /// The speed at which the object rotates
    /// </summary>
    public float rotateSpeed;


    /// <summary>
    /// The amplitude of the movement
    /// </summary>
    public float movementAmplitude;

    #endregion


    #region GameObject Events 

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
        /// Move and ...
        float y =  Mathf.Sin(Time.realtimeSinceStartup) * movementAmplitude;
        Vector3 position = transform.position;
        position.y = y;
        transform.position = position;

        
        /// ... rotate
        transform.rotation = Quaternion.AngleAxis(rotateSpeed * Time.deltaTime, Vector3.up) * transform.rotation;
    }

    #endregion

}