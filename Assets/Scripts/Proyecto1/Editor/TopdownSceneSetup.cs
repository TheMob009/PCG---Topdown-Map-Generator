#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Utilidad de Editor para configurar automaticamente la escena de Proyecto1:
/// - Crea el GameObject Grid con FloorTilemap y WallTilemap si no existen.
/// - Configura el componente MissionVisualizer y lo conecta a MissionGrammarGenerator.
/// - Enlaza todas las referencias en BSPMapGenerator, RandomWalkGenerator y PipelineManager.
/// </summary>
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

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[TopdownSceneSetup] Escena TopDown 2D configurada exitosamente con Grid, FloorTilemap, WallTilemap y MissionVisualizer.");
    }
}
#endif
