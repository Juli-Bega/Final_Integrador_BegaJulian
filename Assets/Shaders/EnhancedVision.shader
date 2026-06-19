
Shader "Custom/EnhancedVision"
{
    Properties
    {
        _NoiseIntensity ("Noise Intensity", Float) = 0.1
        _PixelSize ("Pixel Size", Float) = 4
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
            float _PixelSize;

            float Random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                
                float2 resolution = _ScreenParams.xy;
                float2 pixelCount = resolution / _PixelSize;
                uv = floor(uv * pixelCount) / pixelCount;
                
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                
                float grey = color.r * 0.299 + color.g * 0.587 + color.b * 0.114;
                float noise = Random(uv + _Time.y) * _NoiseIntensity;
                grey = saturate(grey + noise);
                return half4(grey, grey, grey, color.a);
            }
            ENDHLSL
        }
    }
}