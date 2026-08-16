using System;
using UnityEngine;

[RequireComponent(typeof(GridManager))]
public class HexGridSelector : Selector
{
    private HexTileData currentlySelectedTile;

    // Public API Events for external tool integration
    public event Action<HexTileData> OnTileSelected;
    public event Action<HexTileData> OnTileDeselected;

    public HexTileData CurrentSelectedTile => currentlySelectedTile;

    protected override void HandleRaycastHit(RaycastHit hit)
    {
        HexTileData hitTile = hit.collider.GetComponentInParent<HexTileData>();

        if (hitTile != null)
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
        else
        {
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
            currentlySelectedTile.DisableHighlight();
            OnTileDeselected?.Invoke(currentlySelectedTile);
        }

        currentlySelectedTile = newTile;

        if (currentlySelectedTile != null)
        {
            currentlySelectedTile.EnableHighlight();
            OnTileSelected?.Invoke(currentlySelectedTile);
            Debug.Log($"Selected Tile at cube coordinates {currentlySelectedTile.tileCoordinates}");
        }
    }

    private void ClearSelection()
    {
        if (currentlySelectedTile != null)
        {
            currentlySelectedTile.DisableHighlight();
            OnTileDeselected?.Invoke(currentlySelectedTile);
            currentlySelectedTile = null;
            Debug.Log("Tile Selection Cleared.");
        }
    }
}
