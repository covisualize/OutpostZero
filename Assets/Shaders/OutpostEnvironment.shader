// World-space triplanar PBR for the primitive-built architecture (the SG_Environment_Triplanar role).
// Albedo, normal and mask (R metallic, G occlusion, A smoothness) are sampled on three world planes
// so boxes get detail without UVs. Adds a top-surface dirt/moss overlay by world normal, grime that
// darkens toward the ground, wetness from weather (_OutpostWet global or _Wetness), and optional
// vertex-colour AO from the Blender bake. Every material property lives in UnityPerMaterial so
// materials SRP-batch; a MaterialPropertyBlock (district tint) still works but opts that renderer out.
Shader "OutpostZero/EnvironmentTriplanar"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _MaskMap ("Mask (R metal, G AO, A smooth)", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Tiling ("Tiles per Metre", Float) = 0.5
        _BlendSharpness ("Blend Sharpness", Range(1, 16)) = 4
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1
        _TopColor ("Top Overlay Color", Color) = (0.24, 0.26, 0.16, 1)
        _TopAmount ("Top Overlay Amount", Range(0, 1)) = 0.4
        _TopThreshold ("Top Overlay Threshold", Range(0, 1)) = 0.7
        _GrimeHeight ("Grime Height (m)", Float) = 1.2
        _GrimeStrength ("Grime Strength", Range(0, 1)) = 0.35
        _Wetness ("Wetness", Range(0, 1)) = 0
        _VertexAO ("Vertex Colour AO", Range(0, 1)) = 0
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
            half4 _TopColor;
            float _Tiling;
            half _BlendSharpness;
            half _NormalStrength;
            half _TopAmount;
            half _TopThreshold;
            float _GrimeHeight;
            half _GrimeStrength;
            half _Wetness;
            half _VertexAO;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);
        SAMPLER(sampler_BumpMap);
        TEXTURE2D(_MaskMap);
        SAMPLER(sampler_MaskMap);
        float _OutpostWet;

        float3 TriplanarWeights(float3 normalWS)
        {
            float3 w = pow(abs(normalWS), _BlendSharpness);
            return w / max(w.x + w.y + w.z, 1e-4);
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
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half4 color : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                half3 vertexLight : TEXCOORD4;
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
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                output.vertexLight = VertexLighting(pos.positionWS, output.normalWS);
                #endif
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 n = normalize(input.normalWS);
                float3 w = TriplanarWeights(n);
                float3 p = input.positionWS * _Tiling;
                float3 axisSign = step(0.0, n) * 2.0 - 1.0;

                float2 uvX = float2(p.z * axisSign.x, p.y);
                float2 uvY = float2(p.x * axisSign.y, p.z);
                float2 uvZ = float2(-p.x * axisSign.z, p.y);

                half4 aX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX);
                half4 aY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY);
                half4 aZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ);
                half4 mX = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvX);
                half4 mY = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvY);
                half4 mZ = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvZ);
                half3 albedo = (aX.rgb * w.x + aY.rgb * w.y + aZ.rgb * w.z) * _BaseColor.rgb * _Tint.rgb;
                half4 mask = mX * w.x + mY * w.y + mZ * w.z;

                // Whiteout-blended triplanar normals (Golus), axis signs keep mirrored faces lit correctly.
                half3 tX = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvX), _NormalStrength);
                half3 tY = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvY), _NormalStrength);
                half3 tZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvZ), _NormalStrength);
                tX.x *= axisSign.x;
                tY.x *= axisSign.y;
                tZ.x *= -axisSign.z;
                tX = half3(tX.xy + n.zy, abs(tX.z) * n.x);
                tY = half3(tY.xy + n.xz, abs(tY.z) * n.y);
                tZ = half3(tZ.xy + n.xy, abs(tZ.z) * n.z);
                float3 normalWS = normalize(tX.zyx * w.x + tY.xzy * w.y + tZ.xyz * w.z);

                half metallic = mask.r;
                half occlusion = mask.g;
                half smoothness = mask.a;

                // Top overlay: dirt or moss settles on up-facing surfaces, broken up by the albedo's own value.
                half top = saturate((normalWS.y - _TopThreshold) / max(1.0 - _TopThreshold, 1e-3));
                half breakup = saturate(dot(albedo, half3(0.6, 1.2, 0.2)) * 2.0);
                top *= _TopAmount * smoothstep(0.2, 0.8, breakup + top * 0.5);
                albedo = lerp(albedo, _TopColor.rgb, top);
                smoothness = lerp(smoothness, 0.1, top);
                metallic *= 1.0 - top;

                // Grime: darker and rougher near the ground.
                half grime = saturate(1.0 - input.positionWS.y / max(_GrimeHeight, 0.01)) * _GrimeStrength;
                albedo *= 1.0 - grime * 0.45;
                smoothness *= 1.0 - grime * 0.5;

                occlusion *= lerp(1.0, input.color.r, _VertexAO);

                // Wet: darker albedo, glossier, strongest on up-facing surfaces.
                half wet = saturate(max(_Wetness, _OutpostWet)) * saturate(normalWS.y * 0.7 + 0.3);
                albedo *= lerp(1.0, 0.55, wet);
                smoothness = lerp(smoothness, 0.92, wet);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo;
                surface.metallic = metallic;
                surface.specular = half3(0, 0, 0);
                surface.smoothness = smoothness;
                surface.normalTS = half3(0, 0, 1);
                surface.occlusion = occlusion;
                surface.emission = half3(0, 0, 0);
                surface.alpha = 1;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                inputData.vertexLighting = input.vertexLight;
                #endif
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                #if defined(_DBUFFER)
                ApplyDecalToSurfaceData(input.positionCS, surface, inputData);
                #endif

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

            float4 vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirection = normalize(_LightPosition - positionWS);
                #else
                float3 lightDirection = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));
                return ApplyShadowClamping(positionCS);
            }

            half4 frag() : SV_Target
            {
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half frag() : SV_Target
            {
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
                float3 normalWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
