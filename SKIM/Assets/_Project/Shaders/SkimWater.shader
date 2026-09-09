Shader "SKIM/Water"
{
    Properties
    {
        _SurfaceColor ("Surface Color", Color) = (0.102, 0.290, 0.416, 1)
        _DepthColor   ("Depth Color",   Color) = (0.039, 0.165, 0.271, 1)
        _CrestColor   ("Crest Color",   Color) = (0.0, 0.769, 0.800, 1)
        _HorizonColor ("Horizon Color", Color) = (0.051, 0.165, 0.271, 1)
        _WaveAmp      ("Wave Amplitude", Float) = 0.35
        _CrestBoost   ("Crest Boost", Range(0,1)) = 0.35
        _FadeStart    ("Horizon Fade Start", Float) = 12
        _FadeEnd      ("Horizon Fade End",   Float) = 45
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "SkimWaterUnlit"
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _SurfaceColor;
                half4 _DepthColor;
                half4 _CrestColor;
                half4 _HorizonColor;
                float _WaveAmp;
                half  _CrestBoost;
                float _FadeStart;
                float _FadeEnd;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(positionWS);

                // Height within the wave envelope: valley -> 0, crest -> 1.
                float amp = max(_WaveAmp, 0.001);
                float h = saturate(positionWS.y / (amp * 2.0) + 0.5);

                half4 col = lerp(_DepthColor, _SurfaceColor, h);

                // Bioluminescent rim on the crests only.
                half crest = saturate((h - 0.72) / 0.28);
                col = lerp(col, _CrestColor, crest * _CrestBoost);

                // Far water melts into the horizon so the plane has no hard edge.
                float dist = distance(positionWS, GetCameraPositionWS());
                float fade = saturate((dist - _FadeStart) / max(_FadeEnd - _FadeStart, 0.001));
                col = lerp(col, _HorizonColor, fade);

                OUT.color = col;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return IN.color;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
