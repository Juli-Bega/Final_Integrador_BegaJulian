
Shader "Custom/Greyscale"
{
    Properties
    {
        _CircleActive ("Circle Active", Float) = 1
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

            #define RADIUS 0.25
            #define BORDER 0.003
            #define BORDER_COLOR half4(1, 0.5, 0.2, 1)

            float _CircleActive;

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);

                float grey = color.r * 0.299 + color.g * 0.587 + color.b * 0.114;
                half4 greyColor = half4(grey, grey, grey, color.a);

                if (_CircleActive < 0.5)
                    return greyColor;

                float2 uv = input.texcoord - 0.5;
                uv.x *= _ScreenParams.x / _ScreenParams.y;
                float dist = length(uv);

                if (dist < RADIUS)
                    return color;

                if (dist < RADIUS + BORDER)
                    return BORDER_COLOR;

                return greyColor;
            }
            ENDHLSL
        }
    }
}