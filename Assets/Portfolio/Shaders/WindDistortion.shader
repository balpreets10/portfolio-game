Shader "Custom/WindDistortion"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DistortionIntensity ("Distortion Intensity", Range(0, 1)) = 0.5
        _WindSpeed ("Wind Speed", Float) = 1.0
        _WindIntensity ("Wind Intensity", Range(0, 2)) = 1.0
        _Color ("Color", Color) = (1,1,1,0.5)
        _NoiseScale ("Noise Scale", Float) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _DistortionIntensity;
            float _WindSpeed;
            float _WindIntensity;
            float4 _Color;
            float _NoiseScale;
            
            // Simple noise function
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Create wind distortion effect
                float2 windUV = i.uv * _NoiseScale + _Time.y * _WindSpeed;
                float windNoise = noise(windUV);
                
                // Apply distortion to UV coordinates
                float2 distortedUV = i.uv + windNoise * _DistortionIntensity * 0.1;
                
                // Sample texture with distorted coordinates
                fixed4 col = tex2D(_MainTex, distortedUV);
                
                // Apply wind intensity and color
                col *= _Color;
                col.a *= _WindIntensity;
                
                // Add some animated streaks
                float streaks = sin(i.worldPos.y * 10.0 + _Time.y * _WindSpeed * 5.0) * 0.5 + 0.5;
                col.a *= streaks;
                
                return col;
            }
            ENDCG
        }
    }
}