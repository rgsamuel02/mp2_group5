// MP2A "Solar Coaster" / "Solar Blast" shader.
// Unlit URP shader whose UV coordinates are offset by _ScrollSpeed * time.
//  - On a mesh strip (conveyor belt, path lines) it makes the texture slide from one end to the other (Solar Coaster).
//  - On a disc mesh with polar UVs (u = angle, v = radius) scrolling V makes rings travel outward (Solar Blast).
// Single-pass-instanced / multiview safe for Quest.
Shader "Steven/MP2A/ScrollUnlit"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _ScrollSpeed ("Scroll Speed (UV per second)", Vector) = (0, 0.5, 0, 0)
        _Intensity ("Intensity", Float) = 1
        [ToggleUI] _Premultiply ("Multiply RGB by alpha (additive fade)", Float) = 0
        _RadialFade ("Fade toward V = 1 (disc edge)", Range(0, 1)) = 0
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
            Name "ScrollUnlit"
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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _ScrollSpeed;
                half _Intensity;
                half _Premultiply;
                half _RadialFade;
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
                float2 rawUV : TEXCOORD1;
                half4 color : COLOR;
                float fogCoord : TEXCOORD2;
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
                // The UV offset grows with time: this is the texture animation.
                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw + _ScrollSpeed.xy * _Time.y;
                output.rawUV = input.uv;
                output.color = input.color;
                output.fogCoord = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 c = tex * _BaseColor * input.color;
                c.a *= lerp(1.0h, saturate((1.0h - (half)input.rawUV.y) * 2.5h), _RadialFade);
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
