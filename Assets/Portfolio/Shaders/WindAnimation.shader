Shader "Custom/WindAnimation"
{
    Properties
    {
        [Header(Base Properties)]
        _MainTex ("Albedo Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic ("Metallic", Range(0.0, 1.0)) = 0.0
        
        [Header(Wind Animation)]
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0, 0)
        _WindStrength ("Wind Strength", Range(0, 5)) = 1.0
        _WindSpeed ("Wind Speed", Range(0, 10)) = 2.0
        _WindTurbulence ("Wind Turbulence", Range(0, 3)) = 0.5
        
        [Header(Vertex Animation)]
        _SwayAmount ("Sway Amount", Range(0, 2)) = 0.5
        _SwaySpeed ("Sway Speed", Range(0, 5)) = 1.0
        _SwayRandomness ("Sway Randomness", Range(0, 1)) = 0.3
        _HeightInfluence ("Height Influence", Range(0, 2)) = 1.0
        
        [Header(Advanced Wind)]
        _GustStrength ("Gust Strength", Range(0, 2)) = 0.3
        _GustFrequency ("Gust Frequency", Range(0, 5)) = 1.0
        _LeafFlutter ("Leaf Flutter", Range(0, 1)) = 0.2
        _BranchBending ("Branch Bending", Range(0, 1)) = 0.4
        
        [Header(Noise)]
        _NoiseScale ("Noise Scale", Range(0.01, 1)) = 0.1
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.2
        
        [Header(Rendering)]
        [Toggle] _AlphaTest ("Alpha Test", Float) = 1
        [Toggle] _DoubleSided ("Double Sided", Float) = 1
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
            
            Cull [_DoubleSided]
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _DOUBLESIDED_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float4 color : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Cutoff;
                float _Smoothness;
                float _Metallic;
                
                float4 _WindDirection;
                float _WindStrength;
                float _WindSpeed;
                float _WindTurbulence;
                
                float _SwayAmount;
                float _SwaySpeed;
                float _SwayRandomness;
                float _HeightInfluence;
                
                float _GustStrength;
                float _GustFrequency;
                float _LeafFlutter;
                float _BranchBending;
                
                float _NoiseScale;
                float _NoiseStrength;
                
                float _AlphaTest;
                float _DoubleSided;
            CBUFFER_END
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            // Noise functions
            float hash(float n) 
            {
                return frac(sin(n) * 43758.5453123);
            }
            
            float noise(float3 x) 
            {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                
                float n = p.x + p.y * 57.0 + 113.0 * p.z;
                return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
                               lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
                           lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                               lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
            }
            
            float fbm(float3 x) 
            {
                float v = 0.0;
                float a = 0.5;
                float3 shift = float3(100, 100, 100);
                
                for (int i = 0; i < 4; ++i) 
                {
                    v += a * noise(x);
                    x = x * 2.0 + shift;
                    a *= 0.5;
                }
                return v;
            }
            
            // Wind calculation function
            float3 CalculateWind(float3 worldPos, float3 objectPos, float vertexHeight, float time)
            {
                float3 windDir = normalize(_WindDirection.xyz);
                
                // Base wind wave
                float windWave = sin(time * _WindSpeed + worldPos.x * 0.1 + worldPos.z * 0.1) * 0.5 + 0.5;
                
                // Turbulence using noise
                float3 noisePos = worldPos * _NoiseScale + float3(time * _WindSpeed * 0.1, 0, time * _WindSpeed * 0.1);
                float windNoise = fbm(noisePos);
                
                // Gust effect
                float gustWave = sin(time * _GustFrequency + worldPos.x * 0.05) * 0.5 + 0.5;
                float gust = pow(gustWave, 3.0) * _GustStrength;
                
                // Combine wind effects
                float windIntensity = _WindStrength * (windWave + windNoise * _WindTurbulence + gust);
                
                // Height-based influence (higher vertices sway more)
                float heightFactor = pow(vertexHeight, _HeightInfluence);
                
                // Sway motion
                float swayX = sin(time * _SwaySpeed + objectPos.x * 10.0 + windNoise * _SwayRandomness) * _SwayAmount;
                float swayZ = cos(time * _SwaySpeed * 0.7 + objectPos.z * 8.0 + windNoise * _SwayRandomness) * _SwayAmount;
                
                // Leaf flutter (high frequency small movement)
                float flutter = sin(time * _SwaySpeed * 4.0 + worldPos.x * 20.0 + worldPos.z * 15.0) * _LeafFlutter;
                
                // Branch bending (lower frequency, larger movement)
                float bend = sin(time * _SwaySpeed * 0.5 + worldPos.x * 2.0) * _BranchBending;
                
                // Combine all movements
                float3 windOffset = windDir * windIntensity * heightFactor;
                windOffset.x += (swayX + flutter) * heightFactor;
                windOffset.z += (swayZ + flutter) * heightFactor;
                windOffset.y += bend * heightFactor * 0.2; // Slight vertical movement
                
                return windOffset;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 objectOrigin = TransformObjectToWorld(float3(0, 0, 0));
                
                // Calculate vertex height (normalized 0-1 based on vertex color or position)
                float vertexHeight = max(input.color.r, input.positionOS.y + 0.5); // Use red channel or Y position
                
                // Apply wind animation
                float3 windOffset = CalculateWind(positionWS, objectOrigin, vertexHeight, _Time.y);
                positionWS += windOffset;
                
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                
                // Transform normals and tangents
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
                
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color;
                
                // Shadow coordinates
                output.shadowCoord = TransformWorldToShadowCoord(positionWS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // Sample main texture
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                
                // Alpha test
                #if defined(_ALPHATEST_ON)
                    clip(albedo.a - _Cutoff);
                #endif
                
                // Setup lighting data
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalize(input.normalWS);
                lightingInput.viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                lightingInput.shadowCoord = input.shadowCoord;
                lightingInput.fogCoord = 0;
                lightingInput.vertexLighting = half3(0, 0, 0);
                lightingInput.bakedGI = half3(0, 0, 0);
                lightingInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lightingInput.shadowMask = half4(1, 1, 1, 1);
                
                // Setup surface data
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.alpha = albedo.a;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = half3(0, 0, 0);
                surfaceData.occlusion = 1.0;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;
                
                // Calculate final color using URP lighting
                half4 color = UniversalFragmentPBR(lightingInput, surfaceData);
                
                return color;
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
            Cull [_DoubleSided]
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Cutoff;
                float4 _WindDirection;
                float _WindStrength;
                float _WindSpeed;
                float _WindTurbulence;
                float _SwayAmount;
                float _SwaySpeed;
                float _SwayRandomness;
                float _HeightInfluence;
                float _GustStrength;
                float _GustFrequency;
                float _LeafFlutter;
                float _BranchBending;
                float _NoiseScale;
                float _NoiseStrength;
            CBUFFER_END
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            // Include the same wind calculation functions from the main pass
            float hash(float n) 
            {
                return frac(sin(n) * 43758.5453123);
            }
            
            float noise(float3 x) 
            {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                
                float n = p.x + p.y * 57.0 + 113.0 * p.z;
                return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
                               lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
                           lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                               lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
            }
            
            float fbm(float3 x) 
            {
                float v = 0.0;
                float a = 0.5;
                float3 shift = float3(100, 100, 100);
                
                for (int i = 0; i < 4; ++i) 
                {
                    v += a * noise(x);
                    x = x * 2.0 + shift;
                    a *= 0.5;
                }
                return v;
            }
            
            float3 CalculateWind(float3 worldPos, float3 objectPos, float vertexHeight, float time)
            {
                float3 windDir = normalize(_WindDirection.xyz);
                float windWave = sin(time * _WindSpeed + worldPos.x * 0.1 + worldPos.z * 0.1) * 0.5 + 0.5;
                float3 noisePos = worldPos * _NoiseScale + float3(time * _WindSpeed * 0.1, 0, time * _WindSpeed * 0.1);
                float windNoise = fbm(noisePos);
                float gustWave = sin(time * _GustFrequency + worldPos.x * 0.05) * 0.5 + 0.5;
                float gust = pow(gustWave, 3.0) * _GustStrength;
                float windIntensity = _WindStrength * (windWave + windNoise * _WindTurbulence + gust);
                float heightFactor = pow(vertexHeight, _HeightInfluence);
                float swayX = sin(time * _SwaySpeed + objectPos.x * 10.0 + windNoise * _SwayRandomness) * _SwayAmount;
                float swayZ = cos(time * _SwaySpeed * 0.7 + objectPos.z * 8.0 + windNoise * _SwayRandomness) * _SwayAmount;
                float flutter = sin(time * _SwaySpeed * 4.0 + worldPos.x * 20.0 + worldPos.z * 15.0) * _LeafFlutter;
                float bend = sin(time * _SwaySpeed * 0.5 + worldPos.x * 2.0) * _BranchBending;
                float3 windOffset = windDir * windIntensity * heightFactor;
                windOffset.x += (swayX + flutter) * heightFactor;
                windOffset.z += (swayZ + flutter) * heightFactor;
                windOffset.y += bend * heightFactor * 0.2;
                return windOffset;
            }
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 objectOrigin = TransformObjectToWorld(float3(0, 0, 0));
                float vertexHeight = max(input.color.r, input.positionOS.y + 0.5);
                
                // Apply the same wind animation as the main pass
                float3 windOffset = CalculateWind(positionWS, objectOrigin, vertexHeight, _Time.y);
                positionWS += windOffset;
                
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                #if defined(_ALPHATEST_ON)
                    half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                    clip(albedo.a - _Cutoff);
                #endif
                
                return 0;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask 0
            Cull [_DoubleSided]
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Cutoff;
                float4 _WindDirection;
                float _WindStrength;
                float _WindSpeed;
                float _WindTurbulence;
                float _SwayAmount;
                float _SwaySpeed;
                float _SwayRandomness;
                float _HeightInfluence;
                float _GustStrength;
                float _GustFrequency;
                float _LeafFlutter;
                float _BranchBending;
                float _NoiseScale;
                float _NoiseStrength;
            CBUFFER_END
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            // Wind functions (same as above)
            float hash(float n) { return frac(sin(n) * 43758.5453123); }
            float noise(float3 x) 
            {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                float n = p.x + p.y * 57.0 + 113.0 * p.z;
                return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
                               lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
                           lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                               lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
            }
            float fbm(float3 x) 
            {
                float v = 0.0;
                float a = 0.5;
                float3 shift = float3(100, 100, 100);
                for (int i = 0; i < 4; ++i) 
                {
                    v += a * noise(x);
                    x = x * 2.0 + shift;
                    a *= 0.5;
                }
                return v;
            }
            float3 CalculateWind(float3 worldPos, float3 objectPos, float vertexHeight, float time)
            {
                float3 windDir = normalize(_WindDirection.xyz);
                float windWave = sin(time * _WindSpeed + worldPos.x * 0.1 + worldPos.z * 0.1) * 0.5 + 0.5;
                float3 noisePos = worldPos * _NoiseScale + float3(time * _WindSpeed * 0.1, 0, time * _WindSpeed * 0.1);
                float windNoise = fbm(noisePos);
                float gustWave = sin(time * _GustFrequency + worldPos.x * 0.05) * 0.5 + 0.5;
                float gust = pow(gustWave, 3.0) * _GustStrength;
                float windIntensity = _WindStrength * (windWave + windNoise * _WindTurbulence + gust);
                float heightFactor = pow(vertexHeight, _HeightInfluence);
                float swayX = sin(time * _SwaySpeed + objectPos.x * 10.0 + windNoise * _SwayRandomness) * _SwayAmount;
                float swayZ = cos(time * _SwaySpeed * 0.7 + objectPos.z * 8.0 + windNoise * _SwayRandomness) * _SwayAmount;
                float flutter = sin(time * _SwaySpeed * 4.0 + worldPos.x * 20.0 + worldPos.z * 15.0) * _LeafFlutter;
                float bend = sin(time * _SwaySpeed * 0.5 + worldPos.x * 2.0) * _BranchBending;
                float3 windOffset = windDir * windIntensity * heightFactor;
                windOffset.x += (swayX + flutter) * heightFactor;
                windOffset.z += (swayZ + flutter) * heightFactor;
                windOffset.y += bend * heightFactor * 0.2;
                return windOffset;
            }
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 objectOrigin = TransformObjectToWorld(float3(0, 0, 0));
                float vertexHeight = max(input.color.r, input.positionOS.y + 0.5);
                
                float3 windOffset = CalculateWind(positionWS, objectOrigin, vertexHeight, _Time.y);
                positionWS += windOffset;
                
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                
                return output;
            }
            
            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                #if defined(_ALPHATEST_ON)
                    half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                    clip(albedo.a - _Cutoff);
                #endif
                
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}