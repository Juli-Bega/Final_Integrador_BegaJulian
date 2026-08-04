*[Leer en español](README.md)*

# Agent O7

First-person stealth game built in Unity 6.3 LTS with the Universal Render Pipeline.

The player infiltrates a guarded facility, recovers the valuables and escapes without being seen. Their only tool is an **enhanced vision** mode that reveals guards, objectives and patrol vision cones, even through walls.

**[Play on itch.io](https://julian-bega.itch.io/agent.o7)** · **[Download the build](../../releases)**

---

## Contents

1. [The project](#1-the-project)
2. [The shader](#2-the-shader)
3. [Implementation](#3-implementation)
4. [How it works](#4-how-it-works)
5. [Running the project](#5-running-the-project)

---

## 1. The project

Final integrative project for the Game Development program at Image Campus. The assignment calls for a simple 3D game featuring at least one custom shader, with an emphasis on understanding shader concepts and applying them in a game context.

Claude (Anthropic) was used during development for technical consultation and code review. External assets are credited in the game's credits scene.

---

## 2. The shader

An **enhanced vision** effect in the style of the "detective mode" found in games like Assassin's Creed, Batman: Arkham or Hitman.

When activated, the scene turns monochrome with configurable noise and pixelation, while relevant elements are highlighted in colour and stay visible **even when hidden behind walls**.

The system is built from two shaders working together:

| Shader | Role |
|---|---|
| `Highlight.shader` | Draws marked objects into an auxiliary texture, ignoring occlusion |
| `EnhancedVision.shader` | Full-screen post-process that combines the scene with that texture |

---

## 3. Implementation

### 3.1 In-game behaviour

Vision toggles with **F**. While active:

- The scene renders monochrome, with animated noise and pixelation
- Guards, objectives, exits and buttons are each highlighted in their own colour
- Guard vision cones become visible
- All of it is visible **through walls**
- The player **cannot move**

The movement restriction is a design decision: vision is a planning tool, not a navigation one. The player stops, observes patrol routes and positions, then returns to normal mode to act.

### 3.2 Architecture

No gameplay script touches the shaders directly. All communication goes through a single intermediary:

```
PlayerController          (input: F key)
        │
        ▼
ShaderService             (sole point of contact with the render system)
        │
        ├──► EnhancedVisionRendererFeature ──► EnhancedVisionRenderPass ──► EnhancedVision.shader
        ├──► HighlightRendererFeature      ──► HighlightRenderPass      ──► Highlight.shader
        └──► VisionCone[]                  (cone visibility in the scene)
```

`ShaderService` holds no game logic: it receives a `VisionState(bool)` call and translates it into operations on the render pipeline. With vision inactive, both Renderer Features are disabled and their passes are never enqueued — GPU cost is zero.

### 3.3 System scripts

| Script | Responsibility |
|---|---|
| `ShaderService` | Intermediary between gameplay and the render system |
| `EnhancedVisionRendererFeature` | Registers the post-process in URP; exposes its parameters |
| `EnhancedVisionRenderPass` | Executes the post-process through the Render Graph |
| `HighlightRendererFeature` | Registers the highlight pass; defines the highlight types |
| `HighlightRenderPass` | Draws marked objects into the Render Texture |
| `HighlightType` | Serializable struct: name, layers and material for each type |
| `VisionCone` | Generates each guard's vision cone mesh |

### 3.4 Highlight types

Highlight categories are not hardcoded. `HighlightRendererFeature` exposes a list configurable from the Inspector, where each entry defines:

- **Name** — descriptive label, purely for Inspector readability
- **Layers** — layer mask covered by that type
- **Material** — material those objects will be drawn with

Current project setup:

| Name | Layer |
|---|---|
| Enemies | `Enemies` |
| Cone | `ConeHighlight` |
| Objective | `Objectives` |
| Exit | `Exit` |
| Buttons | `Buttons` |

Adding a new category requires no code changes: one more entry in the list and a new material.

**List order determines draw order.** Since there is no depth testing inside the auxiliary texture, types further down the list are drawn on top of earlier ones. Translucent materials, such as the cone, are best placed last.

### 3.5 Project layers

| Layer | Purpose |
|---|---|
| `Default` | Environment geometry (obstacles for raycasts and occlusion) |
| `Player` | Player — excluded from line-of-sight raycasts |
| `Enemies` | Guards |
| `Objectives` | Collectibles |
| `ConeHighlight` | Vision cone meshes |
| `Exit` | Level exit zone |
| `Buttons` | Interactable buttons |

### 3.6 The vision cone

Each guard carries a procedurally generated mesh representing their field of view: a triangle fan built with trigonometric functions.

**It clips against geometry.** Each vertex along the arc is positioned by a raycast: if the ray hits a wall before reaching maximum range, the vertex is placed at the impact point. The mesh is recalculated at a configurable interval while the cone is visible.

**Its parameters come from the guard.** Range and angle are not configured on the cone: they are received from `GuardController`, the single source of truth. This guarantees that what the player sees matches the actual detection area.

---

## 4. How it works

### 4.1 The full frame

With vision active, each frame runs this sequence:

```
1. NORMAL SCENE RENDER (URP)
   Target: camera colour texture
   All objects with their real materials.
        │
2. HIGHLIGHT PASS          (event: AfterRenderingOpaques)
   Target: HighlightRT (dedicated Render Texture, no depth buffer)
   ├── Clear to transparent
   └── For each highlight type: draw its layers with an override
       material and depth testing disabled
        │
3. ENHANCED VISION PASS    (event: AfterRenderingPostProcessing)
   ├── Blit 1: camera → temporary texture, applying the shader
   │           (reads the scene and the HighlightRT, decides per pixel)
   └── Blit 2: temporary texture → camera
        │
4. The result reaches the screen.
```

With vision inactive, steps 2 and 3 never run.

Step 3 uses two blits because a texture cannot be read from and written to within a single draw operation: the temporary texture breaks that dependency.

**The `HighlightRT` is not an overlay layer.** It is a lookup table: step 2 fills it with coloured silhouettes over a transparent background, and step 3 reads it to decide the colour of each screen pixel. It is never displayed directly.

### 4.2 Highlight.shader

Draws marked objects into the auxiliary Render Texture.

**Inputs.** The object's original mesh — only the position attribute is declared, since the fragment returns a flat colour and needs neither UVs nor normals. The object's transformation matrix, provided automatically by Unity. And `_HighlightColor`, defined by the material.

**Override material.** The object's real material is replaced during this pass by the one belonging to its highlight type. The same mesh is drawn with a different program, without duplicating objects or swapping materials at runtime.

**Vertex shader.** A single operation per vertex: `TransformObjectToHClip()`, taking the position from object space to clip space. Since the same camera is used as in the normal render, the silhouette lands on exactly the same pixels the object occupies (or would occupy) in the scene image. That correspondence is what allows both textures to be sampled with the same coordinate.

**Fragment shader.** Returns `_HighlightColor` with no computation. All shape information comes from the geometry; the colour identifies the category.

**Render states.** This is where the core mechanic lives:

```
ZTest Always                          → see through walls
ZWrite Off                            → writes no depth
Cull Off                              → the cone is a single-sided mesh
Blend SrcAlpha OneMinusSrcAlpha       → allows translucent materials
```

`ZTest Always` disables depth comparison: the fragment is always drawn, regardless of what stands in front. In the normal render, a guard behind a wall loses that comparison and is not drawn; here it is drawn anyway.

That line, combined with the pass having its own render target, is what produces the effect: the silhouette is recorded as information without being painted onto the screen.

### 4.3 EnhancedVision.shader

The full-screen post-process. A single draw call affecting every pixel.

**Inputs.** A full-screen quad emitted by URP's `Blitter`, with coordinates spanning (0,0) to (1,1). Two textures: `_BlitTexture` (the rendered scene, assigned automatically by the Blitter) and `_HighlightTexture` (the `HighlightRT`). Plus the parameters exposed in the Renderer Feature.

Since the geometry is always the same, this shader defines no vertex shader of its own: it uses the one provided by `Blit.hlsl`.

**Fragment shader, step by step:**

*Pixelation.* If `_PixelSize > 1`, the coordinate is quantised to a grid:

```hlsl
float2 pixelCount = _ScreenParams.xy / _PixelSize;
uv = floor(uv * pixelCount) / pixelCount;
```

On a 1920-wide screen with `_PixelSize = 8` that yields 240 cells. Every pixel whose scaled coordinate falls within the same cell samples the same point of the texture, forming a uniform block.

*Scene sample.* `_BlitTexture` is sampled with the coordinate — pixelated if applicable. The resulting colour already contains all the lighting computed by the scene's materials.

*Highlight sample.* `_HighlightTexture` is sampled with the **original** coordinate, unpixelated, so highlights keep sharp edges even when the background is blocky.

*Decision.* If the pixel has colour in the highlight texture (any channel above 0.1, a threshold that discards the transparent background), it belongs to a marked object:

```hlsl
float luminance = max(color.r * 0.299 + color.g * 0.587 + color.b * 0.114, 0.4);
return half4(highlight.rgb * luminance, 1);
```

**Luminance** is the perceptual brightness of the original pixel. The weights are not a plain average because human eye sensitivity is not uniform across channels: highest for green, intermediate for red, lowest for blue.

Multiplying the highlight colour by that luminance produces a monochrome scale of that colour: lit areas of the object stay bright and shaded areas go dark. The object retains the volume lighting gave it, with its colour replaced — unlike a flat silhouette, which would read as a shapeless blob.

The `max(..., 0.4)` sets a brightness floor. Without it, an objective in a dark area would have near-zero luminance and its highlight would be illegible. Since highlights are gameplay information, guaranteed legibility takes priority over exact lighting fidelity.

*Background.* If the pixel is not marked, it is converted to monochrome and noise is added:

```hlsl
float grey = color.r * 0.299 + color.g * 0.587 + color.b * 0.114;
float noise = Random(uv + _Time.y) * _NoiseIntensity;
grey = saturate(grey + noise);
return half4(_TintColor.rgb * grey, color.a);
```

`grey` is a value between 0 and 1 representing pixel brightness. Multiplying a colour by that brightness produces the full scale of that colour: with `_TintColor` set to white the result is classic greyscale, and any other colour produces a monochrome scale of that hue.

**The noise generator:**

```hlsl
float Random(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}
```

The dot product collapses the two-dimensional coordinate into a single scalar per pixel. The sine turns it into an oscillating value. Multiplying by a large number makes the result jump thousands of cycles between neighbouring pixels, rendering it chaotic. The fractional part leaves a value between 0 and 1 with no apparent correlation to adjacent pixels.

It is pseudorandom: the same input always yields the same output. That is why `_Time.y` is added — it shifts the input every frame, producing animated grain rather than static.

### 4.4 The cone's invisible material

The cone mesh is drawn twice per frame with two different programs.

In the **normal render**, its `MeshRenderer` uses a transparent material with alpha 0. It goes through the full pipeline, but the blending equation cancels its contribution: the result is the untouched background.

In the **highlight pass**, the override material replaces it with its type's translucent one, and there it does leave its mark in the Render Texture.

The seemingly simpler alternative — excluding the layer from the camera's Culling Mask — does not work: that culling also feeds the custom pass's renderer lists, so the cone would vanish from both contexts.

---

## 5. Running the project

**Requirements**

- Unity **6.3 LTS** (`6000.3.10f1`)
- Universal Render Pipeline
- Packages: Input System, AI Navigation, TextMeshPro

**Getting started**

Open the project and load the `MainMenu` scene, the game's entry point.

| Scene | Description |
|---|---|
| `MainMenu` | Main menu |
| `Credits` | Credits and asset attribution |
| `Level1` · `Level2` · `Level3` | Playable levels |

**Controls**

| Action | Key |
|---|---|
| Move | `WASD` |
| Run | `Shift` |
| Crouch | `Ctrl` |
| Enhanced vision | `F` |
| Interact | `E` |
| Pause | `Esc` |

**Tuning the effect**

Parameters are configured on the `PC_Renderer` asset, inside the Renderer Features:

- **EnhancedVisionRendererFeature** — noise intensity, pixelation size and tint colour
- **HighlightRendererFeature** — highlight type list and the destination Render Texture
