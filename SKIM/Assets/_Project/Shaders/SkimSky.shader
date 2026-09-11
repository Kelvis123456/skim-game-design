Shader "SKIM/GradientSky"
{
    Properties
    {
        _TopColor     ("Sky Top",     Color) = (0.027, 0.063, 0.125, 1)
        _HorizonColor ("Sky Horizon", Color) = (0.051, 0.165, 0.271, 1)
        _Exponent     ("Gradient Falloff", Range(0.1, 4)) = 1.4
        _MoonColor    ("Moon Color",  Color) = (0.910, 0.957, 1.0, 1)
        _MoonSize     ("Moon Size",   Range(0.001, 0.4)) = 0.06
        _MoonOpacity  ("Moon Opacity", Range(0, 1)) = 0.55
        _MoonGlowSize ("Moon Glow Size", Range(0.001, 0.4)) = 0.18
        _MoonGlowOpacity ("Moon Glow Opacity", Range(0, 1)) = 0.15
        _MoonDir      ("Moon Direction", Vector) = (0.05, 0.22, 0.95, 0)
        _StarDensity  ("Star Density", Range(4, 60)) = 26
        _StarOpacity  ("Star Opacity", Range(0, 1)) = 0.7
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
                half   _MoonGlowSize;
                half   _MoonGlowOpacity;
                float4 _MoonDir;
                half   _StarDensity;
                half   _StarOpacity;
            CBUFFER_END

            // Cheap hash — no texture lookup, just enough noise to scatter stars.
            float hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

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

                // Stars: sparse, twinkling points above the horizon band only — a plain
                // dark gradient reads as nothing in particular, this is what makes it
                // unambiguously read as sky rather than more water.
                if (dir.y > 0.08)
                {
                    float3 cell = floor(dir * _StarDensity);
                    float h = hash13(cell);
                    float twinkle = 0.65 + 0.35 * sin(_Time.y * (2.0 + h * 3.0) + h * 20.0);
                    float star = smoothstep(0.982, 1.0, h) * twinkle * saturate((dir.y - 0.08) * 4.0);
                    col.rgb += star * _StarOpacity;
                }

                // Moon: a bright core plus a soft wider glow, no bloom post-process.
                float3 moon = normalize(_MoonDir.xyz);
                float d = distance(dir, moon);
                half glow = 1.0 - smoothstep(0.0, _MoonGlowSize, d);
                col = lerp(col, _MoonColor, glow * glow * _MoonGlowOpacity);
                half disc = 1.0 - smoothstep(_MoonSize * 0.75, _MoonSize, d);
                col = lerp(col, _MoonColor, disc * _MoonOpacity);

                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
