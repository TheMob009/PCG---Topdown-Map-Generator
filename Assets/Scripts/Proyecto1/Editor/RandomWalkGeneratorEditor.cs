#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Agrega un botón "Generar Random Walk" al Inspector, para iterar sobre los
/// parámetros (cantidad de agentes, pasos, semilla) sin entrar a Play Mode.
///
/// Debe estar dentro de una carpeta llamada "Editor" en el proyecto.
/// </summary>
[CustomEditor(typeof(RandomWalkGenerator))]
public class RandomWalkGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var walker = (RandomWalkGenerator)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Herramientas de Editor", EditorStyles.boldLabel);

        if (walker.Result == null)
        {
            EditorGUILayout.HelpBox(
                "El BSP asignado todavía no ha generado un mapa. Genera el BSP primero.",
                MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(walker.Result == null))
        {
            if (GUILayout.Button("Generar Random Walk", GUILayout.Height(30)))
            {
                Undo.RegisterCompleteObjectUndo(walker, "Generar Random Walk");

                walker.Generate();

                EditorUtility.SetDirty(walker);
                if (!Application.isPlaying)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(walker.gameObject.scene);
                }

                SceneView.RepaintAll();
            }
        }
    }
}
#endif