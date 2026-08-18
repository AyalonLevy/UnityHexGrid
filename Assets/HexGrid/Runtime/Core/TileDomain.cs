namespace HexGrid
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "NewTileDomain", menuName = "HexGrid/Tile Domain", order = 52)]
    public class TileDomain : ScriptableObject
    {
        [Header("Domain Settings")]
        [Tooltip("The human-readable display name for this tile domain.")]
        public string displayName;

        public HexType hexType = HexType.Default;
    }
}
