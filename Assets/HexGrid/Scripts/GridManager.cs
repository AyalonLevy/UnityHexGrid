using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CubeTileMapping
{
    public Vector3Int cubeCoordinates;
    public HexTileData tile;
}

public class GridManager : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Pre-populated by the HexGridGenerator.")]
    [SerializeField] private List<CubeTileMapping> serializedTiles = new();

    [Header("Grid Metrics")]
    [SerializeField, Tooltip("Stored automatically by the Generator")]
    private float hexRadius = 1.0f;

    private Dictionary<Vector3Int, HexTileData> grid = new();

    public static List<Vector3Int> cubeDirections = new()
    {
        new Vector3Int(1, 0, -1),   // E
        new Vector3Int(0, 1, -1),   // NE
        new Vector3Int(-1, 1, 0),   // NW
        new Vector3Int(-1, 0, 1),   // W
        new Vector3Int(0, -1, 1),   // SE
        new Vector3Int(1, -1, 0)    // SW
    };

    public float HexRadius => hexRadius;

    private void Awake()
    {
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        grid.Clear();

        foreach (CubeTileMapping mapping in serializedTiles)
        {
            if (mapping.tile != null && !grid.ContainsKey(mapping.cubeCoordinates))
            {
                grid.Add(mapping.cubeCoordinates, mapping.tile);
            }
        }
    }

    public void InjectGridData(List<CubeTileMapping> generatedTiles, float radius)
    {
        serializedTiles = new(generatedTiles);
        hexRadius = radius;

#if UNITY_EDITOR
        // Tell Unity this object's data changed so it saves the list to the scene file
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    public HexTileData GetTileAt(Vector3Int coordinates)
    {
        if (grid.TryGetValue(coordinates, out HexTileData tile))
        {
            return tile;
        }

        return null;
    }

    public List<HexTileData> GetTileNeighbours(Vector3Int centerCoordinates)
    {
        List<HexTileData> neighbours = new();

        foreach (Vector3Int direction in cubeDirections)
        {
            Vector3Int neighbourCoord = centerCoordinates + direction;
            HexTileData neighbourTile = GetTileAt(neighbourCoord);

            if (neighbourTile != null)
            {
                neighbours.Add(neighbourTile);
            }
        }

        return neighbours;
    }

    /// <summary>
    /// Returns all tiles within a specific radius from a center cube coordinate.
    /// </summary>
    public List<HexTileData> GetTilesInRadius(Vector3Int center, int radius)
    {
        List<HexTileData> tilesInRadius = new();

        foreach (var tile in grid)
        {
            if (HexGridMath.GetCubeDistance(center, tile.Key) <= radius)
            {
                if (tile.Value != null)
                {
                    tilesInRadius.Add(tile.Value);
                }
            }
        }

        return tilesInRadius;
    }

    public Vector3Int GetClosestHex(Vector3 worldPosition)
    {
        return HexGridMath.WorldToCubeCoordinates(worldPosition, hexRadius);
    }
}
