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
    [HideInInspector] private List<CubeTileMapping> serializedTiles = new();

    private Dictionary<Vector3Int, HexTileData> grid = new();

    private void Awake()
    {
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        grid.Clear();

        foreach (CubeTileMapping mapping in serializedTiles)
        {
            if (mapping.tile != null && !grid.ContainsKey(mapping.tile.tileCoordinates))
            {
                grid.Add(mapping.cubeCoordinates, mapping.tile);
            }
        }
    }

    public void InjectGridData(List<CubeTileMapping> generatedTiles)
    {
        serializedTiles = new(generatedTiles);

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
}
