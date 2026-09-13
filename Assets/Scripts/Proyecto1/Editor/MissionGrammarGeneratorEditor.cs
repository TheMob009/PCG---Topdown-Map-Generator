#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MissionGrammarGenerator))]
public class MissionGrammarGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var generator = (MissionGrammarGenerator)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Herramientas de Editor", EditorStyles.boldLabel);

        var bspProp = serializedObject.FindProperty("bspGenerator");
        BspMapGenerator bspGen = bspProp.objectReferenceValue as BspMapGenerator;
        bool hasBspResult = bspGen != null && bspGen.Result != null && bspGen.Result.Rooms.Count > 0;

        if (!hasBspResult)
        {
            EditorGUILayout.HelpBox(
                "El BSP asignado todavia no ha generado salas. Genera el BSP primero.",
                MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(!hasBspResult))
        {
            if (GUILayout.Button("Generar Mision", GUILayout.Height(30)))
            {
                Undo.RegisterCompleteObjectUndo(generator, "Generar Mision Gramatica");

                generator.Generate();

                EditorUtility.SetDirty(generator);
                if (!Application.isPlaying)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                        generator.gameObject.scene);
                }

                SceneView.RepaintAll();
            }
        }

        if (generator.Assignments != null && generator.Assignments.Count > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Cadena final: " + generator.FinalChain + "\n" +
                "Salas asignadas: " + generator.Assignments.Count,
                MessageType.Info);
        }
    }
}
#endif
