Shader "OutpostZero/HeightFog"
{
    Properties
    {
        _Strength ("Strength", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Pass
        {
            Name "HeightFog"
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Set by HeightFog.Push. Unset globals read as zero, which draws no fog.
            float4 _OzFogColor;
            float4 _OzFogParams;
            float4 _OzFogRange;

            CBUFFER_START(UnityPerMaterial)
                float _Strength;
            CBUFFER_END

            // Mirrors HeightFog.Depth on the C# side.
            float FogDepth(float thickness, float rise, float baseY, float eyeY, float pointY, float dist)
            {
                float span = min(max(dist, 0.0), _OzFogRange.y) - _OzFogRange.x;
                if (thickness <= 0.0 || rise <= 0.0 || span <= 0.0) return 0.0;
                float fromEye = exp(-(eyeY - baseY) / rise);
                float dy = pointY - eyeY;
                float column = abs(dy) < 0.01
                    ? fromEye
                    : rise * (fromEye - exp(-(pointY - baseY) / rise)) / dy;
                return thickness * span * column;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float raw = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    bool sky = raw <= 0.00001;
                #else
                    bool sky = raw >= 0.99999;
                    raw = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, raw);
                #endif
                float3 world = ComputeWorldSpacePosition(uv, raw, UNITY_MATRIX_I_VP);
                float3 eye = GetCameraPositionWS();
                float3 ray = world - eye;
                float dist = length(ray);
                if (sky)
                {
                    float3 dir = ray / max(dist, 0.0001);
                    dist = _OzFogRange.y;
                    world = eye + dir * dist;
                }
                float depth = FogDepth(_OzFogParams.x, _OzFogParams.y, _OzFogParams.z, eye.y, world.y, dist);
                float amount = _Strength * _OzFogColor.a * (1.0 - exp(-depth));
                return half4(_OzFogColor.rgb, amount);
            }
            ENDHLSL
        }
    }
}
