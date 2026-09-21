Shader "OutpostZero/TriplanarRim"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.34, 0.32, 0.28, 1)
        _RimColor ("Rim Color", Color) = (0.85, 0.28, 0.12, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _Tile ("Tile", Float) = 1.6
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _Wetness ("Wetness", Range(0, 1)) = 0
        _Metallic ("Metallic", Range(0, 1)) = 0.05
        _HasMaps ("Has Maps", Float) = 0
        _BaseMap ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _MaskMap ("Mask", 2D) = "black" {}
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
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
                float _Tile;
                float _Dissolve;
                float _Wetness;
                float _Metallic;
                float _HasMaps;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_MaskMap);
            SAMPLER(sampler_MaskMap);

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
                output.uv = input.uv;
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
                float3 albedo = _BaseColor.rgb;
                float metallic = _Metallic;
                float3 emissive = 0;
                if (_HasMaps > 0.5)
                {
                    albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                    float occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r;
                    albedo *= lerp(1.0, occlusion, 0.85);
                    float3 bump = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv).rgb * 2.0 - 1.0;
                    ndotl = saturate(ndotl + bump.x * 0.35);
                    float4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);
                    metallic = mask.r;
                    emissive = mask.b * albedo;
                }
                float3 color = albedo * (0.25 + ndotl) * lerp(0.85, 1.15, noise);
                float grime = saturate(1.15 - input.positionWS.y * 0.18);
                color *= lerp(1.0, 0.7, grime * 0.4);
                float wet = _Wetness * saturate(normal.y);
                color = lerp(color, color * float3(0.55, 0.62, 0.72), wet * 0.6);
                float3 reflectDir = reflect(-mainLight.direction, normal);
                float spec = pow(saturate(dot(reflectDir, view)), 28.0) * wet;
                color += spec * mainLight.color.rgb * 0.4;
                color = lerp(color, _RimColor.rgb, rim * _RimColor.a);
                color = lerp(color, color * mainLight.color.rgb, metallic);
                color += emissive;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
