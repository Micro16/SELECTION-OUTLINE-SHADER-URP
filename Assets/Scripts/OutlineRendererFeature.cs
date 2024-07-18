using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEditor.Rendering.CameraUI;
using static UnityEngine.Rendering.DebugUI;

#if UNITY_6000_0_OR_NEWER

using UnityEngine.Rendering.RenderGraphModule;

#endif


/// <summary>
/// Scriptable renderer feature to inject render passes into the renderer
/// </summary>
public class OutlineRendererFeature : ScriptableRendererFeature
{

#if UNITY_6000_0_OR_NEWER

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
            /// The name (or ID) of the target render texture in which the selected objects colors will be drawn
            /// </summary>
            public string colorTextureID;


            /// <summary>
            /// The name (or ID) of the target render texture in which the selected objects depths will be drawn
            /// </summary>
            public string depthTextureID;


            /// <summary>
            /// The name which will appears in Frame Debugger, Profiler & Render Graph Viewer for the color rendering pass
            /// </summary>
            public string profilingColorName;


            /// <summary>
            /// The name which will appears in Frame Debugger, Profiler & Render Graph Viewer for the depth rendering pass
            /// </summary>
            public string profilingDepthName;


            /// <summary>
            /// The layer to which the objects to be outlined belong
            /// </summary>
            public LayerMask selectionLayer;


