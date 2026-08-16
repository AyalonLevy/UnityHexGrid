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

    private void OnEnable()
    {
        if (unitSelector != null)
        {
            unitSelector.OnUnitSelected += HandleUnitSelected;
            unitSelector.OnUnitDeselected += HandleUnitDeselected;
        }

        if (hexGridSelector != null)
        {
            hexGridSelector.OnTileSelected += HandleTileSelected;
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
            hexGridSelector.OnTileSelected -= HandleTileSelected;
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
    }

    private void HandleUnitDeselected(Unit unit)
    {
        HideRange();
        selectedUnit = null;
    }

    private void HandleTileSelected(HexTileData clickedTile)
    {
        if (selectedUnit == null) return;

        // Check if the clicked tile is within the valid movement range
        if (IsHexInRange(clickedTile.tileCoordinates))
        {
            // 1. Generate and display the path to the clicked tile
            currentPath = movementRange.GetPathTo(clickedTile.tileCoordinates);

            // Highlight the path visuals
            ShowPath(clickedTile.tileCoordinates);

            // 2. Execute movement along that path
            MoveUnit();

            // 3. Cleanup selection state after moving
            unitSelector.ClearSelection();
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
                tile.DisableHighlight();
            }
        }
        movementRange = new();
    }

    public void ShowRange(Unit unit)
    {
        CalculateRange(unit);

        foreach (Vector3Int hexPosi in movementRange.GetRangePositions())
        {
            HexTileData tile = gridManager.GetTileAt(hexPosi);
            if (tile != null && !tile.IsObstacle())
            {
                tile.EnableHighlight();
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
        if (movementRange.GetRangePositions().Contains(selectedHexPosition))
        {
            foreach (Vector3Int hexPosition in currentPath)
            {
                gridManager.GetTileAt(hexPosition).ResetHighlight();
            }
            currentPath = movementRange.GetPathTo(selectedHexPosition);
            foreach (Vector3Int hexPosition in currentPath)
            {
                gridManager.GetTileAt(hexPosition).HighlightPath();
            }
        }
    }

    public void MoveUnit()
    {
        if (selectedUnit == null || currentPath == null || currentPath.Count == 0) return;

        Debug.Log($"Moving unit {selectedUnit.name}");

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
