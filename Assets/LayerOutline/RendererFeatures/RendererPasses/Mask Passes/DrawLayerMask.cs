using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;


/// <summary>
/// Implements a logical rendering pass which renders (with an overrided material) the masks for each layer to a render graph texture
/// </summary>
public class DrawLayerMask : ScriptableRenderPass
{

    #region Settings

    /// <summary>
    /// A structure which will contains all the settings required for the DrawLayerMask render pass
    /// </summary>
    public struct DrawLayerMaskSettings
    {
        /// <summary>
        /// The material with the shader which draws masks
        /// </summary>
        public Material maskMaterial;


        /// <summary>
        /// The name of the layer concerned by this render pass
        /// This name will appears in Frame Debugger, Profiler & Render Graph Viewer for this render pass
        /// </summary>
        public string layerName;


        /// <summary>
        /// The layer to which the objects to be drawn belong
        /// </summary>
        public LayerMask selectionLayer;


        /// <summary>
        /// Indicates whether Z test should occurs during the render pass
        /// </summary>
        //public bool zTest;
    }

    #endregion


    #region Attributes

    /// <summary>
    /// The material with the shader used to draw the masks
    /// This material will serve as a template to build m_CustomMaterial
    /// </summary>
    private Material m_MaskMaterial;


    /// <summary>
    /// A material with a layer-specific property (ZTest) and created dynamically from m_MaskMaterial
    /// This material will serve as the overrided material for this render pass to draw the masks
    /// </summary>
    private Material m_CustomMaterial;


    /// <summary>
    /// The name of the layer concerned by this render pass
    /// This name will appears in Frame Debugger, Profiler & Render Graph Viewer for this render pass
    /// </summary>
    private string m_LayerName;


    /// <summary>
    /// A struct that represents filtering settings used to initialize the renderer list
    /// </summary>
    private FilteringSettings m_FilteringSettings;


    /// <summary>
    /// A list of shader tags used to target objects that have to be drawn and initialize the renderer list
    /// </summary>
    private List<ShaderTagId> m_ShaderTagIdList;


    /// <summary>
    /// Indicates whether ZTest occurs when rendering masks
    /// </summary>
    //private bool m_ZTest;

    #endregion


    #region PassData

    /// <summary>
    /// This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
    /// </summary>
    private class PassData
    {
        /// <summary>
        /// Reference to the list of all objects that will need to be rendered during this pass
        /// </summary>
        public RendererListHandle listHandle;
    }

    #endregion


    #region Constructor

    /// <summary>
    /// The class constructor
    /// </summary>
    /// <param name="settings">A structure which will contain all the settings required for the DrawLayerMask render pass</param>
    public DrawLayerMask(DrawLayerMaskSettings settings)
    {
        /// On conserve une référence vers le Material 
        m_MaskMaterial = settings.maskMaterial;
        
        
        /// Initializing the custom material used to draw objects
        m_CustomMaterial = null;


        /// Initializing the name which will appears in Frame Debugger, Profiler & Render Graph Viewer for this render pass
        m_LayerName = settings.layerName;


        /// Initializing filtering settings with the layer to which the objects to be drawn belong
        m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, settings.selectionLayer);


        /// Initializing shader tags list
        m_ShaderTagIdList = new List<ShaderTagId>();


        /// Use URP's default shader tags to target most objects
        m_ShaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
        m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
        m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));


        /// Initializing the property which indicates whether ZTest occurs when rendering objects
        //m_ZTest = settings.zTest;
    }

    #endregion


    #region Functions

    /// <summary>
    /// This is where the renderGraph handle can be accessed
    /// Each ScriptableRenderPass can use the RenderGraph handle to add multiple render passes to the render graph
    /// </summary>
    /// <param name="renderGraph">The RenderGraph handle</param>
    /// <param name="frameData">Contains all data relating to the rendering of the frame</param>
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        /// Retrieving settings related to renderer, texture resources, camera and lighting
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();


        /// Create drawing settings from shader tags list, renderer settings, camera settings and lighting settings
        DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData, cameraData, lightData, cameraData.defaultOpaqueSortFlags);


        /// The materials are destroyed while loading another scene so we must initialize m_CustomMaterial again
        if (m_CustomMaterial == null)
            m_CustomMaterial = new Material(m_MaskMaterial);


        /// Enabling or disabling ZTest on the custom material for masks rendering
        //m_CustomMaterial.SetFloat("_ZTest", m_ZTest ? (float)CompareFunction.LessEqual : (float)CompareFunction.Always);


        /// Use m_MaskMaterial to draw masks
        drawSettings.overrideMaterial = m_MaskMaterial; /*m_CustomMaterial;*/


        /// Create renderer list parameters from cull results, drawing settings and filtering settings
        /// Filtering settings specify that we will only render objects which belong to the selected layer 
        RendererListParams param = new RendererListParams(renderingData.cullResults, drawSettings, m_FilteringSettings);


        /// Fetch texture created during the InitLayerMask render pass
        InitLayerMask.MaskData masksData = frameData.Get<InitLayerMask.MaskData>();
        TextureHandle renderTargetHandle = masksData.maskHandle;


        /// Add a raster render pass to the render graph to render masks
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Drawing Layer Mask : " + m_LayerName, out var passData))
        {
            /// Setting data needed by the pass : 
            /// - Setting the renderer list handle to a renderer list created with the above RendererListParams 
            passData.listHandle = renderGraph.CreateRendererList(param);


            /// Check the renderer list handle validity
            if (!passData.listHandle.IsValid())
                return;


            /// We declare the RendererList we just created as an input dependency to this pass
            builder.UseRendererList(passData.listHandle);


            /// Set the texture created during the InitLayerMask render pass as the render target
            builder.SetRenderAttachment(renderTargetHandle, 0, AccessFlags.Write);


            /// Attaching depth datas so ZTest can occurs
            builder.SetRenderAttachmentDepth(resourceData.cameraDepth);


            /// Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
        }
    }


    /// <summary>
    /// This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
    /// </summary>
    /// <param name="data">A structure that stores the data needed by the pass</param>
    /// <param name="context">A structure that permits to access the Command Buffer used for rendering</param>
    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        /// Clearing the render target 
        context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear, 1, 0);


        /// Drawing masks based on the RendererList passed as a parameter
        context.cmd.DrawRendererList(data.listHandle);
    }

    #endregion

}