namespace HexGrid
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(TileDomainDropdownAttribute))]
    public class TileDomainDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Ensure this is applied to TileDomain object reference field
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            // Find the database in the scene to extract available domains
            HexGridDatabase database = FindActiveDatabase();

            if (database == null || database.availableDomains == null || database.availableDomains.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            // Build the option names for the dropdown
            List<string> displayedOptions = new();
            List<TileDomain> domainList = database.availableDomains;

            int currentIndex = 0;
            TileDomain currentAssignedDomain = property.objectReferenceValue as TileDomain;

            for (int i = 0; i < domainList.Count; i++)
            {
                if (domainList[i] != null)
                {
                    displayedOptions.Add(domainList[i].name);
                    if (domainList[i] == currentAssignedDomain)
                    {
                        currentIndex = i;
                    }
                }
                else
                {
                    displayedOptions.Add("Unassigned Domain");
                }
            }

            // Draw the dropdown popup field
            position = EditorGUI.PrefixLabel(position, label);
            int selectedIndex = EditorGUI.Popup(position, currentIndex, displayedOptions.ToArray());

            // Update property reference if a new domain was selected
            if (selectedIndex >= 0 && selectedIndex < domainList.Count)
            {
                property.objectReferenceValue = domainList[selectedIndex];
            }
        }

        private HexGridDatabase FindActiveDatabase()
        {
            // Try to find an active generator in the scene to reference its database
            HexGridGenerator generator = Object.FindFirstObjectByType<HexGridGenerator>();
            if (generator != null)
            {
                // Use SerializedObject to access private/serialized database field safely
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
}