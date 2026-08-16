using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(PropDropdownAttribute))]
public class PropDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ObjectReference)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        // Find the database in the scene to extract props
        HexGridDatabase database = FindActiveDatabase();

        if (database == null || database.props == null || database.props.Count == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        // Get the HexTileData instance we are currently inspecting
        HexTileData tileData = property.serializedObject.targetObject as HexTileData;
        if (tileData == null || tileData.currentDomain == null)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        // Filter props that contain the tile's current domain
        List<HexGridDatabase.PropData> filteredProps = database.props.FindAll(p => p.domains != null && p.domains.Contains(tileData.currentDomain));

        List<string> displayedOptions = new()
        {
            "None"
        };

        int currentIndex = 0;
        GameObject currentAssignedProp = property.objectReferenceValue as GameObject;

        for (int i = 0; i < filteredProps.Count; i++)
        {
            if (filteredProps[i].propPrefab != null)
            {
                displayedOptions.Add(filteredProps[i].propPrefab.name);
                if (filteredProps[i].propPrefab == currentAssignedProp)
                {
                    currentIndex = i + 1;   // +1 because of the "None" at index 0
                }
            }
        }

        // Draw the dropdown popup field
        position = EditorGUI.PrefixLabel(position, label);
        int selectedIndex = EditorGUI.Popup(position, currentIndex, displayedOptions.ToArray());

        // Update property reference based on selection[cite: 21]
        if (selectedIndex == 0)
        {
            property.objectReferenceValue = null; // Properly unassigns prop when "None" is selected
        }
        else if (selectedIndex > 0 && (selectedIndex - 1) < filteredProps.Count)
        {
            property.objectReferenceValue = filteredProps[selectedIndex - 1].propPrefab;
        }
    }

    private HexGridDatabase FindActiveDatabase()
    {
        // Try to find an active generator in the scene to reference its database
        HexGridGenerator generator = Object.FindFirstObjectByType<HexGridGenerator>();
        if (generator != null)
        {
            // Use SerializeObject to access private/serialized database field safely
            SerializedObject serializedGenerator = new(generator);
            SerializedProperty dbProp = serializedGenerator.FindProperty("gridDatabase");
            if (dbProp != null && dbProp.objectReferenceValue is HexGridDatabase db)
            {
                return db;
            }
        }

        return null;
    }
}