            /// <summary>
            /// Indicates whether Z test should occurs during the render pass
            /// </summary>
            public bool zTest;

        }


        /// <summary>
        /// The name (or ID) of the target render texture in which the selected objects colors will be drawn
        /// </summary>
        private string m_ColorTextureID;


        /// <summary>
        /// The name (or ID) of the target render texture in which the selected objects depths will be drawn
        /// </summary>
        private string m_DepthTextureID;


        /// <summary>
        /// The name which will appears in Frame Debugger, Profiler & Render Graph Viewer for the color rendering pass
        /// </summary>
        private string m_ProfilingColorName;


        /// <summary>
        /// The name which will appears in Frame Debugger, Profiler & Render Graph Viewer for the depth rendering pass
        /// </summary>
        private string m_ProfilingDepthName;


        /// <summary>
        /// A struct that represents filtering settings used to initialize the renderer list
        /// </summary>
        private FilteringSettings m_FilteringSettings;


        /// <summary>
        /// A list of shader tags
        /// </summary>
        private List<ShaderTagId> m_ShaderTagIdList;


        /// <summary>
        /// Indicates whether depth datas should be attached to render target when rendering colors
        /// </summary>
        private bool m_AttachDepthDatas;


        /// <summary>
        /// The class constructor
        /// </summary>
        /// <param name="settings">A structure which will contain all the settings required for the LayerRenderPass</param>
        public LayerRenderPass(LayerRenderPassSettings settings)
        {
            /// Initializing the name of the target render texture in which the selected objects colors will be drawn
            m_ColorTextureID = settings.colorTextureID;


            /// Initializing the name of the target render texture in which the selected objects depths will be drawn
            m_DepthTextureID = settings.depthTextureID;


            /// Initializing the name which will appears in Frame Debugger, Profiler & Render Graph Viewer for color rendering
            m_ProfilingColorName = settings.profilingColorName;


            /// Initializing the name which will appears in Frame Debugger, Profiler & Render Graph Viewer for depth rendering
            m_ProfilingDepthName = settings.profilingDepthName;
            

            /// Initializing filtering settings with the layer to which the objects to be outlined belong
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, settings.selectionLayer);


            /// Initializing shader tags list
            m_ShaderTagIdList = new List<ShaderTagId>();


            /// Use URP's default shader tags
            m_ShaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
            m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));


            /// Initializing the property which indicates whether depth datas should be attached to render target when rendering colors
            m_AttachDepthDatas = settings.zTest;
        }


        /// <summary>
        /// This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
        /// </summary>
        private class PassData
        {

            /// <summary>
            /// Reference to the list of all objects that will need to be rendered during this pass
            /// </summary>
            public RendererListHandle listHandle;


            /// <summary>
            /// Reference to destination texture
            /// </summary>
            public TextureHandle destination;


            /// <summary>
            /// Indicates whether to render the depth
            /// </summary>
            public bool depthMode;

        }


        /// <summary>
        /// This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
        /// </summary>
        /// <param name="data">A structure that stores the data needed by the pass</param>
        /// <param name="context">A structure that permits to access the Command Buffer used for rendering</param>
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            /// Clearing the render target 
            if (data.depthMode)
                context.cmd.ClearRenderTarget(RTClearFlags.Depth, Color.clear, 1, 0);
            else
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
           

            /// Create drawing settings from renderer, camera and lighting settings
            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData, cameraData, lightData, cameraData.defaultOpaqueSortFlags);


            /// Create renderer list parameters from cull results, drawing settings and filtering settings
            /// Filtering settings specify that we will only render objects which belong to the selected layer 
            RendererListParams param = new RendererListParams(renderingData.cullResults, drawSettings, m_FilteringSettings);


            /// Add a raster render pass to the render graph to render depth
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Render Selection Depth", out var passData))
            {
                /// Setting a descriptor for rendering depth
                RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 32;
                descriptor.colorFormat = RenderTextureFormat.Depth;
                descriptor.msaaSamples = 1;


                /// Setting data needed by the pass : 
                /// - We indicate to render depth
                /// - Set destination to a render texture created with the above descriptor  
                /// - Set the renderer list handle to a renderer list created with the above RendererListParams 
                passData.depthMode = true;
                passData.destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, m_DepthTextureID, true);
                passData.listHandle = renderGraph.CreateRendererList(param);


                /// Check the renderer list handle validity
                if (!passData.listHandle.IsValid())
                    return;


                /// We declare the RendererList we just created as an input dependency to this pass
                builder.UseRendererList(passData.listHandle);


                /// Set passData.destination as the render target for depth data
                builder.SetRenderAttachmentDepth(passData.destination, AccessFlags.Write);


                /// Set passData.destination as a global texture after the pass 
                builder.SetGlobalTextureAfterPass(passData.destination, Shader.PropertyToID(m_DepthTextureID));


                /// Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }


            /// Add a raster render pass to the render graph to render color
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Render Selection Color", out var passData))
            {
                /// Setting a descriptor for rendering color
                RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                descriptor.colorFormat = RenderTextureFormat.ARGB32;


                /// Setting data needed by the pass : 
                /// - We indicate to render color
                /// - Set destination to a render texture created with the above descriptor  
                /// - Set the renderer list handle to a renderer list created with the above RendererListParams 
                passData.depthMode = false;
                passData.destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, m_ColorTextureID, true);
                passData.listHandle = renderGraph.CreateRendererList(param);


                /// Check the renderer list handle validity
                if (!passData.listHandle.IsValid())
                    return;


                /// We declare the RendererList we just created as an input dependency to this pass
                builder.UseRendererList(passData.listHandle);


                /// Set passData.destination as the render target
                builder.SetRenderAttachment(passData.destination, 0);


                /// Set depth datas as attachment so ZTest can occur
                if (m_AttachDepthDatas)
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);


                /// Set passData.destination as a global texture after the pass 
                builder.SetGlobalTextureAfterPass(passData.destination, Shader.PropertyToID(m_ColorTextureID));


                /// Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }

    }


    /// <summary>
    /// Implements a logical rendering pass which is used to blit the camera texture and to draw the outlines using a shader
    /// </summary>
    class BlitRenderPass : ScriptableRenderPass
    {

        /// <summary>
        /// A structure which will contain all the settings required for the BlitRenderPass
        /// </summary>
        public struct BlitRenderPassSettings
        {

            /// <summary>
            /// The material that contains the shader which will draw the outlines around the selected objects
            /// This material is used when blitting the activeColorTexture to a temporary texture
            /// </summary>
            public Material outlineBlit;


            /// <summary>
            /// The material that contains a default (copy) shader 
            /// This material is used when blitting the "outlined" temporary texture to the activeColorTexture
            /// </summary>
            public Material simpleBlit;


            /// <summary>
            /// The name which will appears in Frame Debugger, Profiler & Render Graph Viewer for the outline blit pass
            /// </summary>
            public string profilingOutlineBlitName;


            /// <summary>
            /// The name which will appears in Frame Debugger, Profiler & Render Graph Viewer for the simple blit pass
            /// </summary>
            public string profilingSimpleBlitName;


            /// <summary>
            /// The name (or ID) of the target render texture that is used to blit activeColorTexture :
            /// activeColorTexture -> outline shader -> blitTexture
            /// blitTexture -> copy shader -> activeColorTexture
            /// </summary>
            public string blitTextureID;

        }
        

        /// <summary>
        /// This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
        /// </summary>
        class PassData
        {

            /// <summary>
            /// The material used during the blit pass
            /// </summary>
            public Material blitMaterial;


            /// <summary>
            /// The blit source 
            /// </summary>
            public TextureHandle source;

        }


        /// <summary>
        /// The material that contains the shader which will draw the outlines around the selected objects
        /// This material is used when blitting the activeColorTexture to a temporary texture
        /// </summary>
        private Material m_OutlineBlitMaterial;


        /// <summary>
        /// The material that contains a default (copy) shader 
        /// This material is used when blitting the "outlined" temporary texture to the activeColorTexture
        /// </summary>
        private Material m_SimpleBlitMaterial;


        /// <summary>
        /// The name (or ID) of the target render texture that is used to blit activeColorTexture :
        /// activeColorTexture -> outline shader -> blitTexture
        /// blitTexture -> copy shader -> activeColorTexture
        /// </summary>
        private string m_BlitTextureID;


        /// <summary>
        /// The class constructor
        /// </summary>
        /// <param name="settings">A structure which will contain all the settings required for the BlitRenderPass</param>
        public BlitRenderPass(BlitRenderPassSettings settings)
        {
            /// Initialize the materials used during the blit passes
            m_OutlineBlitMaterial = settings.outlineBlit;
            m_SimpleBlitMaterial = settings.simpleBlit;


            /// Initialize the name of the target render texture that is used during the blit pass
            m_BlitTextureID = settings.blitTextureID;
        }


        /// <summary>
        /// This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
        /// </summary>
        /// <param name="data">A structure that stores the data needed by the pass</param>
        /// <param name="context">A structure that permits to access the Command Buffer used for rendering</param>
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            /// Execute blit from data.source and using data.blitMaterial
            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.blitMaterial, 0);
        }


        /// <summary>
        /// This is where the renderGraph handle can be accessed
        /// Each ScriptableRenderPass can use the RenderGraph handle to add multiple render passes to the render graph
        /// </summary>
        /// <param name="renderGraph">The RenderGraph handle</param>
        /// <param name="frameData">Contains all data relating to the rendering of the frame</param>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            /// Retrieving settings related to texture resources and camera
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();


            /// Source texture is the active color buffer
            TextureHandle sourceTexture = resourceData.activeColorTexture;
            
            
            /// We will create a temporary destination texture to hold the contents of our blit pass
            /// This texture will match the size and format of the pipeline color buffer
            RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            TextureHandle destinationTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, m_BlitTextureID, true);


            /// This is the first of the two passes that will blit the active color buffer to the temporary destination texture we just created
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Drawing Outline", out var passData))
            {
                /// Set the material that contains the shader which will draw the outlines as the blit material
                passData.blitMaterial = m_OutlineBlitMaterial;
                

                /// UseTexture tells the render graph that sourceTexture is going to be read in this pass as input
                builder.UseTexture(sourceTexture);
                passData.source = sourceTexture;


                /// Set the temporary destination texture as the render target
                builder.SetRenderAttachment(destinationTexture, 0, AccessFlags.Write);


                /// Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
            }


            /// This is the second of the two passes that will blit the temporary destination texture (created above) to the active color buffer
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Blit Resolve", out var passData))
            {
                /// Set the material that contains the shader which will copy the pixels as the blit material
                passData.blitMaterial = m_SimpleBlitMaterial;
                
                
                /// Similar to the previous pass, however now we set destination texture as input and source as output
                builder.UseTexture(destinationTexture,AccessFlags.Read);
                passData.source = destinationTexture;


                /// Set the active color buffer as the render target
                builder.SetRenderAttachment(sourceTexture, 0, AccessFlags.Write);


                /// Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
            }
        }
        
    }

