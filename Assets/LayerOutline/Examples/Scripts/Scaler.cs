using UnityEngine;


/// <summary>
/// This class allows you to control the LayerOutlineRendererFeature via LayerOutlineFeatureAccessor in order 
/// to modify the outline thickness depending on the distance from the camera
/// WARNING: Modifies the outline thickness for ALL objects in this layer
/// </summary>
[ExecuteAlways]
public class Scaler : MonoBehaviour
{

    #region Editor Properties

    /// <summary>
    /// The curve allowing to delinearize the calculation of the outline thickness according to the distance from the camera
    /// </summary>
    public AnimationCurve smooth;


    /// <summary>
    /// Minimum distance from the camera: the distance at which the outline thickness will be at its maximum
    /// </summary>
    public float minDistance = 5.0f;


    /// <summary>
    /// Maximum distance from the camera: the distance at which the outline thickness will be at its minimum
    /// </summary>
    public float maxDistance = 100.0f;


    /// <summary>
    /// Minimum outline thickness
    /// </summary>
    public float minThickness;


    /// <summary>
    /// Maximum outline thickness
    /// </summary>
    public float maxThickness;

    #endregion


    #region GameObject Events

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        /// Initializing LayerOutlineFeatureAccessor
        if (Application.isPlaying)
        {
            LayerOutlineFeatureAccessor.Instance.Initialize(Camera.main);
        }
    }


    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        /// Do nothing if the delinearization curve is not defined by the user
        if (smooth == null)
            return;


        /// Calculating outline thickness based on distance from camera
        float m = Vector3.Magnitude(Camera.main.transform.position - transform.position);
        float n = Mathf.InverseLerp(minDistance, maxDistance, m);
        float s = smooth.Evaluate(n);
        float t = Mathf.Lerp(minThickness, maxThickness, 1.0f - s);


        /// Update outline thickness using LayerOutlineFeatureAccessor
        if (Application.isPlaying)
        {
            LayerOutlineFeatureAccessor.Instance.SetLayerOutlineThickness("Monkey", t);
        }
        else if (Application.isEditor)
        {
            LayerOutlineFeatureAccessor.EditorInstance(Camera.main).SetLayerOutlineThickness("Monkey", t);
        }
    }

    #endregion

}