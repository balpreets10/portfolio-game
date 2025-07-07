// Shader "Custom/LowPoly/Dissolve"
Shader "Custom/Dissolve"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0.5
        _EdgeWidth ("Edge Width", Range(0,0.3)) = 0.1
        _EdgeColor ("Edge Color", Color) = (1,0.5,0,1)
        _EdgeIntensity ("Edge Intensity", Range(0,5)) = 2.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        
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
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                float4 _Color;
                float4 _EdgeColor;
                float _DissolveAmount;
                float _EdgeWidth;
                float _EdgeIntensity;
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
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(cross(ddx(input.positionWS), ddy(input.positionWS)));
                
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 color = texColor * _Color;
                
                // Sample noise for dissolve
                float2 noiseUV = TRANSFORM_TEX(input.uv, _NoiseTex);
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                
                // Dissolve effect
                float dissolveEdge = _DissolveAmount + _EdgeWidth;
                
                // Clip pixels below dissolve threshold
                clip(noise - _DissolveAmount);
                
                // Edge glow
                float edge = 1.0 - smoothstep(_DissolveAmount, dissolveEdge, noise);
                float3 edgeColor = _EdgeColor.rgb * edge * _EdgeIntensity;
                
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normal, mainLight.direction));
                
                float3 lighting = mainLight.color * NdotL;
                float3 ambient = SampleSH(normal);
                
                color.rgb *= lighting + ambient;
                color.rgb += edgeColor;
                
                return color;
            }
            ENDHLSL
        }
    }
}