#else

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
        /// Indicates whether depth datas should be attached to render target when rendering colors
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


            /// Initializing the property which indicates whether depth datas should be attached to render target when rendering colors
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

#endif

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
    private const string PS_DEPTH_NAME = "Layer Depth Rendering";


    /// <summary>
    /// The name which will appears in Frame Debugger & Profiler for the color LayerRenderPass
    /// </summary>
    private const string PS_COLOR_NAME = "Layer Color Rendering";


#if UNITY_6000_0_OR_NEWER

    /// <summary>
    /// The name (or ID) of the target render texture that is used to blit activeColorTexture :
    /// activeColorTexture -> outline shader -> blitTexture
    /// blitTexture -> copy shader -> activeColorTexture 
    /// </summary>
    private const string RT_BLIT_NAME = "_BlitTemporaryTexture";


    /// <summary>
    /// The name which will appears in Frame Debugger & Profiler for the BlitRenderPass first stage
    /// </summary>
    private const string PS_OUTLINE_BLIT = "Outline Blit";


    /// <summary>
    /// The name which will appears in Frame Debugger & Profiler for the BlitRenderPass second step
    /// </summary>
    private const string PS_SIMPLE_BLIT = "Simple Blit";

#else

    /// <summary>
    /// The name which will appears in Frame Debugger & Profiler for the BlitRenderPass
    /// </summary>
    private const string PS_BLIT_NAME = "Blit";

