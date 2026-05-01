using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class WaterPixelizeFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public int pixelHeight = 270;
        public LayerMask waterLayer = 0;
        public bool limitFrameRate = false;      // ADD THIS
        [Range(1, 60)]
        public int pixelFrameRate = 12;  // ADD THIS
        public bool usePalette = false;
        public Texture2D paletteTexture = null;
        [Range(1, 256)]
        public int paletteSize = 16;
    }

    public Settings settings = new Settings();

    private WaterPixelizePass m_Pass;
    private Material m_BlitMaterial;
    private Material m_StencilMaterial;
    private Material m_StencilClearMaterial;
    private Material m_CompositeMaterial;
    private Material m_PaletteMaterial;
    
    public override void Create()
    {
        m_BlitMaterial = CoreUtils.CreateEngineMaterial("Hidden/WaterPixelize");
        m_StencilMaterial = CoreUtils.CreateEngineMaterial("Hidden/WaterStencilWrite");
        m_StencilClearMaterial = CoreUtils.CreateEngineMaterial("Hidden/StencilClear"); // ADD
        m_CompositeMaterial = CoreUtils.CreateEngineMaterial("Hidden/WaterComposite");
        m_PaletteMaterial = CoreUtils.CreateEngineMaterial("Hidden/PaletteQuantize");

        m_Pass = new WaterPixelizePass(m_BlitMaterial, m_StencilMaterial, settings, m_StencilClearMaterial, m_CompositeMaterial, m_PaletteMaterial);
        m_Pass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_BlitMaterial == null || m_StencilMaterial == null) return;
        
        if (settings.usePalette && settings.paletteTexture != null)
        {
            m_PaletteMaterial.SetTexture("_PaletteTex", settings.paletteTexture);
            m_PaletteMaterial.SetFloat("_PaletteSize", settings.paletteSize);
        }
        
        renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_BlitMaterial);
        CoreUtils.Destroy(m_StencilMaterial);
        CoreUtils.Destroy(m_StencilClearMaterial); // ADD
        m_Pass.ReleaseHandles();  // ADD THIS
        CoreUtils.Destroy(m_CompositeMaterial);
        CoreUtils.Destroy(m_PaletteMaterial);

    }
}

public class WaterPixelizePass : ScriptableRenderPass
{
    private const string LOW_RES_TEXTURE_NAME = "_WaterLowResTex";
    private const string WATER_PIXELIZE_PASS_NAME = "WaterPixelizePass";

    private Material m_BlitMaterial;
    private Material m_StencilMaterial;
    private Material m_StencilClearMaterial;
    private Material m_CompositeMaterial;
    private Material m_PaletteMaterial;
    private RTHandle m_FullResCopyHandle;
    private WaterPixelizeFeature.Settings m_Settings;
    private RenderTextureDescriptor myLowResDescription;

    private RTHandle m_LowResHandle;
    private RTHandle m_OriginalColorHandle;
    private RTHandle m_CompositeHandle;
    private float m_LastUpdateTime = 0f;
    private FilteringSettings m_FilteringSettings;
    private ShaderTagId m_ShaderTagId = new ShaderTagId("UniversalForward");

