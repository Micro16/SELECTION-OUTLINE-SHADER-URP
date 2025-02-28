using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class AddOwnTextureToFrameData : ScriptableRendererFeature
{
    AddOwnTexturePass customPass1;
    DrawTrianglePass customPass2;

    public override void Create()
    {
        customPass1 = new AddOwnTexturePass();
        customPass2 = new DrawTrianglePass();

        customPass1.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        customPass2.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(customPass1);
        renderer.EnqueuePass(customPass2);
    }

    // Create the first render pass, which creates a texture and adds it to the frame data
    class AddOwnTexturePass : ScriptableRenderPass
    {

        class PassData
        {
            internal TextureHandle copySourceTexture;
        }

        // Create the custom data class that contains the new texture
        public class CustomData : ContextItem
        {
            public TextureHandle newTextureForFrameData;

            public override void Reset()
            {
                newTextureForFrameData = TextureHandle.nullHandle;
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Create new texture", out var passData))
            {
                // Create a texture and set it as the render target
                RenderTextureDescriptor textureProperties = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
                TextureHandle texture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties, "My texture", false);
                CustomData customData = frameContext.Create<CustomData>();
                customData.newTextureForFrameData = texture;
                builder.SetRenderAttachment(texture, 0, AccessFlags.Write);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            // Clear the render target (the texture) to yellow
            context.cmd.ClearRenderTarget(true, true, Color.yellow);
        }

    }

    // Create the second render pass, which fetches the texture and writes to it
    class DrawTrianglePass : ScriptableRenderPass
    {

        class PassData
        {
            // No local pass data needed
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Fetch texture and draw triangle", out var passData))
            {
                // Fetch the yellow texture from the frame data and set it as the render target
                var customData = frameContext.Get<AddOwnTexturePass.CustomData>();
                var customTexture = customData.newTextureForFrameData;
                builder.SetRenderAttachment(customTexture, 0, AccessFlags.Write);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            // Generate a triangle mesh
            //Mesh mesh = new Mesh();
            //mesh.vertices = new Vector3[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0) };
            //mesh.triangles = new int[] { 0, 1, 2 };

            //// Draw a triangle to the render target (the yellow texture)
            //context.cmd.DrawMesh(mesh, Matrix4x4.identity, new Material(Shader.Find("Universal Render Pipeline/Unlit")));
            context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear, 1, 0);
        }
    }
}