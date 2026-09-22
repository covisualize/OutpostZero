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
        _Sway ("Sway", Range(0, 1)) = 0
        _Metallic ("Metallic", Range(0, 1)) = 0.05
        _HasMaps ("Has Maps", Float) = 0
        _BaseMap ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _MaskMap ("Mask", 2D) = "black" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Emission ("Emission", Color) = (0, 0, 0, 0)
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
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float _Tile;
                float _Wetness;
                float _Sway;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_MaskMap);
            SAMPLER(sampler_MaskMap);
            float _OutpostWet;
            float _WindStrength;
            // A property block can override these. Values inside UnityPerMaterial cannot.
            float4 _BaseColor;
            float4 _RimColor;
            float _RimPower;
            float _Dissolve;
            float _Metallic;
            float _HasMaps;
            float4 _Tint;
            float4 _Emission;

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
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 swayed = input.positionOS.xyz;
                if (_Sway > 0.001)
                {
                    float3 world = TransformObjectToWorld(swayed);
                    float gust = sin(_Time.y * 3.2 + world.x * 0.7 + world.z) * _WindStrength * _Sway;
                    swayed.y += gust;
                }
                VertexPositionInputs pos = GetVertexPositionInputs(swayed);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 normal = normalize(input.normalWS);
                float noise = Triplanar(input.positionWS, normal);
                clip(noise - _Dissolve);
                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                float rim = pow(1.0 - saturate(dot(normal, view)), _RimPower);
                Light mainLight = GetMainLight();
                float ndotl = saturate(dot(normal, mainLight.direction));
                float3 albedo = _BaseColor.rgb * _Tint.rgb;
                float metallic = _Metallic;
                float3 emissive = 0;
                if (_HasMaps > 0.5)
                {
                    albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _Tint.rgb;
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
                float dirt = normal.y > 0.65 ? saturate((normal.y - 0.65) / 0.35) * 0.35 : 0.0;
                color = lerp(color, float3(0.30, 0.27, 0.18), dirt);
                float wet = max(_Wetness, _OutpostWet) * saturate(normal.y);
                float wave = sin(input.positionWS.x * 5.5 + _Time.y * 2.4) * sin(input.positionWS.z * 4.5 - _Time.y * 1.6);
                wave = wave * 0.5 + 0.5;
                color = lerp(color, color * float3(0.55, 0.62, 0.72), wet * 0.6);
                color += wave * wet * 0.08;
                float3 wetNormal = normalize(normal + float3(wave - 0.5, 0.0, wave - 0.5) * wet * 0.35);
                float3 reflectDir = reflect(-mainLight.direction, wetNormal);
                float spec = pow(saturate(dot(reflectDir, view)), 28.0) * wet;
                color += spec * mainLight.color.rgb * 0.4;
                color = lerp(color, _RimColor.rgb, rim * _RimColor.a);
                color = lerp(color, color * mainLight.color.rgb, metallic);
                float gap = noise - _Dissolve;
                float fringe = (_Dissolve > 0.02) ? saturate(1.0 - gap / 0.07) * step(0.0, gap) : 0;
                color += emissive + _Emission.rgb + fringe * float3(1.0, 0.32, 0.06);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
