using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MovementSystem : MonoBehaviour
{
    [Header("Selectors")]
    [SerializeField] private UnitSelector unitSelector;
    [SerializeField] private HexGridSelector hexGridSelector;
    [SerializeField] private GridManager gridManager;

    private BFSResult movementRange = new();
    private List<Vector3Int> currentPath = new();
    private Unit selectedUnit;
    private Unit movingUnit;

    public bool HasActiveUnit => selectedUnit != null;

    private void OnEnable()
    {
        if (unitSelector != null)
        {
            unitSelector.OnUnitSelected += HandleUnitSelected;
            unitSelector.OnUnitDeselected += HandleUnitDeselected;
        }

        if (hexGridSelector != null)
        {
            hexGridSelector.OnTileClicked += ProcessMovementClick;
        }
    }

    private void OnDisable()
    {
        if (unitSelector != null)
        {
            unitSelector.OnUnitSelected -= HandleUnitSelected;
            unitSelector.OnUnitDeselected -= HandleUnitDeselected;
        }

        if (hexGridSelector != null)
        {
            hexGridSelector.OnTileClicked -= ProcessMovementClick;
        }

        if (movingUnit != null)
        {
            movingUnit.MovementFinished -= HandleMovementFinished;
        }
    }

    internal void InjectComponents(GridManager manager, UnitSelector unitSelect, HexGridSelector gridSelector)
    {
        if (manager != null) gridManager = manager;
        if (unitSelect != null) unitSelector = unitSelect;
        if (gridSelector != null) hexGridSelector = gridSelector;
    }

    private void HandleUnitSelected(Unit unit)
    {
        // If a different unit was already active, clear its range first
        if (selectedUnit != null && selectedUnit != unit)
        {
            HideRange();
        }

        selectedUnit = unit;
        ShowRange(selectedUnit);

        if (hexGridSelector != null && hexGridSelector.CurrentSelectedTile != null)
        {
            if (!IsHexInRange(hexGridSelector.CurrentSelectedTile.tileCoordinates))
            {
                hexGridSelector.ClearSelection();
            }
        }
    }

    private void HandleUnitDeselected(Unit unit)
    {
        // If the unit is currently moving, do not hide the path highlights prematurely.
        // HandleMovementFinished will clean them up when the unit reaches its destination.
        if (movingUnit != null) return;

        HideRange();
        selectedUnit = null;
    }

    public void ProcessMovementClick(HexTileData clickedTile)
    {
        if (selectedUnit == null) return;

        // Check if the clicked tile is within the valid movement range
        if (IsHexInRange(clickedTile.tileCoordinates))
        {
            // 1. Generate and display the path to the clicked tile
            //currentPath = movementRange.GetPathTo(clickedTile.tileCoordinates);

            // Highlight the path visuals
            ShowPath(clickedTile.tileCoordinates);

            movingUnit = selectedUnit;
            movingUnit.MovementFinished += HandleMovementFinished;

            // 2. Execute movement along that path
            MoveUnit();

            // 3. Cleanup selection state after moving
            if (unitSelector != null)
            {
                unitSelector.ClearSelection();
            }
            selectedUnit = null;
        }
    }

    public void HideRange()
    {
        if (movementRange.visitedNodesDict == null) return;

        foreach (Vector3Int hexPosition in movementRange.GetRangePositions())
        {
            HexTileData tile = gridManager.GetTileAt(hexPosition);

            if (tile != null)
            {
                tile.DisableHighlight(true);
            }
        }
        movementRange = new();
        currentPath.Clear(); // Clears lingering path references
    }

    private void HandleMovementFinished(Unit unit)
    {
        unit.MovementFinished -= HandleMovementFinished;

        // Reset and turn off path highlights once the unit reaches its target
        foreach (Vector3Int hexPosition in currentPath)
        {
            HexTileData tile = gridManager.GetTileAt(hexPosition);
            if (tile != null)
            {
                tile.DisableHighlight(true);
            }
        }

        movementRange = new();
        currentPath.Clear();
        if (movingUnit == unit) movingUnit = null;
    }

    public void ShowRange(Unit unit)
    {
        CalculateRange(unit);

        // Utilize cached unit tile coordinate directly without secondary world-position lookups
        Vector3Int unitCoord = unit.CurrentTile.tileCoordinates;

        foreach (Vector3Int hexPosition in movementRange.GetRangePositions())
        {
            if (unitCoord == hexPosition) continue;

            HexTileData tile = gridManager.GetTileAt(hexPosition);
            if (tile != null && !tile.IsObstacle())
            {
                tile.EnableHighlight(true);
            }
        }
    }

    private void CalculateRange(Unit unit)
    {
        Vector3Int startCoord;

        if (unit.CurrentTile != null)
        {
            startCoord = unit.CurrentTile.tileCoordinates;
        }
        else
        {
            // Fallback for initial placement/spawn
            startCoord = gridManager.GetClosestHex(unit.transform.position);
            unit.CurrentTile = gridManager.GetTileAt(startCoord);
        }

        movementRange = GraphSearch.BFSGetRange(gridManager, startCoord, unit.MovementPoints);
    }

    public void ShowPath(Vector3Int selectedHexPosition)
    {
        if (IsHexInRange(selectedHexPosition))
        {
            currentPath = movementRange.GetPathTo(selectedHexPosition);
            HashSet<Vector3Int> pathSet = new(currentPath);

            // Iterate through all tiles in the movement range
            foreach (Vector3Int hexPosition in movementRange.GetRangePositions())
            {
                HexTileData tile = gridManager.GetTileAt(hexPosition);
                if (tile == null) continue;

                if (pathSet.Contains(hexPosition))
                {
                    // If tile is in the path, apply highlight
                    tile.EnableHighlight(true);
                }
                else
                {
                    // If tile is in range but NOT in the path, remove highlight completely
                    tile.DisableHighlight(false);
                }
            }
        }
    }

    public void MoveUnit()
    {
        if (selectedUnit == null || currentPath == null || currentPath.Count == 0) return;

        // Update the unit's current tile to the final destination of the path
        Vector3Int destinationCoord = currentPath.Last();
        selectedUnit.CurrentTile = gridManager.GetTileAt(destinationCoord);

        // Build world position list safely
        List<Vector3> worldPositions = new();
        foreach (Vector3Int pos in currentPath)
        {
            HexTileData tile = gridManager.GetTileAt(pos);
            if (tile != null)
            {
                worldPositions.Add(tile.transform.position);
            }
        }

        selectedUnit.MoveThroughPath(worldPositions);
    }

    public bool IsHexInRange(Vector3Int hexPosition)
    {
        return movementRange.IsHexPositionInRange(hexPosition);
    }
}
