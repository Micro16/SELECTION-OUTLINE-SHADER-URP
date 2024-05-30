using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineRenderPassFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {

        private Settings m_Settings;
        private FilteringSettings m_FilteringSettings;
        private ProfilingSampler m_ProfilingSampler;
        private RTHandle m_CustomColor;
        private List<ShaderTagId> m_ShaderTagsList = new List<ShaderTagId>();


        public CustomRenderPass(Settings settings)
        {
            m_Settings = settings;

            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, settings.layerMask);

            m_ShaderTagsList.Add(new ShaderTagId("SRPDefaultUnlit"));
            m_ShaderTagsList.Add(new ShaderTagId("UniversalForward"));
            m_ShaderTagsList.Add(new ShaderTagId("UniversalForwardOnly"));

            m_ProfilingSampler = new ProfilingSampler("OutlineRender");
        }


        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var colorDesc = renderingData.cameraData.cameraTargetDescriptor;
            colorDesc.depthBufferBits = 0;
            colorDesc.colorFormat = RenderTextureFormat.ARGB32;

            if (m_Settings.colorTargetDestinationID != "")
            {
                RenderingUtils.ReAllocateIfNeeded(ref m_CustomColor, colorDesc, name: m_Settings.colorTargetDestinationID);
            }
            else
            {
                m_CustomColor = renderingData.cameraData.renderer.cameraColorTargetHandle;
            }

            RTHandle rtCameraDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;

            ConfigureTarget(m_CustomColor, rtCameraDepth);
            ConfigureClear(ClearFlag.Color, new Color(0, 0, 0, 0));
        }

        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagsList, ref renderingData, sortingCriteria);
                
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);

                
                if (m_Settings.colorTargetDestinationID != "")
                    cmd.SetGlobalTexture(m_Settings.colorTargetDestinationID, m_CustomColor);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        
        public override void OnCameraCleanup(CommandBuffer cmd)
        {

        }


        public void Dispose()
        {
            if (m_Settings.colorTargetDestinationID != "")
                m_CustomColor?.Release();
        }
    }


    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderEvent = RenderPassEvent.AfterRenderingOpaques;

        [Header("Draw Renderers Settings")]
        public LayerMask layerMask = 1;
        public string colorTargetDestinationID = "";
    }

    public Settings settings = new Settings();


    CustomRenderPass m_ScriptablePass;

    
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(settings);
        m_ScriptablePass.renderPassEvent = settings.renderEvent;
    }

    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        CameraType cameraType = renderingData.cameraData.cameraType;
        if (cameraType == CameraType.Preview || cameraType == CameraType.SceneView) 
            return;

        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        m_ScriptablePass.Dispose();
    }
}