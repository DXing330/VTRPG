using UnityEditor;

[CustomEditor(typeof(AutoChessPVPGenomeTemplate))]
public class AutoChessPVPGenomeTemplateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Genome Template", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("templateName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("preferredFaction"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lockFaction"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("geneTemplate"),
            true);

        serializedObject.ApplyModifiedProperties();
    }
}