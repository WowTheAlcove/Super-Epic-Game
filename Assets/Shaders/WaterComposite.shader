Shader "Hidden/WaterComposite"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Composite"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            SAMPLER(sampler_point_clamp);
            TEXTURE2D(_UpsampledTex);
            TEXTURE2D(_OriginalTex);

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 upsampled = SAMPLE_TEXTURE2D(_UpsampledTex, sampler_point_clamp, input.texcoord);
                half4 original = SAMPLE_TEXTURE2D(_OriginalTex, sampler_point_clamp, input.texcoord);

                bool isSentinel = upsampled.r > 0.9 && upsampled.g < 0.1 && upsampled.b > 0.9;
                return isSentinel ? original : upsampled;
            }
            ENDHLSL
        }
    }
}