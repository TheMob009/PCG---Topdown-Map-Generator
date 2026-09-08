#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Agrega botones al Inspector del PipelineManager para generar el mapa
/// completo en el orden correcto (Perlin -> BSP -> Random Walk), o cada
/// etapa por separado, sin entrar a Play Mode.
///
/// Debe estar dentro de una carpeta llamada "Editor" en el proyecto.
/// </summary>
[CustomEditor(typeof(PipelineManager))]
public class PipelineManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var pipeline = (PipelineManager)target;

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Configurar Escena TopDown (Grid y Tilemaps)", GUILayout.Height(28)))
        {
            TopdownSceneSetup.SetupScene();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Pipeline completo", EditorStyles.boldLabel);

        if (GUILayout.Button("Generar Todo (Perlin > BSP > Random Walk > Mision)", GUILayout.Height(35)))
        {
            RunAction(pipeline, "Generar Pipeline Completo", pipeline.GenerateAll);
        }

        EditorGUILayout.Space(3);
        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.75f, 0.90f, 1f);
        if (GUILayout.Button("Generar Todo: Caverna Excavada", GUILayout.Height(35)))
        {
            RunAction(pipeline, "Generar Todo: Caverna Excavada", pipeline.GenerateAllExcavatedCave);
        }
        GUI.backgroundColor = prevBg;

        GUI.backgroundColor = new Color(0.85f, 0.85f, 1f);
        if (GUILayout.Button("Generar Todo: Estacion Espacial", GUILayout.Height(35)))
        {
            RunAction(pipeline, "Generar Todo: Estacion Espacial", pipeline.GenerateAllLunarStation);
        }
        GUI.backgroundColor = prevBg;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Etapas individuales", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("1. Perlin", GUILayout.Height(25)))
            {
                RunAction(pipeline, "Generar Perlin", () =>
                {
                    pipeline.SyncDimensions();
                    pipeline.GeneratePerlinOnly();
                });
            }

            if (GUILayout.Button("2. BSP", GUILayout.Height(25)))
            {
                RunAction(pipeline, "Generar BSP", () =>
                {
                    pipeline.SyncDimensions();
                    pipeline.GenerateBspOnly();
                    pipeline.RenderMap(MapRenderStage.BSPOnly);
                });
            }

            if (GUILayout.Button("3. Random Walk", GUILayout.Height(25)))
            {
                RunAction(pipeline, "Generar Random Walk", () =>
                {
                    pipeline.GenerateRandomWalkOnly();
                    pipeline.RenderMap(MapRenderStage.WithRandomWalk);
                });
            }

            if (GUILayout.Button("4. Mision", GUILayout.Height(25)))
            {
                RunAction(pipeline, "Generar Mision", pipeline.GenerateMissionGrammarOnly);
            }

            if (GUILayout.Button("5. Visualizar", GUILayout.Height(25)))
            {
                RunAction(pipeline, "Visualizar", () =>
                {
                    pipeline.RenderMap(MapRenderStage.Full);
                    pipeline.RenderMissionVisualizer();
                });
            }
        }

        EditorGUILayout.Space(5);
        if (GUILayout.Button("Limpiar Todo (Tilemaps y Marcadores)", GUILayout.Height(25)))
        {
            RunAction(pipeline, "Limpiar Todo", pipeline.ClearAll);
        }

        EditorGUILayout.HelpBox(
            "'Generar Todo' respeta el orden del plan: primero calcula el ruido " +
            "ambiental, luego la estructura de salas y pasillos, y finalmente agrega " +
            "las galerias secundarias sobre esa estructura, y por ultimo " +
            "genera la mision sobre las salas del BSP.\n\n" +
            "'Generar Todo: Caverna Excavada' sobreescribe los parámetros con la calibración " +
            "de cámaras amplias, túneles sinuosos y vetas continuas, usando una semilla nueva en cada pulsación.\n\n" +
            "Los botones individuales sirven para iterar sobre una sola etapa sin " +
            "rehacer las anteriores (por ejemplo, probar otra semilla de Random Walk " +
            "manteniendo el mismo BSP).",
            MessageType.Info);
    }

    /// <summary>
    /// Envuelve cualquier acción del pipeline con Undo y marcado de escena
    /// como sucia, para que los cambios generados en editor no se pierdan
    /// silenciosamente y puedan deshacerse con Ctrl+Z.
    /// </summary>
    private void RunAction(PipelineManager pipeline, string undoLabel, System.Action action)
    {
        var targets = new System.Collections.Generic.List<UnityEngine.Object> { pipeline };
        if (pipeline.PerlinGenerator != null) targets.Add(pipeline.PerlinGenerator);
        if (pipeline.BspGenerator != null) targets.Add(pipeline.BspGenerator);
        if (pipeline.RWGenerator != null) targets.Add(pipeline.RWGenerator);
        if (pipeline.MGGenerator != null) targets.Add(pipeline.MGGenerator);

        Undo.RegisterCompleteObjectUndo(targets.ToArray(), undoLabel);

        action.Invoke();

        foreach (var t in targets)
        {
            if (t != null) EditorUtility.SetDirty(t);
        }

        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pipeline.gameObject.scene);
        }

        SceneView.RepaintAll();
    }
}
#endif
