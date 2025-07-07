Shader "Custom/DissolveBoundary"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0.5
        _EdgeWidth ("Edge Width", Range(0, 0.1)) = 0.02
        _EdgeColor ("Edge Color", Color) = (1,0.5,0,1)
        _EdgeIntensity ("Edge Intensity", Range(0, 5)) = 2
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 1
        _DissolveSpeed ("Dissolve Speed", Range(0, 2)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogCoord : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _EdgeColor;
                float _DissolveAmount;
                float _EdgeWidth;
                float _EdgeIntensity;
                float _NoiseScale;
                float _DissolveSpeed;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogCoord = ComputeFogFactor(output.positionHCS.z);
                
                return output;
            }
            
            // Edge detection function
            float GetEdgeFactor(float2 uv)
            {
                // Sample noise at multiple points for edge detection
                float2 texelSize = 1.0 / _ScreenParams.xy;
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * _NoiseScale + _Time.y * _DissolveSpeed).r;
                
                // Sample surrounding pixels
                float noiseL = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (uv + float2(-texelSize.x, 0)) * _NoiseScale + _Time.y * _DissolveSpeed).r;
                float noiseR = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (uv + float2(texelSize.x, 0)) * _NoiseScale + _Time.y * _DissolveSpeed).r;
                float noiseU = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (uv + float2(0, texelSize.y)) * _NoiseScale + _Time.y * _DissolveSpeed).r;
                float noiseD = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (uv + float2(0, -texelSize.y)) * _NoiseScale + _Time.y * _DissolveSpeed).r;
                
                // Calculate edge intensity
                float edgeIntensity = abs(noise - noiseL) + abs(noise - noiseR) + abs(noise - noiseU) + abs(noise - noiseD);
                return edgeIntensity;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample main texture
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                
                // Sample noise texture with time-based animation
                float2 noiseUV = input.uv * _NoiseScale + _Time.y * _DissolveSpeed;
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                
                // Calculate dissolve threshold
                float dissolveThreshold = _DissolveAmount;
                
                // Edge detection
                float edgeFactor = GetEdgeFactor(input.uv);
                
                // Calculate dissolve mask
                float dissolveMask = noise - dissolveThreshold;
                
                // Create edge glow effect
                float edgeGlow = 0;
                if (dissolveMask > 0 && dissolveMask < _EdgeWidth)
                {
                    edgeGlow = (1 - dissolveMask / _EdgeWidth) * edgeFactor * _EdgeIntensity;
                }
                
                // Apply dissolve
                clip(dissolveMask);
                
                // Add edge color
                half3 finalColor = albedo.rgb + _EdgeColor.rgb * edgeGlow;
                
                // Calculate final alpha with pixelated edge effect
                float alpha = albedo.a;
                if (dissolveMask < _EdgeWidth)
                {
                    // Create pixelated transparency effect near edges
                    float pixelatedAlpha = step(0.5, frac(input.uv.x * 32) + frac(input.uv.y * 32));
                    alpha *= lerp(pixelatedAlpha, 1, dissolveMask / _EdgeWidth);
                }
                
                half4 finalColor4 = half4(finalColor, alpha);
                finalColor4.rgb = MixFog(finalColor4.rgb, input.fogCoord);
                
                return finalColor4;
            }
            ENDHLSL
        }
    }
    
    Fallback "Universal Render Pipeline/Unlit"
}