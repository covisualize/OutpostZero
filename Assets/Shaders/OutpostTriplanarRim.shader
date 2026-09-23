// Character and prop PBR (the SG_Character role). Baked models (_HasMaps = 1) sample their UV
// albedo, tangent normal, AO and mask (R metallic, G roughness, B emissive: zombie eyes and veins);
// untextured meshes fall back to a flat colour with world-space noise. On top: a view rim for
// top-down readability (per faction: survivors cyan, zombies sickly green, overridden by the
// colour-vision palette), _HitFlash, a _Dissolve with a burning edge, wetness and wind sway.
// All material properties live in UnityPerMaterial so materials SRP-batch; property blocks
// (variety, palette rim, dissolve) still apply and opt that renderer out of the batch.
Shader "OutpostZero/TriplanarRim"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.34, 0.32, 0.28, 1)
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _BaseMap ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _MaskMap ("Mask (R metal, G rough, B emissive)", 2D) = "black" {}
        _HasMaps ("Has Maps", Float) = 0
        _Metallic ("Metallic (no maps)", Range(0, 1)) = 0.05
        _Smoothness ("Smoothness (no maps)", Range(0, 1)) = 0.3
        _Tile ("Noise Tile", Float) = 1.6
        _RimColor ("Rim Color (A = strength)", Color) = (0.3, 0.85, 1, 0.5)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _EyeGlow ("Emissive Mask Colour", Color) = (1.2, 0.9, 0.25, 1)
        _Emission ("Emission", Color) = (0, 0, 0, 0)
        _HitFlash ("Hit Flash", Range(0, 1)) = 0
        _HitColor ("Hit Flash Colour", Color) = (1, 0.35, 0.25, 1)
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _DissolveEdge ("Dissolve Edge Colour", Color) = (1, 0.32, 0.06, 1)
        _Wetness ("Wetness", Range(0, 1)) = 0
        _Sway ("Sway", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "UniversalMaterialType" = "Lit" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _Tint;
            half4 _RimColor;
            half4 _EyeGlow;
            half4 _Emission;
            half4 _HitColor;
            half4 _DissolveEdge;
            half _HasMaps;
            half _Metallic;
            half _Smoothness;
            float _Tile;
            half _RimPower;
            half _HitFlash;
            half _Dissolve;
            half _Wetness;
            half _Sway;
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

        float OutpostHash(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p, p.zyx + 31.32);
            return frac((p.x + p.y) * p.z);
        }

        float OutpostNoise(float3 p)
        {
            float3 i = floor(p);
            float3 f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(
                lerp(lerp(OutpostHash(i), OutpostHash(i + float3(1, 0, 0)), f.x), lerp(OutpostHash(i + float3(0, 1, 0)), OutpostHash(i + float3(1, 1, 0)), f.x), f.y),
                lerp(lerp(OutpostHash(i + float3(0, 0, 1)), OutpostHash(i + float3(1, 0, 1)), f.x), lerp(OutpostHash(i + float3(0, 1, 1)), OutpostHash(i + float3(1, 1, 1)), f.x), f.y),
                f.z);
        }

        float WorldNoise(float3 positionWS, float3 normalWS)
        {
            float3 blend = abs(normalWS);
            blend /= max(dot(blend, 1.0), 0.001);
            return OutpostNoise(positionWS.zyx * _Tile) * blend.x
                 + OutpostNoise(positionWS.xzy * _Tile) * blend.y
                 + OutpostNoise(positionWS.xyz * _Tile) * blend.z;
        }

        float3 SwayObject(float3 positionOS)
        {
            if (_Sway > 0.001)
            {
                float3 world = TransformObjectToWorld(positionOS);
                positionOS.y += sin(_Time.y * 3.2 + world.x * 0.7 + world.z) * _WindStrength * _Sway;
            }
            return positionOS;
        }

        void ClipDissolve(float3 positionWS, float3 normalWS)
        {
            if (_Dissolve > 0.001) clip(WorldNoise(positionWS, normalize(normalWS)) - _Dissolve);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                half3 vertexLight : TEXCOORD5;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs pos = GetVertexPositionInputs(SwayObject(input.positionOS.xyz));
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = normals.normalWS;
                output.tangentWS = float4(normals.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                output.vertexLight = VertexLighting(pos.positionWS, normals.normalWS);
                #endif
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 geomNormal = normalize(input.normalWS);
                float noise = WorldNoise(input.positionWS, geomNormal);
                clip(noise - _Dissolve);

                half3 albedo;
                half metallic;
                half smoothness;
                half occlusion = 1;
                half3 emission = _Emission.rgb;
                float3 normalWS = geomNormal;
                if (_HasMaps > 0.5)
                {
                    albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _Tint.rgb;
                    half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv);
                    metallic = mask.r;
                    smoothness = 1.0 - mask.g;
                    occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r;
                    emission += mask.b * _EyeGlow.rgb;
                    half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv));
                    float sgn = input.tangentWS.w;
                    float3 bitangent = sgn * cross(geomNormal, input.tangentWS.xyz);
                    normalWS = normalize(TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent, geomNormal)));
                }
                else
                {
                    albedo = _BaseColor.rgb * _Tint.rgb * lerp(0.85, 1.15, noise);
                    metallic = _Metallic;
                    smoothness = _Smoothness;
                }

                half wet = saturate(max(_Wetness, _OutpostWet)) * saturate(normalWS.y * 0.7 + 0.3);
                albedo *= lerp(1.0, 0.6, wet);
                smoothness = lerp(smoothness, 0.9, wet);

                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half rim = pow(1.0 - saturate(dot(normalWS, view)), _RimPower);
                emission += rim * _RimColor.rgb * _RimColor.a;

                albedo = lerp(albedo, half3(1, 1, 1), _HitFlash * 0.35);
                emission += _HitColor.rgb * _HitFlash;

                float gap = noise - _Dissolve;
                half fringe = _Dissolve > 0.02 ? saturate(1.0 - gap / 0.07) : 0;
                emission += fringe * _DissolveEdge.rgb * 2.0;

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo;
                surface.metallic = metallic;
                surface.specular = half3(0, 0, 0);
                surface.smoothness = saturate(smoothness);
                surface.normalTS = half3(0, 0, 1);
                surface.occlusion = occlusion;
                surface.emission = emission;
                surface.alpha = 1;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = view;
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                inputData.vertexLighting = input.vertexLight;
                #endif
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                float3 positionWS = TransformObjectToWorld(SwayObject(input.positionOS.xyz));
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirection = normalize(_LightPosition - positionWS);
                #else
                float3 lightDirection = _LightDirection;
                #endif
                output.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection)));
                output.positionWS = positionWS;
                output.normalWS = normalWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                ClipDissolve(input.positionWS, input.normalWS);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                float3 positionWS = TransformObjectToWorld(SwayObject(input.positionOS.xyz));
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half frag(Varyings input) : SV_Target
            {
                ClipDissolve(input.positionWS, input.normalWS);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                float3 positionWS = TransformObjectToWorld(SwayObject(input.positionOS.xyz));
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                ClipDissolve(input.positionWS, input.normalWS);
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
