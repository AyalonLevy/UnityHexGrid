using System;
using UnityEngine;

public class Highlight : MonoBehaviour
{
    [Tooltip("The unlit transparent material used for the highlight glow.")]
    [SerializeField] private Material highlightMaterial;

    [Tooltip("Slight scale multiplier to prevent Z-fighting (flickering) over the original mesh.")]
    [SerializeField] private float scaleMultiplier = 1.02f;

    private GameObject generatedOverlayObject;
    private MeshRenderer overlayRenderer;

    private Color validSpaceColor = Color.green;
    private Color originalHighlightColor;

    private void Awake()
    {
        if (highlightMaterial != null)
        {
            originalHighlightColor = highlightMaterial.GetColor("_HighlightColor");
        }
    }

    public void Initialize(Transform targetContainer)
    {
        if (targetContainer != null) targetContainer = transform;

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
        MeshRenderer overlayRenderer = generatedOverlayObject.AddComponent<MeshRenderer>();

        overlayMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

        if (highlightMaterial != null)
        {
            // Create a safe instance of the material so it doesn't leak shared asset modifications
            overlayRenderer.sharedMaterial = new(highlightMaterial);
        }
        else
        {
            Debug.LogWarning("Highlight: No highlight material assigned!", this);
        }

        // Start hidden
        generatedOverlayObject.SetActive(false);
    }

    public void SetHighlight(bool state)
    {
        if (generatedOverlayObject != null)
        {
            generatedOverlayObject.SetActive(state);
        }
    }

    internal void HighlightValidPath()
    {
        if (overlayRenderer != null && overlayRenderer.material != null)
        {
            // Change color to indicate the active path (e.g., Yellow/Gold)
            overlayRenderer.material.SetColor("_HighlightColor", Color.yellow);
        }
    }

    internal void ResetHighlight()
    {
        if (overlayRenderer != null && overlayRenderer.material != null)
        {
            // Reset color back to the standard valid space highlight color
            overlayRenderer.material.SetColor("_HighlightColor", validSpaceColor);
        }
    }
}
