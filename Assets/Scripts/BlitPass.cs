using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class BlitPass : ScriptableRenderPass
{
    ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Blit");
    Material m_Material;
    RTHandle m_TemporaryColorTarget;

    public BlitPass(Material material, RenderPassEvent renderEvt)
    {
        m_Material = material;
        renderPassEvent = renderEvt;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var colorDesc = renderingData.cameraData.cameraTargetDescriptor;
        colorDesc.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref m_TemporaryColorTarget, colorDesc, name: "_TemporaryColorTexture");

        ConfigureTarget(m_TemporaryColorTarget);
        ConfigureClear(ClearFlag.Color, new Color(0, 0, 0, 0));
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            RTHandle camTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            if (camTarget != null && m_TemporaryColorTarget != null && m_Material != null)
            {
                Blitter.BlitCameraTexture(cmd, camTarget, m_TemporaryColorTarget, m_Material, 0);
                Blitter.BlitCameraTexture(cmd, m_TemporaryColorTarget, camTarget);
            }
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }

    public void ReleaseTargets()
    {
        m_TemporaryColorTarget?.Release();
    }
}