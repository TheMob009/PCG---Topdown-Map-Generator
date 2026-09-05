#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Agrega un botón "Generar Perlin Noise" al Inspector, para iterar sobre
/// frequency/seed/interpolación sin entrar a Play Mode.
///
/// Debe estar dentro de una carpeta llamada "Editor" en el proyecto.
/// </summary>
[CustomEditor(typeof(PerlinMapGenerator))]
public class PerlinMapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var perlin = (PerlinMapGenerator)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Herramientas de Editor", EditorStyles.boldLabel);

        if (GUILayout.Button("Generar Perlin Noise", GUILayout.Height(30)))
        {
            Undo.RegisterCompleteObjectUndo(perlin, "Generar Perlin Noise");

            perlin.Generate();

            EditorUtility.SetDirty(perlin);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(perlin.gameObject.scene);
            }

            SceneView.RepaintAll();
        }

        if (perlin.NoiseMap != null)
        {
            EditorGUILayout.HelpBox(
                $"Mapa generado: {perlin.Width} x {perlin.Height}",
                MessageType.Info);
        }
    }
}
#endif