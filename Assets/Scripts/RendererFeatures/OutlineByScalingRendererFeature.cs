using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;


/// <summary>
/// Scriptable renderer feature which draws outlines using rendered scaled objects
/// </summary>
public class OutlineByScalingRendererFeature : ScriptableRendererFeature
{

    #region Textures & Profiling Tags

    /// <summary>
    /// The name of the global texture on which the scaled objects will be drawn
    /// </summary>
    private const string RENDER_TARGET_SCALED_NAME = "_ScaledObjects";


    /// <summary>
    /// The name which will appears in Frame Debugger & Profiler for the Render Pass which will draw the scaled objects
    /// </summary>
    private const string PROFILING_SAMPLER_SCALED_NAME = "Draw Scaled Objects";


    /// <summary>
    /// The name of the global texture on which the masks will be drawn
    /// </summary>
    private const string RENDER_TARGET_MASK_NAME = "_Masks";


    /// <summary>
    /// The name which will appears in Frame Debugger & Profiler for Render Pass which will draw the masks
    /// </summary>
    private const string PROFILING_SAMPLER_MASK_NAME = "Draw Masks";

    #endregion


    /// <summary>
    /// Definition of the structure which will contain the parameters of this Render Feature
    /// </summary>
    [System.Serializable]
    public class Settings
    {

        [Header("Scale Renderer Settings")]

        /// <summary>
        /// The layer to which the selected objects belong
        /// </summary>
        public LayerMask selectionLayerMask = 1;


        /// <summary>
        /// Indicates whether the outline is visible when objects are positioned in front of it
        /// </summary>
        public bool selectionAlwaysVisible = true;


        /// <summary>
        /// The material that contains the shader which will draw the objects with scaling and plain color
        /// </summary>
        public Material scalingMaterial = null;


        /// <summary>
        /// The material that contains the shader which will draw masks for objects 
        /// </summary>
        public Material maskingMaterial = null;


        public AnimationCurve scalingCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

    }


    /// <summary>
    /// Instantiating a Settings structure
    /// </summary>
    public Settings settings = new Settings();


    /// <summary>
    /// Declaration of a variable for the ScriptableRenderPass which will render scaled objects
    /// </summary>
    private CustomMaterialRenderPass m_ScaleRenderPass = null;


    /// <summary>
    /// Declaration of a variable for the ScriptableRenderPass which will render masks
    /// </summary>
    private CustomMaterialRenderPass m_MaskRenderPass = null;


    private Texture2D scalingCurveTexture;


    /// <summary>
    /// Initializes this feature's resources
    /// </summary>
    public override void Create()
    {
        /// We check that the scaling material have been initialized
        if (settings.scalingMaterial != null && settings.maskingMaterial != null && settings.scalingCurve != null)
        {

            int resolution = 1024;
            
            scalingCurveTexture = new Texture2D(resolution, 1, TextureFormat.RFloat, false);
            scalingCurveTexture.wrapMode = TextureWrapMode.Clamp;
            scalingCurveTexture.filterMode = FilterMode.Point;

            Color[] colors = new Color[resolution];

            for (int i = 0; i < resolution; i++)
            {
                float t = (float)i / resolution;

                colors[i].r = settings.scalingCurve.Evaluate(t);
                //colors[i].g = 0.0f;
                //colors[i].b = 0.0f;
                //colors[i].a = 1.0f;
            }

            scalingCurveTexture.SetPixels(colors);
            scalingCurveTexture.Apply(false);
            
            
            
            /// Initialize settings for scaled objects rendering
            CustomMaterialRenderPass.CustomMaterialRenderPassSettings scalePassSettings;
            scalePassSettings.customMaterial = settings.scalingMaterial;
            scalePassSettings.selectionLayer = settings.selectionLayerMask;
            //scalePassSettings.selectionLayer2 = settings.selectionLayerMask2;
            scalePassSettings.targetTextureID = RENDER_TARGET_SCALED_NAME;
            scalePassSettings.targetTextureFormat = RenderTextureFormat.ARGB32;
            scalePassSettings.profilingName = PROFILING_SAMPLER_SCALED_NAME;
            scalePassSettings.zTest = !settings.selectionAlwaysVisible;
            scalePassSettings.scaleTex = scalingCurveTexture;


            /// Instantiating the CustomMaterialRenderPass which will renders objects with scaling and plain color
            m_ScaleRenderPass = new CustomMaterialRenderPass(scalePassSettings);


            /// Injects m_SelectionRenderPass in the renderer after rendering opaques 
            m_ScaleRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;


            /// Initialize settings for masks rendering
            CustomMaterialRenderPass.CustomMaterialRenderPassSettings maskPassSettings;
            maskPassSettings.customMaterial = settings.maskingMaterial;
            maskPassSettings.selectionLayer = settings.selectionLayerMask;
            //maskPassSettings.selectionLayer2 = settings.selectionLayerMask2;
            maskPassSettings.targetTextureID = RENDER_TARGET_MASK_NAME;
            maskPassSettings.targetTextureFormat = RenderTextureFormat.RFloat;
            maskPassSettings.profilingName = PROFILING_SAMPLER_MASK_NAME;
            maskPassSettings.zTest = !settings.selectionAlwaysVisible;
            maskPassSettings.scaleTex = null;


            /// Instantiating the CustomMaterialRenderPass which will renders masks 
            m_MaskRenderPass = new CustomMaterialRenderPass(maskPassSettings);


            /// Injects m_SelectionRenderPass in the renderer after rendering opaques 
            m_MaskRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

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
            /// Injects the scriptable render passes in the renderer
            renderer.EnqueuePass(m_MaskRenderPass);
            renderer.EnqueuePass(m_ScaleRenderPass);
        }
    }

}