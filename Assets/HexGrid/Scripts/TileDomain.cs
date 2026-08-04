using UnityEngine;

[CreateAssetMenu(fileName = "NewTileDomain", menuName = "HexGrid/Tile Domain")]
public class TileDomain : ScriptableObject
{
    public string displayName;
    public Color editorTint = Color.white;
}
