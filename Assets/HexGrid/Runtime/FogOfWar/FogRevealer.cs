using System.Collections.Generic;
using UnityEngine;

public class FogRevealer : MonoBehaviour
{
    [Header("Vision Settings")]
    [SerializeField] private int visionRadius = 3;

    private GridManager gridManager;
    private Vector3Int lastKnownCubeCoord = new(int.MinValue, int.MinValue, int.MinValue);

    // Using HashSets for O(1) lookups and efficient set operations
    private readonly HashSet<HexTileData> currentlyVisibleTiles = new();

    private void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("FogRevealer: No GridManager found in scene!", this);
            enabled = false; // Disable script if dependency is missing to save update cycles
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
        // 1. Fetch newly visible tiles from the grid manager
        List<HexTileData> fetchedTiles = gridManager.GetTilesInRadius(centerCoord, visionRadius);
        HashSet<HexTileData> newlyVisibleTiles = new(fetchedTiles);

        // 2. Identify tiles that are no longer visible (in current but not in new)
        foreach (var tile in currentlyVisibleTiles)
        {
            if (tile != null && !newlyVisibleTiles.Contains(tile))
            {
                SetTileFogState(tile, HexTileFogController.FogState.Explored);
            }
        }

        // 3. Identify newly entered tiles (in new but not in current)
        foreach (var tile in newlyVisibleTiles)
        {
            if (tile != null && !currentlyVisibleTiles.Contains(tile))
            {
                SetTileFogState(tile, HexTileFogController.FogState.Visible);
            }
        }

        // 4. Update our active tracking set
        currentlyVisibleTiles.Clear();
        foreach (var tile in newlyVisibleTiles)
        {
            if (tile != null)
            {
                currentlyVisibleTiles.Add(tile);
            }
        }
    }

    private void SetTileFogState(HexTileData tile, HexTileFogController.FogState targetState)
    {
        if (tile.TryGetComponent<HexTileFogController>(out var fogController))
        {
            // Only update if it's not already in the target state to avoid redundant calls
            if (fogController.GetCurrentState() != targetState)
            {
                fogController.SetFogState(targetState);
            }
        }
    }
}
