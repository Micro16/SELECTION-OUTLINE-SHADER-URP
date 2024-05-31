using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


/// <summary>
/// Scriptable renderer feature to inject a render pass into the renderer
/// </summary>
public class SelectionRendererFeature : ScriptableRendererFeature
{
    /// <summary>
    /// Implements a logical rendering pass which is used to render objects belonging to a specific layer
    /// </summary>
    class SelectionRenderPass : ScriptableRenderPass
    {
        /// <summary>
        /// A struct that represents filtering settings for ScriptableRenderContext.DrawRenderers
        /// </summary>
        private FilteringSettings m_FilteringSettings;


        /// <summary>
        /// Wrapper around CPU and GPU profiling samplers
        /// </summary>
        private ProfilingSampler m_ProfilingSampler;


        /// <summary>
        /// A reference to the rendering texture used to draw the selection
        /// </summary>
        private RTHandle m_NormalsColor;


        /// <summary>
        /// The name (or ID) of the target render texture in which the selected objects will be drawn
        /// </summary>
        private string m_ColorTargetDestinationID;


        /// <summary>
        /// A list of shader tags
        /// </summary>
        private List<ShaderTagId> m_ShaderTagsList = new List<ShaderTagId>();


        /// <summary>
        /// Indicates whether the outline is visible when objects are positioned in front of it
        /// </summary>
        private bool m_OutlineAlwaysVisible;


        /// <summary>
        /// The class constructor
        /// </summary>
        /// <param name="rtname">The name of the target render texture</param>
        /// <param name="layer">The layer to which the objects to be outlined belong</param>
        public SelectionRenderPass(string rtname, LayerMask layer, bool visible)
        {
            /// Initializing the name of the target render texture 
            m_ColorTargetDestinationID = rtname;


            /// Initializing the property which indicates whether the outline is always visible or not
            m_OutlineAlwaysVisible = visible;
           

            /// Initializing filtering settings with the layer to which the objects to be outlined belong
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, layer);


            /// Use URP's default shader tags
            m_ShaderTagsList.Add(new ShaderTagId("SRPDefaultUnlit"));
            m_ShaderTagsList.Add(new ShaderTagId("UniversalForward"));
            m_ShaderTagsList.Add(new ShaderTagId("UniversalForwardOnly"));


            /// Initializing the ProfilingSampler settings with the name which will appears in Frame Debugger & Profiler
            m_ProfilingSampler = new ProfilingSampler("SelectionRenderPass");
        }


        /// <summary>
        /// This method is called by the renderer before rendering a camera
        /// Used to configure render targets and their clear state, and to create temporary render target textures
        /// </summary>
        /// <param name="cmd">CommandBuffer to enqueue rendering commands, this will be executed by the pipeline</param>
        /// <param name="renderingData">Current rendering state information</param>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            /// Configuring the target render texture descriptor based on the camera target descriptor but with some differences
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.colorFormat = RenderTextureFormat.ARGB32;


            /// Reallocation of the render texture if needed 
            RenderingUtils.ReAllocateIfNeeded(ref m_NormalsColor, descriptor, name: m_ColorTargetDestinationID);


            /// If the outline must always be visible, there's no need to attach depth data
            /// So that the selected objects are not masked by other objects located in front of them
            if (m_OutlineAlwaysVisible)
            {
                /// Configures render targets for this render pass without any attachments
                ConfigureTarget(m_NormalsColor);
            }
            else
            {
                /// Configures render targets for this render pass with depth datas as attachment so ZTest can occur
                RTHandle cameraDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;
                ConfigureTarget(m_NormalsColor, cameraDepth);
            }


            /// Configures clearing for the render targets for this render pass.            
            ConfigureClear(ClearFlag.Color, new Color(0, 0, 0, 0));
        }


        /// <summary>
        /// This is where custom rendering occurs
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution</param>
        /// <param name="renderingData">Current rendering state information</param>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            /// Get the command buffer 
            CommandBuffer cmd = CommandBufferPool.Get();


            /// Start profiling for Frame Debugger & Profiler
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                /// Need this so that calls to DrawRenderers are positioned correctly under the profiling scope name in Frame Debugger & Profiler
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();


                /// Get the default sorting criteria for this render pass
                SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;


                /// Creation & initialization of drawing settings
                DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagsList, ref renderingData, sortingCriteria);


                /// Render selected objects to temporary render texture (m_NormalsColor)
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);


                /// Set m_NormalsColor as a Global Texture reference to shaders
                cmd.SetGlobalTexture(m_ColorTargetDestinationID, m_NormalsColor);
            }


            /// Execute, clear and release the command buffer
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }


        /// <summary>
        /// Called when renderer feature is disposed in order to release the render texture
        /// </summary>
        public void Dispose()
        {
            m_NormalsColor?.Release();
        }
    }


    /// <summary>
    /// The name of the global texture, this name will be used by the shader to access the texture
    /// </summary>
    private const string TARGET_RENDER_TEXTURE_NAME = "_Selection";


    /// <summary>
    /// Definition of the structure which will contain the parameters of this Render Feature
    /// </summary>
    [System.Serializable]
    public class Settings
    {
        /// <summary>
        /// The layer to which the selected objects belong
        /// </summary>
        public LayerMask selectionLayerMask = 1;


        /// <summary>
        /// Indicates whether the outline is visible when objects are positioned in front of it
        /// </summary>
        public bool outlinesAlwaysVisible = true;
    }


    /// <summary>
    /// Instantiating a Settings structure
    /// </summary>
    public Settings settings = new Settings();


    /// <summary>
    /// Declaration of a variable for the ScriptableRenderPass
    /// </summary>
    SelectionRenderPass m_ScriptablePass;


    /// <summary>
    /// Initializes this feature's resources
    /// </summary>
    public override void Create()
    {
        /// Instantiating the ScriptableRenderPass
        m_ScriptablePass = new SelectionRenderPass(TARGET_RENDER_TEXTURE_NAME, settings.selectionLayerMask, settings.outlinesAlwaysVisible);


        /// Injects this render pass in the renderer after rendering opaques 
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }


    /// <summary>
    /// Injects one or multiple ScriptableRenderPass in the renderer
    /// </summary>
    /// <param name="renderer">Renderer used for adding render passes</param>
    /// <param name="renderingData">Rendering state used to setup render passes</param>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        /// The RenderPassFeature is active only in Game mode
        CameraType cameraType = renderingData.cameraData.cameraType;
        if (cameraType == CameraType.Preview || cameraType == CameraType.SceneView) 
            return;


        /// Injects the ScriptableRenderPass in the renderer
        renderer.EnqueuePass(m_ScriptablePass);
    }


    /// <summary>
    /// Disposable pattern implementation
    /// </summary>
    /// <param name="disposing">Indicates if dispose is happening ?</param>
    protected override void Dispose(bool disposing)
    {
        /// Dispose our ScriptableRenderPass along with the ScriptableRendererFeature
        m_ScriptablePass.Dispose();
    }
}