    public WaterPixelizePass(Material blitMaterial, Material stencilMaterial,
        WaterPixelizeFeature.Settings settings, Material stencilClearMaterial, Material compositeMaterial, Material paletteMaterial)
    {
        m_BlitMaterial = blitMaterial;
        m_StencilMaterial = stencilMaterial;
        m_StencilClearMaterial = stencilClearMaterial; // ADD
        m_CompositeMaterial = compositeMaterial;
        m_PaletteMaterial = paletteMaterial;
        m_Settings = settings;
        myLowResDescription = new RenderTextureDescriptor(
            Screen.width, Screen.height, RenderTextureFormat.Default, 0);

        m_FilteringSettings = new FilteringSettings(
            RenderQueueRange.all, settings.waterLayer);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        if (resourceData.isActiveTargetBackBuffer) return;
        TextureHandle srcCamColor = resourceData.activeColorTexture;
        
        //store original high res image
        UpdateOriginalColorHandle(
            cameraData.cameraTargetDescriptor.width,
            cameraData.cameraTargetDescriptor.height,
            cameraData.cameraTargetDescriptor.graphicsFormat);
        TextureHandle originalColor = renderGraph.ImportTexture(m_OriginalColorHandle);

        // Store original camera color before we touch anything
        RenderGraphUtils.BlitMaterialParameters storeOriginal =
            new(srcCamColor, originalColor, m_BlitMaterial, 0);
        renderGraph.AddBlitPass(storeOriginal, "StoreOriginalColor");
        
        
        // // ---- Step -1: Snap _Time to choppy intervals ----
        // using (var builder = renderGraph.AddRasterRenderPass<TimePassData>(
        //            "SetChoppyTime", out var choppyTimeData))
        // {
        //     float interval = 1f / m_Settings.pixelFrameRate;
        //     float t = Mathf.Floor(Time.time / interval) * interval;
        //     choppyTimeData.timeVector = new Vector4(t / 20f, t, t * 2f, t * 3f);
        //     choppyTimeData.enabled = m_Settings.limitFrameRate;
        //
        //     builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
        //     builder.AllowPassCulling(false);
        //     builder.AllowGlobalStateModification(true);
        //
        //     builder.SetRenderFunc((TimePassData data, RasterGraphContext ctx) =>
        //     {
        //         if (data.enabled)
        //         {
        //             ctx.cmd.SetGlobalVector("_TimeParameters", data.timeVector);
        //             Debug.Log("Choppy time");
        //         }
        //     });
        // }
        
        // ---- Step 0: Write stencil on water layer objects ----
        using (var builder = renderGraph.AddRasterRenderPass<StencilPassData>(
                   "WaterStencilWrite", out var stencilData))
        {
            // Filter to only objects on the water layer
            FilteringSettings filterSettings = new FilteringSettings(
                RenderQueueRange.all, m_Settings.waterLayer);

            var shaderTagIds = new List<ShaderTagId>
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("LightweightForward"),
            };

            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(
                shaderTagIds, renderingData, cameraData, lightData,
                cameraData.defaultOpaqueSortFlags);

            // Override their material with our stencil writer
            drawSettings.overrideMaterial = m_StencilMaterial;
            drawSettings.overrideMaterialPassIndex = 0;

            var rendererListParams = new RendererListParams(
                renderingData.cullResults, drawSettings, filterSettings);

            stencilData.rendererListHandle =
                renderGraph.CreateRendererList(rendererListParams);

            builder.UseRendererList(stencilData.rendererListHandle);
            // Write to color and depth so stencil is accessible
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture,
                AccessFlags.Write);
            builder.AllowPassCulling(false);
            
