Shader "Hidden/PaletteQuantize"
{
    Properties
    {
        _PaletteTex("Palette Texture", 2D) = "white" {}
        _PaletteSize("Palette Size", Float) = 16
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        
        Stencil
        {
            Ref 65
            Comp Always
        }

        Pass
        {
            Name "PaletteQuantize"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            SAMPLER(sampler_point_clamp);
            TEXTURE2D(_PaletteTex);
            SAMPLER(sampler_PaletteTex);
            float _PaletteSize;

            half3 NearestPaletteColor(half3 color)
            {
                half3 nearest = half3(0, 0, 0);
                float nearestDist = 999999.0;

                for (int i = 0; i < (int)_PaletteSize; i++)
                {
                    float u = (i + 0.5) / _PaletteSize;
                    half3 paletteColor = SAMPLE_TEXTURE2D(_PaletteTex, sampler_PaletteTex, float2(u, 0.5)).rgb;

                    half3 diff = color - paletteColor;
                    float dist = dot(diff, diff);

                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = paletteColor;
                    }
                }

                return nearest;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_point_clamp, input.texcoord);
                color.rgb = NearestPaletteColor(color.rgb);
                return color;
            }
            ENDHLSL
        }
    }
}