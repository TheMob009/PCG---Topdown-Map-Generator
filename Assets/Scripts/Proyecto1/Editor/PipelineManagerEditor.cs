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
            "Los botones individuales sirven para iterar sobre una sola etapa sin " +
            "rehacer las anteriores (por ejemplo, probar otra semilla de Random Walk " +
            "manteniendo el mismo BSP).",
            MessageType.Info);
    }

    /// <summary>
    /// Envuelve cualquier acci�n del pipeline con Undo y marcado de escena
    /// como sucia, para que los cambios generados en editor no se pierdan
    /// silenciosamente y puedan deshacerse con Ctrl+Z.
    /// </summary>
    private void RunAction(PipelineManager pipeline, string undoLabel, System.Action action)
    {
        Undo.RegisterCompleteObjectUndo(pipeline, undoLabel);

        action.Invoke();

        EditorUtility.SetDirty(pipeline);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pipeline.gameObject.scene);
        }

        SceneView.RepaintAll();
    }
}
#endif
