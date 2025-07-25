Shader "Custom/ImplosionEffect"
{
    Properties
    {
        _ImplosionProgress ("Implosion Progress", Range(0, 1)) = 0
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _EnergyIntensity ("Energy Intensity", Range(0, 3)) = 1
        _BaseColor ("Base Color", Color) = (0.5, 0.8, 1, 1)
        _EmissionColor ("Emission Color", Color) = (0.8, 0.9, 1, 1)
        _NoiseScale ("Noise Scale", Float) = 5
        _NoiseTexture ("Noise Texture", 2D) = "white" {}
        _FresnelPower ("Fresnel Power", Range(0.1, 5)) = 2
        _PulseSpeed ("Pulse Speed", Float) = 2
        _PulseAmplitude ("Pulse Amplitude", Range(0, 1)) = 0.3
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float implosionFactor : TEXCOORD4;
            };
            
            // Properties
            CBUFFER_START(UnityPerMaterial)
                float _ImplosionProgress;
                float _DissolveAmount;
                float _EnergyIntensity;
                float4 _BaseColor;
                float4 _EmissionColor;
                float _NoiseScale;
                float _FresnelPower;
                float _PulseSpeed;
                float _PulseAmplitude;
            CBUFFER_END
            
            TEXTURE2D(_NoiseTexture);
            SAMPLER(sampler_NoiseTexture);
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Calculate implosion effect with smooth curve
                float implosionCurve = pow(_ImplosionProgress, 2);
                
                // Move vertices towards center (implosion effect)
                float3 centerOffset = float3(0, 0, 0) - input.positionOS.xyz;
                float3 implodedPosition = input.positionOS.xyz + centerOffset * implosionCurve;
                
                // Transform to world space
                output.positionWS = TransformObjectToWorld(implodedPosition);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                
                // Pass through UV coordinates
                output.uv = input.uv;
                
                // Calculate normal and view direction in world space
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(output.positionWS);
                
                // Pass implosion factor to fragment shader
                output.implosionFactor = implosionCurve;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample noise texture for dissolve effect
                float2 noiseUV = input.uv * _NoiseScale;
                float noise = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, noiseUV).r;
                
                // Dissolve effect - discard fragments based on noise
                float dissolveThreshold = 1.0 - _DissolveAmount;
                clip(noise - dissolveThreshold);
                
                // Calculate fresnel effect
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                fresnel = pow(fresnel, _FresnelPower);
                
                // Pulsing effect based on time
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseAmplitude + 1.0;
                
                // Combine energy effects
                float energyFactor = _EnergyIntensity * pulse;
                
                // Base color with energy modulation
                half4 baseColor = _BaseColor;
                baseColor.rgb *= energyFactor;
                
                // Emission color with fresnel and energy
                half3 emission = _EmissionColor.rgb * fresnel * energyFactor;
                
                // Add implosion intensity - stronger effect as implosion progresses
                float implosionIntensity = 1.0 + input.implosionFactor * 2.0;
                emission *= implosionIntensity;
                
                // Final color
                half4 finalColor;
                finalColor.rgb = baseColor.rgb + emission;
                
                // Alpha calculation
                float edgeAlpha = 1.0 - smoothstep(dissolveThreshold - 0.1, dissolveThreshold, noise);
                float fresnelAlpha = fresnel * 0.8 + 0.2;
                finalColor.a = baseColor.a * edgeAlpha * fresnelAlpha;
                
                // Increase alpha during implosion
                finalColor.a *= (1.0 + input.implosionFactor);
                
                return finalColor;
            }
            
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Unlit"
}