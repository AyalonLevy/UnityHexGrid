namespace HexGrid
{
    using UnityEngine;

    public class HexVolumeMeshGenerator
    {
        private const int HexagonSides = 6;
        private const float DegreesPerSide = 60.0f;
        private const float PointyTopOffset = 30.0f;
        private const float UvCenterOffset = 0.5f;
        private const float UvScale = 0.5f;

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
            int totalTriangles = HexagonSides * 3; // 18 indices

            Vector3[] vertices = new Vector3[totalVertices];
            Vector2[] uv = new Vector2[totalVertices];
            int[] triangles = new int[totalTriangles];

            int vIndex = 0;
            int tIndex = 0;

            // --- CENTER VERTEX ---
            vertices[vIndex] = Vector3.zero;
            uv[vIndex] = new Vector2(UvCenterOffset, UvCenterOffset);
            int centerIndex = vIndex++;

            // --- PERIMETER VERTICES ---
            int[] perimeterIndices = new int[HexagonSides];
            float degToRad = Mathf.Deg2Rad;
            float angleOffsetRad = PointyTopOffset * degToRad;
            float sideStepRad = DegreesPerSide * degToRad;

            for (int i = 0; i < HexagonSides; i++)
            {
                float rad = angleOffsetRad + (i * sideStepRad);
                float cosRad = Mathf.Cos(rad);
                float sinRad = Mathf.Sin(rad);

                float x = outerRadius * cosRad;
                float z = outerRadius * sinRad;

                vertices[vIndex] = new Vector3(x, 0.0f, z);
                uv[vIndex] = new Vector2((cosRad + 1.0f) * UvScale, (sinRad + 1.0f) * UvScale);
                perimeterIndices[i] = vIndex++;
            }

            // --- TRIANGLES ---
            for (int i = 0; i < HexagonSides; i++)
            {
                int next = (i + 1) % HexagonSides;

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
}