
Shader "Custom/Greyscale"
{
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

            // Definimos el radio, controno, el color y grosor del circulo central
            #define RADIUS 0.25
            #define BORDER 0.003
            #define BORDER_COLOR half4(1, 0.5, 0.2, 0)

             half4 frag(Varyings input) : SV_Target
            {
                //Obtenemos el color del pixel, marcamos el centro de la pantalla y calculamos la distancia entre el pixel y el centro.
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                float2 uv = input.texcoord - 0.5;
                float dist = length(uv);

                // Calculamos si la distancia es exactamente la que está entre el limite del area a color y el grosor del borde ponemos el border color
                if (dist > RADIUS && dist < RADIUS + BORDER)
                    return BORDER_COLOR;

                // Calculamos si la distancia es mayor al limite con el borde del area a color y eso lo mantenemos en escala de grises
                if (dist > RADIUS + BORDER)
                {
                    float grey = color.r * 0.299 + color.g * 0.587 + color.b * 0.114;
                    return half4(grey, grey, grey, color.a);
                }
                
                // lo que no cumple las condiciones anteriores está dentro del radio y mantiene su color original
                return color;
            }
            ENDHLSL
        }
    }
}