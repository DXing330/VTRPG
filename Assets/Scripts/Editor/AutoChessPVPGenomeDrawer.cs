using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AutoChessPVPGenome))]
public class AutoChessPVPGenomeDrawer : PropertyDrawer
{
    const float LineHeight = 18f;
    const float Spacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty genes = property.FindPropertyRelative("genes");

        // Header + Gene Pool + Preferred Faction + Foldout + genes
        if (!property.isExpanded)
            return LineHeight;

        return (5 + genes.arraySize) * (LineHeight + Spacing);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty genePool = property.FindPropertyRelative("genePool");
        SerializedProperty preferredFaction = property.FindPropertyRelative("preferredFaction");
        SerializedProperty genes = property.FindPropertyRelative("genes");

        Rect r = new Rect(position.x, position.y, position.width, LineHeight);

        property.isExpanded = EditorGUI.Foldout(r, property.isExpanded, label, true);

        if (!property.isExpanded)
            return;

        EditorGUI.indentLevel++;

        r.y += LineHeight + Spacing;
        EditorGUI.PropertyField(r, genePool);

        r.y += LineHeight + Spacing;
        EditorGUI.PropertyField(r, preferredFaction);

        r.y += LineHeight + Spacing;
        EditorGUI.LabelField(r, "Genes", EditorStyles.boldLabel);

        for (int i = 0; i < genes.arraySize; i++)
        {
            r.y += LineHeight + Spacing;

            string name = i < AutoChessPVPGenome.GeneNames.Count
                ? AutoChessPVPGenome.GeneNames[i]
                : $"Gene {i}";

            SerializedProperty gene = genes.GetArrayElementAtIndex(i);

            EditorGUI.BeginChangeCheck();
            float value = EditorGUI.FloatField(r, name, gene.floatValue);
            if (EditorGUI.EndChangeCheck())
                gene.floatValue = value;
        }

        EditorGUI.indentLevel--;
    }
}