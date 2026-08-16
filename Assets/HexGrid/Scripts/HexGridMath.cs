using UnityEngine;

public class HexGridMath
{
    /// <summary>
    /// Translates a physical world position into an exact integer cube coordinate based on the hex radius.
    /// </summary>
    public static Vector3Int WorldToCubeCoordinates(Vector3 position, float hexRadius)
    {
        float safeRadius = hexRadius > 0.0f ? hexRadius : 1.0f;

        float q = (Mathf.Sqrt(3.0f) / 3.0f * position.x - 1.0f / 3.0f * position.z) / hexRadius;
        float r = (2.0f / 3.0f * position.z) / hexRadius;
        float s = -q - r;

        return CubeRound(q, r, s);
    }

    /// <summary>
    /// Snaps fractional cube coordinates to the nearest valid integer hex coordinate.
    /// </summary>
    public static Vector3Int CubeRound(float fracQ, float fracR, float fracS)
    {
        int q = Mathf.RoundToInt(fracQ);
        int r = Mathf.RoundToInt(fracR);
        int s = Mathf.RoundToInt(fracS);

        float qDiff = Mathf.Abs(q - fracQ);
        float rDiff = Mathf.Abs(r - fracR);
        float sDiff = Mathf.Abs(s - fracS);

        // Fix rounding error -> q + r + s = 0
        if (qDiff > rDiff && qDiff > sDiff)
        {
            q = -r - s;
        }
        else if (rDiff > sDiff)
        {
            r = -q - s;
        }
        else
        {
            s = -q - r;
        }

        return new Vector3Int(q, r, s);
    }

    /// <summary>
    /// Calculates the exact hex distance between two cube coordinates.
    /// </summary>
    public static int GetCubeDistance(Vector3Int a, Vector3Int b)
    {
        return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z)) / 2;
    }
}
