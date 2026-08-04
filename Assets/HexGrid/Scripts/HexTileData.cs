using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HexTileData : MonoBehaviour
{
    [Header("database Reference")]
    [Tooltip("Reference to the main database to fetch new meshed when editing.")]
    public HexGridDatabase database;

    [Header("Tile Data")]
    public int tileIndex;
    [TileDomainDropdown]
    public TileDomain currentDomain;
    [SerializeField, HideInInspector] private TileDomain previousDomain;

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

#if UNITY_EDITOR
        // Only trigger the mesh swap if the index changed and we have a database
        if (database != null && currentDomain != previousDomain && previousDomain != null)
        {
            tileIndex = Mathf.Clamp(tileIndex, 0, database.hexGridTiles.Count - 1);

            EditorApplication.delayCall += () =>
            {
                // Safety check in case the user deletes the object the exact frame they modify it
                if (this == null) return;
                SwapTileMesh();
            };
        }
#endif
    }

    public void UpdatePropContainerHeight()
    {
        if (propsContainer == null)
        {
            propsContainer.localPosition = new Vector3(propsContainer.localPosition.x, tileHeight, propsContainer.localPosition.z);
        }
    }

    public void InitializeData(HexGridDatabase db, int index, TileDomain domain)
    {
        database = db;
        tileIndex = index;
        currentDomain = domain;
        previousDomain = domain;
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

#if UNITY_EDITOR
    private void SwapTileMesh()
    {
        if (visualsContainer == null || database == null || currentDomain == null) return;

        int matchingIndex = database.hexGridTiles.FindIndex(t => t.domain == currentDomain);

        if (matchingIndex == -1)
        {
            Debug.LogWarning($"No tile prefab found in database for domain: {currentDomain.name}");
            previousDomain = currentDomain; // Reset so it doesn't loop
            return;
        }

        tileIndex = matchingIndex;
        var newTile = database.hexGridTiles[matchingIndex];

        // Destroy old visual meshes
        for (int i = visualsContainer.childCount - 1; i >= 0; i--)
            DestroyImmediate(visualsContainer.GetChild(i).gameObject);

        // Destroy the prop
        if (hasProp)
        {
            hasProp = false;
            propIndex = -1;

            for (int i = propsContainer.childCount - 1; i >= 0; i--)
                DestroyImmediate(propsContainer.GetChild(i).gameObject);
        }

        if (newTile.tilePrefab != null)
        {
            GameObject newMesh = Instantiate(newTile.tilePrefab, visualsContainer);
            newMesh.transform.localPosition = Vector3.zero;
            newMesh.transform.localRotation = Quaternion.identity;

            Undo.RegisterCreatedObjectUndo(newMesh, "Swap Hex Tile Mesh");
        }

        previousDomain = currentDomain;

        // Mark the scene as dirty so Unity knows to save these changes
        EditorUtility.SetDirty(this);
    }
#endif
}
