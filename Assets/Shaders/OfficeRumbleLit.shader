Shader "OfficeRumble/Lit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _MainTex ("Base Map", 2D) = "white" {}
        _EmissionColor ("Emission Color", Color) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

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
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD1;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float4 _MainTex_ST;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normInputs.normalWS;
                output.color = input.color;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = saturate(dot(normalize(input.normalWS), lightDir));

                // Stylized office lighting (warm corporate key light + cool ambient)
                float3 ambient = float3(0.42, 0.44, 0.50);
                float3 direct = mainLight.color * (NdotL * 0.70 + 0.15);
                float3 lightTotal = ambient + direct;

                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 albedo = texColor * _BaseColor * input.color;
                float3 finalRgb = albedo.rgb * lightTotal + _EmissionColor.rgb;

                return float4(finalRgb, albedo.a);
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float3 normal : TEXCOORD1;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            fixed4 _BaseColor;
            fixed4 _EmissionColor;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 lightDir = normalize(float3(0.4, 0.8, -0.4));
                float NdotL = saturate(dot(normalize(i.normal), lightDir));
                float3 light = float3(0.4, 0.45, 0.5) + float3(0.8, 0.8, 0.75) * (NdotL * 0.7 + 0.15);
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 col = tex * _BaseColor * i.color;
                return fixed4(col.rgb * light + _EmissionColor.rgb, col.a);
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