            builder.SetRenderFunc((StencilPassData data, RasterGraphContext context) =>
            {
                context.cmd.DrawRendererList(data.rendererListHandle);
            });
        }
        
        // ---- Step 1: Clear stencil on everything that is NOT the water layer ----
        using (var builder = renderGraph.AddRasterRenderPass<StencilPassData>(
                   "WaterStencilClear", out var stencilClearData))
        {
            // Invert the water layer mask to get everything else
            int invertedMask = ~m_Settings.waterLayer.value;
        
            FilteringSettings clearFilterSettings = new FilteringSettings(
                RenderQueueRange.all, invertedMask);
        
            var shaderTagIds = new List<ShaderTagId>
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("LightweightForward"),
            };
        
            DrawingSettings clearDrawSettings = RenderingUtils.CreateDrawingSettings(
                shaderTagIds, renderingData, cameraData, lightData,
                cameraData.defaultOpaqueSortFlags);
        
            clearDrawSettings.overrideMaterial = m_StencilClearMaterial;
            clearDrawSettings.overrideMaterialPassIndex = 0;
        
            var clearRendererListParams = new RendererListParams(
                renderingData.cullResults, clearDrawSettings, clearFilterSettings);
        
            stencilClearData.rendererListHandle =
                renderGraph.CreateRendererList(clearRendererListParams);
        
            builder.UseRendererList(stencilClearData.rendererListHandle);
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);
            builder.AllowPassCulling(false);
        
            builder.SetRenderFunc((StencilPassData data, RasterGraphContext context) =>
            {
                context.cmd.DrawRendererList(data.rendererListHandle);
            });
        }
        
        float aspect = (float)cameraData.cameraTargetDescriptor.width /
                              cameraData.cameraTargetDescriptor.height;
        int lowResHeight = m_Settings.pixelHeight;
        int lowResWidth = Mathf.RoundToInt(lowResHeight * aspect);

        myLowResDescription.width = lowResWidth;
        myLowResDescription.height = lowResHeight;
        myLowResDescription.depthBufferBits = 0;

        // TextureHandle srcCamColor = resourceData.activeColorTexture;
        // TextureHandle lowResDst = UniversalRenderer.CreateRenderGraphTexture(
        //     renderGraph, myLowResDescription, LOW_RES_TEXTURE_NAME, false,
        //     FilterMode.Point);
        //
        // if (!srcCamColor.IsValid() || !lowResDst.IsValid()) return;
        //
        //
        //
        // // Pass 0: Downsample → low-res (no stencil needed, just shrink the image)
        // RenderGraphUtils.BlitMaterialParameters downsample =
        //     new(srcCamColor, lowResDst, m_BlitMaterial, 0);
        // renderGraph.AddBlitPass(downsample, WATER_PIXELIZE_PASS_NAME + "_Downsample");

        //Trying low frame rate
        

        // Persistent low-res handle instead of a temporary texture
        UpdateLowResHandle(lowResWidth, lowResHeight, cameraData.cameraTargetDescriptor.graphicsFormat);
        TextureHandle lowResDst = renderGraph.ImportTexture(m_LowResHandle);

        if (!srcCamColor.IsValid() || !lowResDst.IsValid()) return;

        UpdateFullResCopyHandle(
            cameraData.cameraTargetDescriptor.width,
            cameraData.cameraTargetDescriptor.height,
            cameraData.cameraTargetDescriptor.graphicsFormat);
        TextureHandle fullResCopy = renderGraph.ImportTexture(m_FullResCopyHandle);

        // Step 2: Clear full-res copy to transparent, then blit camera color into it stencil-gated
        using (var builder = renderGraph.AddRasterRenderPass<DownsamplePassData>(
                   "WaterFullResCopy", out var copyData))
        {
            copyData.srcTex = srcCamColor;
            copyData.material = m_BlitMaterial;

            builder.UseTexture(srcCamColor, AccessFlags.Read);
            builder.SetRenderAttachment(fullResCopy, 0, AccessFlags.ReadWrite);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((DownsamplePassData data, RasterGraphContext ctx) =>
            {
                // ctx.cmd.ClearRenderTarget(false, true, Color.clear);
                ctx.cmd.ClearRenderTarget(false, true, Color.magenta); // sentinel color
                Blitter.BlitTexture(ctx.cmd, data.srcTex,
                    new Vector4(1, 1, 0, 0), data.material, 1); // pass 1 = stencil gated upsample
            });
        }

        // Step 3: Shrink full-res copy to low-res, no depth needed
        RenderGraphUtils.BlitMaterialParameters shrink =
            new(fullResCopy, lowResDst, m_BlitMaterial, 0); // pass 0 = plain downsample
        renderGraph.AddBlitPass(shrink, "WaterFullRes_Shrink");
        
        // // Gate the downsample to only run at pixelFrameRate
        // bool shouldUpdate = true;
        //
        // if (m_Settings.limitFrameRate)
        // {
        //     float interval = 1f / m_Settings.pixelFrameRate;
        //     shouldUpdate = (Time.time - m_LastUpdateTime) >= interval;
        // }
        //
        // // if (shouldUpdate) TEMP DISABLING FRAME RATE CONTROL
        // if(true)
        // {
        //     m_LastUpdateTime = Time.time;
        //
        //     using (var builder = renderGraph.AddRasterRenderPass<DownsamplePassData>(
        //                "WaterPixelize_Downsample", out var downsampleData))
        //     {
        //         downsampleData.srcTex = srcCamColor;
        //         downsampleData.material = m_BlitMaterial;
        //
        //         builder.UseTexture(srcCamColor, AccessFlags.Read);
        //         builder.SetRenderAttachment(lowResDst, 0, AccessFlags.ReadWrite);
        //         // Bind the full-res depth so stencil=65 test works
        //         builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
        //         builder.AllowPassCulling(false);
        //
        //         builder.SetRenderFunc((DownsamplePassData data, RasterGraphContext ctx) =>
        //         {
        //             // Clear the low-res RT to transparent first
        //             ctx.cmd.ClearRenderTarget(false, true, Color.clear);
        //             Blitter.BlitTexture(ctx.cmd, data.srcTex,
        //                 new Vector4(1, 1, 0, 0), data.material, 0);
        //         });
        //     }
        // }
        
        // Pass 1: Upsample → camera color, but ONLY on stencil-9 pixels
        using (var builder = renderGraph.AddRasterRenderPass<UpsamplePassData>(
                   "WaterPixelize_Upsample", out var upsampleData))
        {
            upsampleData.lowResTex = lowResDst;
            upsampleData.material = m_BlitMaterial;

            builder.UseTexture(lowResDst, AccessFlags.Read);
            builder.SetRenderAttachment(srcCamColor, 0, AccessFlags.ReadWrite);
            // Bind depth so the stencil buffer is available for the test
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((UpsamplePassData data, RasterGraphContext ctx) =>
            {
                Blitter.BlitTexture(ctx.cmd, data.lowResTex,
                    new Vector4(1, 1, 0, 0), data.material, 1);
            });
        }
        
        // using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(
        //            "WaterComposite", out var compositeData))
        // {
        //     compositeData.upsampled = srcCamColor;
        //     compositeData.original = originalColor;
        //     compositeData.material = m_CompositeMaterial;
        //
        //     builder.UseTexture(originalColor, AccessFlags.Read);
        //     builder.SetRenderAttachment(srcCamColor, 0, AccessFlags.ReadWrite);
        //     builder.AllowPassCulling(false);
        //     builder.AllowGlobalStateModification(true);
        //
        //     builder.SetRenderFunc((CompositePassData data, RasterGraphContext ctx) =>
        //     {
        //         ctx.cmd.SetGlobalTexture("_OriginalTex", data.original);
        //         Blitter.BlitTexture(ctx.cmd, data.upsampled,
        //             new Vector4(1, 1, 0, 0), data.material, 0);
        //     });
        // }
        
        UpdateCompositeHandle(
            cameraData.cameraTargetDescriptor.width,
            cameraData.cameraTargetDescriptor.height,
            cameraData.cameraTargetDescriptor.graphicsFormat);
        TextureHandle compositeOut = renderGraph.ImportTexture(m_CompositeHandle);

        using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(
                   "WaterComposite", out var compositeData))
        {
            compositeData.upsampled = srcCamColor;
            compositeData.original = originalColor;
            compositeData.material = m_CompositeMaterial;

            builder.UseTexture(srcCamColor, AccessFlags.Read);
            builder.UseTexture(originalColor, AccessFlags.Read);
            builder.SetRenderAttachment(compositeOut, 0, AccessFlags.ReadWrite);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            builder.SetRenderFunc((CompositePassData data, RasterGraphContext ctx) =>
            {
                ctx.cmd.SetGlobalTexture("_OriginalTex", data.original);
                ctx.cmd.SetGlobalTexture("_UpsampledTex", data.upsampled);
                Blitter.BlitTexture(ctx.cmd, data.upsampled,
                    new Vector4(1, 1, 0, 0), data.material, 0);
            });
        }

        RenderGraphUtils.BlitMaterialParameters copyBack =
            new(compositeOut, srcCamColor, m_BlitMaterial, 0);
        renderGraph.AddBlitPass(copyBack, "CompositeToCamera");
        
        // Palette quantize — optional, runs last
        if (m_Settings.usePalette && m_Settings.paletteTexture != null)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PalettePassData>(
                       "PaletteQuantize", out var paletteData))
            {
                paletteData.material = m_PaletteMaterial;

                builder.SetRenderAttachment(srcCamColor, 0, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);

                builder.SetRenderFunc((PalettePassData data, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, srcCamColor,
                        new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }
        }
        
        // // ---- Step Last: Restore real _Time ----
        // using (var builder = renderGraph.AddRasterRenderPass<TimePassData>(
        //            "RestoreTime", out var restoreTimeData))
        // {
        //     float t = Time.time;
        //     restoreTimeData.timeVector = new Vector4(t / 20f, t, t * 2f, t * 3f);
        //     restoreTimeData.enabled = m_Settings.limitFrameRate;
        //
        //     builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
        //     builder.AllowPassCulling(false);
        //     builder.AllowGlobalStateModification(true);
        //
        //     builder.SetRenderFunc((TimePassData data, RasterGraphContext ctx) =>
        //     {
        //         if (data.enabled)
        //             ctx.cmd.SetGlobalVector("_TimeParameters", data.timeVector);
        //     });
        // }
    }
    
    private void UpdateFullResCopyHandle(int width, int height, UnityEngine.Experimental.Rendering.GraphicsFormat format)
    {
        if (m_FullResCopyHandle == null ||
            m_FullResCopyHandle.rt.width != width ||
            m_FullResCopyHandle.rt.height != height)
        {
            m_FullResCopyHandle?.Release();
            m_FullResCopyHandle = RTHandles.Alloc(
                width, height,
                colorFormat: format,
                filterMode: FilterMode.Point,
                name: "_WaterFullResCopy"
            );
        }
    }
    
    private void UpdateLowResHandle(int width, int height, UnityEngine.Experimental.Rendering.GraphicsFormat format)
    {
        if (m_LowResHandle == null || m_LowResHandle.rt.width != width || m_LowResHandle.rt.height != height)
        {
            m_LowResHandle?.Release();
            m_LowResHandle = RTHandles.Alloc(
                width, height,
                colorFormat: format,
                filterMode: FilterMode.Point,
                name: "_WaterLowResTex"
            );
        }
    }
    
    //store original pixture for overlaying transparent pixels
    private void UpdateOriginalColorHandle(int width, int height, UnityEngine.Experimental.Rendering.GraphicsFormat format)
    {
        if (m_OriginalColorHandle == null ||
            m_OriginalColorHandle.rt.width != width ||
            m_OriginalColorHandle.rt.height != height)
        {
            m_OriginalColorHandle?.Release();
            m_OriginalColorHandle = RTHandles.Alloc(
                width, height,
                colorFormat: format,
                filterMode: FilterMode.Point,
                name: "_WaterOriginalColor"
            );
        }
    }
    
    private void UpdateCompositeHandle(int width, int height, UnityEngine.Experimental.Rendering.GraphicsFormat format)
    {
        if (m_CompositeHandle == null ||
            m_CompositeHandle.rt.width != width ||
            m_CompositeHandle.rt.height != height)
        {
            m_CompositeHandle?.Release();
            m_CompositeHandle = RTHandles.Alloc(
                width, height,
                colorFormat: format,
                filterMode: FilterMode.Point,
                name: "_WaterComposite"
            );
        }
    }

    public void ReleaseHandles()
    {
        m_LowResHandle?.Release();
        m_LowResHandle = null;
        m_FullResCopyHandle?.Release();
        m_FullResCopyHandle = null;
        m_OriginalColorHandle?.Release();
        m_OriginalColorHandle = null;
        m_CompositeHandle?.Release();
        m_CompositeHandle = null;
    }
    
    private class DownsamplePassData
    {
        public TextureHandle srcTex;
        public Material material;
    }
    
    private class UpsamplePassData
    {
        public TextureHandle lowResTex;
        public Material material;
    }
    
    // <-- ADD THIS CLASS
    private class DebugPassData
    {
        public TextureHandle cameraColorTexture;
        public Material material;
    }
    
    private class StencilPassData
    {
        public RendererListHandle rendererListHandle;
    }
    
    private class TimePassData
    {
        public Vector4 timeVector;
        public bool enabled;
    }
    
    private class CompositePassData
    {
        public TextureHandle upsampled;
        public TextureHandle original;
        public Material material;
    }
    
    private class PalettePassData
    {
        public Material material;
    }
}