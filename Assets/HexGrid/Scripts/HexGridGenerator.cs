using System.Collections.Generic;
using UnityEngine;
using System.IO;




#if UNITY_EDITOR
using UnityEditor;
#endif


[System.Serializable]
public struct GridData
{
    public int tileIndex;
    public Vector3 tilePosition;
    public float tileHeight;
    public bool hasProp;
    public int propIndex;
    public float propRotation;
    public float propScale;
}

[System.Serializable]
public class GridDataWrapper
{
    public List<GridData> gridData = new();
}

public enum GridShape
{
    Rectangle,
    Hexagonal,
    FromFile
}

public class HexGridGenerator : MonoBehaviour
{
    [Header("Database Reference")]
    [Tooltip("Assign your external HexGridDatabase asset here.")]
    [SerializeField] private HexGridDatabase gridDatabase;

    [Header("Core Tool Assets")]
    [Tooltip("The base empty prefab that holds tile data. Found in Assets/HexGrid/Prefabs/")]
    [SerializeField] private GameObject baseHexTilePrefab;

    [Header("Grid Settings")]
    [Tooltip("The length of the hexagon edge")]
    [SerializeField] private float tileEdgeSize = 1;
    [Tooltip("The height to place the props on the hexagon tile")]
    [SerializeField] private float gapBetweenTiles = 0; // Add an option to add a gap between the tiles
    [SerializeField] private Transform gridContainer;

    [Tooltip("Select grid shape")]
    [SerializeField] private GridShape gridShape = GridShape.Rectangle;

    [SerializeField] private Vector2 gridSize = new(4, 5);
    [Tooltip("Number of tiles rings")]
    [Range(0, 50)]
    [SerializeField] int gridRadius = 4;
    [Tooltip("Will create the grid based on the grid data file.")]
    [SerializeField] private TextAsset gridDataFile;

    [SerializeField] private bool addProps = false;
    [SerializeField] private float coveragePercentage = 0.5f;
    [SerializeField] private Vector2 scaleRange = new(0.5f, 1.3f);

    [SerializeField] private string fileName = "GridData";

    //[SerializeField] private bool isFlatTopped;

    private readonly float hexagonAngleDeg = 60.0f;
    private readonly int hexagonSides = 6;


