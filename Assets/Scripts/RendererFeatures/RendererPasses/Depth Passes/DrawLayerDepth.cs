using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;


/// <summary>
/// Implements a logical rendering pass which renders depth data for each layer to a render graph texture
/// </summary>
public class DrawLayerDepth : ScriptableRenderPass
{

    #region Settings

    /// <summary>
    /// A structure which will contains all the settings required for the DrawLayerDepth render pass
    /// </summary>
    public struct DrawLayerDepthSettings
    {
        /// <summary>
        /// The name of the layer concerned by this render pass
        /// This name will appears in Frame Debugger, Profiler & Render Graph Viewer for this render pass
        /// </summary>
        public string layerName;


        /// <summary>
        /// The layer to which the objects to be drawn belong
        /// </summary>
        public LayerMask selectionLayer;
    }

    #endregion


    #region Attributes

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
    /// <param name="settings">A structure which will contain all the settings required for the DrawLayerDepth render pass</param>
    public DrawLayerDepth(DrawLayerDepthSettings settings)
    {
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
        /// Retrieving settings related to renderer, camera and lighting
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();


        /// Create drawing settings from shader tags list, renderer settings, camera settings and lighting settings
        DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData, cameraData, lightData, cameraData.defaultOpaqueSortFlags);


        /// Create renderer list parameters from cull results, drawing settings and filtering settings
        /// Filtering settings specify that we will only render objects which belong to the selected layer 
        RendererListParams param = new RendererListParams(renderingData.cullResults, drawSettings, m_FilteringSettings);


        /// Fetch texture created during the InitLayerDepth render pass
        InitLayerDepth.DepthData depthData = frameData.Get<InitLayerDepth.DepthData>();
        TextureHandle renderTargetHandle = depthData.depthHandle;


        /// Add a raster render pass to the render graph to render masks
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Drawing Layer Depth : " + m_LayerName, out var passData))
        {
            /// Setting data needed by the pass : 
            /// - Setting the renderer list handle to a renderer list created with the above RendererListParams 
            passData.listHandle = renderGraph.CreateRendererList(param);


            /// Check the renderer list handle validity
            if (!passData.listHandle.IsValid())
                return;


            /// We declare the RendererList we just created as an input dependency to this pass
            builder.UseRendererList(passData.listHandle);


            /// Set the texture created during the InitLayerDepth render pass as the depth render target
            builder.SetRenderAttachmentDepth(renderTargetHandle, AccessFlags.Write);


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
        context.cmd.ClearRenderTarget(RTClearFlags.Depth, Color.clear, 1, 0);


        /// Drawing depth data based on the RendererList passed as a parameter
        context.cmd.DrawRendererList(data.listHandle);
    }

    #endregion

}