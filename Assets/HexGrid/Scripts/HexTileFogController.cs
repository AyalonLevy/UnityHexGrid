using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HexTileData))]
public class HexTileFogController : MonoBehaviour
{
    public enum FogState
    {
        Hidden,
        Explored,
        Visible
    }

    [Header("References")]
    [Tooltip("The mesh object sitting on top of the hex representing the fog layer.")]
    [SerializeField] public GameObject fogVisualObject; // Public so the generator can assign to it

    [Header("Assets")]
    [SerializeField] private Material fogMaterial;

    [Header("Visual Tint Settings")]
    [SerializeField] private Color exploredShadowColor = new(0.4f, 0.4f, 0.45f, 1.0f);

    [SerializeField, HideInInspector] private bool isFogOfWarEnabled = false;

    private HexTileData tileData;
    private GameObject propsContainer;
    private GameObject visualsContainer;
    //private FogState currentState = FogState.Hidden;

    // --- DEBUGGING ---
    [Header("--- DEBUG ---")]
    [SerializeField] private FogState currentState = FogState.Hidden;

    private MaterialPropertyBlock propertyBlock;
    // URP uses _BaseColor, standard shaders use _Color
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (tileData == null) tileData = GetComponent<HexTileData>();

        if (tileData != null)
        {
            if (tileData.visualsContainer != null) visualsContainer = tileData.visualsContainer.gameObject;
            if (tileData.propsContainer != null) propsContainer = tileData.propsContainer.gameObject;
        }

        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
    }

    public void InitializeFoW(bool enableFoW, HexTileData data)
    {
        isFogOfWarEnabled = enableFoW;

        tileData = data;
        propsContainer = tileData.propsContainer.gameObject;
        visualsContainer = tileData.visualsContainer.gameObject;
        propertyBlock ??= new MaterialPropertyBlock();

        if (!isFogOfWarEnabled)
        {
            // If FOW is disabled, ensure everything is active and bypass script
            if (fogVisualObject != null) fogVisualObject.SetActive(false);
            if (propsContainer != null) propsContainer.SetActive(true);
            if (visualsContainer != null) visualsContainer.SetActive(true);
            enabled = false;
            return;
        }

        // Apply custom fog material to the fog visual mesh if assigned
        if (fogVisualObject != null && fogMaterial != null)
        {
            if (fogVisualObject.TryGetComponent<Renderer>(out var fogRenderer))
            {
                fogRenderer.material = fogMaterial;
            }
        }

        // Default initial state
        SetFogState(FogState.Hidden);
    }

    public void SetFogState(FogState newState)
    {
        if (!isFogOfWarEnabled) return;

        currentState = newState;

        switch (currentState)
        {
            case FogState.Hidden:
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    // EDIT MODE: Keep the level fully visible
                    ToggleFogVisual(false);
                    ToggleProps(true);
                    ToggleVisuals(true);
                    ApplyShadowTint(false);
                    break;
                }
#endif
                // PLAY MODE (or Built Game): True hidden state 
                ToggleFogVisual(true);
                ToggleProps(false);
                ToggleVisuals(false);
                break;

            case FogState.Explored:
                ToggleFogVisual(false);
                ToggleProps(true);
                ToggleVisuals(true);
                ApplyShadowTint(true);
                break;

            case FogState.Visible:
                ToggleFogVisual(false);
                ToggleProps(true);
                ToggleVisuals(true);
                ApplyShadowTint(false);
                break;
        }
    }

    private void ApplyShadowTint(bool isShadowed)
    {
        if (visualsContainer == null) return;

        propertyBlock ??= new MaterialPropertyBlock();

        List<Renderer> renderers = new();
        renderers.AddRange(visualsContainer.GetComponentsInChildren<Renderer>());

        if (propsContainer != null)
        {
            renderers.AddRange(propsContainer.GetComponentsInChildren<Renderer>());
        }

        foreach (var rend in renderers)
        {
            if (rend == null || rend.sharedMaterial == null) continue;

            if (isShadowed)
            {
                for (int i = 0; i < rend.sharedMaterials.Length; i++)
                {
                    Material mat = rend.sharedMaterials[i];
                    if (mat == null) continue;

                    rend.GetPropertyBlock(propertyBlock, i);

                    // Fetch the original color from the material
                    Color originalColor = Color.white;
                    if (mat.HasProperty(BaseColorId))
                    {
                        originalColor = mat.GetColor(BaseColorId);
                    }
                    else if (mat.HasProperty(ColorId))
                    {
                        originalColor = mat.GetColor(ColorId);
                    }

                    // Multiply original color by the shadow tint
                    propertyBlock.SetColor(BaseColorId, originalColor * exploredShadowColor);

                    rend.SetPropertyBlock(propertyBlock, i);
                }

            }
            else
            {
                // Clear the override block entirely to restore original material
                for (int i = 0; i < rend.sharedMaterials.Length; i++)
                {
                    rend.SetPropertyBlock(null, i);
                }

                rend.SetPropertyBlock(null);    // Clear global just in case
            }
        }
    }

    private void ToggleProps(bool active)
    {
        if (propsContainer != null) propsContainer.SetActive(active);
        else Debug.Log("No Prop Container");
    }

    private void ToggleVisuals(bool active)
    {
        if (visualsContainer != null) visualsContainer.SetActive(active);
        else Debug.Log("No Visual Container");
    }

    private void ToggleFogVisual(bool active)
    {
        if (fogVisualObject != null) fogVisualObject.SetActive(active);
        else Debug.Log("No FogObject Container");
    }

    public FogState GetCurrentState() => currentState;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (isFogOfWarEnabled)
        {
            // Delaying the call prevents Unity Editor warnings about changing components during OnValidate
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    SetFogState(currentState);
                }
            };
        }
    }

    // This draws the clean "Fog Indicator" wireframe in Edit Mode so you know fog is assigned
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying && isFogOfWarEnabled && currentState == FogState.Hidden)
        {
            if (fogVisualObject != null && fogVisualObject.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null)
            {
                Gizmos.color = new Color(0.5f, 0.5f, 0.6f, 0.5f); // Subtle grey-blue indicator
                Gizmos.DrawWireMesh(mf.sharedMesh, fogVisualObject.transform.position, fogVisualObject.transform.rotation, fogVisualObject.transform.localScale);
            }
        }
    }
#endif
}
