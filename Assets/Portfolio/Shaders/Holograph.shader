Shader "Custom/Holograph"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _HoloColor ("Holographic Color", Color) = (0, 1, 1, 1)
        _FresnelPower ("Fresnel Power", Range(0.1, 5)) = 2
        _ScanlineSpeed ("Scanline Speed", Range(0, 10)) = 2
        _ScanlineWidth ("Scanline Width", Range(0.01, 0.5)) = 0.1
        _GlitchIntensity ("Glitch Intensity", Range(0, 1)) = 0.1
        _EmissionIntensity ("Emission Intensity", Range(0, 5)) = 2
        _RimPower ("Rim Power", Range(0.1, 8)) = 3
        _FlickerSpeed ("Flicker Speed", Range(0, 20)) = 5
        _HologramAlpha ("Hologram Alpha", Range(0, 1)) = 0.8
        
        // URP Properties
        [Toggle(_ALPHATEST_ON)] _AlphaTest ("Alpha Test", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(_ALPHAPREMULTIPLY_ON)] _AlphaPremultiply ("Alpha Premultiply", Float) = 0
        
        // Blending
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        LOD 100
        
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        Cull [_Cull]
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // URP Keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTERED_RENDERING
            
            // Unity Keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                float fogCoord : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _HoloColor;
                half _FresnelPower;
                half _ScanlineSpeed;
                half _ScanlineWidth;
                half _GlitchIntensity;
                half _EmissionIntensity;
                half _RimPower;
                half _FlickerSpeed;
                half _HologramAlpha;
                half _Cutoff;
            CBUFFER_END
            
            // Noise function
            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // Scanline effect
            float scanlines(float2 uv)
            {
                float scanline = sin((uv.y + _Time.y * _ScanlineSpeed) * 50.0) * 0.5 + 0.5;
                return pow(scanline, 1.0 / _ScanlineWidth);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                // Glitch vertex displacement
                float glitchOffset = random(input.uv + _Time.y) * _GlitchIntensity * 0.01;
                input.positionOS.xyz += input.normalOS * glitchOffset;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // Base texture
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                // Normalize vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Fresnel effect
                half fresnel = 1.0 - saturate(dot(viewDirWS, normalWS));
                fresnel = pow(fresnel, _FresnelPower);
                
                // Rim lighting
                half rim = 1.0 - saturate(dot(viewDirWS, normalWS));
                rim = pow(rim, _RimPower);
                
                // Scanline effect
                half scanlineEffect = scanlines(input.uv);
                
                // Flicker effect
                half flicker = (sin(_Time.y * _FlickerSpeed) * 0.5 + 0.5) * 0.3 + 0.7;
                
                // Glitch effect on UV
                float2 glitchUV = input.uv;
                if (random(floor(input.uv.y * 20) + _Time.y) > 0.95)
                {
                    glitchUV.x += (random(input.uv.y + _Time.y) - 0.5) * _GlitchIntensity * 0.1;
                }
                
                // Holographic color mixing
                half4 holoCol = albedo * _HoloColor;
                holoCol.rgb += fresnel * _HoloColor.rgb * _EmissionIntensity;
                holoCol.rgb += rim * _HoloColor.rgb * 0.5;
                holoCol.rgb *= scanlineEffect;
                holoCol.rgb *= flicker;
                
                // Digital lines effect
                half lines = abs(sin(input.uv.x * 100)) < 0.1 ? 0.5 : 1.0;
                holoCol.rgb *= lines;
                
                // Transparency
                holoCol.a = _HologramAlpha * fresnel * scanlineEffect * flicker;
                
                // Apply fog
                holoCol.rgb = MixFog(holoCol.rgb, input.fogCoord);
                
                return holoCol;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Unlit"
}