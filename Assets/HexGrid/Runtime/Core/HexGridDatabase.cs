namespace HexGrid
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "HexGridDatabase", menuName = "HexGrid/Database", order = 51)]
    public class HexGridDatabase : ScriptableObject
    {
        [System.Serializable]
        public struct TileData
        {
            [Tooltip("The prefab representing this hex tile.")]
            public GameObject tilePrefab;

            [Tooltip("The environmental domain classification for this tile.")]
            public TileDomain domain;

            [Range(0.0f, 1.0f)]
            [Tooltip("Relative weight/chance for this tile to be selected during random generation.")]
            public float spawnChance;

            [HideInInspector] public bool hasProp;
        }

        [System.Serializable]
        public struct PropData
        {
            [Tooltip("The prefab representing this prop/obstacle.")]
            public GameObject propPrefab;

            [Tooltip("List of all valid tile domains where this prop can be spawned.")]
            public List<TileDomain> domains;

            [Range(0.0f, 1.0f)]
            [Tooltip("Relative weight/chance for this prop to be selected.")]
            public float spawnChance;

            [Header("Gameplay Effects")]
            [Tooltip("The gameplay or movement terrain effect applied by this prop.")]
            public HexType terrainEffect;
        }

        [Header("Available Domains")]
        [Tooltip("Global list of all valid domains used across the grid.")]
        public List<TileDomain> availableDomains = new();

        [Header("Tiles Database")]
        [Tooltip("Collection of all tile types available for generation.")]
        public List<TileData> hexGridTiles = new();

        [Header("Props Database")]
        [Tooltip("Collection of all props and obstacles available for generation.")]
        public List<PropData> props = new();
    }
}