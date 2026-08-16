using UnityEngine;

public class Highlight : MonoBehaviour
{
    [Header("Highlight Settings")]
    [Tooltip("Slight scale multiplier to prevent Z-fighting (flickering) over the original mesh.")]
    [SerializeField] private float scaleMultiplier = 1.02f;

    private GameObject generatedOverlayObject;
    private MeshRenderer overlayRenderer;

    private Color validSpaceColor = Color.green;

    public void InitializeHighlight(Transform targetContainer)
    {
        // Fallback to transform only if null
        if (targetContainer == null) targetContainer = transform;

        // Find the first <eshFilter inside the target container and copy its shape
        MeshFilter sourceMeshFilter = targetContainer.GetComponentInChildren<MeshFilter>();

        if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
        {
            Debug.LogWarning($"Highlight: No MeshFilter found on tile {gameObject.name}. Cannot generate procedural highlight", this);
            return;
        }

        // Create a lightweight child object for the overlay
        generatedOverlayObject = new("ProceduralHighlightOverlay");
        generatedOverlayObject.transform.SetParent(targetContainer, false);
        generatedOverlayObject.transform.localPosition = Vector3.zero;
        generatedOverlayObject.transform.localRotation = Quaternion.identity;
        generatedOverlayObject.transform.localScale = Vector3.one * scaleMultiplier;

        // Add components and assign the mesh and highlight material
        MeshFilter overlayMeshFilter = generatedOverlayObject.AddComponent<MeshFilter>();

        overlayRenderer = generatedOverlayObject.AddComponent<MeshRenderer>();

        overlayMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

        // Start hidden
        generatedOverlayObject.SetActive(false);
    }

    public void SetHighlight(bool state, Material highlightMat)
    {
        if (generatedOverlayObject != null)
        {
            generatedOverlayObject.SetActive(state);

            if (overlayRenderer != null && highlightMat != null)
            {
                // Use sharedMaterial to avoid material instance leaks
                overlayRenderer.sharedMaterial = highlightMat;
            }
        }
    }

    // TODO: Check if it is even used
    internal void HighlightValidPath()
    {
        if (overlayRenderer != null && overlayRenderer.material != null)
        {
            // Change color to indicate the active path (e.g., Yellow/Gold)
            overlayRenderer.sharedMaterial.SetColor("_HighlightColor", Color.yellow);
            Debug.Log("All is Yellow!");
        }
    }

    internal void ResetHighlight()
    {
        if (overlayRenderer != null && overlayRenderer.material != null)
        {
            // Reset color back to the standard valid space highlight color
            overlayRenderer.sharedMaterial.SetColor("_HighlightColor", validSpaceColor);
            Debug.Log("All is Green!");
        }
    }
}
