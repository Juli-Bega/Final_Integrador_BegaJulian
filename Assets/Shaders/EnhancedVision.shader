Shader "Custom/EnhancedVision"
{
    Properties
    {
    _NoiseIntensity ("Noise Intensity", Float) = 0.1
    _PixelSize ("Pixel Size", Float) = 1
    _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
    _HighlightTexture ("Highlight Texture", 2D) = "black" {}
    }
    
    SubShader
    {
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
            float4 _TintColor;
            TEXTURE2D(_HighlightTexture);
            SAMPLER(sampler_HighlightTexture);

            float Random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                if (_PixelSize > 1)
                {
                    float2 pixelCount = _ScreenParams.xy / _PixelSize;
                    uv = floor(uv * pixelCount) / pixelCount;
                }

                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                half4 highlight = SAMPLE_TEXTURE2D(_HighlightTexture, sampler_HighlightTexture, input.texcoord);

                // Si el pixel tiene color en la highlight texture
                if (highlight.r > 0.1 || highlight.g > 0.1 || highlight.b > 0.1)
                {
                    float luminance = max(color.r * 0.299 + color.g * 0.587 + color.b * 0.114, 0.4);
                    return half4(highlight.rgb * luminance, 1);
                }

                float grey = color.r * 0.299 + color.g * 0.587 + color.b * 0.114;
                float noise = Random(uv + _Time.y) * _NoiseIntensity;
                grey = saturate(grey + noise);
                return half4(_TintColor.rgb * grey, color.a);
            }
            ENDHLSL
        }
    }
}