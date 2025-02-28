using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


/// <summary>
/// This class allows access to an OutlineDrawingRendererFeature via scripts
/// </summary>
[ExecuteAlways]
public class LayerOutlineFeatureAccessor : MonoBehaviour
{

    #region Singleton

    /// <summary>
    /// Singleton instance value
    /// </summary>
    private static LayerOutlineFeatureAccessor instance = null;


    /// <summary>
    /// Accessor to the singleton instance
    /// </summary>
    public static LayerOutlineFeatureAccessor Instance { get { return instance; } }


    /// <summary>
    /// Accessor to the singleton instance to be used in Editor Mode only
    /// </summary>
    /// <param name="camera">The camera used to render the scene with the outlined objects - 
    /// This parameter is required to call the Initialize function if needed</param>
    /// <returns></returns>
    public static LayerOutlineFeatureAccessor EditorInstance(Camera camera)
    {
        /// If the instance is null we initialize it with the GameObject present in the hierarchy
        if (instance == null)
        {
            /// Fetch an instance of OutlineFeatureAccessor in the hierarchy
            instance = GameObject.FindFirstObjectByType<LayerOutlineFeatureAccessor>();


            /// If the instance is still null it means that there is no OutlineFeatureAccessor in the hierarchy
            if (instance == null)
                Debug.LogError("There is no instance of OutlineFeatureAccessor in the hierarchy");
        }


        /// Initialize m_OutlineDrawingRendererFeature if needed
        if (instance.m_LayerOutlineRendererFeature == null)
        {
            instance.Initialize(camera);
        }


        /// We return the singleton instance
        return instance;
    }

    #endregion


    #region Attributes

    /// <summary>
    /// A reference to the instance of OutlineDrawingRendererFeature
    /// </summary>
    private LayerOutlineRendererFeature m_LayerOutlineRendererFeature = null;

    #endregion


    #region Functions

    /// <summary>
    /// Function to fetch the instance of OutlineDrawingRendererFeature from the currently used UniversalRenderPipelineAsset
    /// </summary>
    /// <param name="camera">The camera used to render the scene with the outlined objects</param>
    public void Initialize(Camera camera)
    {
        /// We retrieve the ScriptableRenderer attached to the camera
        ScriptableRenderer selectedRenderer = camera.GetUniversalAdditionalCameraData().scriptableRenderer;


        /// We retrieve the currently used UniversalRenderPipelineAsset from the GraphicsSettings
        UniversalRenderPipelineAsset currentRenderPipelineAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;


        /// Searching for the ScriptableRenderer in the list of renderers used by the pipeline asset
        for (int i = 0; i < currentRenderPipelineAsset.renderers.Length; i++)
        {
            if (currentRenderPipelineAsset.renderers[i] == selectedRenderer)
            {
                /// Retrieving the OutlineDrawingRendererFeature from the list of ScriptableRendererData with the index of the ScriptableRenderer attached to the camera
                m_LayerOutlineRendererFeature = currentRenderPipelineAsset.rendererDataList[i].rendererFeatures.Where(x => x is LayerOutlineRendererFeature).Select(x => x as LayerOutlineRendererFeature).FirstOrDefault();
                return;
            }
        }


        /// Display a warning if no Renderer Feature of type OutlineDrawingRendererFeature has been added to the current Renderer
        Debug.LogWarning("There is no OutlineDrawingRendererFeature attached to the current Renderer. Please add one OutlineDrawingRendererFeature to the current Renderer.");
    }


    /// <summary>
    /// Function to modify the color of the outline for a given layer
    /// </summary>
    /// <param name="layerName">The name of the layer</param>
    /// <param name="color">The color of the outline</param>
    public void SetLayerOutlineColor(string layerName, Color color)
    {
        /// Changing the color of the outline for the layer passed as a parameter
        if (m_LayerOutlineRendererFeature != null)
        {
            m_LayerOutlineRendererFeature.SetLayerOutlineColor(layerName, color);
        }
    }


    /// <summary>
    /// Function to modify the thickness of the outline for a given layer
    /// </summary>
    /// <param name="layerName">The name of the layer</param>
    /// <param name="thickness">The thickness of the outline</param>
    public void SetLayerOutlineThickness(string layerName, float thickness)
    {
        /// Changing the thickness of the outline for the layer passed as a parameter
        if (m_LayerOutlineRendererFeature != null)
        {
            m_LayerOutlineRendererFeature.SetLayerOutlineThickness(layerName, thickness);
        }
    }

    #endregion


    #region GameObject Events

    /// <summary>
    /// Awake is called when an enabled script instance is being loaded
    /// </summary>
    private void Awake()
    {
        /// If the static instance is already initialized and is different from this GameObject 
        if (instance != null && instance != this)
        {
            /// Then we have to destroy this GameObject as we need only one instance of OutlineFeatureAccessor
            Destroy(this.gameObject);
        }


        /// If the static instance is not initialized then we attribute this GameObject to it
        instance = this;
    }


    /// <summary>
    /// Start is called once before the first execution of Update after the MonoBehaviour is created
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

    #endregion

}