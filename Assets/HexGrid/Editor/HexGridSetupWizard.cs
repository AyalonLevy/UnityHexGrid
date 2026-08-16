using UnityEditor;
using UnityEngine;

public class HexGridSetupWizard : EditorWindow
{
    // --- Setup Control ---
    private enum WizardStep { Welcome, SetupDatabase, PopulateDatabase, SetupSceneObject }
    private WizardStep currentStep = WizardStep.Welcome;

    // --- Configuration Fields ---
    [Header("Database Settings")]
    [Tooltip("Name of the database asset that will be created.")]
    public string databaseAssetName = "HexGridDatabase";

    [Tooltip("If empty, the wizard can automatically create a HexGridGenerator in your scene.")]
    public HexGridGenerator targetGenerator;
    public bool createNewGenerator = true;

    private HexGridDatabase createdDatabase; // Keeps track if we already generated it
    private GameObject newGridContainer;
    private readonly string baseFolder = "Assets/HexGrid";


    // This creates a menu item at the top of the Unity editor
    [MenuItem("Tools/Hex Grid/Run Setup Wizard")]
    public static void ShowWindow()
    {
        HexGridSetupWizard window = GetWindow<HexGridSetupWizard>(true, "Hex Grid Setup Guide");
        window.minSize = new(500, 380);
        window.currentStep = WizardStep.Welcome;
    }

    private void OnGUI()
    {
        DrawWizardContent();
        DrawBottomButtons();
    }

    private void DrawWizardContent()
    {
        GUILayout.Space(10);

        // Custom style for help text
        GUIStyle helpStyle = new(EditorStyles.helpBox)
        {
            fontSize = 12,
            padding = new RectOffset(10, 10, 10, 10),
            richText = true
        };

        switch (currentStep)
        {
            case WizardStep.Welcome:
                GUILayout.Label("<b>Welcome to the Hex Grid Generator!</b>\n\n" +
                                "This wizard will help you set up your database, tiles, and scene generator without breaking your configuration.\n\n" +
                                "Click <b>Continue</b> to generate your workspace folders and read the setup guide, or <b>Skip Guide</b> to jump straight to creation.", helpStyle);
                break;

            case WizardStep.SetupDatabase:
                GUILayout.Label("<b>STEP 1: Database & Asset Setup</b>\n\n" +
                                "<i>We have automatically created the folder <b>Assets/HexGrid/Data/Domains</b> for you.</i>\n\n" +
                                "This package uses ScriptableObjects to store data. You will interact with two types of assets:\n\n" +
                                "<b>1. Tile Domains (The Categories)</b>\n" +
                                "These represent terrain types (e.g., Grass, Desert). Props use these to know where they can spawn. Try creating one now!\n" +
                                "<i>Action:</i> Right-click inside the new Domains folder -> <b>Create -> HexGrid -> Tile Domain</b>\n\n" +
                                "<b>2. Database (The Master List)</b>\n" +
                                "This holds all your tile prefabs and props.\n\n" +
                                "Choose a name for your master database below so we can create it for you:", helpStyle);

                GUILayout.Space(15);

                // Disable the text field if the database is already created to prevent renaming issues mid-wizard
                GUI.enabled = createdDatabase == null;
                databaseAssetName = EditorGUILayout.TextField("Database Asset Name", databaseAssetName);
                GUI.enabled = true;

                if (string.IsNullOrEmpty(databaseAssetName))
                {
                    EditorGUILayout.HelpBox("Database name cannot be empty!", MessageType.Error);
                }
                break;

            case WizardStep.PopulateDatabase:
                GUILayout.Label("<b>STEP 2: Populating the Database</b>\n\n" +
                                "<i>We've created and selected your Database asset! Check your Inspector window.</i>\n\n" +
                                "Here is how to structure your custom generation rules:\n\n" +
                                "<b>Available Domains:</b>\n" +
                                "Drag the Tile Domain assets you just created into this list.\n\n" +
                                "<b>Tiles Database:</b>\n" +
                                "Add your hex tile prefabs here. For each tile, assign it a Domain (e.g., Grass) and give it a <b>Spawn Chance</b> (0 to 1).\n\n" +
                                "<b>Props Database:</b>\n" +
                                "Add prop prefabs (like trees or rocks). Because of our setup, a single prop can be assigned to <b>multiple</b> Domains by adding them to its list!\n\n" +
                                "<i>Remember: We included some basic prefabs in the package for you to test with!</i>", helpStyle);
                break;

            case WizardStep.SetupSceneObject:
                GUILayout.Label("<b>STEP 3: Scene Generator Setup</b>\n\n" +
                                "The HexGridGenerator script builds your world in Edit Mode.\n\n" +
                                "• If you don't have one in your scene, this wizard can automatically create a GameObject with the component and link your new database to it.", helpStyle);

                GUILayout.Space(15);

                targetGenerator = (HexGridGenerator)EditorGUILayout.ObjectField("Target Generator", targetGenerator, typeof(HexGridGenerator), true);
                createNewGenerator = EditorGUILayout.Toggle("Create New If Missing", createNewGenerator);
                break;
        }
    }

