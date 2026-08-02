using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor.Build.Content;


#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// TODO: Turn the hexGridTile to an array and allow multiple tiles to be generated.
/// The props should each have an identifier to which tilethey belong to.
/// Only related props should be spawned on the specific tiles (forset on gras tiles, rocks on mountain tiles, etc)
/// </summary>


[System.Serializable]
public struct GridData
{
    public int tileIndex;
    public Vector3 tilePosition;
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

[System.Serializable]
public struct WeightedTile
{
    public GameObject tilePrefab;

    [Range(0.0f, 1.0f), Tooltip("Districution weight (0 to 1)")]
    public float weight;
}

public enum GridShape
{
    Rectangle,
    Hexagonal,
    FromFile
}

public class HexGridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("First entry will be set as the default tile")]
    [SerializeField] private WeightedTile[] hexGridTiles;
    [Tooltip("The length of the hexagon edge")]
    [SerializeField] private float tileEdgeSize = 1;
    [Tooltip("The height to place the props on the hexagon tile")]
    [SerializeField] private float tileHeight = 0.1f;
    [SerializeField] private float gapBetweenTiles = 0; // Add an option to add a gap between the tiles
    [SerializeField] private Transform gridContainer;
    
    
    [Tooltip("Select grid shape")]
    [SerializeField] private GridShape gridShape = GridShape.Rectangle;

    [SerializeField] private Vector2 gridSize;
    [Tooltip("Number of tiles rings")]
    [Range(0, 50)]
    [SerializeField] int gridRadius;
    [Tooltip("Will create the grid based on the grid data file.")]
    [SerializeField] private TextAsset gridDataFile;

    [SerializeField] private bool addProps = false;

    [SerializeField] private GameObject[] props;
    [SerializeField] private float coveragePercentage = 0.5f;
    [SerializeField] private Vector2 scaleRange;

    [SerializeField] private string fileName = "GridData";

    //[SerializeField] private bool isFlatTopped;

    private readonly float hexagonAngleDeg = 60.0f;
    private readonly int hexagonSides = 6;
    private readonly string propContainerName = "Props";


    public void GenerateGrid()
    {
        // Clear existing tiles first
        ClearGrid();

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
                    SpawnTile(GetRandomWeightedTile(), GetTilePositionForHorizontalGrid(x, y));
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
                    SpawnTile(GetRandomWeightedTile(),gridPositions[j]);
                }
            }

            SpawnTile(GetRandomWeightedTile(), Vector3.zero);  // Spawn the center tile (ring 0)
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
                SpawnTile(hexGridTiles[gridTiles[i].tileIndex].tilePrefab, gridTiles[i].tilePosition, gridTiles[i].hasProp);

                // Each tile with prop should spawn the prop based on the index in the props array
                if (gridTiles[i].hasProp && gridTiles[i].propIndex >= 0 && gridTiles[i].propIndex < props.Length)
                {
                    GameObject propToPlace = props[gridTiles[i].propIndex];

                    Transform propsContainer = gridContainer.GetChild(i).Find(propContainerName);
                    if (propsContainer == null)
                    {
                        propsContainer = new GameObject(propContainerName).transform;
                        propsContainer.SetParent(gridContainer);
                        propsContainer.localPosition = new(0.0f, tileHeight, 0.0f);
                    }

                    SpawnProp(propToPlace, propsContainer, gridTiles[i].propRotation, gridTiles[i].propScale);
                }
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
        if (gridContainer == null)
        {
            Debug.LogError("GridContainer is empty. No grid tiles to save.");
            return;
        }

        // Get the transform of each child hex tile of gridContainer and store their positions in a list
        GridDataWrapper grid = new();

        foreach (Transform child in gridContainer)
        {
            GridData data = new()
            {
                tilePosition = child.position
            };

            string tileName = child.name.Replace("(Clone)", "").Trim();
            data.tileIndex = System.Array.FindIndex(hexGridTiles, c => c.tilePrefab.name == tileName);

            Transform propContainer = child.Find(propContainerName);
            data.hasProp = propContainer != null && propContainer.childCount > 0;

            // Get the name of the prop and compare it to the props array to find the index. If prop not found, set index to -1
            if (data.hasProp)
            {
                string propName = propContainer.GetChild(0).name.Replace("(Clone)", "").Trim();

                data.propIndex = System.Array.FindIndex(props, p => p.name == propName);

                data.propRotation = propContainer.GetChild(0).transform.rotation.eulerAngles.y;
                data.propScale = propContainer.GetChild(0).transform.localScale.x;
            }
            else
            {
                data.propIndex = -1;
                data.propRotation = 0;
                data.propScale = 1;
            }

            grid.gridData.Add(data);
        }

        // Convert the list of positions to JSON format
        string json = JsonUtility.ToJson(grid, true);

        // Save the JSON to a file
        string filePath = Application.dataPath + $"/Resources/{fileName}.json";
        
        File.WriteAllText(filePath, json);

        Debug.Log($"Saved grid coordinates to {filePath}");

        // Force Unity to see the new asset in the Resources folder
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    private GameObject GetRandomWeightedTile()
    {
        float totalWeight = 0.0f;
        foreach (var tile in hexGridTiles)
        {
            totalWeight += tile.weight;
        }

        // The first entry is the default value
        if (totalWeight < 0.0f) return hexGridTiles[0].tilePrefab;

        float randomValue = Random.Range(0, totalWeight);
        float currentSum = 0.0f;

        foreach (var tile in hexGridTiles)
        {
            currentSum += tile.weight;
            if (randomValue <= currentSum)
            {
                return tile.tilePrefab;
            }
        }

        return hexGridTiles[0].tilePrefab;
    }

    private void SpawnTile(GameObject tilePrefab, Vector3 position, bool fromFile = false)
    {
        if (tilePrefab == null) return;

        GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, gridContainer);

        if (addProps && props.Length > 0 && !fromFile)
        {
            DistributePropsRandomaly(tile.transform);
        }

#if UNITY_EDITOR
        // Allows undo support when deleting in edit mode
        Undo.RegisterCreatedObjectUndo(tile, "Generate Hex Tile");
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
        List <Vector3> cornerPositions = new();

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

    private void DistributePropsRandomaly(Transform tile)
    {
        // Check if transform has a child called "Props" if yes instantiate props under the child, if not instantiate props under the tile transform
        Transform propsContainer = tile.Find(propContainerName);
        if (propsContainer == null)
        {
            propsContainer = new GameObject(propContainerName).transform;
            propsContainer.SetParent(tile);
            propsContainer.localPosition = new(0.0f, tileHeight, 0.0f);
        }

        // Randomaly select a prop from the list of props - use the coverage percentage to determine if a prop should be placed or not
        if (Random.value > coveragePercentage) return;

        GameObject propToPlace = props[Random.Range(0, props.Length)];

        // Random rotation along the y axis
        float randomYRotation = Random.Range(0, 360);

        // Random scale based on the scale range
        float randomScale = Random.Range(scaleRange.x, scaleRange.y);

        SpawnProp(propToPlace, propsContainer, randomYRotation, randomScale);
    }

    private void SpawnProp(GameObject prop, Transform parent, float protRot, float propScale)
    {
        GameObject propInstance = Instantiate(prop, parent);
        propInstance.transform.localRotation = Quaternion.Euler(0, protRot, 0);
        propInstance.transform.localScale = Vector3.one * propScale;
    }

    // TODO: Randomally distribute tyles types?
}