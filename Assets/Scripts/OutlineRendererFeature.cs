using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEditor.Rendering.CameraUI;
using static UnityEngine.Rendering.DebugUI;


/// <summary>
/// Scriptable renderer feature to inject render passes into the renderer
/// </summary>
public class OutlineRendererFeature : ScriptableRendererFeature
{
    
    /// <summary>
    /// Implements a logical rendering pass which is used to render objects belonging to a specific layer
    /// </summary>
    class LayerRenderPass : ScriptableRenderPass
    {

        /// <summary>
        /// A structure which will contain all the settings required for the LayerRenderPass
        /// </summary>
        public struct LayerRenderPassSettings
        {

            /// <summary>
            /// The name (or ID) of the target render texture in which the selected objects will be drawn
            /// </summary>
            public string targetDestinationID;


            /// <summary>
            /// The name which will appears in Frame Debugger & Profiler
            /// </summary>
            public string profilingSamplerName;


            /// <summary>
            /// The layer to which the objects to be outlined belong
            /// </summary>
            public LayerMask selectionLayer;


            /// <summary>
            /// Indicates whether Z test should occurs during the render pass
            /// </summary>
            public bool zTest;


            /// <summary>
            /// Indicates whether to render the depth
            /// </summary>
            public bool depthMode;

        }
        
        
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
        private RTHandle m_TargetRenderTextureHandle;


        /// <summary>
        /// The name (or ID) of the target render texture in which the selected objects will be drawn
        /// </summary>
        private string m_TargetRenderTextureID;


        /// <summary>
        /// A list of shader tags
        /// </summary>
        private List<ShaderTagId> m_ShaderTagsList = new List<ShaderTagId>();


        /// <summary>
        /// Indicates whether depth datas should be attached to render target
        /// </summary>
        private bool m_AttachDepthDatas;


        /// <summary>
        /// Indicates whether to render the depth
        /// </summary>
        private bool m_DepthMode;


        /// <summary>
        /// The class constructor
        /// </summary>
        /// <param name="settings">A structure which will contain all the settings required for the LayerRenderPass</param>
        public LayerRenderPass(LayerRenderPassSettings settings)
        {
            /// Initializing the name of the target render texture 
            m_TargetRenderTextureID = settings.targetDestinationID;


            /// Initializing the property which indicates whether depth datas should be attached to render target
            m_AttachDepthDatas = settings.zTest;


            /// Initializing the property which indicates whether to render the depth
            m_DepthMode = settings.depthMode;
           

            /// Initializing filtering settings with the layer to which the objects to be outlined belong
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, settings.selectionLayer);


            /// Use URP's default shader tags
            m_ShaderTagsList.Add(new ShaderTagId("SRPDefaultUnlit"));
            m_ShaderTagsList.Add(new ShaderTagId("UniversalForward"));
            m_ShaderTagsList.Add(new ShaderTagId("UniversalForwardOnly"));


            /// Initializing the ProfilingSampler with the name which will appears in Frame Debugger & Profiler
            m_ProfilingSampler = new ProfilingSampler(settings.profilingSamplerName);
        }


        /// <summary>
        /// This method is called by the renderer before rendering a camera
        /// Used to configure render targets and their clear state, and to create temporary render target textures
        /// </summary>
        /// <param name="cmd">CommandBuffer to enqueue rendering commands, this will be executed by the pipeline</param>
        /// <param name="renderingData">Current rendering state information</param>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            /// Get the camera target descriptor as we render datas from the scene main camera
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;


            /// Setting the descriptor depending on the rendering type
            if (m_DepthMode)
            {
                /// Setting the descriptor for rendering depth datas
                descriptor.colorFormat = RenderTextureFormat.Depth;
                descriptor.depthBufferBits = 32;
                descriptor.msaaSamples = 1;
            }
            else
            {
                /// Setting the descriptor for rendering colors
                descriptor.colorFormat = RenderTextureFormat.ARGB32;
                descriptor.depthBufferBits = 0;
            }


            /// Reallocation of the render texture if needed 
            RenderingUtils.ReAllocateIfNeeded(ref m_TargetRenderTextureHandle, descriptor, name: m_TargetRenderTextureID);


