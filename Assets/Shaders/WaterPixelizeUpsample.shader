Shader "Hidden/WaterPixelizeUpsample"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off
        ZTest LEqual
        Cull Off
        Blend Off

        Pass
        {
            Name "UpsampleToMesh"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_WaterLowResTex);
            SAMPLER(sampler_point_clamp);

            float4 _WaterLowResTex_TexelSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.screenPos.xy / input.screenPos.w;
                return SAMPLE_TEXTURE2D(_WaterLowResTex, sampler_point_clamp, uv);
            }
            ENDHLSL
        }
    }
}