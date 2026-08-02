using UnityEngine;

public class HexGridLayout : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector2Int gridSize;

    [Header("Tile Settings")]
    public float outerSize = 1.0f;
    public float innerSize = 0.0f;
    public float height = 1.0f;
    public bool isFlatTopped;
    public Material material;

    private void OnEnable()
    {
        LayoutGrid();
    }

    //private void OnValidate()
    //{
    //    if (Application.isPlaying)
    //    {
    //        LayoutGrid();
    //    }
    //}


    private void LayoutGrid()
    {
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                GameObject tile = new($"Hex {x}, {y}", typeof(HexRenderer));
                tile.transform.position = GetPositionForHexFromCoordinate(new Vector2Int(x, y));

                HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
                hexRenderer.isFlatTopped = isFlatTopped;
                hexRenderer.outerSize = outerSize;
                hexRenderer.innerSize = innerSize;
                hexRenderer.height = height;
                hexRenderer.SetMaterial(material);
                hexRenderer.DrawMesh();

                tile.transform.SetParent(transform, true);
            }
        }
    }

    public Vector3 GetPositionForHexFromCoordinate(Vector2Int coordinate)
    {
        int row = coordinate.x;
        int column = coordinate.y;
        float width;
        float height;
        float xPosition;
        float yPosition;
        bool shouldOffset;
        float horizontalDistance;
        float verticalDistance;
        float offset;
        float size = outerSize;

        if (!isFlatTopped)
        {
            shouldOffset = (row % 2) == 0;
            width = Mathf.Sqrt(3) * size;
            height = 2.0f * size;

            horizontalDistance = width;
            verticalDistance = height * (3.0f / 4.0f);

            offset = (shouldOffset) ? width / 2 : 0;

            xPosition = (column * horizontalDistance) + offset;
            yPosition = (row * verticalDistance);
        }
        else
        {
            shouldOffset = (column % 2) == 0;
            width = 2.0f * size;
            height = Mathf.Sqrt(3) * size;

            horizontalDistance = width * (3.0f / 4.0f);
            verticalDistance = height;

            offset = (shouldOffset) ? height / 2 : 0;

            xPosition = (column * horizontalDistance);
            yPosition = (row * verticalDistance) - offset;
        }

            return new Vector3(xPosition, 0, -yPosition);
    }
}
