# Agent O7

Juego de sigilo en primera persona desarrollado en Unity 6.3 LTS con Universal Render Pipeline.

El jugador debe atravesar una instalación evitando ser detectado por los guardias, apoyándose en una mecánica de **visión aumentada** que revela enemigos y objetivos a través de las paredes.

[**Jugar en itch.io**](https://julian-bega.itch.io/agent.o7) — build para Windows y capturas del juego.

---

## Índice

1. [Contexto](#1-contexto)  
2. [El shader elegido](#2-el-shader-elegido)  
3. [Mecánica e implementación en Unity](#3-mecánica-e-implementación-en-unity)  
4. [Funcionamiento interno](#4-funcionamiento-interno)  
5. [Cómo correr el proyecto](#5-cómo-correr-el-proyecto)

---

## 1\. Contexto

Proyecto desarrollado como examen final integrador de la Tecnicatura Superior en Desarrollo de Videojuegos (Image Campus), correspondiente a los contenidos de Unity y Programación de Gráficos.

La consigna pide un videojuego 3D simple que incorpore al menos un shader personalizado, con foco en la comprensión de los conceptos de shaders y su aplicación en un entorno de juego.

**Herramientas y asistencia**

Durante el desarrollo se utilizó Claude (Anthropic) como asistencia para consulta técnica, discusión de arquitectura y revisión de código. Los assets externos utilizados se detallan en la escena de créditos del juego.

---

## 2\. El shader elegido

El shader implementado es una **visión aumentada** al estilo del "modo detective" presente en juegos como Assassin's Creed, Batman: Arkham o Hitman.

Al activarla, la pantalla se transforma: la escena pasa a escala de grises con ruido y pixelación configurable, mientras los elementos relevantes para el jugador se destacan en color y permanecen visibles **incluso cuando están ocultos detrás de paredes**.

Se eligió este efecto por dos motivos. Primero, porque combina varias técnicas de programación de gráficos en un mismo sistema: post-procesado de pantalla completa, render a textura auxiliar, manipulación del depth test y generación procedural de geometría. Segundo, porque es una mecánica que atraviesa el diseño del juego en lugar de ser un efecto decorativo: la información que revela es lo que permite planificar el recorrido.

El sistema se compone de **dos shaders** que trabajan en conjunto:

| Shader | Rol |
| :---- | :---- |
| `Highlight.shader` | Dibuja los objetos marcados en una textura auxiliar, ignorando la oclusión |
| `EnhancedVision.shader` | Post-proceso de pantalla completa que combina la escena con esa textura |

---

## 3\. Mecánica e implementación en Unity

### 3.1 Qué hace en el juego

La visión aumentada se activa y desactiva con **F**. Mientras está activa:

- La escena se ve en escala de grises con ruido animado y pixelación  
- Los **guardias** se destacan en rojo  
- Los **objetivos** (coleccionables, botones, puertas) se destacan en amarillo  
- Los **conos de visión** de los guardias se vuelven visibles en rojo translúcido  
- Todos estos elementos se ven **a través de las paredes**  
- El jugador **no puede moverse** mientras la mantiene activa

La restricción de movimiento es una decisión de diseño: la visión es una herramienta de planificación, no de navegación. El jugador se detiene, observa, memoriza rutas de patrulla y posiciones, y vuelve al modo normal para actuar.

### 3.2 Arquitectura

El sistema respeta una regla: **ningún script de gameplay accede directamente a los shaders**. Toda comunicación pasa por un intermediario.

PlayerController          (input: tecla F)

        │

        ▼

ShaderService             (único punto de contacto con el sistema de render)

        │

        ├──► EnhancedVisionRendererFeature ──► EnhancedVisionRenderPass ──► EnhancedVision.shader

        ├──► HighlightRendererFeature      ──► HighlightRenderPass      ──► Highlight.shader

        └──► VisionCone\[\]                  (visibilidad de los conos en escena)

El `ShaderService` no contiene lógica de juego: recibe la orden `VisionState(bool)` y la traduce en operaciones sobre el pipeline de render. Cuando la visión está inactiva, ambos Renderer Features se desactivan y sus passes no se encolan — el costo de GPU es cero.

### 3.3 Scripts del sistema de render

| Script | Responsabilidad |
| :---- | :---- |
| `ShaderService` | Intermediario entre gameplay y sistema de render |
| `EnhancedVisionRendererFeature` | Registra el post-proceso en URP; expone parámetros (ruido, pixelación) |
| `EnhancedVisionRenderPass` | Ejecuta el post-proceso mediante Render Graph |
| `HighlightRendererFeature` | Registra el pass de highlights; define los tipos de highlight |
| `HighlightRenderPass` | Dibuja los objetos marcados en la Render Texture |
| `HighlightType` | Estructura serializable: nombre, layers y material de cada tipo |
| `VisionCone` | Genera proceduralmente la malla del cono de visión de cada guardia |

### 3.4 Sistema de tipos de highlight

Las categorías de objetos resaltados no están cableadas en código. El `HighlightRendererFeature` expone una lista configurable desde el Inspector, donde cada entrada define:

- **Name** — etiqueta descriptiva (solo para legibilidad del Inspector)  
- **Layers** — máscara de layers que abarca ese tipo  
- **Material** — material con el que se dibujarán esos objetos

La configuración actual del proyecto:

| Name | Layers | Color |
| :---- | :---- | :---- |
| Enemies | `Enemies` | Rojo opaco |
| Objectives | `Objectives` | Amarillo opaco |
| Vision Cones | `ConeHighlight` | Rojo translúcido |

Agregar una categoría nueva —por ejemplo, marcar la salida del nivel en verde— no requiere tocar código: es una entrada más en la lista y un material nuevo.

El orden de la lista determina el orden de dibujado, por lo que los tipos translúcidos conviene ubicarlos al final.

### 3.5 Layers del proyecto

| Layer | Uso |
| :---- | :---- |
| `Default` | Geometría del entorno (obstáculos para raycasts y oclusión) |
| `Player` | Jugador — excluido de los raycasts de línea de vista |
| `Enemies` | Guardias |
| `Objectives` | Coleccionables, botones y puertas |
| `ConeHighlight` | Mallas de los conos de visión |

### 3.6 El cono de visión

Cada guardia tiene una malla generada por código que representa su campo de visión. Es un abanico de triángulos construido a partir de un vértice central y un arco de vértices posicionados con funciones trigonométricas.

Tres características:

**Recorte contra geometría.** Cada vértice del arco se posiciona mediante un raycast: si el rayo golpea una pared antes de alcanzar el alcance máximo, el vértice se coloca en el punto de impacto. El cono se amolda a la habitación en lugar de atravesarla. La malla se recalcula a intervalos configurables mientras el cono es visible.

**Parámetros sincronizados con la detección.** El alcance y el ángulo no se configuran en el cono: los recibe del `GuardController`, que es la única fuente de verdad. Esto garantiza que lo que el jugador ve coincida exactamente con el área de detección real.

**Doble render.** El `MeshRenderer` del cono usa un material completamente transparente, por lo que en el render normal de la escena es invisible. Durante el pass de highlights, ese material es sustituido por el rojo translúcido. El mismo objeto es invisible en un contexto y visible en el otro (ver [4.4](#44-el-material-invisible-del-cono)).

---

## 4\. Funcionamiento interno

Esta sección recorre lo que ocurre en la GPU: qué información recibe cada shader, qué procesa y cómo llega el resultado a pantalla.

### 4.0 El frame completo

Con la visión aumentada activa, cada frame ejecuta esta secuencia:

1\. RENDER NORMAL DE LA ESCENA (URP)

   Destino: color de cámara \+ depth buffer

   Todos los objetos con sus materiales reales.

   Los conos de visión se dibujan con su material invisible.

        │

2\. HIGHLIGHT PASS          (evento: AfterRenderingOpaques)

   Destino: HighlightRT (Render Texture propia, sin depth buffer)

   ├── Limpieza a transparente

   └── Por cada tipo de highlight: dibuja sus layers con material override

       y el depth test desactivado

        │

3\. ENHANCED VISION PASS    (evento: AfterRenderingPostProcessing)

   ├── Blit 1: color de cámara → textura temporal, aplicando el shader

   │           (lee la escena y la HighlightRT, decide píxel por píxel)

   └── Blit 2: textura temporal → color de cámara

        │

4\. PRESENTACIÓN

   URP finaliza el frame y el resultado llega a pantalla.

Con la visión inactiva, los pasos 2 y 3 no se ejecutan.

### 4.1 Highlight.shader

Dibuja los objetos marcados dentro de la Render Texture auxiliar.

**Entrada**

- **Destino:** la `HighlightRT`, establecida con `SetRenderTarget` y limpiada a transparente al inicio de cada frame. Sin esa limpieza, las siluetas del frame anterior permanecerían. La textura no tiene depth buffer: como el depth test está desactivado, sería memoria sin uso.  
- **Geometría:** la malla original del objeto. El shader solo declara el atributo de posición; las UV y normales existen en la malla pero no se leen.  
- **Uniforms:** la matriz de transformación del objeto (que Unity provee automáticamente) y `_HighlightColor`, definido por el material.  
- **Material override:** el material real del objeto es sustituido durante este pass por el correspondiente a su tipo de highlight. El mismo mesh se dibuja con un programa distinto.  
- **Estado:** `ZTest Always`, `ZWrite Off`, `Cull Off`, `Blend SrcAlpha OneMinusSrcAlpha`.

**Vertex shader**

Una única operación por vértice: `TransformObjectToHClip()`, que multiplica la posición local por la matriz Model-View-Projection y la lleva a clip space.

Como se usa la misma cámara que en el render normal, la silueta del objeto queda en **exactamente los mismos píxeles** que ocupa (u ocuparía) en la imagen de la escena. Esa correspondencia es lo que permite después combinar ambas texturas con la misma coordenada.

**Rasterización**

El hardware ensambla los triángulos y genera un fragmento por cada píxel que la silueta cubre. No hay atributos que interpolar más allá de la posición.

**Fragment shader**

Devuelve `_HighlightColor` sin cálculo alguno. Toda la información de forma la aporta la geometría; el color identifica la categoría.

**Tests y blending**

Aquí está el núcleo de la mecánica. El depth test está en `Always`: el fragmento **no compara profundidad y se dibuja siempre**. En el render normal, un guardia detrás de una pared pierde el depth test y no se ve. En este pass ese test está desactivado, por lo que la silueta se dibuja igual.

Esa configuración es la implementación completa de "ver a través de paredes".

El culling de cámara sigue aplicando: un objeto fuera del campo visual no genera draw call. Ocluido sí se dibuja; fuera de cámara, no — que es el comportamiento correcto.

El blending permite que los materiales con alpha menor a 1 (los conos) se mezclen con lo ya presente en la textura.

**Resultado**

Este pass no toca la pantalla. La `HighlightRT` queda conteniendo las siluetas de color sobre fondo transparente: un mapa de qué píxel pertenece a qué categoría, incluyendo objetos ocluidos.

### 4.2 EnhancedVision.shader

El post-proceso de pantalla completa. Un único draw call que afecta a todos los píxeles.

**Entrada**

- **Geometría:** un quad de pantalla completa emitido por el `Blitter` de URP, cuyas UV cubren de (0,0) a (1,1). Su función es garantizar que la rasterización genere un fragmento por píxel de pantalla.  
- **Texturas:** `_BlitTexture` (la escena renderizada e iluminada, que el Blitter asigna automáticamente) y `_HighlightTexture` (la `HighlightRT` del pass anterior).  
- **Uniforms:** `_NoiseIntensity` y `_PixelSize`, expuestos en el Renderer Feature, más los automáticos `_Time` y `_ScreenParams`.  
- **Destino:** una textura temporal gestionada por el Render Graph, del mismo formato y resolución que el color de cámara.  
- **Estado:** `ZTest Always`, `ZWrite Off`, `Blend Off`.

**Vertex shader**

Se utiliza `Vert`, la función provista por `Blit.hlsl` de URP. Posiciona el quad y transfiere las coordenadas de textura hacia la rasterización.

**Rasterización**

El quad cubre la pantalla completa, generando un fragmento por píxel. Las coordenadas se interpolan: cada fragmento recibe su posición exacta en pantalla, normalizada entre 0 y 1\. Esta coordenada es simultáneamente "dónde estoy en la pantalla" y "dónde leo en las texturas".

**Fragment shader, paso a paso**

*Paso 1 — Pixelación.* Si `_PixelSize > 1`, la coordenada se cuantiza a una grilla:

float2 pixelCount \= \_ScreenParams.xy / \_PixelSize;

uv \= floor(uv \* pixelCount) / pixelCount;

Con una pantalla de 1920 de ancho y `_PixelSize = 8` resultan 240 celdas. Un píxel con `uv.x = 0.5031` produce `0.5031 × 240 = 120.7`, que redondeado hacia abajo da 120, y dividido nuevamente da 0.5. Todos los píxeles cuya coordenada escalada caiga entre 120 y 121 leen el mismo punto de la textura, formando un bloque uniforme.

*Paso 2 — Lectura de la escena.* Se muestrea `_BlitTexture` con la coordenada (pixelada si corresponde) mediante `SAMPLE_TEXTURE2D_X`. El color obtenido ya contiene toda la iluminación calculada por los materiales de la escena.

*Paso 3 — Lectura del highlight.* Se muestrea `_HighlightTexture` con la coordenada **original**, sin pixelar, de modo que los highlights conserven bordes nítidos aunque el fondo esté pixelado.

*Paso 4 — Decisión.* Si el píxel tiene color en la textura de highlights (cualquier canal por encima de 0.1, umbral que descarta el fondo transparente), pertenece a un objeto marcado:

float luminance \= max(color.r \* 0.299 \+ color.g \* 0.587 \+ color.b \* 0.114, 0.4);

return half4(highlight.rgb \* luminance, 1);

La **luminancia** es el brillo perceptual del píxel original. Los pesos no son un promedio simple porque la sensibilidad del ojo humano no es uniforme entre canales: es mayor al verde, intermedia al rojo y menor al azul. Un promedio haría que un azul puro pareciera tan brillante como un verde puro, lo cual es perceptualmente incorrecto.

Multiplicar el color del highlight por esa luminancia produce una "escala de rojos" o "escala de amarillos": las zonas iluminadas del objeto quedan claras y las sombreadas oscuras. El objeto conserva el volumen que la iluminación le dio, con el color reemplazado.

El `max(..., 0.4)` establece un piso de brillo. Sin él, un objetivo en una zona oscura tendría luminancia cercana a cero y su highlight resultaría ilegible. Dado que los highlights son información de gameplay, se prioriza la legibilidad garantizada por sobre la fidelidad lumínica exacta.

*Paso 5 — Fondo.* Si el píxel no está marcado, se convierte a escala de grises y se le suma ruido:

float grey \= color.r \* 0.299 \+ color.g \* 0.587 \+ color.b \* 0.114;

float noise \= Random(uv \+ \_Time.y) \* \_NoiseIntensity;

grey \= saturate(grey \+ noise);

return half4(\_TintColor.rgb \* grey, color.a);

`grey` es un valor entre 0 y 1 que representa el brillo del píxel. Multiplicar un color por ese brillo produce la escala completa de ese color: con `grey` en 0 da negro, con `grey` en 1 da el color puro.

Con `_TintColor` en blanco, la multiplicación devuelve los tres canales iguales — que es exactamente la conversión a escala de grises. Cualquier otro color produce una escala monocroma de ese tono, configurable desde el Inspector sin tocar el shader.

El generador de ruido:

float Random(float2 uv)

{

    return frac(sin(dot(uv, float2(12.9898, 78.233))) \* 43758.5453);

}

El producto punto colapsa la coordenada bidimensional a un escalar único por píxel. El seno lo convierte en un valor oscilante. La multiplicación por un número grande hace que entre píxeles vecinos el resultado salte miles de ciclos de la función, volviéndolo caótico. La parte fraccionaria descarta el entero y deja un valor entre 0 y 1 sin correlación aparente con el píxel adyacente.

Es **pseudo**aleatorio: es una función pura, la misma entrada produce siempre la misma salida. Por eso se suma `_Time.y`, que desplaza la entrada en cada frame y genera un patrón distinto, produciendo grano animado. Las constantes numéricas no tienen significado particular: son valores establecidos empíricamente por su buena distribución.

**Salida**

El resultado se escribe en la textura temporal. Un segundo blit la copia de vuelta al color de cámara, y URP finaliza el frame.

### 4.3 Por qué hay dos blits

No es posible leer y escribir la misma textura dentro de un mismo draw call: millones de fragmentos se ejecutan en paralelo y uno podría leer una zona que otro ya modificó, produciendo comportamiento indefinido.

La textura temporal rompe esa dependencia: se lee de la textura de cámara mientras se escribe en la temporal, y luego se copia de vuelta.

### 4.4 El material invisible del cono

El mismo mesh del cono se dibuja dos veces por frame con dos programas distintos.

En el **render normal**, su `MeshRenderer` usa un material transparente con alpha 0\. Atraviesa el pipeline completo, pero la ecuación de blending anula por completo su contribución: el resultado es el fondo intacto. El cono se dibuja de forma invisible.

En el **pass de highlights**, el material override lo sustituye por el rojo translúcido, y allí sí deja su marca en la Render Texture.

La alternativa aparentemente más simple —excluir el layer del Culling Mask de la cámara— no funciona: ese culling alimenta también las listas de renderizado del pass propio, por lo que el cono desaparecería de ambos contextos. Mantenerlo visible pero transparente preserva su presencia en el culling con costo despreciable.

### 4.5 Por qué no se utilizó el stencil buffer

La alternativa habitual para marcar píxeles pertenecientes a ciertos objetos es el stencil buffer: los objetos escriben un valor y un pass posterior lo consulta.

No es aplicable aquí. El stencil se escribe únicamente para fragmentos que superan el depth test, y un objeto oculto tras una pared no lo supera: nunca deja su marca. El stencil resuelve efectos sobre lo **visible** —un contorno de selección, por ejemplo—, mientras que el requisito de este proyecto era marcar precisamente lo que **no** se dibuja.

La solución fue un pass con destino propio y depth test desactivado.

### 4.6 Render Graph

Unity 6 introduce el Render Graph como forma de organizar los passes de URP. Cada pass **declara** primero qué recursos utiliza y con qué permisos, y luego define su ejecución:

builder.UseTexture(source, AccessFlags.Read);

builder.UseTexture(destination, AccessFlags.Write);

builder.SetRenderFunc(...);

Conociendo el grafo completo de dependencias, Unity puede reordenar passes independientes, reutilizar memoria de texturas que ya no se usan y descartar passes cuyo resultado nadie consume.

Ambos passes del proyecto están implementados sobre esta API. La `HighlightRT` se incorpora al grafo con `ImportTexture` por ser un recurso persistente y externo, mientras que la textura temporal del post-proceso se crea con `CreateTexture` y existe únicamente durante el frame.

---

## 5\. Cómo correr el proyecto

**Requisitos**

- Unity **6.3 LTS** (`6000.3.10f1`)  
- Universal Render Pipeline  
- Paquetes: Input System, AI Navigation, TextMeshPro

**Ejecución**

Abrir el proyecto y cargar la escena `MainMenu`, que es el punto de entrada del juego.

**Escenas**

| Escena | Descripción |
| :---- | :---- |
| `MainMenu` | Menú principal |
| `Credits` | Créditos y atribución de assets |
| `Level1` | Nivel jugable |
| `Level2` | Nivel jugable |
| `Level3` | Nivel jugable |

**Controles**

| Acción | Tecla |
| :---- | :---- |
| Moverse | `WASD` |
| Correr | `Shift` |
| Agacharse | `Ctrl` |
| Visión aumentada | `F` |
| Interactuar | `E` |
| Pausa | `Esc` |

**Ajustar el efecto**

Los parámetros del shader se configuran en el asset `PC_Renderer` del proyecto, dentro de los Renderer Features:

- **EnhancedVisionRendererFeature** — intensidad del ruido, tamaño de pixelación y color de tinte  
- **HighlightRendererFeature** — lista de tipos de highlight (layers y materiales)