#endif


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
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

#if UNITY_6000_0_OR_NEWER

        /// <summary>
        /// The material with the outline shader
        /// </summary>
        public Material outlineBlitMaterial;


        /// <summary>
        /// The material with the copy shader
        /// </summary>
        public Material simpleBlitMaterial;

#else

        /// <summary>
        /// The material with the outline shader
        /// </summary>
        public Material blitMaterial;

#endif

    }


    /// <summary>
    /// Instantiating a Settings structure
    /// </summary>
    public Settings settings = new Settings();


#if UNITY_6000_0_OR_NEWER

    /// <summary>
    /// Declaration of a variable for the ScriptableRenderPass which will render the selection's depth & color to render textures
    /// </summary>
    private LayerRenderPass m_SelectionRenderPass;


    /// <summary>
    /// Declaration of a variable for the ScriptableRenderPass which will do the blit 
    /// </summary>
    private BlitRenderPass m_BlitRenderPass = null;

#else

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
    BlitRenderPass m_BlitPass = null;

#endif


    /// <summary>
    /// Initializes this feature's resources
    /// </summary>
    public override void Create()
    {
#if UNITY_6000_0_OR_NEWER

        /// Initialize settings for depth & color rendering
        LayerRenderPass.LayerRenderPassSettings renderPassSettings;
        renderPassSettings.selectionLayer = settings.selectionLayerMask;
        renderPassSettings.colorTextureID = RT_COLOR_NAME;
        renderPassSettings.depthTextureID = RT_DEPTH_NAME;
        renderPassSettings.profilingColorName = PS_COLOR_NAME;
        renderPassSettings.profilingDepthName = PS_DEPTH_NAME;
        renderPassSettings.zTest = !settings.selectionAlwaysVisible;


        /// Instantiating the LayerRenderPass which will render the selection's depth & color to render textures
        m_SelectionRenderPass = new LayerRenderPass(renderPassSettings);


        /// Injects m_SelectionRenderPass in the renderer after rendering opaques 
        m_SelectionRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;


        /// We check that the materials have been initialized
        if (settings.outlineBlitMaterial != null && settings.simpleBlitMaterial != null)
        {
            /// Initialize settings for blit pass
            BlitRenderPass.BlitRenderPassSettings blitRenderPassSettings;
            blitRenderPassSettings.outlineBlit = settings.outlineBlitMaterial;
            blitRenderPassSettings.simpleBlit = settings.simpleBlitMaterial;
            blitRenderPassSettings.profilingOutlineBlitName = PS_OUTLINE_BLIT;
            blitRenderPassSettings.profilingSimpleBlitName = PS_SIMPLE_BLIT;
            blitRenderPassSettings.blitTextureID = RT_BLIT_NAME;


            /// Instantiating the BlitRenderPass which will draws the outlines on the camera color texture
            m_BlitRenderPass = new BlitRenderPass(blitRenderPassSettings);


            /// Injects m_BlitRenderPass at the event defined in settings
            m_BlitRenderPass.renderPassEvent = settings.renderPassEvent;
        }

#else

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


        /// We check that the material has been initialized
        if (settings.blitMaterial != null)
        {
            /// Instantiating the BlitRenderPass which will draws the outlines on the camera color texture
            m_BlitPass = new BlitRenderPass(settings.blitMaterial, PS_BLIT_NAME);


            /// Injects m_BlitPass in the renderer at the event defined in settings
            m_BlitPass.renderPassEvent = settings.renderPassEvent;
        }
#endif
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
        if (cameraType == CameraType.Game)
        {
#if UNITY_6000_0_OR_NEWER

            /// Injects the scriptable render passes in the renderer
            renderer.EnqueuePass(m_SelectionRenderPass);
            if (m_BlitRenderPass != null)
                renderer.EnqueuePass(m_BlitRenderPass);

#else

            /// Injects the scriptable render passes in the renderer
            renderer.EnqueuePass(m_DepthPass);
            renderer.EnqueuePass(m_ColorPass);
            if (m_BlitPass != null) 
                renderer.EnqueuePass(m_BlitPass);

#endif
        }
    }

#if !UNITY_6000_0_OR_NEWER

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

#endif

}