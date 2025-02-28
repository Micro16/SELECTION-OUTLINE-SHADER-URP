using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;


/// <summary>
/// Implements a logical rendering pass which blits the outlines on the current active color target texture (back buffer)
/// </summary>
public class FinalBlit : ScriptableRenderPass
{

    #region Settings

    /// <summary>
    /// A structure which will contains all the settings required for the FinalBlit render pass
    /// </summary>
    public struct FinalBlitSettings
    {
        /// <summary>
        /// The material with the shader which blits the outlines on the current active color target texture (back buffer) 
        /// during the first blit pass
        /// </summary>
        public Material transferMaterial;


        /// <summary>
        /// The material with the copy shader used during the second blit pass 
        /// </summary>
        public Material blitMaterial;
    }

    #endregion


    #region Attributes

    /// <summary>
    /// The material with the shader which blits the outlines on the current active color target texture (back buffer) 
    /// during the first blit pass
    /// </summary>
    private Material m_TransferMaterial;


    /// <summary>
    /// The material with the copy shader used during the second blit pass 
    /// </summary>
    private Material m_BlitMaterial;

    #endregion


    #region PassData

    /// <summary>
    /// This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
    /// </summary>
    private class PassData
    {
        /// <summary>
        /// The material used during the blit pass 
        /// </summary>
        public Material material;


        /// <summary>
        /// Reference to the render texture which holds outline datas for all the layers
        /// </summary>
        public TextureHandle datas;


        /// <summary>
        /// Reference to the render texture that must be used by the blit render pass as a source
        /// </summary>
        public TextureHandle source;


        /// <summary>
        /// Indicates which pass of the blit is in progress : 
        /// - First blit pass to blit/transfer the outlines in a temporary render texture (which is a copy of the current active 
        ///   color target texture)
        /// - Second blit pass to copy datas from the temporary texture to the current active color target texture
        /// </summary>
        public bool transfer;
    }

    #endregion


    #region Constructor

    /// <summary>
    /// The class constructor
    /// </summary>
    /// <param name="settings">A structure which will contain all the settings required for the FinalBlit render pass</param>
    public FinalBlit(FinalBlitSettings settings)
    {
        /// Initializing the material with the shader which blits the outlines on the current active color target texture
        m_TransferMaterial = settings.transferMaterial;


        /// Initializing the material with the copy shader used during the second blit pass
        m_BlitMaterial = settings.blitMaterial;
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
        /// Retrieving settings related to texture resources and camera
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();


        /// Creating a temporary texture in which will be copied successively :  
        /// - The current active color target texture
        /// - The outlines drawn during the DrawLayerOutline render passes
        RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
        descriptor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.B8G8R8A8_UNorm;
        descriptor.depthBufferBits = 0;
        TextureHandle bufferTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "Final Blit Buffer", true);


        /// Get the current active color target texture
        TextureHandle activeColor = resourceData.activeColorTexture;


        /// Fetch texture created during the InitLayerOutline render pass
        InitLayerOutline.OutlineData outlineData = frameData.Get<InitLayerOutline.OutlineData>();
        TextureHandle outlineHandle = outlineData.outlineHandle;


        /// This is the first of the two passes that will blit the current active color target texture to the 
        /// temporary render texture (created above) while blitting all layers outlines   
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Final Blit : First Pass", out var passData))
        {

            /// Setting datas needed by the pass : 
            /// - A material that contains a shader to copy the pixels of the active color target as well as the outlines
            /// - Setting the reference to the render texture which holds outlines datas
            /// - Setting the reference to the current active color target texture that must be used by the blit render pass as a source
            /// - Indicating that this raster render pass will copy pixels from outlines datas (outlineHandle)
            passData.material = m_TransferMaterial;
            passData.datas = outlineHandle;
            passData.source = activeColor;
            passData.transfer = true;


            /// Tells the render graph which textures is going to be read in this pass as input
            builder.UseTexture(passData.source, AccessFlags.Read);
            builder.UseTexture(passData.datas, AccessFlags.Read);


            /// Set the texture created above as the render target
            builder.SetRenderAttachment(bufferTexture, 0, AccessFlags.Write);


            /// Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
        }


        /// This is the second of the two passes that will blit the temporary texture (created above) to the 
        /// current active color target texture
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Final Blit: Second Pass", out var passData))
        {
            /// Setting datas needed by the pass : 
            /// - Setting the material that contains a simple copy shader
            /// - We don't need outlines datas for this render pass 
            /// - Setting the reference to the render texture that must be used by the blit render pass as a source
            /// - Indicating that this raster render pass will just copy texture (a simple blit)
            passData.material = m_BlitMaterial;
            passData.datas = TextureHandle.nullHandle;
            passData.source = bufferTexture;
            passData.transfer = false;


            /// Tells the render graph which textures is going to be read in this pass as input
            builder.UseTexture(passData.source, AccessFlags.Read);


            /// Set the current active color target texture as the render target
            builder.SetRenderAttachment(activeColor, 0, AccessFlags.Write);


            /// Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
        }
    }


    /// <summary>
    /// This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
    /// </summary>
    /// <param name="data">A structure that stores the data needed by the pass</param>
    /// <param name="context">A structure that permits to access the Command Buffer used for rendering</param>
    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        /// Passing outlines datas to the shader if this render pass has to draw the outlines while copying pixels from active color texture
        /// This has to be done here and once we told the render graph which textures is going to be read in this pass as input
        if (data.transfer)
            data.material.SetTexture("_Outlines", data.datas);


        /// Execute blit from data.source and using data.material
        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
    }

    #endregion

}