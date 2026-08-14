Shader "Hidden/OverlayPaintShader"
{
    Properties
    {
        _BrushColor("Brush Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float2 _BrushPosition = float2(0.0, 0.0);
            float _BrushSize = 0.1;
            float _AspectRatio = 1.0;
            float4 _BrushColor = float4(1.0, 1.0, 1.0, 1.0);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = IN.positionOS;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 currentOverlay = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                float2 uvDiff = IN.uv - _BrushPosition;
                uvDiff.y /= max(_AspectRatio, 0.0001);

                if (length(uvDiff) < _BrushSize)
                {
                    return _BrushColor;
                }

                return currentOverlay;
            }

            ENDHLSL
        }
    }
}