using System;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(GridManager))]
public class HexGridSelector : Selector
{
    private HexTileData currentlySelectedTile;

    // Public API Events for external tool integration
    public event Action<HexTileData> OnTileSelected;
    public event Action<HexTileData> OnTileDeselected;
    public event Action<HexTileData> OnTileClicked;

    [HideInInspector] public bool isSelectable = false;

    public HexTileData CurrentSelectedTile => currentlySelectedTile;
    public void SetSelectableState(bool canBeSelected) { isSelectable = canBeSelected; }

    protected override void HandleRaycastHit(RaycastHit hit)
    {
        HexTileData hitTile = hit.collider.GetComponentInParent<HexTileData>();

        if (hitTile != null)
        {
            // 1. Check if pathfinding is active and this clicked tile is a highlighted movement target
            if (TryGetComponent<MovementSystem>(out var movementSystem) &&
                movementSystem.HasActiveUnit &&
                movementSystem.IsHexInRange(hitTile.tileCoordinates))
            {
                // It is highlighted as a valid move destination! Send action and return.
                movementSystem.ProcessMovementClick(hitTile);
                return;
            }

            // 2. Otherwise, it is NOT highlighted for movement. Continue with normal tile selection/building logic.
            if (isSelectable)
            {
                if (hitTile != currentlySelectedTile)
                {
                    SelectTile(hitTile);
                }
                else
                {
                    ClearSelection();
                }
            }
        }
        else
        {
            // Check if the tile is part of a possible path
            if (TryGetComponent<MovementSystem>(out var movementSystem) && movementSystem.HasActiveUnit)
            {
                if (currentlySelectedTile != null && movementSystem.IsHexInRange(currentlySelectedTile.tileCoordinates))
                {
                    return;
                }
            }

            ClearSelection();
        }
    }

    protected override void HandleRaycastMiss()
    {
        ClearSelection();
    }

    private void SelectTile(HexTileData newTile)
    {
        if (currentlySelectedTile != null)
        {
            currentlySelectedTile.DisableHighlight(false);
            OnTileDeselected?.Invoke(currentlySelectedTile);
        }

        currentlySelectedTile = newTile;

        if (currentlySelectedTile != null)
        {
            currentlySelectedTile.EnableHighlight(false);
            OnTileSelected?.Invoke(currentlySelectedTile);
        }
    }

    public void ClearSelection()
    {
        if (currentlySelectedTile != null)
        {
            currentlySelectedTile.DisableHighlight(false);
            OnTileDeselected?.Invoke(currentlySelectedTile);
            currentlySelectedTile = null;
        }
    }
}
