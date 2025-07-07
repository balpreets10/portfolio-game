// Shader "Custom/LowPoly/RimLightToon"
Shader "Custom/RimLightToon"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.1, 8)) = 2.0
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 1.0
        _ToonSteps ("Toon Steps", Range(2, 10)) = 4
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
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float _ToonSteps;
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
                output.viewDirWS = GetWorldSpaceViewDir(output.positionWS);
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(cross(ddx(input.positionWS), ddy(input.positionWS)));
                float3 viewDir = normalize(input.viewDirWS);
                
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 color = texColor * _Color;
                
                Light mainLight = GetMainLight();
                float NdotL = dot(normal, mainLight.direction);
                
                // Toon shading
                float toonShade = floor(saturate(NdotL) * _ToonSteps) / _ToonSteps;
                
                // Rim lighting
                float rim = 1.0 - saturate(dot(viewDir, normal));
                rim = pow(rim, _RimPower);
                float3 rimColor = _RimColor.rgb * rim * _RimIntensity;
                
                color.rgb *= toonShade;
                color.rgb += rimColor;
                
                return color;
            }
            ENDHLSL
        }
    }
}