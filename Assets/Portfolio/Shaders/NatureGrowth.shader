Shader "Custom/NatureGrowth"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _GrowthSpeed ("Growth Speed", Range(0, 5)) = 1.0
        _GrowthHeight ("Growth Height", Range(0, 10)) = 2.0
        _GrowthCycle ("Growth Cycle", Range(1, 10)) = 3.0
        _DissolveNoise ("Dissolve Noise", 2D) = "white" {}
        _DissolveStrength ("Dissolve Strength", Range(0, 1)) = 0.2
        _EdgeGlow ("Edge Glow", Color) = (0, 1, 0, 1)
        _EdgeWidth ("Edge Width", Range(0, 1)) = 0.1
    }
    
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" }
        LOD 100
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Cull Back
            
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
                float growthFactor : TEXCOORD2;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_DissolveNoise);
            SAMPLER(sampler_DissolveNoise);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _DissolveNoise_ST;
                float4 _Color;
                float4 _EdgeGlow;
                float _GrowthSpeed;
                float _GrowthHeight;
                float _GrowthCycle;
                float _DissolveStrength;
                float _EdgeWidth;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // Calculate growth factor based on time and height
                float timePhase = sin(_Time.y * _GrowthSpeed / _GrowthCycle) * 0.5 + 0.5;
                float heightNormalized = (input.positionOS.y + _GrowthHeight * 0.5) / _GrowthHeight;
                output.growthFactor = saturate(timePhase * 2.0 - heightNormalized);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Sample noise for dissolve effect
                half noise = SAMPLE_TEXTURE2D(_DissolveNoise, sampler_DissolveNoise, input.uv).r;
                
                // Calculate dissolve based on growth factor
                float dissolveThreshold = input.growthFactor - _DissolveStrength;
                float dissolve = dissolveThreshold + noise * _DissolveStrength;
                
                // Clip pixels below threshold
                clip(dissolve);
                
                // Edge glow effect
                float edgeGlow = 1.0 - smoothstep(0.0, _EdgeWidth, dissolve);
                col.rgb = lerp(col.rgb * _Color.rgb, _EdgeGlow.rgb, edgeGlow * _EdgeGlow.a);
                
                col *= input.color;
                
                return col;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float growthFactor : TEXCOORD1;
            };
            
            TEXTURE2D(_DissolveNoise);
            SAMPLER(sampler_DissolveNoise);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _DissolveNoise_ST;
                float _GrowthSpeed;
                float _GrowthHeight;
                float _GrowthCycle;
                float _DissolveStrength;
            CBUFFER_END
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _DissolveNoise);
                
                float timePhase = sin(_Time.y * _GrowthSpeed / _GrowthCycle) * 0.5 + 0.5;
                float heightNormalized = (input.positionOS.y + _GrowthHeight * 0.5) / _GrowthHeight;
                output.growthFactor = saturate(timePhase * 2.0 - heightNormalized);
                
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                half noise = SAMPLE_TEXTURE2D(_DissolveNoise, sampler_DissolveNoise, input.uv).r;
                float dissolveThreshold = input.growthFactor - _DissolveStrength;
                float dissolve = dissolveThreshold + noise * _DissolveStrength;
                
                clip(dissolve);
                return 0;
            }
            ENDHLSL
        }
    }
}