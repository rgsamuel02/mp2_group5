// MP2A "UV Distortion" shader.
// A second noise texture is scrolled over time and its value displaces the UV coordinates used to sample the
// main texture, producing wobbly, flickering, blobby motion (grease-fire flames, bubbling fryer oil, boiling
// stock, sink water). Single-pass-instanced / multiview safe for Quest.
Shader "Steven/MP2A/UVDistort"
{
    Properties
    {
        _BaseMap ("Main Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _NoiseMap ("Noise Texture", 2D) = "gray" {}
        _NoiseScale ("Noise Tiling", Float) = 1
        _NoiseScroll ("Noise Scroll (UV per second)", Vector) = (0, -0.6, 0, 0)
        _BaseScroll ("Main Scroll (UV per second)", Vector) = (0, 0, 0, 0)
        _DistortStrength ("Distortion Strength", Range(0, 0.5)) = 0.08
        _Intensity ("Intensity", Float) = 1
        [ToggleUI] _Premultiply ("Multiply RGB by alpha (additive fade)", Float) = 0
        _VerticalFade ("Fade toward V = 1 (flame tips)", Range(0, 1)) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "IgnoreProjector" = "True" }
        LOD 100

        Pass
        {
            Name "UVDistort"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _NoiseMap_ST;
                half4 _BaseColor;
                float _NoiseScale;
                float4 _NoiseScroll;
                float4 _BaseScroll;
                float _DistortStrength;
                half _Intensity;
                half _Premultiply;
                half _VerticalFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 noiseUV : TEXCOORD1;
                float2 rawUV : TEXCOORD2;
                half4 color : COLOR;
                float fogCoord : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw + _BaseScroll.xy * _Time.y;
                // The noise texture scrolls on its own...
                output.noiseUV = input.uv * _NoiseScale + _NoiseScroll.xy * _Time.y;
                output.rawUV = input.uv;
                output.color = input.color;
                output.fogCoord = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // ...and its value displaces the UVs of the main texture.
                half2 n = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, input.noiseUV).rg * 2.0h - 1.0h;
                float2 uv = input.uv + n * _DistortStrength;
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);

                half4 c = tex * _BaseColor * input.color;
                c.a *= lerp(1.0h, saturate((1.0h - (half)input.rawUV.y) * 1.6h), _VerticalFade);
                c.rgb *= _Intensity;
                c.rgb *= lerp(1.0h, c.a, _Premultiply);
                c.rgb = MixFog(c.rgb, input.fogCoord);
                return c;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
