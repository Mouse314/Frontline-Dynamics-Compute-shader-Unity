Shader "Custom/DrawShader"
{
    Properties
    {
        [MainColor] _BrushColor("Brush Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Main Texture", 2D) = "white" {}
        _BrushPosition("Brush Position", Vector) = (0, 0, 0, 0)
        _BrushSize("Brush Size", Float) = 0.1
        _AspectRatio("Aspect Ratio", Float) = 1.0
        _IsDrawing("Is Drawing", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha , One Zero
        
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
                float3 worldPosition : TEXCOORD1;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _BrushPosition;
                float _BrushSize;
                float _AspectRatio;
                float _IsDrawing;
                float _IsErasing;
                float4 _BrushColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.worldPosition = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                float2 uvDiff = IN.uv - _BrushPosition.xy;
                uvDiff.y /= max(_AspectRatio, 0.0001);

                half4 resultColor = 0.0;

                float sqrDistance = dot(uvDiff, uvDiff);

                if (sqrDistance < _BrushSize * _BrushSize)
                {
                    if (_IsDrawing < 0.5)
                    {
                        resultColor = col;
                    } 
                    else {
                        if (_IsErasing > 0.5) {
                            resultColor = float4(1, 1, 1, 0.0);
                        } 
                        else {
                            resultColor = _BrushColor;
                        }
                    }
                } 
                else {
                    resultColor = col;
                }

                return resultColor;
            }
            ENDHLSL
        }
    }
}
