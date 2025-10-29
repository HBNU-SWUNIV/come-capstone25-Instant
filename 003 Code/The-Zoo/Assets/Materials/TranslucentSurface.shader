Shader "Custom/URP/GhostlyTransparent"
{
    Properties
    {
        _MainTex       ("Albedo (RGB)", 2D)    = "white" {}
        _BaseColor     ("Tint Color", Color)    = (1,1,1,1)
        _Alpha         ("Base Opacity", Range(0,1)) = 1.0
        _FresnelPower  ("Edge Fade Power", Range(0.1,10)) = 0.3
        _NoiseTex      ("Dither Noise", 2D)    = "gray" {}
        _NoiseScale    ("Noise UV Scale", Float) = 10
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
        }

        // 1) 깊이만 기록해서 뒤쪽 면 가리기
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull Back
        }

        // 2) 실제 유령 효과 투명 패스
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD1;
                float3 worldNormal: TEXCOORD2;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _BaseColor;
            float  _Alpha;
            float  _FresnelPower;
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            float  _NoiseScale;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS    = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos      = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.worldNormal   = TransformObjectToWorldNormal(IN.positionOS.xyz);
                OUT.uv            = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 1) 기본 컬러 & 텍스쳐
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _BaseColor;

                // 2) Fresnel 엣지 페이드: ViewDir ⋅ Normal
                float3 V = normalize(_WorldSpaceCameraPos - IN.worldPos);
                float NdotV = saturate(dot(normalize(IN.worldNormal), V));
                float fresnel = pow(1 - NdotV, _FresnelPower);

                // 3) 노이즈(dither) 샘플링
                float2 noiseUV = IN.uv * _NoiseScale + _Time.y;
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                // 노이즈를 [0.8,1] 범위로 스케일
                noise = lerp(0.8, 1.0, noise);

                // 4) 최종 알파: 기본α × Fresnel × 노이즈
                col.a = _Alpha * fresnel * noise;

                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
