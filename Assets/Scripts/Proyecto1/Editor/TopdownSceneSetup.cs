#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TopdownSceneSetup
{
    [InitializeOnLoadMethod]
    private static void AutoSetupOnCompile()
    {
        EditorApplication.delayCall += () =>
        {
            if (!Application.isPlaying)
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.isLoaded && scene.name == "SceneProyecto1")
                {
                    SetupScene();
                }
            }
        };
    }

    [MenuItem("Proyecto 1/Configurar Escena TopDown 2D", false, 10)]
    public static void SetupScene()
    {
        Undo.IncrementCurrentGroup();
        string undoGroup = "Configurar Escena TopDown 2D";
        Undo.SetCurrentGroupName(undoGroup);

        // 1. Buscar o crear el GameObject Grid
        Grid grid = Object.FindFirstObjectByType<Grid>();
        if (grid == null)
        {
            GameObject gridGo = new GameObject("Grid", typeof(Grid));
            Undo.RegisterCreatedObjectUndo(gridGo, "Crear Grid");
            grid = gridGo.GetComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);
        }

        // 2. Buscar o crear FloorTilemap
        Tilemap floorTilemap = null;
        Transform floorT = grid.transform.Find("FloorTilemap");
        if (floorT != null)
        {
            floorTilemap = floorT.GetComponent<Tilemap>();
        }
        if (floorTilemap == null)
        {
            GameObject floorGo = new GameObject("FloorTilemap", typeof(Tilemap), typeof(TilemapRenderer));
            Undo.RegisterCreatedObjectUndo(floorGo, "Crear FloorTilemap");
            floorGo.transform.SetParent(grid.transform, false);
            floorTilemap = floorGo.GetComponent<Tilemap>();
            
            var renderer = floorGo.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = 0;
        }

        // 3. Buscar o crear WallTilemap
        Tilemap wallTilemap = null;
        Transform wallT = grid.transform.Find("WallTilemap");
        if (wallT != null)
        {
            wallTilemap = wallT.GetComponent<Tilemap>();
        }
        if (wallTilemap == null)
        {
            GameObject wallGo = new GameObject("WallTilemap", typeof(Tilemap), typeof(TilemapRenderer));
            Undo.RegisterCreatedObjectUndo(wallGo, "Crear WallTilemap");
            wallGo.transform.SetParent(grid.transform, false);
            wallTilemap = wallGo.GetComponent<Tilemap>();

            var renderer = wallGo.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = 1;
        }

        // 4. Buscar o crear MissionVisualizer
        MissionVisualizer missionVisualizer = Object.FindFirstObjectByType<MissionVisualizer>();
        MissionGrammarGenerator grammarGen = Object.FindFirstObjectByType<MissionGrammarGenerator>();

        if (missionVisualizer == null)
        {
            GameObject visualizerGo;
            if (grammarGen != null)
            {
                visualizerGo = grammarGen.gameObject;
                missionVisualizer = Undo.AddComponent<MissionVisualizer>(visualizerGo);
            }
            else
            {
                visualizerGo = new GameObject("MissionVisualizer", typeof(MissionVisualizer));
                Undo.RegisterCreatedObjectUndo(visualizerGo, "Crear MissionVisualizer");
                missionVisualizer = visualizerGo.GetComponent<MissionVisualizer>();
            }
        }

        // Asignar grammarGen a missionVisualizer mediante SerializedObject
        if (missionVisualizer != null && grammarGen != null)
        {
            SerializedObject soVis = new SerializedObject(missionVisualizer);
            SerializedProperty propGen = soVis.FindProperty("missionGenerator");
            if (propGen != null && propGen.objectReferenceValue == null)
            {
                propGen.objectReferenceValue = grammarGen;
                soVis.ApplyModifiedProperties();
            }
        }

        // 5. Buscar o crear MapVisualizer (renderizador centralizado del mapa)
        MapVisualizer mapVisualizer = Object.FindFirstObjectByType<MapVisualizer>();
        BspMapGenerator bspGen = Object.FindFirstObjectByType<BspMapGenerator>();
        RandomWalkGenerator rwGen = Object.FindFirstObjectByType<RandomWalkGenerator>();
        PerlinMapGenerator perlinGen = Object.FindFirstObjectByType<PerlinMapGenerator>();

        if (mapVisualizer == null)
        {
            // Crear en el mismo GameObject que el PipelineManager si existe,
            // o en uno nuevo si no.
            PipelineManager pipeline = Object.FindFirstObjectByType<PipelineManager>();
            GameObject host = pipeline != null ? pipeline.gameObject : new GameObject("MapVisualizer");
            if (pipeline == null) Undo.RegisterCreatedObjectUndo(host, "Crear MapVisualizer");
            mapVisualizer = Undo.AddComponent<MapVisualizer>(host);
        }

        // Enlazar tilemaps y generadores en MapVisualizer
        if (mapVisualizer != null)
        {
            SerializedObject soMap = new SerializedObject(mapVisualizer);
            soMap.Update();

            SerializedProperty pFloor = soMap.FindProperty("floorTilemap");
            SerializedProperty pWall = soMap.FindProperty("wallTilemap");
            if (pFloor != null) pFloor.objectReferenceValue = floorTilemap;
            if (pWall != null) pWall.objectReferenceValue = wallTilemap;

            SerializedProperty pBsp = soMap.FindProperty("bspGenerator");
            SerializedProperty pRw = soMap.FindProperty("randomWalkGenerator");
            SerializedProperty pPerlin = soMap.FindProperty("perlinGenerator");
            if (pBsp != null && bspGen != null) pBsp.objectReferenceValue = bspGen;
            if (pRw != null && rwGen != null) pRw.objectReferenceValue = rwGen;
            if (pPerlin != null && perlinGen != null) pPerlin.objectReferenceValue = perlinGen;

            // Asignar tiles de Caverna y Estacion Espacial si estan disponibles
            AssignContextTiles(soMap.FindProperty("caveTiles"),
                wallPath: "Assets/Sprites/CaveContext/CaveWall.asset",
                emptyPath: null,
                renderEmpty: false,
                floorAPath: "Assets/Sprites/CaveContext/CaveFloor.asset",
                walkAPath: "Assets/Sprites/CaveContext/CaveWalk.asset",
                floorBPath: "Assets/Sprites/CaveContext/CaveAltFloor.asset",
                walkBPath: "Assets/Sprites/CaveContext/CaveAltWalk.asset");

            AssignContextTiles(soMap.FindProperty("spaceTiles"),
                wallPath: "Assets/Sprites/SpaceContext/SpaceWall.asset",
                emptyPath: "Assets/Sprites/SpaceContext/SpaceRocks.asset",
                renderEmpty: true,
                floorAPath: "Assets/Sprites/SpaceContext/SpaceFloor.asset",
                walkAPath: "Assets/Sprites/SpaceContext/SpaceWalk.asset",
                floorBPath: "Assets/Sprites/SpaceContext/SpaceAlt.asset",
                walkBPath: "Assets/Sprites/SpaceContext/SpaceWalk.asset");

            soMap.ApplyModifiedProperties();
            EditorUtility.SetDirty(mapVisualizer);
        }

        // 6. Enlazar MapVisualizer y MissionVisualizer en PipelineManager
        PipelineManager pipelineManager = Object.FindFirstObjectByType<PipelineManager>();
        if (pipelineManager != null)
        {
            SerializedObject soPipe = new SerializedObject(pipelineManager);
            soPipe.Update();

            SerializedProperty pVis = soPipe.FindProperty("missionVisualizer");
            if (pVis != null) pVis.objectReferenceValue = missionVisualizer;

            SerializedProperty pMapVis = soPipe.FindProperty("mapVisualizer");
            if (pMapVis != null) pMapVis.objectReferenceValue = mapVisualizer;

            SerializedProperty pGenStart = soPipe.FindProperty("generateOnStart");
            if (pGenStart != null) pGenStart.boolValue = true;

            soPipe.ApplyModifiedProperties();
            EditorUtility.SetDirty(pipelineManager);
        }

        // 7. Buscar o crear la Main Camera y añadir CameraController
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camGo = new GameObject("Main Camera",
                typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            Undo.RegisterCreatedObjectUndo(camGo, "Crear Main Camera");
            mainCam = camGo.GetComponent<Camera>();
            mainCam.orthographic = true;
            mainCam.orthographicSize = 10f;
            mainCam.transform.position = new Vector3(0f, 0f, -10f);
        }

        CameraController camCtrl = mainCam.GetComponent<CameraController>();
        if (camCtrl == null)
        {
            camCtrl = Undo.AddComponent<CameraController>(mainCam.gameObject);
        }

        // Ajustar zoom inicial al tamaño del mapa si hay PipelineManager
        if (pipelineManager != null && camCtrl != null)
        {
            Vector3 camPos = mainCam.transform.position;
            camPos.x = pipelineManager.MapWidth * 0.5f;
            camPos.y = pipelineManager.MapHeight * 0.5f;
            mainCam.transform.position = camPos;

            // FitToArea no puede llamarse en Edit Mode (necesita Screen), lo dejamos en los defaults.
            // El usuario puede llamarlo manualmente en Play Mode.
        }

        EditorUtility.SetDirty(mainCam);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[TopdownSceneSetup] Escena TopDown 2D configurada exitosamente con Grid, FloorTilemap, WallTilemap, MissionVisualizer y CameraController.");
    }

    private static void AssignContextTiles(
        SerializedProperty contextProp,
        string wallPath,
        string emptyPath,
        bool renderEmpty,
        string floorAPath,
        string walkAPath,
        string floorBPath,
        string walkBPath)
    {
        if (contextProp == null) return;

        AssignTileProperty(contextProp.FindPropertyRelative("wallTile"), wallPath);
        AssignTileProperty(contextProp.FindPropertyRelative("emptyOrRockTile"), emptyPath);

        var pRenderEmpty = contextProp.FindPropertyRelative("renderEmptyAsRock");
        if (pRenderEmpty != null) pRenderEmpty.boolValue = renderEmpty;

        AssignTileProperty(contextProp.FindPropertyRelative("floorTileA"), floorAPath);
        AssignTileProperty(contextProp.FindPropertyRelative("walkFloorTileA"), walkAPath);
        AssignTileProperty(contextProp.FindPropertyRelative("floorTileB"), floorBPath);
        AssignTileProperty(contextProp.FindPropertyRelative("walkFloorTileB"), walkBPath);
    }

    private static void AssignTileProperty(SerializedProperty prop, string path)
    {
        if (prop == null || string.IsNullOrEmpty(path)) return;
        if (prop.objectReferenceValue == null)
        {
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile != null)
            {
                prop.objectReferenceValue = tile;
            }
        }
    }
}
#endif
