Shader "PetualanganIK/CyberNeonPulse"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0, 0.8, 1, 1)
        _EmissionColor ("Emission Color", Color) = (0, 4, 6, 1)
        _PulseSpeed ("Pulse Speed", Float) = 2
        _PulseStrength ("Pulse Strength", Range(0, 2)) = 0.45
        _LineFrequency ("Line Frequency", Float) = 8
        _LineSharpness ("Line Sharpness", Range(1, 32)) = 14
        _BaseBoost ("Base Boost", Range(0, 2)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "CyberNeonPulse"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float _PulseSpeed;
                float _PulseStrength;
                float _LineFrequency;
                float _LineSharpness;
                float _BaseBoost;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 gridUV = input.positionWS.xz * max(_LineFrequency, 0.001);
                float gridX = 1.0 - abs(frac(gridUV.x) - 0.5) * 2.0;
                float gridZ = 1.0 - abs(frac(gridUV.y) - 0.5) * 2.0;
                float grid = saturate(pow(max(gridX, gridZ), max(_LineSharpness, 1.0)));
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed + input.positionWS.y * 2.7) * _PulseStrength;
                float3 color = _BaseColor.rgb * _BaseBoost + _EmissionColor.rgb * (0.65 + grid * 0.75) * pulse;
                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
