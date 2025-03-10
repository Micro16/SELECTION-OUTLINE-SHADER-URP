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
    /// The multiplier applied to the mouse scroll value to adjust the movement along the Z axis
    /// </summary>
    public float mouseScrollMultiplier;


    /// <summary>
    /// The speed of movement along the Z axis when a UI button is pressed
    /// </summary>
    public float uiButtonSpeed;


    /// <summary>
    /// The direction of movement given by the UI buttons
    /// </summary>
    private float direction;
    
    
    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        Vector3 position = transform.position;
        position.z = zStart;
        direction = 0.0f;
    }


    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        /// Move GameObject along Z axis according to mouse wheel input and UI buttons
        Vector3 position = transform.position;
        if (direction == 0.0f)
            position.z += Input.mouseScrollDelta.y * mouseScrollMultiplier;
        else
            position.z += direction * uiButtonSpeed * Time.deltaTime;
        position.z = Mathf.Clamp(position.z, zMin, zMax);
        transform.position = position;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="dir"></param>
    public void StartMove(float dir)
    {
        direction = dir;
    }


    /// <summary>
    /// 
    /// </summary>
    public void StopMove()
    {
        direction = 0.0f;
    }

}