using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;


/// <summary>
/// Implements a logical rendering pass which renders (with an overrided material) the outlines for each layer to a render graph texture
/// </summary>
public class DrawLayerOutline : ScriptableRenderPass
{

    #region Settings

    /// <summary>
    /// A structure which will contains all the settings required for the DrawLayerOutline render pass
    /// </summary>
    public struct DrawLayerOutlineSettings
    {
        /// <summary>
        /// The material with the shader which draws the outlines during the first blit pass
        /// </summary>
        public Material outlineMaterial;


        /// <summary>
        /// The material with the copy shader used during the second blit pass 
        /// </summary>
        public Material blitMaterial;


        /// <summary>
        /// The outline color for this layer
        /// </summary>
        public Color outlineColor;


        /// <summary>
        /// The thickness of the outline for this layer
        /// </summary>
        public int outlineThickness;


        /// <summary>
        /// The name of the layer concerned by this render pass
        /// This name will appears in Frame Debugger, Profiler & Render Graph Viewer for this render pass
        /// </summary>
        public string layerName;
    }

    #endregion


    #region Attributes

    /// <summary>
    /// The material with the shader which draws the outlines during the first blit pass
    /// </summary>
    private Material m_OutlineMaterial;


    /// <summary>
    /// A material with layer-specific properties (color and thickness) and created dynamically from m_OutlineMaterial
    /// This material will serve as the overrided material for this render pass to draw the outlines
    /// </summary>
    private Material m_LayerOutlineMaterial;


    /// <summary>
    /// The material with the copy shader used during the second blit pass 
    /// </summary>
    private Material m_BlitMaterial;


    /// <summary>
    /// The name of the layer concerned by this render pass
    /// This name will appears in Frame Debugger, Profiler & Render Graph Viewer for this render pass
    /// </summary>
    private string m_LayerName;


    /// <summary>
    /// The thickness of the outline for this layer
    /// </summary>
    private float m_LayerOutlineThickness;


    /// <summary>
    /// The outline color for this layer
    /// </summary>
    private Color m_LayerOutlineColor;

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
        /// Reference to the render texture which holds mask datas for this layer
        /// Those datas will be used to draw outlines
        /// </summary>
        public TextureHandle maskHandle;
        

        /// <summary>
        /// Reference to the render texture that must be used by the blit render pass as a source
        /// </summary>
        public TextureHandle source;


