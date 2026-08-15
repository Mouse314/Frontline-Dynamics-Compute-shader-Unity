Shader "Custom/DisplayShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [Texture] _MapTexture("Map Texture", 2D) = "white" {}

        [Color] _WaterColor("Water Color", Color) = (0, 0.5, 1, 1)
        [Range(0.0, 1.0)] _WaterCutoffTolerance("Water Cutoff", Float) = 0.1

        [Range(0.0, 1.0)] _GlobalAlpha("Global Alpha", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha, One Zero

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
            TEXTURE2D(_MapTexture);
            SAMPLER(sampler_MapTexture);

            float2 _BrushPosition;
            float _BrushSize;
            float _AspectRatio;
            float4 _WaterColor;

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float _GlobalAlpha;
                float _WaterCutoffTolerance;
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
                float sqrDistance = dot(uvDiff, uvDiff);

                float outerRadiusSq = _BrushSize * _BrushSize;
                float innerRadiusSq = outerRadiusSq * 0.95;

                float isInsideBrush = step(sqrDistance, outerRadiusSq);
                float isBorder = step(innerRadiusSq, sqrDistance) * isInsideBrush;

                half4 mapCol = SAMPLE_TEXTURE2D(_MapTexture, sampler_MapTexture, IN.uv); 
                
                half3 colorDiff = mapCol.rgb - _WaterColor.rgb;
                float sqrColorDist = dot(colorDiff, colorDiff);
                float isWater = step(sqrColorDist, _WaterCutoffTolerance * _WaterCutoffTolerance);

                half4 inverseCol = 1.0 - col * float4(0.9, 0.99, 1.0, 0.0);
                
                half4 waterCutoffColor = col * (1.0 - isWater); 

                half4 resultColor = lerp(waterCutoffColor, inverseCol, isBorder);
                resultColor = lerp(col, resultColor, isInsideBrush);

                resultColor = lerp(col * (1.0 - isWater), resultColor, isInsideBrush);
                resultColor = lerp(resultColor, inverseCol, isBorder);

                resultColor.a *= _GlobalAlpha;
                return resultColor;
            }

            ENDHLSL
        }
    }
}