            /// If the outline must always be visible or if we are rendering depth datas there's no need to attach depth datas
            /// So that the selected objects are not masked by other objects located in front of them
            if (m_DepthMode || !m_AttachDepthDatas)
            {
                /// Set render target for this render pass without any attachments
                ConfigureTarget(m_TargetRenderTextureHandle);
            }
            else
            {
                /// Set render target for this render pass with depth datas as attachment so ZTest can occur
                RTHandle cameraDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;
                ConfigureTarget(m_TargetRenderTextureHandle, cameraDepth);
            }


            /// Configures clearing for the render target for this render pass   
            if (m_DepthMode)
                ConfigureClear(ClearFlag.Depth, Color.clear);
            else
                ConfigureClear(ClearFlag.Color, Color.clear);
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


                /// Render selected objects to temporary render texture (m_TargetRenderTextureHandle)
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);


                /// Set m_NormalsColor as a Global Texture reference to shaders
                cmd.SetGlobalTexture(m_TargetRenderTextureID, m_TargetRenderTextureHandle);
            }


            /// Execute, clear and release the command buffer
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }


        /// <summary>
        /// Called when renderer feature is disposed in order to release the target render texture
        /// </summary>
        public void Dispose()
        {
            m_TargetRenderTextureHandle?.Release();
        }

    }


    /// <summary>
    /// Implements a logical rendering pass which is used to blit the camera texture and to draw the outlines using a shader
    /// </summary>
    class BlitRenderPass : ScriptableRenderPass
    {

        /// <summary>
        /// Wrapper around CPU and GPU profiling samplers
        /// </summary>
        ProfilingSampler m_ProfilingSampler;
        

        /// <summary>
        /// Material used to draw the outlines by using his shader
        /// </summary>
        Material m_Material;


        /// <summary>
        /// A reference to the rendering texture used to blit the camera texture and to draw the outlines
        /// </summary>
        RTHandle m_TemporaryRenderTexture;


        /// <summary>
        /// The class constructor
        /// </summary>
        /// <param name="material">Material used to draw the outlines by using his shader</param>
        /// <param name="profilingName">The name which will appears in Frame Debugger & Profiler</param>
        public BlitRenderPass(Material material, string profilingName)
        {
            /// Initializing the material used to draw the outlines
            m_Material = material;


            /// Initializing the ProfilingSampler settings with the name which will appears in Frame Debugger & Profiler
            m_ProfilingSampler = new ProfilingSampler(profilingName);
        }


        /// <summary>
        /// This method is called by the renderer before rendering a camera
        /// Used to configure render targets and their clear state, and to create temporary render target textures
        /// </summary>
        /// <param name="cmd">CommandBuffer to enqueue rendering commands, this will be executed by the pipeline</param>
        /// <param name="renderingData">Current rendering state information</param>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            /// Get the camera target descriptor as we render datas from the scene main camera
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;


            /// Set depth buffer bits to zero as we render colors only
            descriptor.depthBufferBits = 0;


            /// Reallocation of the render texture if needed 
            RenderingUtils.ReAllocateIfNeeded(ref m_TemporaryRenderTexture, descriptor, name: "_TemporaryRenderTexture");


            /// Set render target for this render pass
            ConfigureTarget(m_TemporaryRenderTexture);


            /// Configures clearing for the render target for this render pass 
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


                /// Get the camera target handle
                RTHandle camTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                

                /// If the camera target handle, the temporary render texture and material are initialized
                if (camTarget != null && m_TemporaryRenderTexture != null && m_Material != null)
                {
                    /// Blit camera target texture on temporary render texture using shader to draw outlines 
                    Blitter.BlitCameraTexture(cmd, camTarget, m_TemporaryRenderTexture, m_Material, 0);


                    /// Blit the final texture with outlines on camera target texture  
                    Blitter.BlitCameraTexture(cmd, m_TemporaryRenderTexture, camTarget);
                }
            }


            /// Execute, clear and release the command buffer
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }


        /// <summary>
        /// Called when renderer feature is disposed in order to release the temporary target render texture
        /// </summary>
        public void Dispose()
        {
            m_TemporaryRenderTexture?.Release();
        }

    }


    /// <summary>
    /// The name of the global texture which will contains color datas, this name will be used by the shader to access the texture
    /// </summary>
    private const string RT_COLOR_NAME = "_SelectionColor";


    /// <summary>
    /// The name of the global texture which will contains depth datas, this name will be used by the shader to access the texture
    /// </summary>
    private const string RT_DEPTH_NAME = "_SelectionDepth";


    /// <summary>
    /// The name which will appears in Frame Debugger & Profiler for the color LayerRenderPass
    /// </summary>
    private const string PS_DEPTH_NAME = "LayerRenderDepth";


    /// <summary>
    /// The name which will appears in Frame Debugger & Profiler for the color LayerRenderPass
    /// </summary>
    private const string PS_COLOR_NAME = "LayerRenderColor";


    /// <summary>
    /// The name which will appears in Frame Debugger & Profiler for the BlitRenderPass
    /// </summary>
    private const string PS_BLIT_NAME = "Blit";


    /// <summary>
    /// Definition of the structure which will contain the parameters of this Render Feature
    /// </summary>
    [System.Serializable]
    public class Settings
    {

        [Header("Layer Renderer Settings")]

        /// <summary>
        /// The layer to which the selected objects belong
        /// </summary>
        public LayerMask selectionLayerMask = 1;


        /// <summary>
        /// Indicates whether the outline is visible when objects are positioned in front of it
        /// </summary>
        public bool selectionAlwaysVisible = true;


        [Header("Blit Settings")]

        /// <summary>
        /// Indicates at which rendering event the RenderPass will be injected
        /// </summary>
        public RenderPassEvent renderPassEvent;
        

        /// <summary>
        /// The material with the outline shader
        /// </summary>
        public Material blitMaterial;

    }


    /// <summary>
    /// Instantiating a Settings structure
    /// </summary>
    public Settings settings = new Settings();


    /// <summary>
    /// Declaration of a variable for the ScriptableRenderPass which will render the selection's depth to a render texture
    /// </summary>
    LayerRenderPass m_DepthPass;


    /// <summary>
    /// Declaration of a variable for the ScriptableRenderPass which will render the selection's color to a render texture
    /// </summary>
    LayerRenderPass m_ColorPass;


    /// <summary>
    /// Declaration of a variable for the ScriptableRenderPass which will do the blit  
    /// </summary>
    BlitRenderPass m_BlitPass;


    /// <summary>
    /// Initializes this feature's resources
    /// </summary>
    public override void Create()
    {
        /// Initialize settings for depth rendering
        LayerRenderPass.LayerRenderPassSettings depthSettings;
        depthSettings.targetDestinationID = RT_DEPTH_NAME;
        depthSettings.profilingSamplerName = PS_DEPTH_NAME;
        depthSettings.selectionLayer = settings.selectionLayerMask;
        depthSettings.zTest = false;
        depthSettings.depthMode = true;


        /// Instantiating the LayerRenderPass which will render the selection's depth to a render texture
        m_DepthPass = new LayerRenderPass(depthSettings);


        /// Injects m_DepthPass in the renderer after rendering opaques 
        m_DepthPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;


        /// Initialize settings for color rendering
        LayerRenderPass.LayerRenderPassSettings colorSettings;
        colorSettings.targetDestinationID = RT_COLOR_NAME;
        colorSettings.profilingSamplerName = PS_COLOR_NAME;
        colorSettings.selectionLayer = settings.selectionLayerMask;
        colorSettings.zTest = !settings.selectionAlwaysVisible;
        colorSettings.depthMode = false;


        /// Instantiating the LayerRenderPass which will render the selection's colors to a render texture
        m_ColorPass = new LayerRenderPass(colorSettings);


        /// Injects m_ColorPass in the renderer after rendering opaques 
        m_ColorPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;


        /// Instantiating the BlitRenderPass which will draws the outlines on the camera color texture
        m_BlitPass = new BlitRenderPass(settings.blitMaterial, PS_BLIT_NAME);


        /// Injects m_BlitPass in the renderer at the event defined in settings
        m_BlitPass.renderPassEvent = settings.renderPassEvent;
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


        /// Injects the render passes in the renderer
        renderer.EnqueuePass(m_DepthPass);
        renderer.EnqueuePass(m_ColorPass);
        renderer.EnqueuePass(m_BlitPass);
    }


    /// <summary>
    /// Disposable pattern implementation
    /// </summary>
    /// <param name="disposing">Indicates if dispose is happening ?</param>
    protected override void Dispose(bool disposing)
    {
        /// Dispose our render passes along with the ScriptableRendererFeature
        m_DepthPass.Dispose();
        m_ColorPass.Dispose();
        m_BlitPass.Dispose();
    }

}