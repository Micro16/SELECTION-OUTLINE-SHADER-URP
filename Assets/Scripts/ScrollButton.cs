using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;


/// <summary>
/// Behavior that defines the logic of the buttons to move the camera forward/backward
/// </summary>
public class ScrollButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{

    /// <summary>
    /// Defines an enumeration to indicate the direction of movement along the Z axis for the button to which this script is attached
    /// </summary>
    public enum Direction { Forward, Backward };


    /// <summary>
    /// The direction variable (see above) to be set in the inspector
    /// </summary>
    public Direction direction;


    /// <summary>
    /// The ScrollMove component of the main camera
    /// </summary>
    private ScrollMove scrollCamera;
    

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        scrollCamera = Camera.main.GetComponent<ScrollMove>();
        if (scrollCamera == null)
            Debug.LogError("ScrollMove component on main camera not found");
    }


    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        
    }


    /// <summary>
    /// Evaluate current state and transition to pressed state
    /// </summary>
    /// <param name="eventData">The EventData usually sent by the EventSystem</param>
    public void OnPointerDown(PointerEventData eventData)
    {
        /// Start moving camera with the right direction
        if (scrollCamera != null)
            scrollCamera.StartMove(direction == Direction.Forward ? 1.0f : -1.0f);
    }


    /// <summary>
    /// Evaluate current state and transition to released state
    /// </summary>
    /// <param name="eventData">The EventData usually sent by the EventSystem</param>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (scrollCamera != null)
            scrollCamera.StopMove();
    }

}