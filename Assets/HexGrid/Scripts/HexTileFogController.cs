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

    private HexTileData tileData;
    private GameObject propsContainer;
    private GameObject visualsContainer;
    private FogState currentState = FogState.Hidden;
    private bool isFogOfWarEnabled = false;

    private void Awake()
    {
        tileData = GetComponent<HexTileData>();
        propsContainer = tileData.propsContainer.gameObject;
        visualsContainer = tileData.visualsContainer.gameObject;
    }

    public void InitializeFoW(bool enableFoW)
    {
        isFogOfWarEnabled = enableFoW;

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

    private void SetFogState(FogState newState)
    {
        if (!isFogOfWarEnabled) return;

        currentState = newState;

        switch (currentState)
        {
            case FogState.Hidden:
                ToggleFogVisual(true);
                ToggleProps(false);
                ToggleVisuals(false);
                break;

            case FogState.Explored:
                ToggleFogVisual(false);
                ToggleProps(true);
                ToggleVisuals(true);
                // Optional: You can dim or shade the tile/props here if desired for "memory" view
                break;

            case FogState.Visible:
                ToggleFogVisual(false);
                ToggleProps(true);
                ToggleVisuals(true);
                break;
        }
    }

    private void ToggleProps(bool active)
    {
        if (propsContainer != null) propsContainer.SetActive(active);
    }

    private void ToggleVisuals(bool active)
    {
        if (visualsContainer != null) visualsContainer.SetActive(active);
    }

    private void ToggleFogVisual(bool active)
    {
        if (fogVisualObject != null) fogVisualObject.SetActive(active);
    }

    public FogState GetCurrentState() => currentState;
}
