namespace HexGrid
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public class MovementSystem : MonoBehaviour
    {
        [Header("Selectors")]
        [SerializeField] private UnitSelector unitSelector;
        [SerializeField] private HexGridSelector hexGridSelector;
        [SerializeField] private GridManager gridManager;

        private enum MovementState
        {
            Idle,
            WaitingForDestination,
            Moving
        }

        private MovementState currentState = MovementState.Idle;
        private BFSResult movementRange = new();
        private List<Vector3Int> currentPath = new();
        private Unit selectedUnit;

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

            if (selectedUnit != null)
            {
                selectedUnit.MovementFinished -= HandleMovementFinished;
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
                CleanupUnit(selectedUnit);
            }

            selectedUnit = unit;
            selectedUnit.MovementFinished += HandleMovementFinished;

            CalculateAndShowRange(selectedUnit);
            currentState = MovementState.WaitingForDestination;

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
            // Prevent deselection interruption while the unit is actively walking
            if (currentState == MovementState.Moving) return;

            CleanupUnit(unit);
            selectedUnit = null;
            currentState = MovementState.Idle;
        }

        private void CleanupUnit(Unit unit)
        {
            if (unit != null)
            {
                unit.MovementFinished -= HandleMovementFinished;
            }
            HideRange();
        }

        public void ProcessMovementClick(HexTileData clickedTile)
        {
            if (currentState != MovementState.WaitingForDestination || selectedUnit == null) return;

            // Check if the clicked tile is within the valid movement range
            if (IsHexInRange(clickedTile.tileCoordinates))
            {
                // 1. Generate and display the path to the clicked tile
                currentPath = movementRange.GetPathTo(clickedTile.tileCoordinates);

                // Highlight the path visuals
                ShowPath(clickedTile.tileCoordinates);

                // 2. Execute movement along that path
                currentState = MovementState.Moving;
                MoveUnit();
            }
            else
            {
                // Clicked a tile that is NOT part of the valid path/range -> Deselect the unit
                if (unitSelector != null)
                {
                    unitSelector.ClearSelection();
                }
            }
        }

        public void MoveUnit()
        {
            if (selectedUnit == null || currentPath == null || currentPath.Count == 0) return;

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

        private void HandleMovementFinished(Unit unit)
        {
            // Update the unit's logical current tile to the final destination
            if (currentPath.Count > 0)
            {
                Vector3Int destinationCoord = currentPath.Last();
                unit.CurrentTile = gridManager.GetTileAt(destinationCoord);
            }

            ClearPathHighlights();
            currentPath.Clear();

            CalculateAndShowRange(selectedUnit);
            currentState = MovementState.WaitingForDestination;
        }

        private void CalculateAndShowRange(Unit unit)
        {
            EnsureUnitTile(unit);
            movementRange = GraphSearch.BFSGetRange(gridManager, unit.CurrentTile.tileCoordinates, unit.MovementPoints);

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

        private void EnsureUnitTile(Unit unit)
        {
            if (unit.CurrentTile == null)
            {
                Vector3Int startCoord = gridManager.GetClosestHex(unit.transform.position);
                unit.CurrentTile = gridManager.GetTileAt(startCoord);
            }
        }

        public void ShowPath(Vector3Int selectedHexPosition)
        {
            HashSet<Vector3Int> pathSet = new(currentPath);

            if (IsHexInRange(selectedHexPosition))
            {
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

        private void ClearPathHighlights()
        {
            foreach (Vector3Int hexPosition in currentPath)
            {
                HexTileData tile = gridManager.GetTileAt(hexPosition);
                if (tile != null)
                {
                    tile.DisableHighlight(true);
                }
            }
        }

        public bool IsHexInRange(Vector3Int hexPosition)
        {
            return movementRange.IsHexPositionInRange(hexPosition);
        }
    }
}