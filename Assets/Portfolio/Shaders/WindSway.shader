Shader "Custom/WindSway"
{
    Properties
    {
        [Header(Base Properties)]
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        
        [Header(Wind Properties)]
        _WindStrength ("Wind Strength", Range(0, 5)) = 1
        _WindSpeed ("Wind Speed", Range(0, 10)) = 1
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0, 0)
        
        [Header(Sway Properties)]
        _SwayHeight ("Sway Height (0=none, 1=full)", Range(0, 1)) = 0.5
        _SwayType ("Sway Type (0=continuous, 1=random)", Range(0, 1)) = 0
        _SwayFrequency ("Sway Frequency", Range(0.1, 5)) = 1
        _SwayRandomness ("Random Sway Intensity", Range(0, 2)) = 0.5
        
        [Header(Advanced Sway)]
        _BendStiffness ("Bend Stiffness", Range(0.1, 2)) = 1
        _TurbulenceScale ("Turbulence Scale", Range(0.1, 5)) = 1
        _HeightGradient ("Height Gradient Power", Range(0.5, 3)) = 1.5
        
        [Header(Object Bounds)]
        _ObjectHeight ("Object Height", Float) = 1.0
        _PivotOffset ("Pivot Offset Y", Float) = 0.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Off
            AlphaToMask On
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 color : COLOR;
                float fogCoord : TEXCOORD3;
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
                float _WindStrength;
                float _WindSpeed;
                float4 _WindDirection;
                float _SwayHeight;
                float _SwayType;
                float _SwayFrequency;
                float _SwayRandomness;
                float _BendStiffness;
                float _TurbulenceScale;
                float _HeightGradient;
                float _ObjectHeight;
                float _PivotOffset;
            CBUFFER_END
            
            // Noise function for wind turbulence
            float3 hash33(float3 p3)
            {
                p3 = frac(p3 * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yxz + 33.33);
                return frac((p3.xxy + p3.yxx) * p3.zyx);
            }
            
            float noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                return lerp(lerp(lerp(hash33(i + float3(0,0,0)).x, hash33(i + float3(1,0,0)).x, f.x),
                               lerp(hash33(i + float3(0,1,0)).x, hash33(i + float3(1,1,0)).x, f.x), f.y),
                           lerp(lerp(hash33(i + float3(0,0,1)).x, hash33(i + float3(1,0,1)).x, f.x),
                               lerp(hash33(i + float3(0,1,1)).x, hash33(i + float3(1,1,1)).x, f.x), f.y), f.z);
            }
            
            float3 CalculateWindSway(float3 positionOS, float3 positionWS, float vertexHeight)
            {
                // Calculate normalized height from bottom (0) to top (1)
                float normalizedHeight = saturate((vertexHeight - _PivotOffset) / _ObjectHeight);
                
                // Calculate which part of the object should be affected
                float affectedThreshold = 1.0 - _SwayHeight; // If SwayHeight = 0.5, only top 50% is affected
                
                // Only affect vertices above the threshold
                if (normalizedHeight < affectedThreshold)
                    return float3(0, 0, 0);
                
                // Calculate influence based on height within the affected region
                float heightInfluence = (normalizedHeight - affectedThreshold) / _SwayHeight;
                heightInfluence = pow(heightInfluence, _HeightGradient);
                
                float time = _Time.y * _WindSpeed;
                float3 worldPos = positionWS;
                
                // Base wind direction
                float3 windDir = normalize(_WindDirection.xyz);
                
                // Continuous wind sway
                float continuousSway = sin(time * _SwayFrequency) * 0.6 + 
                                      sin(time * _SwayFrequency * 1.7) * 0.3 +
                                      sin(time * _SwayFrequency * 2.3) * 0.1;
                
                // Random wind sway using noise
                float3 noisePos = worldPos * _TurbulenceScale + time * 0.5;
                float randomNoise = noise(noisePos) * 2.0 - 1.0;
                float randomNoise2 = noise(noisePos + 100.0) * 2.0 - 1.0;
                float randomNoise3 = noise(noisePos + 200.0) * 2.0 - 1.0;
                
                float3 randomSway = float3(randomNoise, 0, randomNoise2) * _SwayRandomness;
                randomSway.y = randomNoise3 * _SwayRandomness * 0.05; // Very small vertical movement
                
                // Blend between continuous and random based on _SwayType
                float3 finalSway = lerp(
                    windDir * continuousSway + float3(sin(time * _SwayFrequency * 1.3), 0, cos(time * _SwayFrequency * 1.1)) * 0.1,
                    randomSway,
                    _SwayType
                );
                
                // Apply wind strength and height influence
                finalSway *= _WindStrength * heightInfluence;
                
                // Apply bend stiffness
                finalSway /= _BendStiffness;
                
                return finalSway;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Use the actual vertex height
                float vertexHeight = input.positionOS.y;
                
                // Calculate world position before applying sway
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // Calculate wind sway
                float3 sway = CalculateWindSway(input.positionOS.xyz, positionWS, vertexHeight);
                
                // Apply sway to vertex position
                float3 swayedPositionOS = input.positionOS.xyz + TransformWorldToObjectDir(sway);
                
                // Transform to clip space
                output.positionCS = TransformObjectToHClip(swayedPositionOS);
                output.positionWS = TransformObjectToWorld(swayedPositionOS);
                
                // Transform normal
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // UV and color
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                
                // Fog
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample base texture
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                baseColor *= _BaseColor * input.color;
                
                // Alpha test
                clip(baseColor.a - _Cutoff);
                
                // Simple lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 lighting = mainLight.color * NdotL + SampleSH(input.normalWS);
                
                baseColor.rgb *= lighting;
                
                // Apply fog
                baseColor.rgb = MixFog(baseColor.rgb, input.fogCoord);
                
                return baseColor;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
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
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
                float _WindStrength;
                float _WindSpeed;
                float4 _WindDirection;
                float _SwayHeight;
                float _SwayType;
                float _SwayFrequency;
                float _SwayRandomness;
                float _BendStiffness;
                float _TurbulenceScale;
                float _HeightGradient;
                float _ObjectHeight;
                float _PivotOffset;
            CBUFFER_END
            
            // Include the same wind calculation functions
            float3 hash33(float3 p3)
            {
                p3 = frac(p3 * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yxz + 33.33);
                return frac((p3.xxy + p3.yxx) * p3.zyx);
            }
            
            float noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                return lerp(lerp(lerp(hash33(i + float3(0,0,0)).x, hash33(i + float3(1,0,0)).x, f.x),
                               lerp(hash33(i + float3(0,1,0)).x, hash33(i + float3(1,1,0)).x, f.x), f.y),
                           lerp(lerp(hash33(i + float3(0,0,1)).x, hash33(i + float3(1,0,1)).x, f.x),
                               lerp(hash33(i + float3(0,1,1)).x, hash33(i + float3(1,1,1)).x, f.x), f.y), f.z);
            }
            
            float3 CalculateWindSway(float3 positionOS, float3 positionWS, float vertexHeight)
            {
                float normalizedHeight = saturate((vertexHeight - _PivotOffset) / _ObjectHeight);
                float affectedThreshold = 1.0 - _SwayHeight;
                
                if (normalizedHeight < affectedThreshold)
                    return float3(0, 0, 0);
                
                float heightInfluence = (normalizedHeight - affectedThreshold) / _SwayHeight;
                heightInfluence = pow(heightInfluence, _HeightGradient);
                
                float time = _Time.y * _WindSpeed;
                float3 worldPos = positionWS;
                
                float3 windDir = normalize(_WindDirection.xyz);
                
                float continuousSway = sin(time * _SwayFrequency) * 0.6 + 
                                      sin(time * _SwayFrequency * 1.7) * 0.3 +
                                      sin(time * _SwayFrequency * 2.3) * 0.1;
                
                float3 noisePos = worldPos * _TurbulenceScale + time * 0.5;
                float randomNoise = noise(noisePos) * 2.0 - 1.0;
                float randomNoise2 = noise(noisePos + 100.0) * 2.0 - 1.0;
                float randomNoise3 = noise(noisePos + 200.0) * 2.0 - 1.0;
                
                float3 randomSway = float3(randomNoise, 0, randomNoise2) * _SwayRandomness;
                randomSway.y = randomNoise3 * _SwayRandomness * 0.05;
                
                float3 finalSway = lerp(
                    windDir * continuousSway + float3(sin(time * _SwayFrequency * 1.3), 0, cos(time * _SwayFrequency * 1.1)) * 0.1,
                    randomSway,
                    _SwayType
                );
                
                finalSway *= _WindStrength * heightInfluence;
                finalSway /= _BendStiffness;
                
                return finalSway;
            }
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                
                float vertexHeight = input.positionOS.y;
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 sway = CalculateWindSway(input.positionOS.xyz, positionWS, vertexHeight);
                float3 swayedPositionOS = input.positionOS.xyz + TransformWorldToObjectDir(sway);
                
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                float3 swayedPositionWS = TransformObjectToWorld(swayedPositionOS);
                float4 clipPos = TransformWorldToHClip(ApplyShadowBias(swayedPositionWS, normalWS, _MainLightPosition.xyz));
                
                #if UNITY_REVERSED_Z
                    clipPos.z = min(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    clipPos.z = max(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                output.positionCS = clipPos;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(baseColor.a - _Cutoff);
                return 0;
            }
            
            ENDHLSL
        }
    }
}