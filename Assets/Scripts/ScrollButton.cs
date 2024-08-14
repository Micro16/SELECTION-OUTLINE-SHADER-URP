using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScrollButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{

    /// <summary>
    /// 
    /// </summary>
    public enum Direction { Forward, Backward };


    /// <summary>
    /// 
    /// </summary>
    public Direction direction;


    /// <summary>
    /// 
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
    /// 
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(PointerEventData eventData)
    {
        /// Start moving camera with the 
        if (scrollCamera != null)
            scrollCamera.StartMove(direction == Direction.Forward ? 1.0f : -1.0f);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (scrollCamera != null)
            scrollCamera.StopMove();
    }

}