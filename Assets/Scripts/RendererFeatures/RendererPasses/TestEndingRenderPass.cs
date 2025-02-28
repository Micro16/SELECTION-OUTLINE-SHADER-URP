using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;


public class TestEndingRenderPass : ScriptableRenderPass
{
    private class PassData
    {
        public TextureHandle maskHandle;
        public Material material;
        public TextureHandle source;
    }

    private Material m_TestMaterial;

    public TestEndingRenderPass(Material testMaterial)
    {
        m_TestMaterial = testMaterial;
    }

    static void ExecutePass(PassData data, RasterGraphContext context)
    {

        data.material.SetTexture("_MainTex", data.maskHandle);

        /// Execute blit from data.source and using data.blitMaterial
        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);

        //context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear, 1, 0);

    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        InitLayerMask.MaskData masksData = frameData.Get<InitLayerMask.MaskData>();
        TextureHandle renderTargetHandle = masksData.maskHandle;


        /// Retrieving settings related to texture resources and camera
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();


        /// Source texture is the active color buffer
        TextureHandle sourceTexture = resourceData.activeColorTexture;


        /// We will create a temporary destination texture to hold the contents of our blit pass
        /// This texture will match the size and format of the pipeline color buffer
        RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        TextureHandle destinationTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "Blit_Test", true);


        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Drawing Outline", out var passData))
        {
            /// Set the material that contains the shader which will draw the outlines as the blit material
            passData.material = m_TestMaterial;
            passData.maskHandle = renderTargetHandle;


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
            passData.material = m_TestMaterial;


            /// Similar to the previous pass, however now we set destination texture as input and source as output
            builder.UseTexture(destinationTexture, AccessFlags.Read);
            passData.source = destinationTexture;


            /// Set the active color buffer as the render target
            builder.SetRenderAttachment(sourceTexture, 0, AccessFlags.Write);


            /// Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
        }

        //using (var builder = renderGraph.AddRasterRenderPass<PassData>("Fetch texture and draw triangle", out var passData))
        //{
        //    // Fetch the yellow texture from the frame data and set it as the render target
        //    var customData = frameData.Get<CreateMasksTHRenderPass.MasksData>();
        //    var customTexture = customData.masksHandle;
        //    builder.SetRenderAttachment(customTexture, 0, AccessFlags.Write);

        //    builder.AllowPassCulling(false);

        //    builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
        //}
    }
}