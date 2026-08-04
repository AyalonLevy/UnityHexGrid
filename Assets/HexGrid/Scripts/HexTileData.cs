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

    [PropDropdown]
    public GameObject manualPropSelection;
    [SerializeField, HideInInspector] private GameObject previousManualProp;

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
        // 1. Check if Domain changed (Clear props as you decided)
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

        // 2. Check if Manual Prop Selection changed in the Inspector
        if (database != null && manualPropSelection != previousManualProp)
        {
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                UpdateManualProp();
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
            newMesh.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            Undo.RegisterCreatedObjectUndo(newMesh, "Swap Hex Tile Mesh");
        }

        previousDomain = currentDomain;

        // Mark the scene as dirty so Unity knows to save these changes
        EditorUtility.SetDirty(this);
    }
#endif

#if UNITY_EDITOR
    private void UpdateManualProp()
    {
        if (propsContainer == null || database == null) return;

        // Destroy the prop
        for (int i = propsContainer.childCount - 1; i >= 0; i--)
            DestroyImmediate(propsContainer.GetChild(i).gameObject);

        if (manualPropSelection == null)
        {
            hasProp = false;
            propIndex = -1;
            previousManualProp = null;
            EditorUtility.SetDirty(this);
            return;
        }

        // Find the master index in the database
        int masterIndex = database.props.FindIndex(t => t.domains.Contains(currentDomain));

        if (masterIndex == -1)
        {
            Debug.LogWarning($"No prop prefabs found in database for domain: {currentDomain.name}");
            return;
        }

        propIndex = masterIndex;
        hasProp = true;
        // propRotation = Random.Range(0.0f, 360.0f);
        // propScale = 1.0f;

        // Instantiate the prop visual
        GameObject propInstance = Instantiate(manualPropSelection, propsContainer);
        propInstance.transform.localRotation = Quaternion.Euler(0, propRotation, 0);
        propInstance.transform.localScale = Vector3.one * propScale;

        Undo.RegisterCreatedObjectUndo(propInstance, "Manual Prop Placement");

        previousManualProp = manualPropSelection;
        EditorUtility.SetDirty(this);
    }
#endif
}
