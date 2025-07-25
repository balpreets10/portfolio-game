Shader "Custom/EnergyParticle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 5)) = 1
        _GlowPower ("Glow Power", Range(0.1, 10)) = 2
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 3
        _DistortionStrength ("Distortion", Range(0, 1)) = 0.1
    }
    
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent+100"
            "IgnoreProjector"="True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_particles
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _GlowPower;
                float _PulseSpeed;
                float _DistortionStrength;
            CBUFFER_END
            
            v2f vert (appdata v)
            {
                v2f o;
                
                // Apply slight distortion based on time
                float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                float distortion = sin(_Time.y * _PulseSpeed + worldPos.x + worldPos.z) * _DistortionStrength;
                v.vertex.xyz += v.normal * distortion;
                
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.color = v.color;
                o.uv = v.uv;
                o.worldPos = worldPos;
                o.normal = TransformObjectToWorldNormal(v.normal);
                o.fogFactor = ComputeFogFactor(o.pos.z);
                
                return o;
            }
            
            half4 frag (v2f i) : SV_Target
            {
                // Sample texture
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                
                // Create energy pulse effect
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                float dynamicIntensity = _Intensity * (0.7 + pulse * 0.3);
                
                // Distance from center for radial falloff
                float2 center = i.uv - 0.5;
                float distFromCenter = length(center);
                
                // Create glow effect
                float glow = pow(1.0 - saturate(distFromCenter * 2), _GlowPower);
                
                // Energy swirl effect
                float angle = atan2(center.y, center.x) + _Time.y * 2;
                float swirl = sin(angle * 3 + distFromCenter * 10) * 0.1 + 0.9;
                
                // Combine effects
                half4 finalColor = tex * i.color * _Color;
                finalColor.rgb *= dynamicIntensity * glow * swirl;
                finalColor.a *= glow;
                
                // Apply fog
                finalColor.rgb = MixFog(finalColor.rgb, i.fogFactor);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}