        /// <summary>
        /// Indicates which pass of the blit is in progress : 
        /// - First blit pass to draw the outlines in a temporary render texture (which is a copy of the render texture created 
        ///   during the InitLayerOutline render pass)
        /// - Second blit pass to copy datas from the temporary texture to the render texture created during the InitLayerOutline 
        ///   render pass
        /// </summary>
        public bool outline;
    }

    #endregion


    #region Accessors 

    /// <summary>
    /// Accessor to m_LayerName
    /// </summary>
    public string LayerName { get { return m_LayerName; } }


    /// <summary>
    /// Accessor to the shader color property 
    /// </summary>
    public Color LayerColor 
    { 
        get 
        {
            /// Returns a default color if the material is not accessible
            if (m_LayerOutlineMaterial == null)
                return Color.white;
            else
                return m_LayerOutlineMaterial.GetColor("_Color"); 
        } 
        set 
        {
            /// Set the outline color if material is accessible
            if (m_LayerOutlineMaterial != null)
                m_LayerOutlineMaterial.SetColor("_Color", value); 
        } 
    }


    /// <summary>
    /// Accessor to the shader thickness property 
    /// </summary>
    public float LayerThickness 
    { 
        get 
        {
            /// Returns a default thickness if the material is not accessible
            if (m_LayerOutlineMaterial == null)
                return 0f;
            else
                return m_LayerOutlineMaterial.GetFloat("_Thickness");
    } 
        set 
        {
            /// Set the outline thickness if material is accessible
            if (m_LayerOutlineMaterial != null)
                m_LayerOutlineMaterial.SetFloat("_Thickness", value); 
        } 
    }

    #endregion 


    #region Constructor

    /// <summary>
    /// The class constructor
    /// </summary>
    /// <param name="settings">A structure which will contain all the settings required for the DrawLayerOutline render pass</param>
    public DrawLayerOutline(DrawLayerOutlineSettings settings)
    {
        /// Stores the material with the shader which draws the outlines
        m_OutlineMaterial = settings.outlineMaterial;


        /// Initializing a new material that will draws the outlines during the first blit pass 
        /// and that will holds the color ant thickness values for this layer
        m_LayerOutlineMaterial = new Material(m_OutlineMaterial);


        /// Initializing the material with the copy shader used during the second blit pass
        m_BlitMaterial = settings.blitMaterial;


        /// Initializing the name of the layer concerned by this render pass
        m_LayerName = settings.layerName;


        /// Stores thickness and color values for this layer
        m_LayerOutlineThickness = settings.outlineThickness;
        m_LayerOutlineColor = settings.outlineColor;


        /// Set the properties of m_LayerOutlineMaterial to draw outlines with a specific color and thickness 
        m_LayerOutlineMaterial.SetColor("_Color", m_LayerOutlineColor);
        m_LayerOutlineMaterial.SetFloat("_Thickness", m_LayerOutlineThickness);
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


        /// Creating a temporary texture in which the layer outlines will be drawn and to hold the contents of our blit pass
        RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
        descriptor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.B8G8R8A8_UNorm;
        descriptor.depthBufferBits = 0;
        TextureHandle bufferTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "Blit Buffer : " + m_LayerName, true);


        /// The materials are destroyed while loading another scene so we must initialize m_LayerOutlineMaterial again
        if (m_LayerOutlineMaterial == null)
        {
            /// Initializing a new material that will draws the outlines during the first blit pass 
            /// and that will holds the color ant thickness values for this layer
            m_LayerOutlineMaterial = new Material(m_OutlineMaterial);


            /// Set the properties of m_LayerOutlineMaterial to draw outlines with a specific color and thickness 
            m_LayerOutlineMaterial.SetColor("_Color", m_LayerOutlineColor);
            m_LayerOutlineMaterial.SetFloat("_Thickness", m_LayerOutlineThickness);
        }


        /// Fetch texture created during the InitLayerMask render pass
        InitLayerMask.MaskData maskData = frameData.Get<InitLayerMask.MaskData>();
        TextureHandle maskHandle = maskData.maskHandle;


        /// Fetch texture created during the InitLayerOutline render pass
        InitLayerOutline.OutlineData outlineData = frameData.Get<InitLayerOutline.OutlineData>();
        TextureHandle outlineHandle = outlineData.outlineHandle;


        /// This is the first of the two passes that will blit the render texture created during the InitLayerOutline render pass 
        /// to the temporary render texture (created above) while drawing outlines for this layer  
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Drawing Layer Outline : " + m_LayerName, out var passData))
        {
            /// Setting datas needed by the pass : 
            /// - Setting the material that contains the shader which will draw the outlines as the blit material
            /// - Setting the reference to the render texture which holds mask datas that will be used to draw outlines
            /// - Setting the reference to the render texture that must be used by the blit render pass as a source
            /// - Indicating that this raster render pass will draw the outlines while copying texture (blit)
            passData.material = m_LayerOutlineMaterial;
            passData.maskHandle = maskHandle;
            passData.source = outlineHandle;
            passData.outline = true;


            /// Tells the render graph which textures is going to be read in this pass as input
            builder.UseTexture(passData.source, AccessFlags.Read);
            builder.UseTexture(passData.maskHandle, AccessFlags.Read);


            /// Set the temporary render texture as the render target
            builder.SetRenderAttachment(bufferTexture, 0, AccessFlags.Write);


            /// Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
            builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
        }


        /// This is the second of the two passes that will blit the temporary texture (created above) to the 
        /// outlines render texture created during the InitLayerOutline render pass
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Blitting Layer Outline : " + m_LayerName, out var passData))
        {
            /// Setting datas needed by the pass : 
            /// - Setting the material that contains a simple copy shader
            /// - Setting the reference to the render texture which holds mask datas that will be used to draw outlines
            /// - Setting the reference to the render texture that must be used by the blit render pass as a source
            /// - Indicating that this raster render pass will just copy texture (a simple blit)
            passData.material = m_BlitMaterial;
            passData.maskHandle = TextureHandle.nullHandle;
            passData.source = bufferTexture;
            passData.outline = false;


            /// Similar to the previous pass, however now we set destination texture as input and source as output
            builder.UseTexture(bufferTexture, AccessFlags.Read);


            /// Set the render texture created during the InitLayerOutline render pass as the render target
            builder.SetRenderAttachment(outlineHandle, 0, AccessFlags.Write);


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
        /// Passing layer mask datas to the shader if this render pass has to draw the outlines while copying texture
        /// This has to be done here and once we told the render graph which textures is going to be read in this pass as input
        if (data.outline)
        {
            data.material.SetTexture("_LayerMask", data.maskHandle);
        }


        /// Execute blit from data.source and using data.blitMaterial
        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
    }

    #endregion

}