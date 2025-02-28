using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;


/// <summary>
/// Scriptable renderer feature to inject render passes into the renderer
/// This Renderer Feature allows you to draw outlines around 3D objects
/// </summary>
public class LayerOutlineRendererFeature : ScriptableRendererFeature
{

    #region Materials Paths

    /// <summary>
    /// The path to the Outline_Mask Material
    /// </summary>
    private const string OUTLINE_MASK = "Assets/LayerOutline/Materials/Outline_Mask.mat";


    /// <summary>
    /// The path to the Outline_Layer Material
    /// </summary>
    private const string OUTLINE_LAYER = "Assets/LayerOutline/Materials/Outline_Layer.mat";


    /// <summary>
    /// The path to the Outline_Transfer Material
    /// </summary>
    private const string OUTLINE_TRANSFER = "Assets/LayerOutline/Materials/Outline_Transfer.mat";


    /// <summary>
    /// The path to the Outline_Blit Material
    /// </summary>
    private const string OUTLINE_BLIT = "Assets/LayerOutline/Materials/Outline_Blit.mat";

    #endregion


    #region Settings

    /// <summary>
    /// Contains all the settings needed to draw outlines around objects belonging to a specific layer
    /// </summary>
    [Serializable]
    public class LayerSettings
    {
        /// <summary>
        /// Allows you to specify the layer (this Renderer Feature will draw an outline around all objects in this layer) 
        /// </summary>
        [CustomLayerMask]
        public LayerMask layerMask = 0;


        /// <summary>
        /// The outline color for this layer
        /// </summary>
        public UnityEngine.Color color = UnityEngine.Color.white;


        /// <summary>
        /// The thickness of the outline for this layer
        /// </summary>
        public int thickness = 10;
    }


    /// <summary>
    /// Contains all the settings needed to scale outlines based on the distance of objects from the camera
    /// </summary>
    [Serializable]
    public class ScaleSettings
    {
        /// <summary>
        /// The minimum depth at which the outline thickness is at its maximum
        /// </summary>
        public float minDepth = 0.005f;


        /// <summary>
        /// The maximum depth at which the outline thickness is at its minimum
        /// </summary>
        public float maxDepth = 0.01f;


        /// <summary>
        /// The curve that will allow the delinearization of the outlines scaling
        /// </summary>
        public AnimationCurve scaleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }


    /// <summary>
    /// Contains all the settings needed for this Renderer Feature
    /// </summary>
    [Serializable]
    public class Settings
    {
        /// <summary>
        /// Displaying a custom text on Editor GUI
        /// </summary>
        [CustomText("The order of the elements in the Outline Layers array\nbelow determines the rendering order of the outlines", 2)]


        /// <summary>
        /// The list of layers for which this Renderer Feature will draw outlines
        /// </summary>
        public LayerSettings[] outlineLayers;
    }

    #endregion


    #region Attributes

    /// <summary>
    /// The Material used to draw the masks of the objects belonging to the layers listed in the settings
    /// </summary>
    private Material m_Mask = null;


    /// <summary>
    /// The Material used to draw the outlines using the masks described above
    /// </summary>
    private Material m_Layer = null;


    /// <summary>
    /// The Material used to copy the outlines on the active color texture
    /// </summary>
    private Material m_Transfer = null;


    /// <summary>
    /// A Material with a copy shader (used during blits)
    /// </summary>
    private Material m_Blit = null;


    /// <summary>
    /// Instance of a InitLayerMask allowing to create the texture in which the masks will be drawn
    /// InitLayerMask instances also provide access to this texture during other passes
    /// </summary>
    private InitLayerMask m_InitLayerMask = null;


    /// <summary>
    /// The list of passes that will draw the masks (one pass per layer)
    /// </summary>
    private DrawLayerMask[] m_DrawLayerMask = null;


    /// <summary>
    /// Instance of a InitLayerOutline allowing to create the texture in which the outlines will be drawn
    /// InitLayerOutline instances also provide access to this texture during other passes
    /// </summary>
    private InitLayerOutline m_InitLayerOutline = null;


    /// <summary>
    /// The list of passes that will draw the outlines (one pass per layer)
    /// </summary>
    private DrawLayerOutline[] m_DrawLayerOutline = null;


    /// <summary>
    /// Instance of a FinalBlit pass that will blit the outlines on the current active color target texture
    /// </summary>
    private FinalBlit m_FinalBlit = null;

    #endregion


    #region Editor Properties

    /// <summary>
    /// Settings instance to expose settings in the editor
    /// </summary>
    public Settings settings = new Settings();

    #endregion


    #region Functions

