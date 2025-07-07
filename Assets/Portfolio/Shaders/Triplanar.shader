// Shader "Custom/LowPoly/Triplanar"
Shader "Custom/LowPoly/Triplanar"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Scale ("Texture Scale", Float) = 1.0
        _BlendSharpness ("Blend Sharpness", Range(1,10)) = 2.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Scale;
                float _BlendSharpness;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(cross(ddx(input.positionWS), ddy(input.positionWS)));
                float3 worldPos = input.positionWS;
                
                // Calculate blend weights based on normal
                float3 blendWeights = pow(abs(normal), _BlendSharpness);
                blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);
                
                // Sample texture from three directions
                float4 texX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, worldPos.zy * _Scale);
                float4 texY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, worldPos.xz * _Scale);
                float4 texZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, worldPos.xy * _Scale);
                
                // Blend the three samples
                float4 texColor = texX * blendWeights.x + texY * blendWeights.y + texZ * blendWeights.z;
                float4 color = texColor * _Color;
                
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normal, mainLight.direction));
                
                float3 lighting = mainLight.color * NdotL;
                float3 ambient = SampleSH(normal);
                
                color.rgb *= lighting + ambient;
                
                return color;
            }
            ENDHLSL
        }
    }
}