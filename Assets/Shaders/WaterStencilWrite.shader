Shader "Hidden/WaterStencilWrite"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest LEqual
        Cull Off
        ColorMask 0

        Stencil
        {
            Ref 65
            Comp Always
            Pass Replace
        }

        Pass
        {
            Name "StencilWrite"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 screenUV : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // Convert clip space to screen UV
                output.screenUV = output.positionCS.xy / output.positionCS.w * 0.5 + 0.5;
                #if UNITY_UV_STARTS_AT_TOP
                output.screenUV.y = 1.0 - output.screenUV.y;
                #endif
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 camColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, input.screenUV);
                if (camColor.a < 0.01)
                    discard;
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}