    public void GenerateGrid()
    {
        // Clear existing tiles first
        ClearGrid();

        if (gridDatabase == null)
        {
            Debug.LogError("HexGridDatabase is missing! Please assign a database asset to the generator.");
            return;
        }

        if (gridContainer == null)
        {
            Debug.Log("Grid Container is empty, please select a container for all grid tiles.");
            return;
        }

        if (gridShape == GridShape.Rectangle)
        {
            // Along the same line: Moves 2 * sin(30) in X direction
            // Accross the next line: Moves sin(30) in X direction and 1 + cos(30)
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    if (gridDatabase.hexGridTiles != null)
                    {
                        SpawnTile(GetRandomWeightedTile(gridDatabase.hexGridTiles), GetTilePositionForHorizontalGrid(x, y));
                    }
                }
            }
        }
        else if (gridShape == GridShape.Hexagonal)
        {

            for (int i = 0; i < gridRadius; i++)
            {
                List<Vector3> gridPositions = GetHexagonalPosition(i);
                for (int j = 0; j < gridPositions.Count; j++)
                {
                    if (gridDatabase.hexGridTiles != null)
                    {
                        SpawnTile(GetRandomWeightedTile(gridDatabase.hexGridTiles), gridPositions[j]);
                    }
                }
            }
            if (gridDatabase.hexGridTiles != null)
            {
                SpawnTile(GetRandomWeightedTile(gridDatabase.hexGridTiles), Vector3.zero);  // Spawn the center tile (ring 0)
            }
        }
        else if (gridShape == GridShape.FromFile)
        {
            // Read file and spawn tiles based on coordinates
            if (gridDataFile == null)
            {
                Debug.Log("Grid Coordinates file is empty, please select a valid file.");
                return;
            }

            List<GridData> gridTiles = ParseJSON();

            for (int i = 0; i < gridTiles.Count; i++)
            {
                SpawnTile(gridDatabase.hexGridTiles[gridTiles[i].tileIndex], gridTiles[i].tilePosition, gridTiles[i]);
            }
        }
    }

    public void ClearGrid()
    {
        if (gridContainer == null) return;

        // Loop backwards to safely destroy all children
        for (int i = gridContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = gridContainer.GetChild(i).gameObject;

#if UNITY_EDITOR
            // Allows undo support when deleting in edit mode
            Undo.DestroyObjectImmediate(child);
#else
            Destroy(child);
#endif
        }
    }

    public void SaveGridToFile()
    {
        if (gridDatabase == null || gridContainer == null)
        {
            Debug.LogError("Database or GridContainer is missing. Cannot save grid.");
            return;
        }

        // Get the transform of each child hex tile of gridContainer and store their positions in a list
        GridDataWrapper grid = new();

        foreach (Transform child in gridContainer)
        {
            HexTileData TileData = child.GetComponent<HexTileData>();

            if (TileData == null)
            {
                Debug.LogWarning($"Object '{child.name}' in GridContainer is missing HexTiledata! Skipping.");
                continue;
            }

            GridData data = new()
            {
                tilePosition = child.position,
                tileIndex = TileData.tileIndex,
                tileHeight = TileData.tileHeight,
                hasProp = TileData.hasProp,
                propIndex = TileData.propIndex,
                propRotation = TileData.propRotation,
                propScale = TileData.propScale
            };

            grid.gridData.Add(data);
        }

        // Convert the list of positions to JSON format
        string json = JsonUtility.ToJson(grid, true);

#if UNITY_EDITOR
        // Opens a nice "Save As" window in the Unity Editor
        string defaultName = string.IsNullOrEmpty(fileName) ? "NewGridData" : fileName;

        // Save the JSON to a file
        string filePath = UnityEditor.EditorUtility.SaveFilePanelInProject(
            "Save Grid Data",
            defaultName,
            "json",
            "Choose where to save your grid layout data."
            );

        // If the user clicks "Cancel" in the save window, filePath will be empty
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.Log("Save cancelled by user.");
            return;
        }

        File.WriteAllText(filePath, json);

        // Force Unity to see the new asset in the Resources folder
        UnityEditor.AssetDatabase.Refresh();

        // Optional: Automatically ping the newly saved file so the user sees it
        Object savedAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(filePath);
        if (savedAsset != null)
        {
            UnityEditor.EditorGUIUtility.PingObject(savedAsset);
        }

        Debug.Log($"<color=green>Successfully saved grid coordinates to:</color> {filePath}");
#else
        // Fallback just in case this is ever executed in a compiled runtime build
        string filePath = Application.persistentDataPath + $"/{fileName}.json";
        File.WriteAllText(filePath, json);
        Debug.Log($"Saved grid coordinates to {filePath}");
