namespace HexGrid
{
    using System;
    using UnityEngine;

    public class UnitSelector : Selector
    {
        private Unit currentlySelectedUnit;

        // Public API Events
        public event Action<Unit> OnUnitSelected;
        public event Action<Unit> OnUnitDeselected;

        public Unit CurrentlySelectedUnit => currentlySelectedUnit;

        protected override void HandleRaycastHit(RaycastHit hit)
        {
            if (hit.collider == null) return;

            Unit hitUnit = hit.collider.GetComponentInParent<Unit>();

            if (hitUnit != null)
            {
                if (hitUnit != currentlySelectedUnit)
                {
                    SelectUnit(hitUnit);
                }
                else
                {
                    ClearSelection();
                }
            }
        }


        protected override void HandleRaycastMiss()
        {
            ClearSelection();
        }

        private void SelectUnit(Unit newUnit)
        {
            if (currentlySelectedUnit != null)
            {
                currentlySelectedUnit.Deselect();
                OnUnitDeselected?.Invoke(currentlySelectedUnit);
            }

            currentlySelectedUnit = newUnit;

            if (currentlySelectedUnit != null)
            {
                currentlySelectedUnit.Select();
                OnUnitSelected?.Invoke(currentlySelectedUnit);
            }
        }

        public void ClearSelection()
        {
            if (currentlySelectedUnit != null)
            {
                currentlySelectedUnit.Deselect();
                OnUnitDeselected?.Invoke(currentlySelectedUnit);
                currentlySelectedUnit = null;
            }
        }
    }
}