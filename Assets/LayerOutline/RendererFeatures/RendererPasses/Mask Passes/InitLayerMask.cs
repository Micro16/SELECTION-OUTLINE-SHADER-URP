using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;


/// <summary>
/// Implements a logical rendering pass which creates and provides access to a Render Texture in which the mask for each layer will be drawn
/// </summary>
public class InitLayerMask : ScriptableRenderPass
{

    #region PassData

    /// <summary>
    /// This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
    /// </summary>
    private class PassData
    {
        
    }

    #endregion


    #region ContextItem

    /// <summary>
    /// This class derived from ContextItem allows to transfer to other passes the texture on which the masks are drawn
    /// The texture will therefore be accessible to other passes via this class
    /// </summary>
    public class MaskData : ContextItem
    {
        /// <summary>
        /// A reference to the texture on which the masks are drawn
        /// </summary>
        public TextureHandle maskHandle;
        

        /// <summary>
        /// Called when the frame resets
        /// </summary>
        public override void Reset()
        {
            /// Reset the texture when the frame resets
            maskHandle = TextureHandle.nullHandle;
        }
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
        /// Retrieving settings related to camera
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();


        /// Add a raster render pass to the render graph to create a Render Texture which will be used to render masks for each layer 
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Layer Mask : Initialization", out var passData))
        {
            /// Setting a descriptor for rendering color
            RenderTextureDescriptor textureDesc = cameraData.cameraTargetDescriptor;
            textureDesc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.B8G8R8A8_UNorm;
            textureDesc.depthBufferBits = 0;


            /// Creating the texture that will be used to draw the masks for each layer
            TextureHandle texture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureDesc, "Layer Mask", true);


            /// Instantiation of a MaskData allowing the texture created above to be transferred to the other passes
            MaskData customData = frameData.Create<MaskData>();
            customData.maskHandle = texture;


            /// Set the texture created above as the render target
            builder.SetRenderAttachment(texture, 0, AccessFlags.Write);


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
    }

    #endregion

}