#endif
    }

    private HexGridDatabase.TileEntry GetRandomWeightedTile(List<HexGridDatabase.TileEntry> tiles)
    {
        float totalWeight = 0.0f;
        foreach (var tile in tiles)
        {
            totalWeight += tile.spawnChance;
        }

        // The first entry is the default value
        if (totalWeight < 0.0f) return gridDatabase.hexGridTiles[0];

        float randomValue = Random.Range(0, totalWeight);
        float currentSum = 0.0f;

        foreach (var tile in tiles)
        {
            currentSum += tile.spawnChance;
            if (randomValue <= currentSum)
            {
                return tile;
            }
        }

        return tiles[0];
    }

    private GameObject GetRandomWeightedProp(List<HexGridDatabase.PropEntry> props)
    {
        float totalWeight = 0.0f;
        foreach (var prop in props)
        {
            totalWeight += prop.spawnChance;
        }

        // The first entry is the default value
        if (totalWeight < 0.0f) return gridDatabase.props[0].propPrefab;
        float randomValue = Random.Range(0, totalWeight);
        float currentSum = 0.0f;

        foreach (var prop in props)
        {
            currentSum += prop.spawnChance;
            if (randomValue <= currentSum)
            {
                return prop.propPrefab;
            }
        }

        return props[0].propPrefab;
    }

    private void SpawnTile(HexGridDatabase.TileEntry tile, Vector3 position, GridData? loadedData = null)
    {
        if (tile.tilePrefab == null) return;

        // Instantiate the structures Base Prefab
        GameObject tileObj = Instantiate(baseHexTilePrefab, position, Quaternion.identity, gridContainer);
        HexTileData tileData = tileObj.GetComponent<HexTileData>();

        tileData.database = this.gridDatabase;
        tileData.InitializeData(this.gridDatabase, loadedData != null ? loadedData.Value.tileIndex : gridDatabase.hexGridTiles.IndexOf(tile), tile.domain);

        // Set the base data
        tileData.currentDomain = tile.domain;

        // Instantiate the visual mesh to the Visuals container
        GameObject visualMesh = Instantiate(tile.tilePrefab, tileData.visualsContainer);

        // Verify the visuals has no transform
        visualMesh.transform.localPosition = Vector3.zero;

        // Handle Props
        if (loadedData != null)
        {
            // Loading from file
            tileData.tileIndex = loadedData.Value.tileIndex;
            tileData.tileHeight = loadedData.Value.tileHeight;
            tileData.hasProp = loadedData.Value.hasProp;
            tileData.propIndex = loadedData.Value.propIndex;
            tileData.propRotation = loadedData.Value.propRotation;
            tileData.propScale = loadedData.Value.propScale;

            tileData.UpdatePropContainerHeight();

            if (tileData.hasProp && tileData.propIndex >= 0 && tileData.propIndex < gridDatabase.props.Count)
            {
                GameObject propPrefab = gridDatabase.props[tileData.propIndex].propPrefab;
                SpawnProp(propPrefab, tileData.propsContainer, tileData.propRotation, tileData.propScale);
            }

            // Empty tiles can either get a new prop or not - based on the `addProp` value
            if (!tileData.hasProp && addProps && gridDatabase.props.Count > 0 && Random.value <= coveragePercentage)
            {
                tileData.hasProp = true;
                GenerateProp(tile.domain, tileData);
            }
        }
        else
        {
            // Generating props
            tileData.tileIndex = gridDatabase.hexGridTiles.IndexOf(tile);

            if (addProps && gridDatabase.props.Count > 0 && Random.value <= coveragePercentage)
            {
                tileData.hasProp = true;
                GenerateProp(tile.domain, tileData);
            }
            else
            {
                tileData.hasProp = false;
                tileData.propIndex = -1;
            }
        }

#if UNITY_EDITOR
        // Allows undo support when deleting in edit mode
        Undo.RegisterCreatedObjectUndo(tileObj, "Generate Hex Tile");
#endif
    }

    private Vector3 GetTilePositionForHorizontalGrid(int xPos, int yPos)
    {
        // Calculates the tile position in world coordinates based on the grid position
        float offset = (tileEdgeSize + gapBetweenTiles) * Mathf.Sin(Mathf.Deg2Rad * hexagonAngleDeg);
        float xShift = xPos * 2.0f * offset;
        float yShift = yPos * (tileEdgeSize + gapBetweenTiles) * (Mathf.Cos(Mathf.Deg2Rad * hexagonAngleDeg) + 1);

        if (yPos % 2 == 0)
        {
            return new Vector3(xShift, 0, yShift);  // 2 is for twice the distance (from each hex tile)
        }
        else
        {
            return new Vector3(xShift + offset, 0, yShift);
        }
    }

    private List<Vector3> GetHexagonCornersPosition(int ringNum)
    {
        List<Vector3> cornerPositions = new();

        // Generate layers based on the radius. There are 6 * (r - 1) tiles
        for (int hexTileNum = 0; hexTileNum < hexagonSides * ringNum; hexTileNum += ringNum)
        {
            float theta = hexTileNum * 2 * Mathf.PI / (hexagonSides * ringNum);
            float radius = (tileEdgeSize + gapBetweenTiles) * Mathf.Sin(Mathf.Deg2Rad * hexagonAngleDeg);
            Vector3 tilePos = new(ringNum * 2 * radius * Mathf.Cos(theta), 0, ringNum * 2 * radius * Mathf.Sin(theta));

            cornerPositions.Add(tilePos);
        }

        return cornerPositions;
    }

    private List<Vector3> GetHexagonalPosition(int ringNum)
    {
        List<Vector3> positions = new();
        List<Vector3> cornerPositions = GetHexagonCornersPosition(ringNum);

        if (ringNum < 2)
        {
            positions = cornerPositions;
        }
        else
        {
            for (int idx = 0; idx < cornerPositions.Count; idx++)
            {
                for (int i = 1; i < ringNum; i++)
                {
                    // Some vector math. Takes the 2 adjecant corners and add the fraction of the distance between them to the first point
                    Vector3 p0 = cornerPositions[idx];
                    Vector3 p1 = cornerPositions[idx + 1 >= hexagonSides ? idx + 1 - hexagonSides : idx + 1];

                    float frac = (float)i / (float)ringNum;
                    Vector3 absDiff = new(Mathf.Abs(p1.x - p0.x), Mathf.Abs(p1.y - p0.y), Mathf.Abs(p1.z - p0.z));

                    positions.Add(p0 + (p1 - p0) * frac);
                }
            }

            positions.AddRange(cornerPositions);
        }

        return positions;
    }

    private List<GridData> ParseJSON()
    {
        if (gridDataFile != null)
        {
            return JsonUtility.FromJson<GridDataWrapper>(gridDataFile.text).gridData;
        }

        return new List<GridData>();
    }

    private void GenerateProp(TileDomain tileDomain, HexTileData tileData)
    {
        GameObject propPrefab = GetPropToSpawn(tileDomain);

        if (propPrefab == null)
        {
            tileData.hasProp = false;
            tileData.propIndex = -1;
            return;
        }

        tileData.propIndex = gridDatabase.props.FindIndex(p => p.propPrefab == propPrefab);
        tileData.hasProp = true;

        // Random rotation along the y axis
        float randomYRotation = Random.Range(0, 360);

        // Random scale based on the scale range
        float randomScale = Random.Range(scaleRange.x, scaleRange.y);

        tileData.propRotation = randomYRotation;
        tileData.propScale = randomScale;

        SpawnProp(propPrefab, tileData.propsContainer, randomYRotation, randomScale);
    }

    private void SpawnProp(GameObject prop, Transform parent, float propRot, float propScale)
    {
        GameObject propInstance = Instantiate(prop, parent);
        propInstance.transform.localRotation = Quaternion.Euler(0, propRot, 0);
        propInstance.transform.localScale = Vector3.one * propScale;

#if UNITY_EDITOR
        // Allows undo support when deleting in edit mode
        Undo.RegisterCreatedObjectUndo(propInstance, "Generate Hex Tile Prop");
#endif
    }

    private GameObject GetPropToSpawn(TileDomain tileDomain)
    {
        // Filter all the props based on the domain
        List<HexGridDatabase.PropEntry> filteredProps = gridDatabase.props.FindAll(p => p.domains != null && p.domains.Contains(tileDomain));

        if (filteredProps.Count == 0) return null;

        return GetRandomWeightedProp(filteredProps);
    }
}