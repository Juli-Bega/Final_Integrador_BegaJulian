*[Read in English](README.en.md)*

# Agent O7

Juego de sigilo en primera persona desarrollado en Unity 6.3 LTS con Universal Render Pipeline.

El jugador se infiltra en una instalación vigilada, recupera los objetos de valor y escapa sin ser detectado. Su única herramienta es una **visión aumentada** que revela guardias, objetivos y conos de visión, incluso a través de las paredes.

**[Jugar en itch.io](https://julian-bega.itch.io/agent.o7)** · **[Descargar la build](../../releases)**

---

## Índice

1. [El proyecto](#1-el-proyecto)
2. [El shader](#2-el-shader)
3. [Implementación](#3-implementación)
4. [Funcionamiento interno](#4-funcionamiento-interno)
5. [Cómo correr el proyecto](#5-cómo-correr-el-proyecto)

---

## 1. El proyecto

Examen final integrador de la Tecnicatura Superior en Desarrollo de Videojuegos (Image Campus). La consigna pide un juego 3D simple que incorpore al menos un shader personalizado, con foco en la comprensión de los conceptos de shaders y su aplicación en un entorno de juego.

Durante el desarrollo se utilizó Claude (Anthropic) como asistencia para consulta técnica y revisión de código. Los assets externos utilizados se detallan en la escena de créditos del juego.

---

## 2. El shader

Una **visión aumentada** al estilo del "modo detective" de juegos como Assassin's Creed, Batman: Arkham o Hitman.

Al activarla, la escena pasa a monocromo con ruido y pixelación configurables, mientras los elementos relevantes se destacan en color y permanecen visibles **aunque estén ocultos detrás de paredes**.

El sistema se compone de dos shaders que trabajan en conjunto:

| Shader | Rol |
|---|---|
| `Highlight.shader` | Dibuja los objetos marcados en una textura auxiliar, ignorando la oclusión |
| `EnhancedVision.shader` | Post-proceso de pantalla completa que combina la escena con esa textura |

---

## 3. Implementación

### 3.1 Qué hace en el juego

La visión se activa y desactiva con **F**. Mientras está activa:

- La escena se ve monocroma, con ruido animado y pixelación
- Los guardias, objetivos, salidas y botones se destacan cada uno en su color
- Los conos de visión de los guardias se vuelven visibles
- Todo eso se ve **a través de las paredes**
- El jugador **no puede moverse**

La restricción de movimiento es una decisión de diseño: la visión es una herramienta de planificación, no de navegación. El jugador se detiene, observa rutas de patrulla y posiciones, y vuelve al modo normal para actuar.

### 3.2 Arquitectura

Ningún script de gameplay accede directamente a los shaders. Toda comunicación pasa por un intermediario:

```
PlayerController          (input: tecla F)
        │
        ▼
ShaderService             (único punto de contacto con el sistema de render)
        │
        ├──► EnhancedVisionRendererFeature ──► EnhancedVisionRenderPass ──► EnhancedVision.shader
        ├──► HighlightRendererFeature      ──► HighlightRenderPass      ──► Highlight.shader
        └──► VisionCone[]                  (visibilidad de los conos en escena)
```

El `ShaderService` no contiene lógica de juego: recibe la orden `VisionState(bool)` y la traduce en operaciones sobre el pipeline de render. Con la visión inactiva, ambos Renderer Features se desactivan y sus passes no se encolan — el costo de GPU es cero.

### 3.3 Scripts del sistema

| Script | Responsabilidad |
|---|---|
| `ShaderService` | Intermediario entre gameplay y sistema de render |
| `EnhancedVisionRendererFeature` | Registra el post-proceso en URP; expone sus parámetros |
| `EnhancedVisionRenderPass` | Ejecuta el post-proceso mediante Render Graph |
| `HighlightRendererFeature` | Registra el pass de highlights; define los tipos |
| `HighlightRenderPass` | Dibuja los objetos marcados en la Render Texture |
| `HighlightType` | Estructura serializable: nombre, layers y material de cada tipo |
| `VisionCone` | Genera la malla del cono de visión de cada guardia |

### 3.4 Tipos de highlight

Las categorías de objetos resaltados no están cableadas en código. El `HighlightRendererFeature` expone una lista configurable desde el Inspector, donde cada entrada define:

- **Name** — etiqueta descriptiva, solo para legibilidad del Inspector
- **Layers** — máscara de layers que abarca ese tipo
- **Material** — material con el que se dibujarán esos objetos

La configuración actual del proyecto:

| Name | Layer |
|---|---|
| Enemies | `Enemies` |
| Cone | `ConeHighlight` |
| Objective | `Objectives` |
| Exit | `Exit` |
| Buttons | `Buttons` |

Agregar una categoría nueva no requiere tocar código: es una entrada más en la lista y un material nuevo.

**El orden de la lista determina el orden de dibujado.** Como dentro de la textura auxiliar no hay test de profundidad, los tipos que están más abajo se dibujan encima de los anteriores. Los materiales translúcidos, como el del cono, conviene ubicarlos al final.

### 3.5 Layers del proyecto

| Layer | Uso |
|---|---|
| `Default` | Geometría del entorno (obstáculos para raycasts y oclusión) |
| `Player` | Jugador — excluido de los raycasts de línea de vista |
| `Enemies` | Guardias |
| `Objectives` | Coleccionables |
| `ConeHighlight` | Mallas de los conos de visión |
| `Exit` | Zona de salida del nivel |
| `Buttons` | Botones interactuables |

### 3.6 El cono de visión

Cada guardia tiene una malla generada por código que representa su campo de visión: un abanico de triángulos construido con funciones trigonométricas.

**Se recorta contra la geometría.** Cada vértice del arco se posiciona mediante un raycast: si el rayo golpea una pared antes de alcanzar el rango máximo, el vértice se coloca en el punto de impacto. La malla se recalcula a intervalos configurables mientras el cono es visible.

**Sus parámetros vienen del guardia.** El alcance y el ángulo no se configuran en el cono: los recibe del `GuardController`, que es la única fuente de verdad. Así lo que el jugador ve coincide exactamente con el área de detección real.

---

## 4. Funcionamiento interno

### 4.1 El frame completo

Con la visión activa, cada frame ejecuta esta secuencia:

```
1. RENDER NORMAL DE LA ESCENA (URP)
   Destino: textura de color de la cámara
   Todos los objetos con sus materiales reales.
        │
2. HIGHLIGHT PASS          (evento: AfterRenderingOpaques)
   Destino: HighlightRT (Render Texture propia, sin depth buffer)
   ├── Limpieza a transparente
   └── Por cada tipo de highlight: dibuja sus layers con material
       override y el depth test desactivado
        │
3. ENHANCED VISION PASS    (evento: AfterRenderingPostProcessing)
   ├── Blit 1: cámara → textura temporal, aplicando el shader
   │           (lee la escena y la HighlightRT, decide píxel por píxel)
   └── Blit 2: textura temporal → cámara
        │
4. El resultado llega a pantalla.
```

Con la visión inactiva, los pasos 2 y 3 no se ejecutan.

El paso 3 usa dos blits porque no es posible leer y escribir la misma textura en una sola operación de dibujado: la textura temporal rompe esa dependencia.

**La `HighlightRT` no es una capa que se superpone.** Es una tabla de consulta: el paso 2 la llena con siluetas de color sobre fondo transparente, y el paso 3 la lee para decidir qué color dar a cada píxel de la pantalla. Nunca se muestra directamente.

### 4.2 Highlight.shader

Dibuja los objetos marcados dentro de la Render Texture auxiliar.

**Qué recibe.** La malla original del objeto — de sus atributos solo declara la posición, porque el fragment devuelve un color plano y no necesita UVs ni normales. La matriz de transformación del objeto, que Unity provee automáticamente. Y `_HighlightColor`, definido por el material.

**Material override.** El material real del objeto es sustituido durante este pass por el correspondiente a su tipo de highlight. El mismo mesh se dibuja con un programa distinto, sin duplicar objetos ni cambiar materiales en runtime.

**Vertex shader.** Una única operación por vértice: `TransformObjectToHClip()`, que lleva la posición de espacio local a espacio de proyección. Como se usa la misma cámara que en el render normal, la silueta queda en exactamente los mismos píxeles que ocupa (u ocuparía) el objeto en la imagen de la escena. Esa correspondencia es lo que permite combinar ambas texturas con la misma coordenada.

**Fragment shader.** Devuelve `_HighlightColor` sin cálculo alguno. Toda la información de forma la aporta la geometría; el color identifica la categoría.

**Render states.** Acá está el núcleo de la mecánica:

```
ZTest Always                          → ver a través de paredes
ZWrite Off                            → no escribe profundidad
Cull Off                              → el cono es una malla de un solo lado
Blend SrcAlpha OneMinusSrcAlpha       → permite materiales translúcidos
```

`ZTest Always` desactiva la comparación de profundidad: el fragmento se dibuja siempre, sin importar qué haya delante. En el render normal, un guardia detrás de una pared pierde esa comparación y no se dibuja; acá se dibuja igual.

Esa línea, combinada con el hecho de que el pass tiene su propio destino, es lo que produce el efecto: la silueta se registra como información sin pintarse sobre la pantalla.

### 4.3 EnhancedVision.shader

El post-proceso de pantalla completa. Un único draw call que afecta a todos los píxeles.

**Qué recibe.** Un quad de pantalla completa emitido por el `Blitter` de URP, cuyas coordenadas cubren de (0,0) a (1,1). Dos texturas: `_BlitTexture` (la escena renderizada, que el Blitter asigna automáticamente) y `_HighlightTexture` (la `HighlightRT`). Y los parámetros expuestos en el Renderer Feature.

Como la geometría es siempre la misma, este shader no define su propio vertex shader: usa el que provee `Blit.hlsl`.

**Fragment shader, paso a paso:**

*Pixelación.* Si `_PixelSize > 1`, la coordenada se cuantiza a una grilla:

```hlsl
float2 pixelCount = _ScreenParams.xy / _PixelSize;
uv = floor(uv * pixelCount) / pixelCount;
```

Con una pantalla de 1920 de ancho y `_PixelSize = 8` resultan 240 celdas. Todos los píxeles cuya coordenada escalada caiga dentro de la misma celda leen el mismo punto de la textura, formando un bloque uniforme.

*Lectura de la escena.* Se muestrea `_BlitTexture` con la coordenada — pixelada si corresponde. El color obtenido ya contiene toda la iluminación calculada por los materiales de la escena.

*Lectura del highlight.* Se muestrea `_HighlightTexture` con la coordenada **original**, sin pixelar, de modo que los highlights conserven bordes nítidos aunque el fondo esté pixelado.

*Decisión.* Si el píxel tiene color en la textura de highlights (cualquier canal por encima de 0.1, umbral que descarta el fondo transparente), pertenece a un objeto marcado:

```hlsl
float luminance = max(color.r * 0.299 + color.g * 0.587 + color.b * 0.114, 0.4);
return half4(highlight.rgb * luminance, 1);
```

La **luminancia** es el brillo perceptual del píxel original. Los pesos no son un promedio simple porque la sensibilidad del ojo humano no es uniforme entre canales: es mayor al verde, intermedia al rojo y menor al azul.

Multiplicar el color del highlight por esa luminancia produce una escala monocroma de ese color: las zonas iluminadas del objeto quedan claras y las sombreadas oscuras. El objeto conserva el volumen que la iluminación le dio, con el color reemplazado — a diferencia de una silueta plana, que se vería como una mancha sin forma interna.

El `max(..., 0.4)` establece un piso de brillo. Sin él, un objetivo en una zona oscura tendría luminancia cercana a cero y su highlight resultaría ilegible. Dado que los highlights son información de gameplay, se prioriza la legibilidad garantizada por sobre la fidelidad lumínica exacta.

*Fondo.* Si el píxel no está marcado, se convierte a monocromo y se le suma ruido:

```hlsl
float grey = color.r * 0.299 + color.g * 0.587 + color.b * 0.114;
float noise = Random(uv + _Time.y) * _NoiseIntensity;
grey = saturate(grey + noise);
return half4(_TintColor.rgb * grey, color.a);
```

`grey` es un valor entre 0 y 1 que representa el brillo del píxel. Multiplicar un color por ese brillo produce la escala completa de ese color: con `_TintColor` en blanco el resultado es la escala de grises clásica, y cualquier otro color produce una escala monocroma de ese tono.

**El generador de ruido:**

```hlsl
float Random(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}
```

El producto punto colapsa la coordenada bidimensional a un escalar único por píxel. El seno lo convierte en un valor oscilante. La multiplicación por un número grande hace que entre píxeles vecinos el resultado salte miles de ciclos de la función, volviéndolo caótico. La parte fraccionaria deja un valor entre 0 y 1 sin correlación aparente con el píxel adyacente.

Es pseudoaleatorio: la misma entrada produce siempre la misma salida. Por eso se suma `_Time.y`, que desplaza la entrada en cada frame y genera grano animado en lugar de estático.

### 4.4 El material invisible del cono

El mesh del cono se dibuja dos veces por frame con dos programas distintos.

En el **render normal**, su `MeshRenderer` usa un material transparente con alpha 0. Atraviesa el pipeline completo, pero la ecuación de blending anula su contribución: el resultado es el fondo intacto.

En el **pass de highlights**, el material override lo sustituye por el translúcido de su tipo, y allí sí deja su marca en la Render Texture.

La alternativa aparentemente más simple —excluir el layer del Culling Mask de la cámara— no funciona: ese culling alimenta también las listas de renderizado del pass propio, por lo que el cono desaparecería de ambos contextos.

---

## 5. Cómo correr el proyecto

**Requisitos**

- Unity **6.3 LTS** (`6000.3.10f1`)
- Universal Render Pipeline
- Paquetes: Input System, AI Navigation, TextMeshPro

**Ejecución**

Abrir el proyecto y cargar la escena `MainMenu`, que es el punto de entrada del juego.

| Escena | Descripción |
|---|---|
| `MainMenu` | Menú principal |
| `Credits` | Créditos y atribución de assets |
| `Level1` · `Level2` · `Level3` | Niveles jugables |

**Controles**

| Acción | Tecla |
|---|---|
| Moverse | `WASD` |
| Correr | `Shift` |
| Agacharse | `Ctrl` |
| Visión aumentada | `F` |
| Interactuar | `E` |
| Pausa | `Esc` |

**Ajustar el efecto**

Los parámetros se configuran en el asset `PC_Renderer`, dentro de los Renderer Features:

- **EnhancedVisionRendererFeature** — intensidad del ruido, tamaño de pixelación y color de tinte
- **HighlightRendererFeature** — lista de tipos de highlight y la Render Texture de destino
