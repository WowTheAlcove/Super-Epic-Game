Shader "Hidden/WaterPixelize"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        // Pass 0: Downsample — only where stencil == 1
        Pass
        {
            Name "Downsample"

            Stencil
            {
                Ref 65
                Comp Equal
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            SAMPLER(sampler_point_clamp);

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_point_clamp, input.texcoord);
            }
            ENDHLSL
        }

        // Pass 1: Upsample — only where stencil == 1
        Pass
        {
            Name "Upsample"
            Stencil
            {
                Ref 65
                Comp Equal
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragPoint
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            SAMPLER(sampler_point_clamp);

            half4 FragPoint(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_point_clamp, input.texcoord);
            }
            ENDHLSL
        }
    }
}