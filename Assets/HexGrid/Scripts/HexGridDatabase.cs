using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HexGridDatabase", menuName = "HexGrid/Database")]
public class HexGridDatabase : ScriptableObject
{
    [System.Serializable]
    public struct TileData
    {
        public GameObject tilePrefab;
        public TileDomain domain;
        [Range(0.0f, 1.0f)]
        public float spawnChance;
        public bool hasProp;
    }

    [System.Serializable]
    public struct PropData
    {
        public GameObject propPrefab;
        [Tooltip("List of all valid tile domains where this prop can be spawned.")]
        public List<TileDomain> domains;
        [Range(0.0f, 1.0f)]
        public float spawnChance;

        [Header("Gameplay Effects")]
        public HexType terrainEffect;

    }

    [Header("Available Domains")]
    public List<TileDomain> availableDomains = new();

    [Header("Tiles Database")]
    public List<TileData> hexGridTiles = new();

    [Header("Props Database")]
    public List<PropData> props = new();
}
