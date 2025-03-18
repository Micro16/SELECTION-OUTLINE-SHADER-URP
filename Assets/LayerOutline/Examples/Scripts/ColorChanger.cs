using UnityEngine;


/// <summary>
/// This class allows you to control the LayerOutlineRendererFeature via LayerOutlineFeatureAccessor in order 
/// to modify the outlines colors depending on time elapsed
/// </summary>
public class ColorChanger : MonoBehaviour
{

    #region Editor Properties

    /// <summary>
    /// The first color value for the "Monkey" layer
    /// </summary>
    public Color monkeyStart;


    /// <summary>
    /// The second color value for the "Monkey" layer
    /// </summary>
    public Color monkeyEnd;


    /// <summary>
    /// The first color value for the "Plane" layer
    /// </summary>
    public Color planeStart;


    /// <summary>
    /// The second color value for the "Plane" layer
    /// </summary>
    public Color planeEnd;


    /// <summary>
    /// The time taken to change from one color to another and vice versa
    /// </summary>
    public float lerpTime = 2.0f;

    #endregion


    #region Attributes

    /// <summary>
    /// The time elapsed since the lerp starting
    /// </summary>
    private float curentTime;

    #endregion


    #region Functions

    /// <summary>
    /// Swap two colors values
    /// </summary>
    /// <param name="a">The first color value</param>
    /// <param name="b">The second color value</param>
    void SwapColors(ref Color a, ref Color b)
    {
        Color temp = a;
        a = b;
        b = temp;
    }

    #endregion


    #region GameObject Events

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        /// Initialize LayerOutlineFeatureAccessor
        LayerOutlineFeatureAccessor.Instance.Initialize(Camera.main);


        /// Starting the lerp at the same time as the Behaviour 
        curentTime = 0.0f;
    }


    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        /// Updating current time and swap colors if needed
        curentTime += Time.deltaTime;
        if (curentTime >= lerpTime)
        {
            curentTime = 0.0f;
            SwapColors(ref monkeyStart, ref monkeyEnd);
            SwapColors(ref planeStart, ref planeEnd);
        }


        /// Normalize time
        float t = curentTime / lerpTime;


        /// Colors lerp
        Color monkey = Color.Lerp(monkeyStart, monkeyEnd, t);
        Color plane = Color.Lerp(planeStart, planeEnd, t);


        /// Update outlines colors via LayerOutlineFeatureAccessor
        LayerOutlineFeatureAccessor.Instance.SetLayerOutlineColor("Monkey", monkey);
        LayerOutlineFeatureAccessor.Instance.SetLayerOutlineColor("Plane", plane);
    }

    #endregion

}