using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class BlitRendererFeature : ScriptableRendererFeature
{
    public RenderPassEvent m_RenderPassEvent;
    public Material m_Material;
    

    BlitPass m_RenderPass = null;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        CameraType cameraType = renderingData.cameraData.cameraType;
        if (cameraType == CameraType.Preview || cameraType == CameraType.SceneView) return;
        renderer.EnqueuePass(m_RenderPass);
    }

    public override void Create()
    {
        m_RenderPass = new BlitPass(m_Material, m_RenderPassEvent);
    }

    protected override void Dispose(bool disposing)
    {
        m_RenderPass.ReleaseTargets();
    }
}