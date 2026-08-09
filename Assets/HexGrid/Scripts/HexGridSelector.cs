using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GridManager))]
public class HexGridSelector : MonoBehaviour
{
    private const float MAX_RAYCAST_DISTANCE = 1000.0f;

    [Header("Optional Input Asset Configuration")]
    [Tooltip("Leave blank to use default New Input System device polling (Mouse/Touch).")]
    [SerializeField] private InputActionReference selectAction;

    [Tooltip("Leave blank to use default New Input System device polling (Mouse/Touch).")]
    [SerializeField] private InputActionReference pointerPositionAction;

    [Header("Dependencies")]
    [SerializeField] private Camera mainCamera;

    private GridManager gridManager;
    private HexTileData currentlySelectedTile;

    private void Awake()
    {
        gridManager = GetComponent<GridManager>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        if (selectAction != null)
        {
            selectAction.action.Enable();
            selectAction.action.performed += OnSelectPerformed;
        }

        if (pointerPositionAction != null)
        {
            pointerPositionAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (selectAction != null)
        {
            selectAction.action.performed -= OnSelectPerformed;
            selectAction.action.Disable();
        }

        if (pointerPositionAction != null)
        {
            pointerPositionAction.action.Disable();
        }
    }

    private void Update()
    {
        // Fallback: If no custom InputActionReference is assigned, poll the New Input System directly so it works instantly with zero setup.
        if (selectAction == null)
        {
            bool isPressed = false;
            Vector2 pointerPos = Vector2.zero;

            // Check Mouse
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                isPressed = true;
                pointerPos = Mouse.current.position.ReadValue();
            }
            // Checl Touch / Generic Pointer (Mobile support)
            else if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                isPressed = true;
                pointerPos = Pointer.current.position.ReadValue();
            }

            if (isPressed)
            {
                ProcessClickAt(pointerPos);
            }
        }
    }

    private void OnSelectPerformed(InputAction.CallbackContext context)
    {
        if (pointerPositionAction == null || mainCamera == null) return;

        Vector2 screenPosition = pointerPositionAction.action.ReadValue<Vector2>();

        ProcessClickAt(screenPosition);
    }

    /// <summary>
    /// PUBLIC API: Call this method directly from your own 
    /// custom controllers, PlayerInput components, or UI managers.
    /// </summary>
    public void ProcessClickAt(Vector2 screenPosition)
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, MAX_RAYCAST_DISTANCE))
        {
            HexTileData hitTile = hit.collider.GetComponentInParent<HexTileData>();

            if (hitTile != currentlySelectedTile)
            {
                ProcessTileSelection(hitTile);
            }
            else
            {
                ClearSelection();
            }
        }
        else
        {
            ClearSelection();
        }
    }


    private void ProcessTileSelection(HexTileData newTile)
    {
        if (newTile == currentlySelectedTile) return;

        if (currentlySelectedTile != null)
        {
            currentlySelectedTile.DisableHighlight();
        }

        currentlySelectedTile = newTile;

        if (currentlySelectedTile != null)
        {
            currentlySelectedTile.EnableHighlight();

            Debug.Log($"Selected Tile at Cube Coordinates {currentlySelectedTile.tileCoordinates} ");
        }
    }

    private void ClearSelection()
    {
        if (currentlySelectedTile != null)
        {
            currentlySelectedTile = null;
            Debug.Log("Selection Cleared.");
        }
    }
}
