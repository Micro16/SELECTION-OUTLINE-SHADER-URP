using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.VisualScripting;


/// <summary>
/// Implements a logical rendering pass which renders objects with an overrided material to a render graph texture
/// </summary>
class CustomMaterialRenderPass : ScriptableRenderPass
{

    #region Settings

    /// <summary>
    /// A structure which will contains all the settings required for the CustomMaterialRenderPass
    /// </summary>
    public struct CustomMaterialRenderPassSettings
    {

        /// <summary>
        /// The custom material used to draw objects
        /// </summary>
        public Material customMaterial;


        /// <summary>
        /// The name (or ID) of the target render graph texture in which the objects will be drawn
        /// </summary>
        public string targetTextureID;


        /// <summary>
        /// The format of the target rendering texture, this parameter will impact the size of the texture in memory
        /// </summary>
        public RenderTextureFormat targetTextureFormat;


        /// <summary>
        /// The name which will appears in Frame Debugger, Profiler & Render Graph Viewer for this render pass
        /// </summary>
        public string profilingName;


        /// <summary>
        /// The first layer to which the objects to be drawn belong
        /// </summary>
        public LayerMask selectionLayer;


        /// <summary>
        /// Indicates whether Z test should occurs during the render pass
        /// </summary>
        public bool zTest;


        public Texture2D scaleTex;

    }

    #endregion


    #region Attributes

    /// <summary>
    /// The custom material used to draw objects
    /// </summary>
    private Material m_CustomMaterial;

    private Material m_Material1;
    private Material m_Material2;


    /// <summary>
    /// The name (or ID) of the target render graph texture in which the objects will be drawn
    /// </summary>
    private string m_TargetTextureID;


    /// <summary>
    /// The format of the target rendering texture, this attribute will impact the size of the texture in memory
    /// </summary>
    private RenderTextureFormat m_TargetTextureFormat;


    /// <summary>
    /// The name which will appears in Frame Debugger, Profiler & Render Graph Viewer for this render pass
    /// </summary>
    private string m_ProfilingName;


    /// <summary>
    /// A struct that represents filtering settings used to initialize the renderer list
    /// </summary>
    private FilteringSettings m_FilteringSettings;


    ///// <summary>
    ///// A struct that represents filtering settings used to initialize the renderer list
    ///// </summary>
    //private FilteringSettings m_FilteringSettings2;


    /// <summary>
    /// A list of shader tags used to target objects that have to be drawn and initialize the renderer list
    /// </summary>
    private List<ShaderTagId> m_ShaderTagIdList;


    /// <summary>
    /// Indicates whether ZTest occurs when rendering objects
    /// </summary>
    private bool m_ZTest;


    private Texture2D m_ScaleTexture;

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


        ///// <summary>
        ///// Reference to destination texture
        ///// </summary>
        //public TextureHandle destination;


        ///// <summary>
        ///// Reference to the material used to draw objects
        ///// </summary>
        //public Material material;