    private void DrawBottomButtons()
    {
        // Pushes the buttons to the very bottom of the window
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();

        // --- Back Button ---
        GUI.enabled = currentStep != WizardStep.Welcome;
        if (GUILayout.Button("Back", GUILayout.Width(80), GUILayout.Height(25)))
        {
            currentStep--;
        }
        GUI.enabled = true; // Reset GUI state

        GUILayout.FlexibleSpace();  // Space between left and right buttons

        // --- Right Side Button (Skip/Continue/Finish) ---
        if (currentStep == WizardStep.SetupSceneObject)
        {
            if (GUILayout.Button("Finish Setup", GUILayout.Width(120), GUILayout.Height(25)))
            {
                FinalizeSetup();
                Close();    // Explicitly close the window when done
            }
        }
        else
        {
            if (GUILayout.Button("Skip Guide", GUILayout.Width(100), GUILayout.Height(25)))
            {
                CreateWorkspaceFolders();
                CreateDatabaseAsset();
                FinalizeSetup();
                Close();    // Explicitly close the window when done
            }

            GUILayout.Space(5);

            // Disable the Continue button if the database name is empty on Step 1
            bool canContinue = currentStep != WizardStep.SetupDatabase || !string.IsNullOrEmpty(databaseAssetName);
            GUI.enabled = canContinue;

            if (GUILayout.Button("Continue", GUILayout.Width(120), GUILayout.Height(25)))
            {
                if (currentStep == WizardStep.Welcome)
                {
                    CreateWorkspaceFolders();
                    PingDomainFolders();
                }
                else if (currentStep == WizardStep.SetupDatabase)
                {
                    CreateDatabaseAsset();
                    if (createdDatabase != null)
                    {
                        // Select the newly created database so the inspector will be visible
                        Selection.activeObject = createdDatabase;
                        EditorGUIUtility.PingObject(createdDatabase);
                    }
                }

                currentStep++;
            }

            GUI.enabled = true;
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    private void CreateWorkspaceFolders()
    {
        string dataFolder = $"{baseFolder}/Data";
        string domainsFolder = $"{dataFolder}/Domains";

        if (!AssetDatabase.IsValidFolder(baseFolder))
            AssetDatabase.CreateFolder("Assets", "HexGrid");

        if (!AssetDatabase.IsValidFolder(dataFolder))
            AssetDatabase.CreateFolder(baseFolder, "Data");

        if (!AssetDatabase.IsValidFolder(domainsFolder))
            AssetDatabase.CreateFolder(dataFolder, "Domains");

        AssetDatabase.Refresh();
    }

    private void CreateDatabaseAsset()
    {
        // Don't create if the user clicked "Back" and then "Continue" again
        if (createdDatabase != null) return;

        string dataFolder = $"{baseFolder}/Data";

        createdDatabase = ScriptableObject.CreateInstance<HexGridDatabase>();
        string assetPath = $"{dataFolder}/{databaseAssetName}.asset";

        AssetDatabase.CreateAsset(createdDatabase, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void PingDomainFolders()
    {
        // Highlight the new Data folder in the Project window to guide the user
        Object folderObj = AssetDatabase.LoadAssetAtPath<Object>($"{baseFolder}/Data/Domains");
        if (folderObj != null)
        {
            // Focuses the Project window, select the folder, and slightly flashes/pings it
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = folderObj;
            EditorGUIUtility.PingObject(folderObj);
        }
    }

    private void FinalizeSetup()
    {
        // 1. Handle Generator checking / creation
        if (targetGenerator == null && createNewGenerator)
        {
            GameObject newGameObj = new("HexGridGenerator");
            targetGenerator = newGameObj.AddComponent<HexGridGenerator>();

            // Undo support for creation
            Undo.RegisterCreatedObjectUndo(newGameObj, "Create Hex Grid Generator");

            // Create the GridContainer
            GameObject newGridContainerObj = new("GridContainer");

            // Undo support for creation
            Undo.RegisterCreatedObjectUndo(newGridContainerObj, "Create Grid Container");
            newGridContainer = newGridContainerObj;

            Debug.Log("Create a new HexGridGenerator GameObject in the scene.");
        }

        // 2. Automatically link database to generator
        if (targetGenerator != null)
        {
            // Use SerializedObject to safely assign reference in Editor scripts
            SerializedObject serializeGenerator = new(targetGenerator);
            SerializedProperty dbProp = serializeGenerator.FindProperty("gridDatabase");

            dbProp.objectReferenceValue = createdDatabase;

            if (newGridContainer != null)
            {
                SerializedProperty gridContainerProp = serializeGenerator.FindProperty("gridContainer");
                gridContainerProp.objectReferenceValue = newGridContainer.transform;
            }

            serializeGenerator.ApplyModifiedProperties();

            Selection.activeGameObject = targetGenerator.gameObject;
        }

        Debug.Log($"<color=green>Hex Grid Setup Complete!</Color> You are ready to generate.");
    }
}