    /// <summary>
    /// Loads the Materials needed for this Renderer Feature to work properly
    /// </summary>
    /// <returns>A boolean indicating whether all Materials have been loaded</returns>
    private bool FetchAndSetMaterials()
    {
        /// Loading m_Mask from AssetDatabase
        /// Display an error message if loading failed
        m_Mask = (Material)AssetDatabase.LoadAssetAtPath(OUTLINE_MASK, typeof(Material));
        if (m_Mask == null)
        {
            Debug.LogError("LayerOutlineRendererFeature error : Can't find " + OUTLINE_MASK);
            return false;
        }


        /// Loading m_Layer from AssetDatabase
        /// Display an error message if loading failed
        m_Layer = (Material)AssetDatabase.LoadAssetAtPath(OUTLINE_LAYER, typeof(Material));
        if (m_Layer == null)
        {
            Debug.LogError("OutlineRendereFeature error : Can't find " + OUTLINE_LAYER);
            return false;
        }


        /// Loading m_Transfer from AssetDatabase
        /// Display an error message if loading failed
        m_Transfer = (Material)AssetDatabase.LoadAssetAtPath(OUTLINE_TRANSFER, typeof(Material));
        if (m_Transfer == null)
        {
            Debug.LogError("OutlineRendereFeature error : Can't find " + OUTLINE_TRANSFER);
            return false;
        }


        /// Loading m_Blit from AssetDatabase
        /// Display an error message if loading failed
        m_Blit = (Material)AssetDatabase.LoadAssetAtPath(OUTLINE_BLIT, typeof(Material));
        if (m_Blit == null)
        {
            Debug.LogError("OutlineRendereFeature error : Can't find " + OUTLINE_BLIT);
            return false;
        }


        /// Return true if all materials loaded successfully
        return true;
    }


    /// <summary>
    /// Checks if all passes are initialized before injecting them into the rendering pipeline
    /// </summary>
    /// <returns>A boolean indicating whether all passes have been initialized</returns>
    private bool CheckPasses()
    {
        if (m_InitLayerMask == null || m_DrawLayerMask == null || m_InitLayerOutline == null || m_DrawLayerOutline == null || m_FinalBlit == null)
            return false;
        return true;
    }


    /// <summary>
    /// Check the value of each Layer in the settings
    /// </summary>
    /// <returns>A boolean indicating whether all layers are initialized</returns>
    private bool CheckLayers()
    {
        /// Check failed if outline layers array has not been initialized 
        if (settings.outlineLayers == null)
            return false;


        /// If a layer is equal to zero then the check fails
        for (int i = 0; i < settings.outlineLayers.Length; i++)
        {
            if (settings.outlineLayers[i].layerMask == 0)
            {
                return false;
            }
        }


        /// Check succeed
        return true;
    }


    /// <summary>
    /// Converts a layer mask to a layer number
    /// </summary>
    /// <param name="layerValue">The layer mask</param>
    /// <returns>The layer number (or layer index)</returns>
    private int LayerValueToLayerNumber(int layerValue)
    {
        /// Creating a variable containing the number (or index) of the layer
        int layerNumber = 0;


        /// Successive bit shifts to zero, we increment the layer number at each iteration
        while (layerValue > 0)
        {
            layerValue = layerValue >> 1;
            layerNumber++;
        }


        /// We return the layer number 
        if (layerNumber == 0)
            return layerNumber;
        else
            return layerNumber - 1;
    }


