# Unity Hex Grid Generator & Pathfinding System

<div align="center">
  <table border="0" cellpadding="0" cellspacing="0" style="border-collapse: collapse; border: none;">
    <tr>
      <!-- GIF 1 + Caption -->
      <td align="center" style="padding: 0; border: none;">
        <img src="Assets/HexGrid/Documentation~/Media/GridGenerator.gif" alt="Procedural Generation" width="220" /><br>
        <b>Procedural Generation</b>
      </td>
      <!-- Gap 1 (Adjust width to control spacing) -->
      <td width="25" style="border: none;"></td>
      <!-- GIF 2 + Caption -->
      <td align="center" style="padding: 0; border: none;">
        <img src="Assets/HexGrid/Documentation~/Media/GridFromFile.gif" alt="Load From File" width="220" /><br>
        <b>Load From JSON</b>
      </td>
      <!-- Gap 2 (Adjust width to control spacing) -->
      <td width="25" style="border: none;"></td>
      <!-- GIF 3 + Caption -->
      <td align="center" style="padding: 0; border: none;">
        <img src="Assets/HexGrid/Documentation~/Media/FoW.gif" alt="Fog of War & Pathfinding" width="220" /><br>
        <b>Fog of War & Pathfinding</b>
      </td>
    </tr>
  </table>
</div>

A robust, data-driven Hexagonal Grid generation framework built for **Unity 6.3**. This package provides a highly modular architecture for generating grid-based worlds, handling terrain types, spawning environmental props, and managing advanced tactical subsystems like Fog of War and BFS Pathfinding.

## Features
* **Procedural Grid Generation:** Generate Hexagonal rings, Rectangular bounds, or load custom layouts directly from JSON files.
* **Data-Driven Architecture:** Manage biomes and spawn rules using ScriptableObjects (`HexGridDatabase` and `TileDomain`).
* **Prop Management:** Automatically spawn, scale, and rotate environmental props (trees, mountains, bridges).
* **Dynamic Terrain Evaluation:** Props dynamically override base terrain types (e.g., placing a bridge over water converts the tile to a passable road).
* **Core Subsystems:**
  * **BFS Pathfinding:** Built-in movement cost evaluation (Difficult terrain, Water, Obstacles).
  * **Fog of War:** Manage Hidden, Explored, and Visible tile states with custom mesh revealer volumes.
  * **Grid Selection:** Raycast-based mouse input and highlight materials.
* **Editor Tooling:** Includes an interactive Setup Wizard and custom Inspector buttons for seamless Edit-Mode generation.

## System Requirements
* **Engine:** Unity 6.3 or higher
* **Pipeline:** Universal Render Pipeline (URP)
* **Dependencies:** None (Uses standard Unity UI and Physics for raycasting)

## Installation
1. Clone this repository or download the source code.
2. Place the `HexGrid` folder into your Unity project's `Assets/` directory.
3. Open the Setup Wizard via the top menu: `Tools -> Hex Grid -> Run Setup Wizard`.

## Quickstart Guide
1. **Run the Wizard:** Use `Tools -> Hex Grid -> Run Setup Wizard` to instantly generate your required folder structure and base database.
2. **Assign Prefabs:** Drag your `HexTile` base prefab into the generator component in the Inspector.
3. **Generate:** Click the green **Generate Grid** button on the `HexGridGenerator` script in your scene to build the environment.
4. **Test Pathfinding:** Ensure `Enable Path Finder` is checked before grid generation, and hit Play to test the `ExamplePlayer` movement.

## Folder Structure
```text
Assets/HexGrid/
├── Editor/            # Custom inspectors and Setup Wizard
├── Runtime/           # Core generation, pathfinding, and interaction logic
├── _SampleAssets/     # Demo scene, example database, and sample prefabs
├── Documentation~/    # Detailed manual and API reference (hidden from Unity compiler)
└── package.json       # UPM Package definition
```

## Developer API & Integration

The Hex Grid framework is highly modular. Instead of forcing rigid function calls, the architecture is event-driven. Other systems (like UI, Game Managers, or AI) can easily interact with the grid by subscribing to selector events.

### 1. Listening for Grid Interactions
The `HexGridSelector` broadcasts events when a player hovers, selects, or clicks on the grid. You can subscribe to these instance events to trigger custom game logic (e.g., updating UI panels).

```csharp
using UnityEngine;

public class CustomGameManager : MonoBehaviour
{
    [SerializeField] private HexGridSelector gridSelector;

    private void OnEnable()
    {
        // Subscribe to grid interaction events
        if (gridSelector != null)
        {
            gridSelector.OnTileSelected += HandleTileSelected;
            gridSelector.OnTileClicked += HandleTileClicked;
        }
    }

    private void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks
        if (gridSelector != null)
        {
            gridSelector.OnTileSelected -= HandleTileSelected;
            gridSelector.OnTileClicked -= HandleTileClicked;
        }
    }

    private void HandleTileSelected(HexTileData selectedTile)
    {
        Debug.Log($"Tile highlighted at Cube Coordinates: {selectedTile.tileCoordinates}");
        Debug.Log($"Terrain Type: {selectedTile.hexType} | Movement Cost: {selectedTile.GetCost()}");
    }

    private void HandleTileClicked(HexTileData clickedTile)
    {
        Debug.Log($"Action executed on tile: {clickedTile.tileCoordinates}");
    }
}
```

### 2. Pathfinding & Movement Pipeline

Pathfinding is handled automatically via the MovementSystem component, which orchestrates communication between the grid and your units. You do not need to call pathfinding math manually; instead, the system reacts to player input:
1. **Unit Selection:** When a unit is selected via the UnitSelector, the MovementSystem detects this and automatically calculates the valid Breadth-First Search (BFS) movement range based on the unit's MovementPoints.
2. **Highlighting:** Valid tiles within range are highlighted automatically.
3. **Execution:** When the player clicks a valid tile, the HexGridSelector fires OnTileClicked. The MovementSystem catches this, generates the exact path, and commands the Unit to begin moving along the world coordinates.

If you need to inject these dependencies manually (e.g., if spawning a map at runtime), you can initialize the system via code:

```csharp
// Example of runtime dependency injection
movementSystem.InjectComponents(gridManager, unitSelector, hexGridSelector);
```

## Sources & Inspiration
This project was built with the help of the following incredible resources:
### Main Source
* [Red Blob Games: Hexagonal Grids Reference Guide](https://www.redblobgames.com/grids/hexagons/)

### YouTube Videos & Tutorials

* [Houdini - Finding Patterns – The Math Behind HEXAGONA | Christos Stavridis | EPC 2023](https://www.youtube.com/watch?v=nbxhnCvexdA)
* [Game Dev Guide - Generating A Hex Map With Fog Of War in Unity](https://www.youtube.com/watch?v=wxVgIH0j8Wg&t=10s)
* [Sunny Valley Studio - Hexagonal Grid Based Movement Tutorial (Playlist)](https://www.youtube.com/playlist?list=PLcRSafycjWFdahp7K-GJBl4NUwzhVmAby)

## License
This project is licensed under the MIT License - see the LICENSE file for details.

Copyright (c) 2026 Ayalon Levy
