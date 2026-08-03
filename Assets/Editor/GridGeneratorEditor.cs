using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HexGridGenerator))]
public class GridGeneratorEditor : Editor
{
    // SerializedProperties link to the underlying field safety
    private SerializedProperty gridShapeProp;
    private SerializedProperty gridSizeProp;
    private SerializedProperty gridRadiusProp;
    private SerializedProperty gridDataFileProp;
    private SerializedProperty addProps;
    private SerializedProperty fileNameProps;

    private void OnEnable()
    {
        // Cache the properties for performance and safety
        gridShapeProp = serializedObject.FindProperty("gridShape");
        gridSizeProp = serializedObject.FindProperty("gridSize");
        gridRadiusProp = serializedObject.FindProperty("gridRadius");
        gridDataFileProp = serializedObject.FindProperty("gridDataFile");
        addProps = serializedObject.FindProperty("addProps");
        fileNameProps = serializedObject.FindProperty("fileName");
    }

    public override void OnInspectorGUI()
    {
        // Always update the serialized object at the start of GUI rendering
        serializedObject.Update();

        // Draw everything in the script automatically, EXCLUDING the specified properties
        DrawPropertiesExcluding(serializedObject, "gridShape", "gridSize", "gridRadius", "gridDataFile", "addProps", "hexProps", "coveragePercentage", "scaleRange", "fileName");

        // Draw the grid shape enum dropdown first
        EditorGUILayout.PropertyField(gridShapeProp);

        EditorGUILayout.Space();

        // Check the current enum value (0 = Rectangle, 1 = Hexagonal)
        GridShape selectedShape = (GridShape)gridShapeProp.enumValueIndex;

        if (selectedShape == GridShape.Rectangle)
        {
            EditorGUILayout.LabelField("Rectangle Grid Parameters", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(gridSizeProp);
        } 
        else if (selectedShape == GridShape.Hexagonal)
        {
            EditorGUILayout.LabelField("Hexagonal Grid Parameters", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(gridRadiusProp);
        }
        else if (selectedShape == GridShape.FromFile)
        {
            EditorGUILayout.LabelField("Grid From File Parameters", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(gridDataFileProp);
        }

        EditorGUILayout.Space(15);

        EditorGUILayout.LabelField("Props Parameters", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(addProps);

        if (addProps.boolValue)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hexProps"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("coveragePercentage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleRange"));
        }

        // Apply any changes made in the inspector to the target object
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(15);

        // Reference to our target script component
        HexGridGenerator generator = (HexGridGenerator)target;

        // Draw Generate, Clear Save and buttons in the Inspector
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.2f, 1.0f, 0.3f);
        if (GUILayout.Button("Generate Grid", GUILayout.Height(35)))
        {
            generator.GenerateGrid();
        }

        GUI.backgroundColor = new Color(1.0f, 0.2f, 0.3f);
        if (GUILayout.Button("Clear Grid", GUILayout.Height(25)))
        {
            generator.ClearGrid();
        }

        EditorGUILayout.Space(3);

        GUI.backgroundColor = originalColor;
        EditorGUILayout.LabelField("Save Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(fileNameProps);

        EditorGUILayout.Space(15);

        serializedObject.ApplyModifiedProperties();

        if (GUILayout.Button("Save Grid to File", GUILayout.Height(35)))
        {
            generator.SaveGridToFile();
        }
    }
}
