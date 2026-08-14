Shader "Custom/DisplayShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white"
        [Range(0.0, 1.0)] _GlobalAlpha("Global Alpha", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float2 _BrushPosition;
            float _BrushSize;
            float _AspectRatio;
            int _IsDrawing;

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float _GlobalAlpha;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                float2 uvDiff = IN.uv - _BrushPosition.xy;
                uvDiff.y /= max(_AspectRatio, 0.0001);

                half4 resultColor = 0.0;

                if (length(uvDiff) < _BrushSize)
                {
                    
                    if (length(uvDiff) > _BrushSize * 0.95)
                    {
                        half4 inverseCol = 1.0 - col;
                        resultColor = inverseCol;
                    }
                    else {
                        resultColor = col;
                    }
                    
                } 
                else {
                    resultColor = col;
                }

                resultColor.a *= _GlobalAlpha;
                return resultColor;
            }
            ENDHLSL
        }
    }
}
