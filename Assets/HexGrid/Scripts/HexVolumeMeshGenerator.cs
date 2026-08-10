using UnityEngine;

public class HexVolumeMeshGenerator
{
    private const int HEXAGON_SIDES = 6;
    private const float DEGREES_PER_SIDE = 60.0f;
    private const float POINTY_TOP_OFFSET = 30.0f;
    private const float UV_CENTER_OFFSET = 0.5f;
    private const float UV_SCALE = 0.5f;

    /// <summary>
    /// Generates a flat 2D Hexagonal Plane mesh facing upwards.
    /// </summary>
    public static Mesh CreateHexPlaneMesh(float outerRadius)
    {
        Mesh mesh = new()
        {
            name = "HexPlaneMesh"
        };

        int totalVertices = 7; // 1 center + 6 outer
        int totalTriangles = HEXAGON_SIDES * 3; // 18 indices

        Vector3[] vertices = new Vector3[totalVertices];
        Vector2[] uv = new Vector2[totalVertices];
        int[] triangles = new int[totalTriangles];

        float angleOffset = POINTY_TOP_OFFSET;
        int vIndex = 0;
        int tIndex = 0;

        // --- CENTER VERTEX ---
        vertices[vIndex] = Vector3.zero;
        uv[vIndex] = new Vector2(UV_CENTER_OFFSET, UV_CENTER_OFFSET);
        int centerIndex = vIndex++;

        // --- PERIMETER VERTICES ---
        int[] perimeterIndices = new int[HEXAGON_SIDES];
        for (int i = 0; i < HEXAGON_SIDES; i++)
        {
            float rad = Mathf.Deg2Rad * (angleOffset + i * DEGREES_PER_SIDE);
            float x = outerRadius * Mathf.Cos(rad);
            float z = outerRadius * Mathf.Sin(rad);

            vertices[vIndex] = new Vector3(x, 0.0f, z);
            uv[vIndex] = new Vector2((Mathf.Cos(rad) + 1.0f) * UV_SCALE, (Mathf.Sin(rad) + 1.0f) * UV_SCALE);
            perimeterIndices[i] = vIndex++;
        }

        // --- TRIANGLES ---
        for (int i = 0; i < HEXAGON_SIDES; i++)
        {
            int next = (i + 1) % HEXAGON_SIDES;

            // Winding order set to ensure normals face UP in Unity
            triangles[tIndex++] = centerIndex;
            triangles[tIndex++] = perimeterIndices[next];
            triangles[tIndex++] = perimeterIndices[i];
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}