        ///// <summary>
        ///// Color to be used to draw objects
        ///// </summary>
        //public Color color;

    }

    #endregion


    #region Constructor

    /// <summary>
    /// The class constructor
    /// </summary>
    /// <param name="settings">A structure which will contain all the settings required for the ScaleRenderPass</param>
    public CustomMaterialRenderPass(CustomMaterialRenderPassSettings settings)
    {
        /// Initializing the custom material used to draw objects
        m_CustomMaterial = settings.customMaterial;


        //m_Material1 = new Material(m_CustomMaterial);
        //m_Material1.SetColor("_Color", Color.red);

        //m_Material2 = new Material(m_CustomMaterial);
        //m_Material2.SetColor("_Color", Color.blue);

        /// Initializing the name of the target render texture in which the selected objects will be drawn
        m_TargetTextureID = settings.targetTextureID;


        /// Initializing the format of the target rendering texture, this attribute will impact the size of the texture in memory
        m_TargetTextureFormat = settings.targetTextureFormat;


        /// Initializing the name which will appears in Frame Debugger, Profiler & Render Graph Viewer for this render pass
        m_ProfilingName = settings.profilingName;


        /// Initializing filtering settings with the layer to which the objects to be drawn belong
        m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, settings.selectionLayer);


        /// Initializing filtering settings with the layer to which the objects to be drawn belong
        //m_FilteringSettings2 = new FilteringSettings(RenderQueueRange.opaque, settings.selectionLayer2);


        /// Initializing shader tags list
        m_ShaderTagIdList = new List<ShaderTagId>();


        /// Use URP's default shader tags to target most objects
        m_ShaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
        m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
        m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));


        /// Initializing the property which indicates whether ZTest occurs when rendering objects
        m_ZTest = settings.zTest;

        m_ScaleTexture = settings.scaleTex;
    }

    #endregion


    #region Functions

    /// <summary>
    /// This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
    /// </summary>
    /// <param name="data">A structure that stores the data needed by the pass</param>
    /// <param name="context">A structure that permits to access the Command Buffer used for rendering</param>
    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        /// Clearing the render target 
        context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear, 1, 0);
        

        /// Drawing all objects that will need to be rendered during this pass
        context.cmd.DrawRendererList(data.listHandle);
    }


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

        //drawSettings.enableDynamicBatching = false;

        /// Enabling or disabling ZTest for objects rendering
        m_CustomMaterial.SetFloat("_ZTest", m_ZTest ? (float)CompareFunction.LessEqual : (float)CompareFunction.Always);


        if (m_ScaleTexture != null)
            m_CustomMaterial.SetTexture("_ScaleTex", m_ScaleTexture);


        /// Use the custom material to draw objects
        drawSettings.overrideMaterial = m_CustomMaterial;


        /// Create renderer list parameters from cull results, drawing settings and filtering settings
        /// Filtering settings specify that we will only render objects which belong to the selected layer 
        RendererListParams param = new RendererListParams(renderingData.cullResults, drawSettings, m_FilteringSettings);


        /// Setting a descriptor for rendering color
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(Screen.width, Screen.height, m_TargetTextureFormat, 0);
        descriptor.depthBufferBits = 0;
        descriptor.colorFormat = m_TargetTextureFormat;
        descriptor.msaaSamples = cameraData.cameraTargetDescriptor.msaaSamples;


        /// Create a target render texture using the above descriptor
        TextureHandle renderTargetHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, m_TargetTextureID, true);


        /// Add a raster render pass to the render graph to render scaled objects
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(m_ProfilingName, out var passData))
        {
            /// Setting data needed by the pass : 
            /// - Set destination with the previously created Render Graph Texture 
            /// - Set the renderer list handle to a renderer list created with the above RendererListParams 
            //passData.destination = renderTargetHandle;
            passData.listHandle = renderGraph.CreateRendererList(param);


            /// Check the renderer list handle validity
            if (!passData.listHandle.IsValid())
                return;


            /// We declare the RendererList we just created as an input dependency to this pass
            builder.UseRendererList(passData.listHandle);


            /// Set passData.destination as the render target
            builder.SetRenderAttachment(renderTargetHandle, 0);


            /// Set passData.destination as a global texture after the pass
            builder.SetGlobalTextureAfterPass(renderTargetHandle, Shader.PropertyToID(m_TargetTextureID));


            /// Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
        }


        //param.filteringSettings = m_FilteringSettings2;
        //param.drawSettings.overrideMaterial = m_Material2;


        //using (var builder = renderGraph.AddRasterRenderPass<PassData>("Pass 2", out var passData))
        //{
        //    /// Setting data needed by the pass : 
        //    /// - Set destination with the previously created Render Graph Texture 
        //    /// - Set the renderer list handle to a renderer list created with the above RendererListParams 
        //    //passData.destination = renderTargetHandle;
        //    passData.listHandle = renderGraph.CreateRendererList(param);


        //    /// Check the renderer list handle validity
        //    if (!passData.listHandle.IsValid())
        //        return;


        //    /// We declare the RendererList we just created as an input dependency to this pass
        //    builder.UseRendererList(passData.listHandle);


        //    /// Set passData.destination as the render target
        //    builder.SetRenderAttachment(renderTargetHandle, 0);


        //    /// Set passData.destination as a global texture after the pass 
        //    builder.SetGlobalTextureAfterPass(renderTargetHandle, Shader.PropertyToID(m_TargetTextureID));


        //    /// Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
        //    builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
        //}
    }

    #endregion

}