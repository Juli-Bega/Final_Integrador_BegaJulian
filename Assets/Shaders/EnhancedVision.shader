
Shader "Custom/EnhancedVision"
{
    Properties
    {
        _NoiseIntensity ("Noise Intensity", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always ZWrite Off Cull Off Blend Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _NoiseIntensity;

            float Random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                float grey = color.r * 0.299 + color.g * 0.587 + color.b * 0.114;
                float noise = Random(input.texcoord + _Time.y) * _NoiseIntensity;
                grey = saturate(grey + noise);
                return half4(grey, grey, grey, color.a);
            }
            ENDHLSL
        }
    }
}