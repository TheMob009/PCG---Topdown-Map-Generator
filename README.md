# Proyecto 1 — Procedural Content Generation Pipeline

> A Unity 2D procedural dungeon generator that combines **BSP partitioning**, **Random Walk**, **Perlin Noise** and a **Mission Grammar** system into a fully configurable pipeline.

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Pipeline Architecture](#pipeline-architecture)
- [Algorithms](#algorithms)
  - [1. Perlin Noise — Environmental Layer](#1-perlin-noise--environmental-layer)
  - [2. BSP Map Generator — Structural Layout](#2-bsp-map-generator--structural-layout)
  - [3. Random Walk — Organic Detail](#3-random-walk--organic-detail)
  - [4. Mission Grammar — Narrative Structure](#4-mission-grammar--narrative-structure)
- [Presets](#presets)
- [Project Structure](#project-structure)
- [Screenshots / Demo](#screenshots--demo)
- [Assets & Credits](#assets--credits)

---

## Overview

**Proyecto 1** is an academic project developed for the *Procedural Content Generation* course. It implements a layered PCG pipeline inside Unity that generates complete playable dungeon maps with assigned mission objectives — all from a single master seed.

The system supports two thematic contexts out of the box:

| Context | Map style | Narrative |
|---|---|---|
| **Cave** | Organic cavern with erratic tunnels | Mining mission |
| **Space Station** | Modular rooms connected by straight ducts | Colony mission |

---

## Features

- **Full pipeline in one click** — `PipelineManager` orchestrates all four stages in the correct order.
- **Deterministic via master seed** — A single integer seeds every generator independently, guaranteeing reproducibility.
- **Synchronized dimensions** — Map width/height is set once in `PipelineManager` and automatically propagated to BSP and Perlin.
- **Dual tile sets** — Each context supports a Set A / Set B tile swap driven by the Perlin noise threshold.
- **Mission visualization** — Colour-coded Gizmo overlays and text labels show mission room assignments directly in the Scene view.
- **In-editor custom inspectors** — Every generator exposes a dedicated `[CustomEditor]` with one-click generation buttons.
- **Layered Gizmos** — BSP, Random Walk and Perlin draw at different Z depths so all three layers can be inspected simultaneously.

---

## Pipeline Architecture

```
Master Seed
    │
    ├─► [Stage 1] PerlinMapGenerator   →  float[,] NoiseMap
    │
    ├─► [Stage 2] BspMapGenerator      →  BspMapResult (rooms + corridors + tree)
    │                                           │
    ├─► [Stage 3] RandomWalkGenerator  →  carves organic tunnels over BspMapResult
    │
    ├─► [Stage 4] MissionGrammarGenerator → assigns symbols to rooms via BFS
    │
    └─► [Stage 5] MapVisualizer        →  paints Tilemap using Perlin threshold
                  MissionVisualizer    →  spawns floating markers on assigned rooms
```

All stages share the same `CellType[,]` grid owned by `BspMapResult`. Stages 3–5 read from or write to that grid in order; they must not be executed out of sequence.

---

## Algorithms

### 1. Perlin Noise — Environmental Layer

**File:** [`PerlinMapGenerator.cs`](PerlinNoise/PerlinMapGenerator.cs)

Generates a continuous `float[,]` height map over the full map grid using gradient (Perlin) noise. The implementation delegates the per-cell computation to a shared `PerlinNoiseGenerator`/`HeightmapGenerator` utility that supports multiple **interpolation modes**:

| Mode | Description |
|---|---|
| `Bilinear` | Fast, slightly blocky transitions |
| `Bicubic` | Smooth gradients, default for caves |

Key parameters:

| Parameter | Effect |
|---|---|
| `frequency` | Number of noise cycles across the map. Higher = more detail, smaller features. |
| `seed` | Offsets the sample coordinates, changing the noise pattern entirely. |
| `threshold` | Cut-off value used by `MapVisualizer` to select between tile Set A and Set B. |

The raw gradient noise output (~`[-1, 1]`) is remapped to `[0, 1]` and then further normalized per-run to the actual `[NoiseMin, NoiseMax]` range for consistent visual contrast. The noise layer does **not** affect the walkable geometry — it only drives tile selection and can optionally influence room classification for the mission grammar.

---

### 2. BSP Map Generator — Structural Layout

**Files:** [`BSP/BSPMapGenerator.cs`](BSP/BSPMapGenerator.cs) · [`BSP/BSPNode.cs`](BSP/BSPNode.cs)

Implements **Binary Space Partitioning** to produce a structured room layout.

**Step-by-step:**

1. **Partition** — The root `BspNode` covers the full map rect. `BuildTree()` recursively splits each node either horizontally or vertically (chosen randomly) until `maxIterations` is reached or a partition is smaller than `minPartitionSize`.

2. **Room carving** — Each leaf node receives one room placed randomly within its partition bounds, respecting `roomPadding` and `minRoomSize`. The room rect is carved as `CellType.Floor` into the shared grid.

3. **Corridor connection** — `ConnectRooms()` traverses the tree bottom-up. At each internal node, the representative rooms of the left and right subtrees are connected. Two corridor styles are supported:
   - **L-shaped** (default): randomly picks horizontal-then-vertical or vertical-then-horizontal.
   - **Straight**: uses the parent's split axis to draw a single segment aligned to the midpoint between the two rooms.

4. **Wall painting** — `MapUtils.PaintWalls()` marks every `Empty` cell adjacent to a `Floor` cell as `Wall`.

The complete **BSP tree** is exposed via `BspMapGenerator.Root` so downstream systems (Random Walk, Mission Grammar) can traverse it without re-running the generation.

Key parameters:

| Parameter | Effect |
|---|---|
| `minPartitionSize` | Minimum cell count for a split to occur. Prevents tiny rooms. |
| `maxIterations` | Recursion depth. More iterations → more rooms. |
| `roomPadding` | Gap between room edge and partition edge. |
| `corridorWidth` | Thickness of the carved corridor (1 = single-cell path). |
| `useStraightCorridors` | Toggles L-shaped vs. straight corridor style. |

---

### 3. Random Walk — Organic Detail

**File:** [`RandomWalk/RandomWalkGenerator.cs`](RandomWalk/RandomWalkGenerator.cs)

Runs **N independent drunkard-walk agents** over the existing BSP grid to add organic tunnel variation. Each agent starts inside a BSP room and carves `Floor` cells as it moves.

**Agent behaviour per step:**

```
direction = (rand() < directionPersistence) ? lastDirection : randomDirection
next = pos + direction
if next out of bounds → pick random fallback direction
move to next, carve square of radius (walkWidth/2) at new pos
```

The `directionPersistence` parameter is the key to controlling aesthetic output:

| Value | Result |
|---|---|
| `0.0` | Fully random — erratic cavern-like tunnels |
| `0.35` | Moderately directed — natural gallery feel |
| `0.85` | Highly persistent — straight ducts with occasional bends |

Additional options:

- **`spawnFromRoomEdge`** — agents start at a room corner instead of the center, making tunnels feel like ducts that run along walls.
- **`walkWidth`** — controls the carving radius; values > 1 produce gallery-width passages.

Cells carved exclusively by the Random Walk are tracked in a `HashSet<Vector2Int>` (`CarvedByWalk`) and used by `MapVisualizer` to apply a distinct tile set to walk tunnels vs. BSP rooms.

After all agents finish, `MapUtils.PaintWalls()` is called again to update wall borders around newly carved tiles.

---

### 4. Mission Grammar — Narrative Structure

**Files:** [`MissionGrammar/MissionGrammarGenerator.cs`](MissionGrammar/MissionGrammarGenerator.cs) · [`MissionGrammar/MissionSymbol.cs`](MissionGrammar/MissionSymbol.cs)

Generates a **mission sequence** by applying a simple sequential string grammar, then assigns each terminal symbol to a BSP room via **BFS traversal** of the room adjacency graph.

#### 4a. Grammar expansion

The grammar is configurable entirely from the Inspector:

| Config field | Purpose |
|---|---|
| `startSymbol` | Axiom (e.g. `"M"`) |
| `startProduction` | Initial expansion (e.g. `"SETG"`) |
| `taskSymbol` | Non-terminal to expand repeatedly (e.g. `"T"`) |
| `taskProductions` | Rules for the non-terminal (e.g. `"RT"`, `"CT"`, `"KTL"`, `"ET"`) |
| `terminalProduction` | Replaces leftover non-terminals at the end (e.g. `"C"`) |
| `expansionSteps` | How many times the non-terminal is expanded before termination |

Example derivation (`expansionSteps = 2`):

```
M       →  SETG
SETG    →  SERTG       (T → RT)
SERTG   →  SERCTG      (T → CT)
SERCTG  →  SERCCG      (remaining T → C, terminal)
Final chain: SERCCG
```

#### 4b. Room assignment

1. **Adjacency graph** — Built by recursively walking the BSP tree. Sibling nodes whose representative rooms were connected by a BSP corridor become edges in the graph.
2. **BFS from room 0** — Rooms are visited in breadth-first order (with shuffled neighbours for variety). Each terminal symbol in the chain is assigned to the next room in BFS order.
3. **Overflow handling** — If the chain is longer than the number of available rooms, intermediate symbols are discarded and the last symbol is pinned to the last room.

#### 4c. Mission symbols

| Char | Symbol | Cave context | Colony context |
|---|---|---|---|
| `S` | Start | *Begin at the main gallery* | *Begin at the central module* |
| `E` | Explore | *Explore the gallery* | *Inspect the module* |
| `R` | Collect | *Collect mineral* | *Recover resources* |
| `C` | Combat | *Defeat the enemies* | *Eliminate the threat* |
| `K` | Key | *Find the key* | *Get the access card* |
| `L` | Lock | *Open the gate* | *Unlock the room* |
| `G` | Goal | *Reach the elevator* | *Reactivate the system* |

---

## Presets

`PipelineManager` exposes two ready-to-use presets accessible via **Context Menu** in the Inspector:

### Excavated Cave
```
Map: 60 × 40
Perlin frequency: 2.0  |  interpolation: Bicubic  |  threshold: 0.5
BSP: minPartition=14, iterations=3, padding=3, minRoom=8, corridor=1 (L-shape)
Walk: 6 agents × 100 steps, width=2, persistence=0.35, center spawn
Mission context: Mina, 2 expansion steps
```

### Space Station
```
Map: 80 × 60
Perlin frequency: 6.0  |  interpolation: Bicubic  |  threshold: 0.55
BSP: minPartition=25, iterations=4, padding=6, minRoom=15, corridor=3 (straight)
Walk: 3 agents × 80 steps, width=1, persistence=0.85, edge spawn
Mission context: Colonia, 3 expansion steps
```

---

## Project Structure

```
Assets/Scripts/Proyecto1/
│
├── PipelineManager.cs          # Orchestrates all four stages; exposes presets
├── MapVisualizer.cs            # Paints the Tilemap from BspMapResult + Perlin
│
├── BSP/
│   ├── BSPMapGenerator.cs      # BSP tree generation, room carving, corridors
│   ├── BSPNode.cs              # Tree node: partition rect, split logic, room ref
│   └── MapData.cs              # BspRoom, BspMapResult, CellType enum
│
├── PerlinNoise/
│   └── PerlinMapGenerator.cs   # Gradient noise sampler, normalisation, threshold
│
├── RandomWalk/
│   ├── RandomWalkGenerator.cs  # Multi-agent drunkard walk with persistence
│   └── MapUtils.cs             # Shared wall-painting utility
│
├── MissionGrammar/
│   ├── MissionGrammarGenerator.cs  # Grammar expansion + BFS room assignment
│   ├── MissionSymbol.cs            # Symbol enum, colours, contextual descriptions
│   ├── MissionRoomAssignment.cs    # Data class: room ↔ symbol pairing
│   ├── MissionVisualizer.cs        # Spawns/clears floating markers in the scene
│   └── FloatingMarker.cs           # MonoBehaviour for world-space label markers
│
└── Editor/
    ├── BSPMapGeneratorEditor.cs
    ├── PerlinMapGeneratorEditor.cs
    ├── RandomWalkGeneratorEditor.cs
    ├── MissionGrammarGeneratorEditor.cs
    ├── PipelineManagerEditor.cs
    └── TopdownSceneSetup.cs        # Editor utility for quick scene scaffolding
```

---

## Assets & Credits

### Tile Sets & Art

| Asset | Author | Link |
|---|---|---|
| Cave Platformer Tileset (floor, wall & platformer set) | **RottingPixels** | [itch.io](https://rottingpixels.itch.io/cave-platformer-tileset-16x16free) |
| Free Sci-Fi TileSet — Space Station | **Aske4** | [itch.io](https://aske4.itch.io/free-sci-fi-tileset-space-station) |
| Fantasy UI Borders | **Kenney** | [kenney.nl](https://kenney.nl/assets/fantasy-ui-borders) |

### Fonts

| Font | Author | Link |
|---|---|---|
| Monogram | **Datagoblin** | [itch.io](https://datagoblin.itch.io/monogram) |

### Unity Packages

| Package | Purpose |
|---|---|
| **2D Tilemap Extras** (Unity Technologies) | Rule Tiles and animated tile support |
| **TextMesh Pro** (Unity Technologies) | UI text rendering for the in-game panel |

### References

- Shaker, N., Togelius, J., & Nelson, M. J. (Eds.). (2016). *Procedural Content Generation in Games*. Springer. Available at: [http://pcgbook.com](http://pcgbook.com)
- Adams, D. (2010). *Dungeon generation using BSP trees*. Roguebasin. [http://www.roguebasin.com/index.php/Basic_BSP_Dungeon_generation](http://www.roguebasin.com/index.php/Basic_BSP_Dungeon_generation)
- Perlin, K. (2002). *Improving Noise*. SIGGRAPH 2002. [https://mrl.nyu.edu/~perlin/paper445.pdf](https://mrl.nyu.edu/~perlin/paper445.pdf)
