Shader "Custom/SeasonalColorShift"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SpringColor ("Spring Color", Color) = (0.2, 0.8, 0.2, 1)
        _SummerColor ("Summer Color", Color) = (0.1, 0.6, 0.1, 1)
        _AutumnColor ("Autumn Color", Color) = (0.8, 0.4, 0.1, 1)
        _WinterColor ("Winter Color", Color) = (0.9, 0.9, 0.9, 1)
        _CycleSpeed ("Cycle Speed", Range(0, 2)) = 0.5
        _TransitionSmoothness ("Transition Smoothness", Range(0, 1)) = 0.3
        _ColorIntensity ("Color Intensity", Range(0, 2)) = 1.0
        _NoiseInfluence ("Noise Influence", Range(0, 1)) = 0.2
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
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _SpringColor;
                float4 _SummerColor;
                float4 _AutumnColor;
                float4 _WinterColor;
                float _CycleSpeed;
                float _TransitionSmoothness;
                float _ColorIntensity;
                float _NoiseInfluence;
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
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Calculate seasonal cycle (0-4 for four seasons)
                float time = _Time.y * _CycleSpeed;
                float seasonCycle = fmod(time, 4.0);
                
                // Add noise variation
                float noiseValue = noise(input.positionWS.xy * 0.5) * _NoiseInfluence;
                seasonCycle += noiseValue;
                
                float3 seasonalColor;
                
                if (seasonCycle < 1.0)
                {
                    // Spring to Summer
                    float t = smoothstep(0.0, 1.0, seasonCycle);
                    seasonalColor = lerp(_SpringColor.rgb, _SummerColor.rgb, t);
                }
                else if (seasonCycle < 2.0)
                {
                    // Summer to Autumn
                    float t = smoothstep(1.0, 2.0, seasonCycle);
                    seasonalColor = lerp(_SummerColor.rgb, _AutumnColor.rgb, t);
                }
                else if (seasonCycle < 3.0)
                {
                    // Autumn to Winter
                    float t = smoothstep(2.0, 3.0, seasonCycle);
                    seasonalColor = lerp(_AutumnColor.rgb, _WinterColor.rgb, t);
                }
                else
                {
                    // Winter to Spring
                    float t = smoothstep(3.0, 4.0, seasonCycle);
                    seasonalColor = lerp(_WinterColor.rgb, _SpringColor.rgb, t);
                }
                
                // Apply transition smoothness
                float transitionMask = 1.0 - _TransitionSmoothness;
                seasonalColor = pow(seasonalColor, transitionMask + 1.0);
                
                // Blend with base texture
                float3 finalColor = baseTex.rgb * seasonalColor * _ColorIntensity;
                finalColor *= input.color.rgb;
                
                return half4(finalColor, baseTex.a * input.color.a);
            }
            ENDHLSL
        }
    }
}