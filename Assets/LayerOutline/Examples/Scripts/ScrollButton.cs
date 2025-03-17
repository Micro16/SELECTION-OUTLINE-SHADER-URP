using UnityEngine;
using UnityEngine.EventSystems;


/// <summary>
/// Attach this Behaviour to a UI Button to move the main Camera (with an attached ScrollMove Behaviour) along the Z axis 
/// </summary>
public class ScrollButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{

    #region Editor Properties

    /// <summary>
    /// Defining an enum for the camera's movement directions
    /// </summary>
    public enum Direction { Forward, Backward };


    /// <summary>
    /// The direction the Camera will move when pressing this button 
    /// </summary>
    public Direction direction;

    #endregion


    #region Attributes

    /// <summary>
    /// The ScrollMove Behavior attached to the Main Camera
    /// </summary>
    private ScrollMove scrollCamera;

    #endregion


    #region Functions

    /// <summary>
    /// Called when the button is pressed
    /// </summary>
    /// <param name="eventData">The EventData usually sent by the EventSystem</param>
    public void OnPointerDown(PointerEventData eventData)
    {
        /// We start the movement in the direction defined for this button
        if (scrollCamera != null)
            scrollCamera.StartMove(direction == Direction.Forward ? 1.0f : -1.0f);
    }


    /// <summary>
    /// Called when the button is released
    /// </summary>
    /// <param name="eventData">The EventData usually sent by the EventSystem</param>
    public void OnPointerUp(PointerEventData eventData)
    {
        /// We stop the movement 
        if (scrollCamera != null)
            scrollCamera.StopMove();
    }

    #endregion


    #region GameObject Events

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        /// Get the ScrollMove Behavior attached to the main camera
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

    #endregion

}