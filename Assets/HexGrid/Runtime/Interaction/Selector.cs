namespace HexGrid
{
    using UnityEngine;
    using UnityEngine.InputSystem;

    public abstract class Selector : MonoBehaviour
    {
        private const float MAX_RAYCAST_DISTANCE = 1000.0f;

        [Header("Optional Input Asset Configuration")]
        [Tooltip("Leave blank to use default New Input System device polling (Mouse/Touch).")]
        [SerializeField] private InputActionReference selectAction;

        [Tooltip("Leave blank to use default New Input System device polling (Mouse/Touch).")]
        [SerializeField] private InputActionReference pointerPositionAction;

        [Header("Dependencies")]
        [SerializeField] private Camera mainCamera;

        protected virtual void Awake()
        {
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
                // Check Touch / Generic Pointer (Mobile support)
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

        public void ProcessClickAt(Vector2 screenPosition)
        {
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, MAX_RAYCAST_DISTANCE))
            {
                HandleRaycastHit(hit);
            }
            else
            {
                HandleRaycastMiss();
            }
        }

        // Abstract methods that derived selectors must implement
        protected abstract void HandleRaycastHit(RaycastHit hit);
        protected abstract void HandleRaycastMiss();
    }
}