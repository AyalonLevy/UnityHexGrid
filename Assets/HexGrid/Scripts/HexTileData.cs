using UnityEngine;

public class HexTileData : MonoBehaviour
{
    [Header("Tile Data")]
    public int tileIndex;
    public TileDomain currentDomain;

    [Header("PropData")]
    public bool hasProp;
    public int propIndex;
    public float propRotation;
    public float propScale;

    [Header("Settings")]
    public float tileHeight = 0.1f;

    [Header("Hierarchy References")]
    public Transform visualsContainer;
    public Transform propsContainer;


    private void OnValidate()
    {
        UpdatePropContainerHeight();
    }

    public void UpdatePropContainerHeight()
    {
        if (propsContainer == null)
        {
            propsContainer.localPosition = new Vector3(propsContainer.localPosition.x, tileHeight, propsContainer.localPosition.z);
        }
    }

    public void ClearTile()
    {
        for (int i = visualsContainer.childCount - 1; i >= 0; i--)
            Destroy(visualsContainer.GetChild(i).gameObject);

        for (int i = propsContainer.childCount - 1; i >= 0; i--)
            Destroy(propsContainer.GetChild(i).gameObject);

        hasProp = false;
        propIndex = -1;
    }
}
