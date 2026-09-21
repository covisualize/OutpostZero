Shader "OutpostZero/TriplanarRim"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.34, 0.32, 0.28, 1)
        _RimColor ("Rim Color", Color) = (0.85, 0.28, 0.12, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _Tile ("Tile", Float) = 1.6
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _Metallic ("Metallic", Range(0, 1)) = 0.05
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
                float _Tile;
                float _Dissolve;
                float _Metallic;
            CBUFFER_END

            float Hash(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float Noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(lerp(Hash(i), Hash(i + float3(1, 0, 0)), f.x), lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
                    lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x), lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y),
                    f.z);
            }

            float Triplanar(float3 positionWS, float3 normalWS)
            {
                float3 blend = abs(normalWS);
                blend /= max(dot(blend, 1.0), 0.001);
                float nx = Noise(positionWS.zyx * _Tile);
                float ny = Noise(positionWS.xzy * _Tile);
                float nz = Noise(positionWS.xyz * _Tile);
                return nx * blend.x + ny * blend.y + nz * blend.z;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float noise = Triplanar(input.positionWS, normal);
                clip(noise - _Dissolve);
                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                float rim = pow(1.0 - saturate(dot(normal, view)), _RimPower);
                Light mainLight = GetMainLight();
                float ndotl = saturate(dot(normal, mainLight.direction));
                float3 color = _BaseColor.rgb * (0.25 + ndotl) * lerp(0.85, 1.15, noise);
                color = lerp(color, _RimColor.rgb, rim * _RimColor.a);
                color = lerp(color, color * mainLight.color.rgb, _Metallic);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
