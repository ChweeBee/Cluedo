Shader "Cluedo/TurnHighlight"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (1.0, 0.85, 0.25, 1.0)
        _GlowPower ("Rim Power", Range(0.5, 8.0)) = 3.0
        _GlowIntensity ("Intensity", Range(0.0, 8.0)) = 2.5
        _PulseSpeed ("Pulse Speed", Range(0.0, 10.0)) = 2.5
        _PulseAmount ("Pulse Amount", Range(0.0, 1.0)) = 0.35
        _NormalPush ("Normal Push", Range(0.0, 0.05)) = 0.005
    }

    SubShader
    {
        Tags
        {
            "RenderType"   = "Transparent"
            "Queue"        = "Transparent+10"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "TurnHighlightRim"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float  _GlowPower;
                float  _GlowIntensity;
                float  _PulseSpeed;
                float  _PulseAmount;
                float  _NormalPush;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 pushedOS = IN.positionOS.xyz + IN.normalOS * _NormalPush;
                VertexPositionInputs vp = GetVertexPositionInputs(pushedOS);
                VertexNormalInputs   vn = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = vp.positionCS;
                OUT.normalWS    = vn.normalWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(vp.positionWS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                float rim = pow(saturate(1.0 - saturate(dot(N, V))), _GlowPower);
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                half3 col = _GlowColor.rgb * rim * _GlowIntensity * pulse;
                half  a   = saturate(rim * pulse) * _GlowColor.a;
                return half4(col, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
