// Shader "Custom/LowPoly/FlatShaded"
Shader "Custom/FlatShaded"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
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
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Glossiness;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                // Calculate flat normals using cross product of face edges
                output.normalWS = normalize(cross(ddx(output.positionWS), ddy(output.positionWS)));
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Recalculate flat normals in fragment shader
                float3 normal = normalize(cross(ddx(input.positionWS), ddy(input.positionWS)));
                
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
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