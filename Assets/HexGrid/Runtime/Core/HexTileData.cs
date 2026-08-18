namespace HexGrid
{
    using UnityEngine;


#if UNITY_EDITOR
    using UnityEditor;
#endif

    public enum HexType
    {
        None,
        Default,
        Difficult,
        Road,
        Water,
        Obstacle
    }

    [SelectionBase]
    public class HexTileData : MonoBehaviour
    {
        [Header("Database Reference")]
        [Tooltip("Reference to the main database to fetch new meshed when editing.")]
        public HexGridDatabase database;

        [Header("Tile Data")]
        public int tileIndex;

        [TileDomainDropdown]
        public TileDomain currentDomain;
        [SerializeField, HideInInspector] private TileDomain previousDomain;
        public HexType hexType;

        [Header("Prop Data")]
        [HideInInspector] public bool hasProp;

        [PropDropdown]
        public GameObject manualPropSelection;
        [SerializeField, HideInInspector] private GameObject previousManualProp;

        public int propIndex = -1;
        public float propRotation = 0.0f;
        public float propScale = 1.0f;
        [SerializeField, HideInInspector] private float previousRotation;
        [SerializeField, HideInInspector] private float previousScale = 1.0f;

        [Header("Settings")]
        public float tileHeight = 0.1f;

        [Header("Highlight Settings")]
        [Tooltip("The unlit transparent material used for the highlight glow.")]
        [SerializeField] private Material highlightMaterial;
        [Tooltip("The unlit transparent material used for the path highlight glow.")]
        [SerializeField] private Material pathMaterial;

        [Header("Hierarchy References")]
        public Transform visualsContainer;
        public Transform propsContainer;

        [HideInInspector] public bool isExplored = true;
        [HideInInspector] public Vector3Int tileCoordinates;

        private Highlight highlight;

        private void OnValidate()
        {
            UpdatePropContainerHeight();

#if UNITY_EDITOR
            // 1. Check if Domain changed (Clear props if required)
            if (database != null && currentDomain != previousDomain && previousDomain != null)
            {
                tileIndex = Mathf.Clamp(tileIndex, 0, database.hexGridTiles.Count - 1);

                EditorApplication.delayCall += () =>
                {
                    // Safety check in case the user deletes the object the exact frame they modify it
                    if (this == null) return;
                    SwapTileMesh();
                    EvaluateHexType();
                };
            }

            // 2. Check if Manual Prop Selection changed in the Inspector
            if (database != null && manualPropSelection != previousManualProp)
            {
                EditorApplication.delayCall += () =>
                {
                    if (this == null) return;
                    UpdateManualProp();
                    EvaluateHexType();
                };
            }

            // 3. Rotation or Scale changed manually in the Inspector
            if (hasProp && propsContainer != null && propsContainer.childCount > 0)
            {
                if (propRotation != previousRotation || propScale != previousScale)
                {
                    Transform activeProp = propsContainer.GetChild(0);
                    if (activeProp != null)
                    {
                        activeProp.localRotation = Quaternion.Euler(0, propRotation, 0);
                        activeProp.localScale = Vector3.one * propScale;

                        previousRotation = propRotation;
                        previousScale = propScale;
                        EditorUtility.SetDirty(this);
                    }
                }
            }
#endif
        }

        private void Awake()
        {
            highlight = GetComponent<Highlight>();

            if (highlight != null)
            {
                Transform targetContainer = visualsContainer != null ? visualsContainer : transform;
                highlight.InitializeHighlight(targetContainer);
            }
        }

        public void UpdatePropContainerHeight()
        {
            if (propsContainer != null)
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
            if (visualsContainer != null)
            {
                for (int i = visualsContainer.childCount - 1; i >= 0; i--)
                    Destroy(visualsContainer.GetChild(i).gameObject);
            }

            if (propsContainer != null)
            {
                for (int i = propsContainer.childCount - 1; i >= 0; i--)
                    Destroy(propsContainer.GetChild(i).gameObject);
            }

            hasProp = false;
            propIndex = -1;

            hexType = HexType.Default;
            EvaluateHexType();
        }

        public int GetCost() => hexType switch
        {
            HexType.Road => 5,
            HexType.Default => 10,
            HexType.Difficult => 20,
            HexType.Water => 30,    // TODO: When implementing unit traits/skills (e.g., Water-Walking), evaluate the traversing unit here to return a lower cost or passable state.
            HexType.Obstacle => int.MaxValue,
            _ => 10 // Default
        };

        public bool IsObstacle()
        {
            return this.hexType == HexType.Obstacle;
        }

        public void EnableHighlight(bool isPath)
        {
            if (highlight != null) highlight.SetHighlight(true, isPath ? pathMaterial : highlightMaterial);
        }

        public void DisableHighlight(bool isPath)
        {
            if (highlight != null) highlight.SetHighlight(false, isPath ? pathMaterial : highlightMaterial);
        }

#if UNITY_EDITOR
        // For debuging
        public void UpdateTileCoordinates(Vector3Int coord)
        {
            tileCoordinates = coord;
        }

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
                manualPropSelection = null;
                propIndex = -1;

                if (propsContainer != null)
                {
                    for (int i = propsContainer.childCount - 1; i >= 0; i--)
                        DestroyImmediate(propsContainer.GetChild(i).gameObject);
                }
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

        private void UpdateManualProp()
        {
            if (propsContainer == null || database == null) return;

            float targetScale = 1.0f;
            float targetRotation = 0.0f;

            if (propsContainer.childCount > 0)
            {
                Transform existingProp = propsContainer.GetChild(0);
                targetScale = existingProp.localScale.x;
                targetRotation = existingProp.localRotation.eulerAngles.y;
            }
            else
            {
                // Fallback to random rotation and scale of 1
                targetScale = propScale > 0 ? propScale : 1f;
                targetRotation = Random.Range(0.0f, 360.0f);
            }

            // Destroy the prop
            for (int i = propsContainer.childCount - 1; i >= 0; i--)
                DestroyImmediate(propsContainer.GetChild(i).gameObject);

            if (manualPropSelection == null)
            {
                hasProp = false;
                propIndex = -1;
                previousManualProp = null;
                EvaluateHexType();
                EditorUtility.SetDirty(this);
                return;
            }

            // Find the master index in the database
            int masterIndex = database.props.FindIndex(t => t.propPrefab == manualPropSelection);

            if (masterIndex == -1)
            {
                Debug.LogWarning($"No prop prefabs found in database for domain: {currentDomain.name}");
                return;
            }

            propIndex = masterIndex;
            hasProp = true;
            propRotation = targetRotation;
            propScale = targetScale;

            // Instantiate the prop visual
            GameObject propInstance = Instantiate(manualPropSelection, propsContainer);
            propInstance.transform.localRotation = Quaternion.Euler(0, propRotation, 0);
            propInstance.transform.localScale = Vector3.one * propScale;

            Undo.RegisterCreatedObjectUndo(propInstance, "Manual Prop Placement");

            previousManualProp = manualPropSelection;
            EditorUtility.SetDirty(this);
        }

        public void SyncPropDropdown()
        {
            if (database == null) return;

            if (hasProp && propIndex >= 0 && propIndex < database.props.Count)
            {
                manualPropSelection = database.props[propIndex].propPrefab;
                previousManualProp = manualPropSelection;
            }
            else
            {
                manualPropSelection = null;
                previousManualProp = null;
                hasProp = false;
                propIndex = -1;
            }

            EditorUtility.SetDirty(this);
        }

        public void EvaluateHexType()
        {
            // 1. Establish the base terrain from the Domain
            hexType = currentDomain != null ? currentDomain.hexType : HexType.Default;

            // 2. Apply Prop Overrides (if a prop exists)
            if (hasProp && database != null && propIndex >= 0 && propIndex < database.props.Count)
            {
                HexType propEffect = database.props[propIndex].terrainEffect;

                // Treat standard/empty props as Difficult terrain by default
                HexType appliedPropEffect = (propEffect == HexType.None || propEffect == HexType.Default) ? HexType.Difficult : propEffect;

                // 3. Severity & Override Hierarchy
                if (appliedPropEffect == HexType.Obstacle)
                {
                    // Obstacles always block the tile completely
                    hexType = HexType.Obstacle;
                }
                else if (appliedPropEffect == HexType.Road)
                {
                    // A bridge/road prop can pave over water or difficult terrain
                    if (hexType != HexType.Obstacle)
                    {
                        hexType = HexType.Road;
                    }
                }
                else if (appliedPropEffect == HexType.Difficult)
                {
                    // Debris shouldn't downgrade Water or Obstacles
                    if (hexType != HexType.Obstacle && hexType != HexType.Water)
                    {
                        hexType = HexType.Difficult;
                    }
                }
                else if (appliedPropEffect == HexType.Water)
                {
                    if (hexType != HexType.Obstacle)
                    {
                        hexType = HexType.Water;
                    }
                }
            }

            EditorUtility.SetDirty(this);
        }
#endif
    }
}