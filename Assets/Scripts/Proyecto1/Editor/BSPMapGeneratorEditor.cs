#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(BspMapGenerator))]
public class BspMapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Dibuja el inspector normal (todos los campos serializados) primero.
        DrawDefaultInspector();

        var generator = (BspMapGenerator)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Herramientas de Editor", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generar Mapa", GUILayout.Height(30)))
            {
                GenerateInEditor(generator);
            }

            if (GUILayout.Button("Limpiar", GUILayout.Height(30)))
            {
                ClearInEditor(generator);
            }
        }

        if (generator.Result != null)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                $"Salas generadas: {generator.Result.Rooms.Count}\n" +
                $"Tamaño de grid: {generator.Result.Width} x {generator.Result.Height}",
                MessageType.Info);
        }
    }

    private void GenerateInEditor(BspMapGenerator generator)
    {
        Undo.RegisterCompleteObjectUndo(generator, "Generar Mapa BSP");
        generator.Generate();
        EditorUtility.SetDirty(generator);
        if (!Application.isPlaying)
        {
            EditorSceneManager_MarkSceneDirty(generator);
        }
        SceneView.RepaintAll();
    }

    private void ClearInEditor(BspMapGenerator generator)
    {
        Undo.RegisterCompleteObjectUndo(generator, "Limpiar Mapa BSP");

        generator.ClearMap();

        EditorUtility.SetDirty(generator);
        if (!Application.isPlaying)
        {
            EditorSceneManager_MarkSceneDirty(generator);
        }

        SceneView.RepaintAll();
    }

    private void EditorSceneManager_MarkSceneDirty(Component component)
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
    }
}
#endif