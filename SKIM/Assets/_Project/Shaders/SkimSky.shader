Shader "SKIM/GradientSky"
{
    Properties
    {
        _TopColor     ("Sky Top",     Color) = (0.027, 0.063, 0.125, 1)
        _HorizonColor ("Sky Horizon", Color) = (0.051, 0.165, 0.271, 1)
        _Exponent     ("Gradient Falloff", Range(0.1, 4)) = 1.4
        _MoonColor    ("Moon Color",  Color) = (0.910, 0.957, 1.0, 1)
        _MoonSize     ("Moon Size",   Range(0.001, 0.2)) = 0.035
        _MoonOpacity  ("Moon Opacity", Range(0, 1)) = 0.15
        _MoonDir      ("Moon Direction", Vector) = (-0.45, 0.22, 0.86, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            Name "SkimGradientSky"

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
                float3 viewDirOS  : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4  _TopColor;
                half4  _HorizonColor;
                half   _Exponent;
                half4  _MoonColor;
                half   _MoonSize;
                half   _MoonOpacity;
                float4 _MoonDir;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.viewDirOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.viewDirOS);

                // Vertical gradient: horizon at y=0 climbing to the deep sky above.
                half t = pow(saturate(dir.y), 1.0 / max(_Exponent, 0.001));
                half4 col = lerp(_HorizonColor, _TopColor, t);

                // Moon: a soft disc, no bloom, sitting low in the sky.
                float3 moon = normalize(_MoonDir.xyz);
                float d = distance(dir, moon);
                half disc = 1.0 - smoothstep(_MoonSize * 0.75, _MoonSize, d);
                col = lerp(col, _MoonColor, disc * _MoonOpacity);

                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
