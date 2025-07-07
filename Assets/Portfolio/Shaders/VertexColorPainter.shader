// Shader "Custom/LowPoly/VertexColorPainter"
Shader "Custom/VertexColorPainter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorStrength ("Color Strength", Range(0,2)) = 1.0
        _UseFlatShading ("Use Flat Shading", Float) = 1.0
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
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 color : COLOR;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _ColorStrength;
                float _UseFlatShading;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;
                
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                float3 normal = _UseFlatShading > 0.5 ? 
                    normalize(cross(ddx(input.positionWS), ddy(input.positionWS))) : 
                    normalize(input.normalWS);
                
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 vertexColor = input.color * _ColorStrength;
                
                float4 color = texColor * vertexColor;
                
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