    /// <summary>
    /// Creating the passes that will be used in this Renderer Feature
    /// </summary>
    public override void Create()
    {
        /// Loading materials and checking layers
        if (!FetchAndSetMaterials() || !CheckLayers())
        {
            m_InitLayerMask = null;
            m_DrawLayerMask = null;
            m_InitLayerOutline = null;
            m_DrawLayerOutline = null;
            m_FinalBlit = null;
            return;
        }


        /// Creating all the passes needed by this renderer feature
        #region Creating Passes

        #region Initialization Passes

        /// Instantiating a InitLayerMask pass
        m_InitLayerMask = new InitLayerMask();
        m_InitLayerMask.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;


        /// Instantiating a InitLayerOutline pass
        m_InitLayerOutline = new InitLayerOutline();
        m_InitLayerOutline.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        #endregion


        #region Drawing Passes

        /// Creating DrawLayerMask and DrawLayerOutline pass lists
        m_DrawLayerMask = new DrawLayerMask[settings.outlineLayers.Length];
        m_DrawLayerOutline = new DrawLayerOutline[settings.outlineLayers.Length];


        /// For each layer ...
        for (int i = 0; i < settings.outlineLayers.Length; i++)
        {
            /// ... converting the layer mask to a layer number and avoiding builtin layers
            int layerNumber = LayerValueToLayerNumber(settings.outlineLayers[i].layerMask.value);


            /// ... creating and initializing settings for a DrawLayerMask pass
            DrawLayerMask.DrawLayerMaskSettings drawLayerMaskSettings;
            drawLayerMaskSettings.maskMaterial = m_Mask;
            drawLayerMaskSettings.layerName = LayerMask.LayerToName(layerNumber);
            drawLayerMaskSettings.selectionLayer = settings.outlineLayers[i].layerMask;
            //drawLayerMaskSettings.zTest = false;


            /// ... instantiation of a DrawLayerMask pass
            m_DrawLayerMask[i] = new DrawLayerMask(drawLayerMaskSettings);
            m_DrawLayerMask[i].renderPassEvent = RenderPassEvent.AfterRenderingOpaques;


            /// ... creating and initializing settings for a DrawLayerOutline pass
            DrawLayerOutline.DrawLayerOutlineSettings drawLayerOutlineSettings;
            drawLayerOutlineSettings.blitMaterial = m_Blit;
            drawLayerOutlineSettings.outlineMaterial = m_Layer;
            drawLayerOutlineSettings.outlineThickness = settings.outlineLayers[i].thickness;
            drawLayerOutlineSettings.outlineColor = settings.outlineLayers[i].color;
            drawLayerOutlineSettings.layerName = LayerMask.LayerToName(layerNumber);


            /// ... instantiating a DrawLayerOutline pass
            m_DrawLayerOutline[i] = new DrawLayerOutline(drawLayerOutlineSettings);
            m_DrawLayerOutline[i].renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        #endregion


        #region Final Pass

        /// Creating and initializing settings for an FinalBlit pass
        FinalBlit.FinalBlitSettings finalBlitSettings;
        finalBlitSettings.transferMaterial = m_Transfer;
        finalBlitSettings.blitMaterial = m_Blit;


        /// Instantiating a FinalBlit pass
        m_FinalBlit = new FinalBlit(finalBlitSettings);
        m_FinalBlit.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        #endregion

        #endregion
    }


    /// <summary>
    /// Injects one or multiple ScriptableRenderPass in the renderer
    /// </summary>
    /// <param name="renderer">Renderer used for adding render passes</param>
    /// <param name="renderingData">Rendering state used to setup render passes</param>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        /// Checking that all passes are initialized
        if (CheckPasses())
        {
            /// The RenderPassFeature is active only in Game mode
            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Game)
            {
                /// Injecting initialization render passes into the rendering pipeline
                renderer.EnqueuePass(m_InitLayerMask);
                renderer.EnqueuePass(m_InitLayerOutline);


                /// Injecting render passes into the rendering pipeline
                for (int i = 0; i < m_DrawLayerMask.Length; i++)
                {
                    renderer.EnqueuePass(m_DrawLayerMask[i]);
                    renderer.EnqueuePass(m_DrawLayerOutline[i]);
                }


                /// Injecting final render pass into the rendering pipeline
                renderer.EnqueuePass(m_FinalBlit);
            }
        }
    }


    /// <summary>
    /// Allows you to modify during runtime the outline color of a layer passed as a parameter 
    /// </summary>
    /// <param name="layerName">The layer name</param>
    /// <param name="color">The outline color</param>
    public void SetLayerOutlineColor(string layerName, UnityEngine.Color color)
    {
        /// Exit if the list of passes that will draw the outlines is null or empty 
        if (m_DrawLayerOutline == null || m_DrawLayerOutline.Length == 0)
            return;


        /// Changing the outline color of the affected DrawLayerOutline
        foreach (DrawLayerOutline pass in m_DrawLayerOutline)
        {
            if (pass.LayerName == layerName)
            {
                pass.LayerColor = color;
                return;
            }
        }
    }


    /// <summary>
    /// Allows you to modify during runtime the outline thickness of a layer passed as a parameter 
    /// </summary>
    /// <param name="layerName">The layer name</param>
    /// <param name="thickness">The outline thickness</param>
    public void SetLayerOutlineThickness(string layerName, float thickness)
    {
        /// Exit if the list of passes that will draw the outlines is null or empty 
        if (m_DrawLayerOutline == null || m_DrawLayerOutline.Length == 0)
            return;


        /// Changing the outline thickness of the affected DrawLayerOutline
        foreach (DrawLayerOutline pass in m_DrawLayerOutline)
        {
            if (pass.LayerName == layerName)
            {
                pass.LayerThickness = thickness;
                return;
            }
        }
    }

    #endregion

}