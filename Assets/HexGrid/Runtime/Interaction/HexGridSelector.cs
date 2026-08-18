namespace HexGrid
{
    using System;
    using UnityEngine;

    [RequireComponent(typeof(GridManager))]
    public class HexGridSelector : Selector
    {
        private HexTileData currentlySelectedTile;
        private MovementSystem movementSystem;

        // Public API Events for external tool integration
        public event Action<HexTileData> OnTileSelected;
        public event Action<HexTileData> OnTileDeselected;
        public event Action<HexTileData> OnTileClicked;

        [HideInInspector] public bool isSelectable = false;

        public HexTileData CurrentSelectedTile => currentlySelectedTile;
        public void SetSelectableState(bool canBeSelected) { isSelectable = canBeSelected; }

        protected override void Awake()
        {
            base.Awake();
            movementSystem = GetComponent<MovementSystem>();
        }

        protected override void HandleRaycastHit(RaycastHit hit)
        {
            if (hit.collider == null) return;

            HexTileData hitTile = hit.collider.GetComponentInParent<HexTileData>();

            if (movementSystem == null) movementSystem = GetComponent<MovementSystem>();

            if (hitTile != null)
            {
                // 1. Check if pathfinding is active and this clicked tile is a highlighted movement target
                if (movementSystem != null && movementSystem.HasActiveUnit)
                {
                    // It is highlighted as a valid move destination! Send action and return.
                    movementSystem.ProcessMovementClick(hitTile);
                    OnTileClicked?.Invoke(hitTile);
                    return;
                }

                // 2. Otherwise, it is NOT highlighted for movement. Continue with normal tile selection/building logic.
                if (isSelectable)
                {
                    if (hitTile != currentlySelectedTile)
                    {
                        SelectTile(hitTile);
                    }
                    else
                    {
                        ClearSelection();
                    }
                }
            }
            else
            {
                ClearSelection();
            }
        }

        protected override void HandleRaycastMiss()
        {
            ClearSelection();
        }

        private void SelectTile(HexTileData newTile)
        {
            if (currentlySelectedTile != null)
            {
                currentlySelectedTile.DisableHighlight(false);
                OnTileDeselected?.Invoke(currentlySelectedTile);
            }

            currentlySelectedTile = newTile;

            if (currentlySelectedTile != null)
            {
                currentlySelectedTile.EnableHighlight(false);
                OnTileSelected?.Invoke(currentlySelectedTile);
            }
        }

        public void ClearSelection()
        {
            if (currentlySelectedTile != null)
            {
                currentlySelectedTile.DisableHighlight(false);
                OnTileDeselected?.Invoke(currentlySelectedTile);
                currentlySelectedTile = null;
            }
        }
    }
}