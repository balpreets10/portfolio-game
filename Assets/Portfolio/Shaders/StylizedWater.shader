// Shader "Custom/LowPoly/StylizedWater"
Shader "Custom/StylizedWater"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.2, 0.6, 1.0, 0.8)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveScale ("Wave Scale", Float) = 1.0
        _WaveHeight ("Wave Height", Float) = 0.1
        _FoamDistance ("Foam Distance", Float) = 0.5
        _Transparency ("Transparency", Range(0,1)) = 0.8
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _FoamColor;
                float _WaveSpeed;
                float _WaveScale;
                float _WaveHeight;
                float _FoamDistance;
                float _Transparency;
            CBUFFER_END
            
            float noise(float2 uv)
            {
                return sin(uv.x * 12.9898 + uv.y * 78.233) * 43758.5453;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Wave animation
                float wave = sin((input.positionOS.x + input.positionOS.z) * _WaveScale + _Time.y * _WaveSpeed) * _WaveHeight;
                float4 animatedPos = input.positionOS;
                animatedPos.y += wave;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(animatedPos.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.screenPos = vertexInput.positionNDC;
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;
                
                output.uv = input.uv;
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                
                // Sample depth for foam
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float depth = SampleSceneDepth(screenUV);
                float sceneZ = LinearEyeDepth(depth, _ZBufferParams);
                float surfaceZ = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float depthDifference = sceneZ - surfaceZ;
                
                // Foam calculation
                float foam = 1.0 - saturate(depthDifference / _FoamDistance);
                foam = pow(foam, 2.0);
                
                // Animated foam pattern
                float2 foamUV = input.uv + _Time.y * 0.1;
                float foamNoise = frac(sin(dot(foamUV, float2(12.9898, 78.233))) * 43758.5453);
                foam *= step(0.5, foamNoise);
                
                float4 waterColor = _Color;
                float4 finalColor = lerp(waterColor, _FoamColor, foam);
                
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normal, mainLight.direction));
                finalColor.rgb *= NdotL * 0.5 + 0.5;
                
                finalColor.a *= _Transparency;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}
