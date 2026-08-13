using System.Collections.Generic;
using UnityEngine;

public class FogRevealer : MonoBehaviour
{
    [Header("Vision Settings")]
    [SerializeField] private int visionRadius = 3;

    private GridManager gridManager;
    private Vector3Int lastKnownCubeCoord = new(int.MinValue, int.MinValue, int.MinValue);
    private List<HexTileData> currentlyVisibleTiles = new();

    private void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("FogRevealer: No GridManager found in scene!", this);
            return;
        }
    }

    private void Update()
    {
        if (gridManager == null) return;

        Vector3Int currentCubeCoord = HexGridMath.WorldToCubeCoordinates(transform.position, gridManager.HexRadius);

        if (currentCubeCoord != lastKnownCubeCoord)
        {
            lastKnownCubeCoord = currentCubeCoord;
            UpdateVision(currentCubeCoord);
        }
    }

    private void UpdateVision(Vector3Int centerCoord)
    {
        foreach (var tile in currentlyVisibleTiles)
        {
            if (tile != null)
            {
                if (tile.TryGetComponent<HexTileFogController>(out var fogController))
                {
                    if (fogController.GetCurrentState() == HexTileFogController.FogState.Visible)
                    {
                        fogController.SetFogState(HexTileFogController.FogState.Explored);
                    }
                }
            }
        }

        currentlyVisibleTiles.Clear();

        List<HexTileData> newlyVisibleTiles = gridManager.GetTilesInRadius(centerCoord, visionRadius);

        foreach (var tile in newlyVisibleTiles)
        {
            if (tile != null)
            {
                if (tile.TryGetComponent<HexTileFogController>(out var fogController))
                {
                    fogController.SetFogState(HexTileFogController.FogState.Visible);
                    currentlyVisibleTiles.Add(tile);
                }
            }
        }
    }
}
