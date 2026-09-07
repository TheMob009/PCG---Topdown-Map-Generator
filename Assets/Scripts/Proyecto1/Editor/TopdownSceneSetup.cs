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

        // 5. Enlazar Tilemaps en BspMapGenerator
        BspMapGenerator bspGen = Object.FindFirstObjectByType<BspMapGenerator>();
        if (bspGen != null)
        {
            SerializedObject soBsp = new SerializedObject(bspGen);
            soBsp.Update();

            SerializedProperty pFloor = soBsp.FindProperty("floorTilemap");
            SerializedProperty pWall = soBsp.FindProperty("wallTilemap");
            if (pFloor != null) pFloor.objectReferenceValue = floorTilemap;
            if (pWall != null) pWall.objectReferenceValue = wallTilemap;

            soBsp.ApplyModifiedProperties();
            EditorUtility.SetDirty(bspGen);
        }

        // 6. Enlazar Tilemaps en RandomWalkGenerator
        RandomWalkGenerator rwGen = Object.FindFirstObjectByType<RandomWalkGenerator>();
        if (rwGen != null)
        {
            SerializedObject soRw = new SerializedObject(rwGen);
            soRw.Update();

            SerializedProperty pFloor = soRw.FindProperty("floorTilemap");
            SerializedProperty pWall = soRw.FindProperty("wallTilemap");
            if (pFloor != null) pFloor.objectReferenceValue = floorTilemap;
            if (pWall != null) pWall.objectReferenceValue = wallTilemap;

            soRw.ApplyModifiedProperties();
            EditorUtility.SetDirty(rwGen);
        }

        // 7. Enlazar MissionVisualizer en PipelineManager
        PipelineManager pipeline = Object.FindFirstObjectByType<PipelineManager>();
        if (pipeline != null)
        {
            SerializedObject soPipe = new SerializedObject(pipeline);
            soPipe.Update();

            SerializedProperty pVis = soPipe.FindProperty("missionVisualizer");
            if (pVis != null) pVis.objectReferenceValue = missionVisualizer;

            SerializedProperty pGenStart = soPipe.FindProperty("generateOnStart");
            if (pGenStart != null) pGenStart.boolValue = true;

            soPipe.ApplyModifiedProperties();
            EditorUtility.SetDirty(pipeline);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[TopdownSceneSetup] Escena TopDown 2D configurada exitosamente con Grid, FloorTilemap, WallTilemap y MissionVisualizer.");
    }
}
#endif
