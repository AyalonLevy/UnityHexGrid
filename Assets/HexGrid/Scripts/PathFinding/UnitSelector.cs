using System;
using Unity.VisualScripting;
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
            Debug.Log($"Selected Unit: {currentlySelectedUnit.name}");
        }
    }

    public void ClearSelection()
    {
        if (currentlySelectedUnit != null)
        {
            currentlySelectedUnit.Deselect();
            OnUnitDeselected?.Invoke(currentlySelectedUnit);
            currentlySelectedUnit = null;
            Debug.Log("Unit Selection Cleared.");
        }
    }
}
