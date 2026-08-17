# Hex Grid Generator - User Manual

Welcome to the detailed documentation for the Hex Grid Generator. This manual covers the core architecture, data management, and subsystem configurations.

## Table of Contents
1. [Core Architecture & Data](#1-core-architecture--data)
2. [Terrain & Overrides (HexTypes)](#2-terrain--overrides-hextypes)
3. [World Generation Methods](#3-world-generation-methods)
4. [Subsystems (Fog, Pathfinding)](#4-subsystems-fog-pathfinding)

---

## 1. Core Architecture & Data

The system relies on a strictly decoupled data architecture to ensure designers can build worlds without touching code.

### HexGridDatabase
The master `ScriptableObject` that holds references to all available Hex Tiles and Props in your project.
* **Hex Grid Tiles:** Contains a list of visual hex meshes, their associated `TileDomain`, and their weighted **Spawn Chance**.
* **Props:** Contains environmental assets (trees, rocks) and defines which `TileDomain` they are allowed to spawn on.

### TileDomain
A `ScriptableObject` acting as a "Biome" or "Category" tag (e.g., Grass, Desert, Water). 
* Defines the base `HexType` (e.g., `Default`, `Water`, `Obstacle`).
* Props use Domains to determine valid spawn locations.

---

## 2. Terrain & Overrides (HexTypes)

Pathfinding relies heavily on `HexType` to determine movement costs. A tile's final `HexType` is dynamically evaluated via the `HexTileData.EvaluateHexType()` method.

**The Override Hierarchy:**
1. **Base Layer:** The tile takes its base terrain from its `TileDomain` (e.g., Water = 30 cost).
2. **Prop Modifier:** If a prop spawns on the tile, it overrides the base domain based on severity:
   * **Obstacles:** Always override everything (`int.MaxValue` cost).
   * **Roads:** Override `Difficult` or `Water` terrain, lowering movement cost (e.g., a Bridge prop).
   * **Difficult:** Overrides `Default` terrain, but *cannot* accidentally downgrade inherently impassable terrain (like Water).

*Note: Water traversal logic (e.g., swimming units) should be handled inside the `MovementSystem` by comparing the unit's capabilities against the tile's declared `HexType.Water`.*

---

## 3. World Generation Methods

The `HexGridGenerator.cs` script builds the physical world inside the `GridContainer`. It supports three distinct shapes:

* **Rectangle:** Generates a standard staggered grid based on an `X` and `Y` size.
* **Hexagonal:** Generates concentric rings radiating outward based on a `Radius` parameter.
* **From File:** Instantiates a pre-defined grid from a JSON file. Use the **Save Grid to File** inspector button to serialize a customized edit-mode grid into a text asset for permanent storage or level-loading.

---

## 4. Subsystems (Fog, Pathfinding)

Subsystems are toggled directly on the `HexGridGenerator`. When enabled, the generator automatically attaches the necessary manager scripts to the `GridContainer` during generation.

### Fog of War (`HexTileFogController` & `FogRevealer`)
* When enabled, a secondary `Fog` child object is instantiated above every tile.
* Units equipped with a `FogRevealer` component will raycast or calculate distance to disable the visual fog mesh on overlapping tiles, shifting them from `Hidden` to `Explored` or `Visible`.

### BFS Pathfinding (`MovementSystem` & `UnitSelector`)
* Injects all generated coordinates into a Breadth-First Search (BFS) graph.
* Handles movement execution by evaluating `GetCost()` on neighboring tiles.
* Path selection highlights are mapped using the `Highlight.cs` unlit materials.

### Grid Selection (`HexGridSelector`)
* Manages physics raycasting against the convex `MeshCollider` generated on each tile.
* Broadcasts selection events for UI or Unit controllers to consume.