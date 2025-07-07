// Shader "Custom/LowPoly/GradientSky"
Shader "Custom/GradientSky"
{
    Properties
    {
        _HorizonColor ("Horizon Color", Color) = (1, 0.6, 0.3, 1)
        _ZenithColor ("Zenith Color", Color) = (0.2, 0.5, 1, 1)
        _SunColor ("Sun Color", Color) = (1, 1, 0.8, 1)
        _SunDirection ("Sun Direction", Vector) = (0.5, 0.5, 0.5, 0)
        _SunSize ("Sun Size", Range(0.01, 0.5)) = 0.1
        _SunIntensity ("Sun Intensity", Range(0, 5)) = 2.0
        _GradientPower ("Gradient Power", Range(0.1, 5)) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _HorizonColor;
                float4 _ZenithColor;
                float4 _SunColor;
                float4 _SunDirection;
                float _SunSize;
                float _SunIntensity;
                float _GradientPower;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.worldPos = input.positionOS.xyz;
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = normalize(input.worldPos);
                
                // Calculate gradient based on Y component
                float gradientFactor = pow(saturate(viewDir.y), _GradientPower);
                float4 skyColor = lerp(_HorizonColor, _ZenithColor, gradientFactor);
                
                // Add sun
                float3 sunDir = normalize(_SunDirection.xyz);
                float sunDot = dot(viewDir, sunDir);
                float sunMask = step(1.0 - _SunSize, sunDot);
                
                float4 finalColor = lerp(skyColor, _SunColor * _SunIntensity, sunMask);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}