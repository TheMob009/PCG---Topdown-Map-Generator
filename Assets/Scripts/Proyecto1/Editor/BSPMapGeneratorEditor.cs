#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Agrega un botón "Generar Mapa" al Inspector de BspMapGenerator, para poder
/// iterar sobre los parámetros (semilla, tamaño de salas, iteraciones, etc.)
/// sin necesidad de entrar a Play Mode cada vez.
///
/// IMPORTANTE: este archivo debe estar dentro de una carpeta llamada "Editor"
/// en tu proyecto (por ejemplo Assets/Scripts/Editor/), o Unity intentará
/// compilarlo junto con el código de juego y fallará (UnityEditor no está
/// disponible en builds).
/// </summary>
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

        // Info rápida del último resultado, útil mientras ajustas parámetros.
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
        // Registra la acción en el sistema de Undo de Unity, por si quieres
        // deshacer la generación (Ctrl+Z) mientras estás en el editor.
        Undo.RegisterCompleteObjectUndo(generator, "Generar Mapa BSP");

        generator.Generate();

        // Marca la escena y el objeto como modificados para que Unity
        // no descarte los cambios (los Tilemaps se pintan directamente
        // sobre datos de escena, no sobre el asset del script).
        EditorUtility.SetDirty(generator);
        if (!Application.isPlaying)
        {
            EditorSceneManager_MarkSceneDirty(generator);
        }

        // Fuerza un repintado de la vista Scene para ver los Gizmos actualizados
        // de inmediato, sin esperar a que el mouse se mueva sobre esa ventana.
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

    // Envuelto en su propio método para mantener un solo using de UnityEditor.SceneManagement
    // y dejar claro por qué se llama (evitar que Unity descarte los tiles pintados en editor).
    private void EditorSceneManager_MarkSceneDirty(Component component)
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
    }
}
#endif