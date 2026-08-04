using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HexGridDatabase", menuName = "HexGrid/Database")]
public class HexGridDatabase : ScriptableObject
{
    [System.Serializable]
    public struct TileEntry
    {
        public GameObject tilePrefab;
        public TileDomain domain;
        [Range(0.0f, 1.0f)]
        public float spawnChance;
        public bool hasProp;
    }

    [System.Serializable]
    public struct PropEntry
    {
        public GameObject propPrefab;
        [Tooltip("List of all valid tile domains where this prop can be spawned.")]
        public List<TileDomain> domains;
        [Range(0.0f, 1.0f)]
        public float spawnChance;
    }

    [Header("Available Domains")]
    public List<TileDomain> availableDomains = new();

    [Header("Tiles Database")]
    public List<TileEntry> hexGridTiles = new();

    [Header("Props Database")]
    public List<PropEntry> props = new();
}
