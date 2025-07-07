Shader "Custom/MagicalParticleTrail"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _TrailTex ("Trail Texture", 2D) = "white" {}
        _TrailColor ("Trail Color", Color) = (1, 1, 0, 1)
        _TrailSpeed ("Trail Speed", Range(0, 5)) = 1.0
        _TrailSize ("Trail Size", Range(0, 0.5)) = 0.1
        _TrailFrequency ("Trail Frequency", Range(0, 20)) = 5.0
        _TrailIntensity ("Trail Intensity", Range(0, 5)) = 2.0
        _NoiseScale ("Noise Scale", Range(0, 10)) = 1.0
        _EmissionStrength ("Emission Strength", Range(0, 10)) = 3.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
                float2 trailUV : TEXCOORD2;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_TrailTex);
            SAMPLER(sampler_TrailTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TrailTex_ST;
                float4 _Color;
                float4 _TrailColor;
                float _TrailSpeed;
                float _TrailSize;
                float _TrailFrequency;
                float _TrailIntensity;
                float _NoiseScale;
                float _EmissionStrength;
            CBUFFER_END
            
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }
            
            float noise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // Calculate trail UV coordinates
                float time = _Time.y * _TrailSpeed;
                output.trailUV = float2(
                    input.uv.x + time,
                    input.uv.y + sin(time + input.uv.x * _TrailFrequency) * 0.1
                );
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Sample trail texture with animated UV
                half4 trail = SAMPLE_TEXTURE2D(_TrailTex, sampler_TrailTex, input.trailUV);
                
                // Create particle trail effect along edges
                float2 edgeUV = input.uv;
                float edgeDistance = min(min(edgeUV.x, 1.0 - edgeUV.x), min(edgeUV.y, 1.0 - edgeUV.y));
                float edgeMask = 1.0 - smoothstep(0.0, _TrailSize, edgeDistance);
                
                // Add noise to trail
                float noiseValue = noise(input.positionWS.xy * _NoiseScale + _Time.y * _TrailSpeed);
                float trailMask = edgeMask * trail.r * noiseValue;
                
                // Pulsing effect
                float pulse = sin(_Time.y * _TrailFrequency + input.positionWS.x + input.positionWS.z) * 0.5 + 0.5;
                trailMask *= pulse;
                
                // Combine base color with trail effect
                float3 baseColor = col.rgb * _Color.rgb;
                float3 trailEmission = _TrailColor.rgb * trailMask * _TrailIntensity;
                
                float3 finalColor = baseColor + trailEmission * _EmissionStrength;
                
                return half4(finalColor, col.a);
            }
            ENDHLSL
        }
    }
}