using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System;
using UnityEditor.SearchService;

public class MaskObjectRendererFeature : ScriptableRendererFeature
{
    //private GameObject gameObject;
    private Material material;
    private GameObject gameObject;
    
    AddOwnTexturePass customPass1;
    DrawObjectPass customPass2;
    

    public override void Create()
    {
        material = Resources.Load<Material>("Materials/Outline/Outline_Mask");
        gameObject = GameObject.FindGameObjectsWithTag("Outline")[0];

        customPass1 = new AddOwnTexturePass();
        customPass2 = new DrawObjectPass(gameObject, material);

        customPass1.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        customPass2.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        //Debug.Log("Pass into Create");

    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(customPass1);
        renderer.EnqueuePass(customPass2);

        Debug.Log("Call to AddRenderPasses");
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
    class DrawObjectPass : ScriptableRenderPass
    {

        private Material customMaterial;
        private GameObject gameObjectToDraw;

        class PassData
        {
            public Mesh mesh;
            public Matrix4x4 matrix;
            public Material material;
        }

        public DrawObjectPass(GameObject gameObject, Material material)
        {
            customMaterial = new Material(material);
            gameObjectToDraw = gameObject;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Fetch texture and draw object", out var passData))
            {
                passData.mesh = gameObjectToDraw.GetComponent<MeshFilter>().sharedMesh;
                passData.matrix = gameObjectToDraw.transform.localToWorldMatrix;
                passData.material = customMaterial;
                
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
            //mesh.vertices = new Vector3[] { new Vector3(-1, 0, 4), new Vector3(0, 1, 4), new Vector3(1, 0, 4) };
            //mesh.triangles = new int[] { 0, 1, 2 };
            //mesh.RecalculateNormals();
            

            // Draw a triangle to the render target (the yellow texture)
            context.cmd.DrawMesh(data.mesh, data.matrix, data.material);
        }
    }
}