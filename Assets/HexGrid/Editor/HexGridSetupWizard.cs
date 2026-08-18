namespace HexGrid
{
    using UnityEditor;
    using UnityEngine;

    public class HexGridSetupWizard : EditorWindow
    {
        // --- Layout Constants ---
        private const float WindowMinWidth = 520f;
        private const float WindowMinHeight = 480f;
        private const int CustomFontSize = 12;
        private const int BoxPadding = 10;
        private const float ContentSpacing = 10f;
        private const float SectionSpacing = 15f;
        private const float ButtonSmallWidth = 80f;
        private const float ButtonMediumWidth = 100f;
        private const float ButtonLargeWidth = 120f;
        private const float StandardButtonHeight = 25f;

        // --- Path & String Constants ---
        private const string BaseFolderPath = "Assets/HexGrid";
        private const string DataFolderPath = BaseFolderPath + "/Data";
        private const string DomainsFolderPath = DataFolderPath + "/Domains";
        private const string PrefabsFolderPath = BaseFolderPath + "/Prefabs";
        private const string SamplePrefabsFolderPath = BaseFolderPath + "/_SampleAssets/Prefabs";
        private const string SampleDatabasePath = BaseFolderPath + "/_SampleAssets/Data/ExampleHexGridDatabase.asset";

        private const string DefaultGeneratorName = "HexGridGenerator";
        private const string DefaultContainerName = "GridContainer";
        private const string HexTilePrefabName = "HexTile.prefab";
        private const string ExamplePlayerPrefabName = "ExamplePlayer.prefab";
        private const string ExamplePlayerGameObjectName = "ExamplePlayer";

        private const string DatabasePropertyPath = "gridDatabase";
        private const string ContainerPropertyPath = "gridContainer";
        private const string UndoGeneratorCreation = "Create Hex Grid Generator";
        private const string UndoContainerCreation = "Create Grid Container";
        private const string UndoPlayerInstantiate = "Instantiate Example Player";

        // --- Gameplay Constants ---
        private const float PlayerSpawnHeight = 0.6f;

        // --- Setup Control ---
        private enum WizardStep
        {
            Welcome,
            Configuration,
            PopulateDatabase,
            HexTilePrefabSetup,
            FeatureSelection,
            GenerateGrid,
            UnitSetupGuide
        }

        private WizardStep currentStep = WizardStep.Welcome;

        // --- Configuration Fields ---
        [Header("Database Settings")]
        [Tooltip("Name of the database asset that will be created.")]
        public string databaseAssetName = "HexGridDatabase";

        [Tooltip("If true, uses the supplied ExampleHexGridDatabase asset.")]
        public bool useExampleDatabase = true;

        [Tooltip("If empty, the wizard can automatically create a HexGridGenerator in your scene.")]
        public HexGridGenerator targetGenerator;
        public bool createNewGenerator = true;

        private HexGridDatabase activeDatabase;
        private GameObject newGridContainer;

        [MenuItem("Tools/Hex Grid/Run Setup Wizard")]
        public static void ShowWindow()
        {
            HexGridSetupWizard window = GetWindow<HexGridSetupWizard>(true, "Hex Grid Setup Guide");
            window.minSize = new Vector2(WindowMinWidth, WindowMinHeight);

            // Reset variables upon explicit window opening to prevent state caching
            window.currentStep = WizardStep.Welcome;
            window.activeDatabase = null;
        }

        private void OnGUI()
        {
            DrawWizardContent();
            DrawBottomButtons();
        }

        private void DrawWizardContent()
        {
            GUILayout.Space(ContentSpacing);

            GUIStyle helpStyle = new(EditorStyles.helpBox)
            {
                fontSize = CustomFontSize,
                padding = new RectOffset(BoxPadding, BoxPadding, BoxPadding, BoxPadding),
                richText = true
            };

            switch (currentStep)
            {
                case WizardStep.Welcome:
                    GUILayout.Label("<b>Welcome to the Hex Grid Generator!</b>\n\n" +
                                    "This wizard will help you set up your database, tiles, and scene generator.\n\n" +
                                    "Click <b>Continue</b> to configure your workspace, or <b>Skip Guide</b> to create a clean, unpopulated environment for manual setup.", helpStyle);
                    break;

                case WizardStep.Configuration:
                    GUILayout.Label("<b>STEP 1: Global Configuration</b>\n\n" +
                                    "Configure your initial project settings before we generate the workspace folders.\n\n" +
                                    "<b>1. Database Settings:</b>\n" +
                                    "The database stores all your tile prefabs and domains.\n\n" +
                                    "<b>2. Scene Generator Setup:</b>\n" +
                                    "The generator builds your world in Edit Mode.", helpStyle);

                    GUILayout.Space(SectionSpacing);

                    EditorGUI.BeginChangeCheck();
                    useExampleDatabase = EditorGUILayout.Toggle("Use Example Database", useExampleDatabase);

                    GUI.enabled = !useExampleDatabase;
                    databaseAssetName = EditorGUILayout.TextField("Database Asset Name", databaseAssetName);
                    GUI.enabled = true;

                    if (!useExampleDatabase && string.IsNullOrEmpty(databaseAssetName))
                    {
                        EditorGUILayout.HelpBox("Database name cannot be empty!", MessageType.Error);
                    }

                    GUILayout.Space(ContentSpacing);

                    targetGenerator = (HexGridGenerator)EditorGUILayout.ObjectField("Target Generator", targetGenerator, typeof(HexGridGenerator), true);
                    createNewGenerator = EditorGUILayout.Toggle("Create New If Missing", createNewGenerator);
                    break;

                case WizardStep.PopulateDatabase:
                    if (useExampleDatabase)
                    {
                        GUILayout.Label("<b>STEP 2: Reviewing the Database</b>\n\n" +
                                        "<i>We have assigned the Example Database to your generator and highlighted it for you.</i>\n\n" +
                                        "Take a moment to explore it in the Inspector as a reference for how data is structured. " +
                                        "It contains pre-configured Domains, Hex Tiles, and Props.", helpStyle);
                    }
                    else
                    {
                        GUILayout.Label("<b>STEP 2: Populating Your Database</b>\n\n" +
                                        "<i>We have highlighted your empty Domains folder in the Project window.</i>\n\n" +
                                        "<b>1. Available Domains:</b> Right-click in the highlighted folder -> Create -> HexGrid -> Tile Domain. Create a few and add them to your Database.\n" +
                                        "<b>2. Tiles Database:</b> Add your hex tile prefabs. Assign each a Domain and a <b>Spawn Chance</b>.\n" +
                                        "<b>3. Props Database:</b> Add props (trees, rocks) and assign them to multiple Domains.", helpStyle);

                        GUILayout.Space(SectionSpacing);

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Ping My Domains Folder", GUILayout.Height(StandardButtonHeight)))
                        {
                            PingFolder(DomainsFolderPath, DataFolderPath);
                        }
                        if (GUILayout.Button("Ping My Database", GUILayout.Height(StandardButtonHeight)))
                        {
                            PingAsset(activeDatabase);
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.Space(ContentSpacing);

                        if (GUILayout.Button("View Example Database (Reference)", GUILayout.Height(StandardButtonHeight)))
                        {
                            PingExampleDatabase();
                        }
                    }
                    break;

                case WizardStep.HexTilePrefabSetup:
                    GUILayout.Label("<b>STEP 3: Hex Tile Prefab Setup</b>\n\n" +
                                    "<i>We have kept your generator selected and pinged your HexTile prefab in the Project window.</i>\n\n" +
                                    "<b>Action Required:</b>\n" +
                                    "• Drag the pinged <b>HexTile prefab</b> from the Project window into the <b>`Base Hex Tile Prefab`</b> field on the `HexGridGenerator` in the Inspector.\n\n" +
                                    "<b>Prefab Structure Reminder:</b>\n" +
                                    "• This prefab acts as the empty container for your geometry (Visuals & Props) that you set up in the database.", helpStyle);
                    break;

                case WizardStep.FeatureSelection:
                    GUILayout.Label("<b>STEP 4: Generator Settings & Features</b>\n\n" +
                                    "<i>Review the properties on your generator component in the Inspector.</i>\n\n" +
                                    "<b>Action Required:</b>\n" +
                                    "• Choose and enable the options you need for your game:\n\n" +
                                    "• <b>Hex Orientation:</b> The example prefabs use point-top hexes. If you prefer flat-top hexes, ensure the <b>Is Flat Topped</b> option is checked.\n" +
                                    "• <b>Add Props:</b> Check this to allow the generator to automatically spawn environmental props on your tiles.\n" +
                                    "• <b>Grid Selection:</b> Enables raycasting and mouse input polling.\n" +
                                    "• <b>Fog of War:</b> Manages Hidden, Explored, and Visible tile states.\n" +
                                    "• <b>Pathfinding:</b> Integrates BFS range calculations and unit movement.", helpStyle);
                    break;

                case WizardStep.GenerateGrid:
                    GUILayout.Label("<b>STEP 5: Generate Grid</b>\n\n" +
                                    "You are fully configured and ready to build your world!\n\n" +
                                    "<i>We have selected your generator in the scene.</i>\n\n" +
                                    "• Navigate to the Inspector window and click the <b>Generate Grid</b> button directly on the `HexGridGenerator` component.\n" +
                                    "• Once generated, click Continue to proceed.", helpStyle);
                    break;

                case WizardStep.UnitSetupGuide:
                    GUILayout.Label("<b>STEP 6: Unit Setup & Example Player</b>\n\n" +
                                    "<i>Pathfinding is enabled on the generator, so we have instantiated and selected the `ExamplePlayer` in your scene.</i>\n\n" +
                                    "• Inspect the player unit to verify its integration with movement event channels and coordinate snapping.", helpStyle);
                    break;
            }
        }

        private void DrawBottomButtons()
        {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            // --- Back Button ---
            GUI.enabled = currentStep != WizardStep.Welcome;
            if (GUILayout.Button("Back", GUILayout.Width(ButtonSmallWidth), GUILayout.Height(StandardButtonHeight)))
            {
                HandleStepBackward();
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            // --- Right Side Navigation ---
            bool isPathfindingEnabled = targetGenerator != null && targetGenerator.EnablePathFinder;
            bool isLastStep = (currentStep == WizardStep.UnitSetupGuide) || (currentStep == WizardStep.GenerateGrid && !isPathfindingEnabled);

            if (isLastStep)
            {
                if (GUILayout.Button("Finish Setup", GUILayout.Width(ButtonLargeWidth), GUILayout.Height(StandardButtonHeight)))
                {
                    Close();
                }
            }
            else
            {
                if (currentStep == WizardStep.Welcome || currentStep == WizardStep.Configuration)
                {
                    if (GUILayout.Button("Skip Guide", GUILayout.Width(ButtonMediumWidth), GUILayout.Height(StandardButtonHeight)))
                    {
                        SkipSetup();
                        return;
                    }
                    GUILayout.Space(ContentSpacing);
                }

                bool canContinue = currentStep != WizardStep.Configuration || useExampleDatabase || !string.IsNullOrEmpty(databaseAssetName);
                GUI.enabled = canContinue;

                if (GUILayout.Button("Continue", GUILayout.Width(ButtonLargeWidth), GUILayout.Height(StandardButtonHeight)))
                {
                    HandleStepForward(isPathfindingEnabled);
                }
                GUI.enabled = true;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(ContentSpacing);
        }

        private void HandleStepForward(bool isPathfindingEnabled)
        {
            switch (currentStep)
            {
                case WizardStep.Welcome:
                    currentStep = WizardStep.Configuration;
                    break;

                case WizardStep.Configuration:
                    ExecuteFullSetup();
                    currentStep = WizardStep.PopulateDatabase;
                    PingAsset(activeDatabase);
                    break;

                case WizardStep.PopulateDatabase:
                    currentStep = WizardStep.HexTilePrefabSetup;
                    PingHexTilePrefab();
                    break;

                case WizardStep.HexTilePrefabSetup:
                    currentStep = WizardStep.FeatureSelection;
                    HighlightGeneratorInInspector();
                    break;

                case WizardStep.FeatureSelection:
                    currentStep = WizardStep.GenerateGrid;
                    break;

                case WizardStep.GenerateGrid:
                    if (isPathfindingEnabled)
                    {
                        InstantiateExamplePlayer();
                        currentStep = WizardStep.UnitSetupGuide;
                    }
                    break;
            }
        }

        private void HandleStepBackward()
        {
            currentStep--;

            switch (currentStep)
            {
                case WizardStep.PopulateDatabase:
                    if (useExampleDatabase)
                    {
                        PingAsset(activeDatabase);
                    }
                    else
                    {
                        PingFolder(DomainsFolderPath, DataFolderPath);
                    }
                    break;

                case WizardStep.HexTilePrefabSetup:
                    PingHexTilePrefab();
                    break;

                case WizardStep.FeatureSelection:
                case WizardStep.GenerateGrid:
                    HighlightGeneratorInInspector();
                    break;
            }
        }

        private void SkipSetup()
        {
            CreateWorkspaceFolders();

            if (targetGenerator == null && createNewGenerator)
            {
                CreateSceneGeneratorOnly();
            }

            Close();
        }

        private void ExecuteFullSetup()
        {
            CreateWorkspaceFolders();
            CreateDatabaseAsset();

            if (targetGenerator == null && createNewGenerator)
            {
                CreateSceneGeneratorOnly();
            }

            LinkDatabaseToGenerator();
        }

        private void CreateWorkspaceFolders()
        {
            if (!AssetDatabase.IsValidFolder(BaseFolderPath))
                AssetDatabase.CreateFolder("Assets", "HexGrid");

            if (!AssetDatabase.IsValidFolder(DataFolderPath))
                AssetDatabase.CreateFolder(BaseFolderPath, "Data");

            if (!AssetDatabase.IsValidFolder(DomainsFolderPath))
                AssetDatabase.CreateFolder(DataFolderPath, "Domains");

            if (!AssetDatabase.IsValidFolder(PrefabsFolderPath))
                AssetDatabase.CreateFolder(BaseFolderPath, "Prefabs");

            AssetDatabase.Refresh();
        }

        private void CreateDatabaseAsset()
        {
            // Re-evaluate database state dynamically based on user choices
            if (useExampleDatabase)
            {
                // Attempt 1: Strict explicit path
                HexGridDatabase existingSampleDb = AssetDatabase.LoadAssetAtPath<HexGridDatabase>(SampleDatabasePath);
                if (existingSampleDb != null)
                {
                    activeDatabase = existingSampleDb;
                    return;
                }

                // Attempt 2: Fallback GUID search in case the user moved the folder
                string[] guids = AssetDatabase.FindAssets("ExampleHexGridDatabase t:HexGridDatabase");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    activeDatabase = AssetDatabase.LoadAssetAtPath<HexGridDatabase>(path);
                    if (activeDatabase != null) return;
                }

                Debug.LogWarning("Example Database not found anywhere in the project. Generating a new blank one instead.");
            }

            // --- Create or Load Custom Database ---
            string assetPath = $"{DataFolderPath}/{databaseAssetName}.asset";
            HexGridDatabase existingDb = AssetDatabase.LoadAssetAtPath<HexGridDatabase>(assetPath);

            if (existingDb != null)
            {
                activeDatabase = existingDb;
            }
            else
            {
                activeDatabase = ScriptableObject.CreateInstance<HexGridDatabase>();
                AssetDatabase.CreateAsset(activeDatabase, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void CreateSceneGeneratorOnly()
        {
            GameObject newGameObj = new(DefaultGeneratorName);
            targetGenerator = newGameObj.AddComponent<HexGridGenerator>();
            Undo.RegisterCreatedObjectUndo(newGameObj, UndoGeneratorCreation);

            GameObject newGridContainerObj = new(DefaultContainerName);
            Undo.RegisterCreatedObjectUndo(newGridContainerObj, UndoContainerCreation);
            newGridContainer = newGridContainerObj;
        }

        private void LinkDatabaseToGenerator()
        {
            if (targetGenerator == null) return;

            SerializedObject serializedGenerator = new(targetGenerator);

            if (activeDatabase != null)
            {
                SerializedProperty dbProp = serializedGenerator.FindProperty(DatabasePropertyPath);
                if (dbProp != null) dbProp.objectReferenceValue = activeDatabase;
            }

            if (newGridContainer != null)
            {
                SerializedProperty gridContainerProp = serializedGenerator.FindProperty(ContainerPropertyPath);
                if (gridContainerProp != null) gridContainerProp.objectReferenceValue = newGridContainer.transform;
            }

            serializedGenerator.ApplyModifiedProperties();
        }

        private void PingAsset(Object asset)
        {
            if (asset != null)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        private void PingExampleDatabase()
        {
            Object exampleDb = AssetDatabase.LoadAssetAtPath<Object>(SampleDatabasePath);
            if (exampleDb != null)
            {
                PingAsset(exampleDb);
                return;
            }

            string[] guids = AssetDatabase.FindAssets("ExampleHexGridDatabase t:HexGridDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Object fallbackDb = AssetDatabase.LoadAssetAtPath<Object>(path);
                PingAsset(fallbackDb);
            }
            else
            {
                Debug.LogWarning("Example Database could not be found.");
            }
        }

        private void PingHexTilePrefab()
        {
            string prefabPath = $"{PrefabsFolderPath}/{HexTilePrefabName}";
            Object prefabObj = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);

            if (prefabObj != null)
            {
                EditorGUIUtility.PingObject(prefabObj);
            }
            else
            {
                PingFolder(PrefabsFolderPath, PrefabsFolderPath);
            }

            if (targetGenerator != null)
            {
                Selection.activeGameObject = targetGenerator.gameObject;
            }
        }

        private void PingFolder(string primaryPath, string fallbackPath)
        {
            string targetPath = AssetDatabase.IsValidFolder(primaryPath) ? primaryPath : fallbackPath;
            Object folderObj = AssetDatabase.LoadAssetAtPath<Object>(targetPath);
            if (folderObj != null)
            {
                EditorGUIUtility.PingObject(folderObj);
            }
        }

        private void HighlightGeneratorInInspector()
        {
            if (targetGenerator != null)
            {
                Selection.activeGameObject = targetGenerator.gameObject;
                EditorGUIUtility.PingObject(targetGenerator.gameObject);
            }
        }

        private void InstantiateExamplePlayer()
        {
            string primaryPrefabPath = $"{SamplePrefabsFolderPath}/{ExamplePlayerPrefabName}";
            string fallbackPrefabPath = $"{PrefabsFolderPath}/{ExamplePlayerPrefabName}";

            string finalPath = AssetDatabase.LoadAssetAtPath<GameObject>(primaryPrefabPath) != null ? primaryPrefabPath : fallbackPrefabPath;
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(finalPath);

            if (playerPrefab != null)
            {
                GameObject existingPlayer = GameObject.Find(ExamplePlayerGameObjectName);
                if (existingPlayer == null)
                {
                    GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                    if (playerInstance != null)
                    {
                        Undo.RegisterCreatedObjectUndo(playerInstance, UndoPlayerInstantiate);
                        playerInstance.transform.position = new Vector3(0f, PlayerSpawnHeight, 0f);
                        Selection.activeGameObject = playerInstance;
                        EditorGUIUtility.PingObject(playerInstance);
                    }
                }
                else
                {
                    Selection.activeGameObject = existingPlayer;
                    EditorGUIUtility.PingObject(existingPlayer);
                }
            }
        }
    }
}