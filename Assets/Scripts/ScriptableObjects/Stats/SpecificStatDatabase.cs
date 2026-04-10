using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CreateAssetMenu(fileName = "SpecificStat", menuName = "ScriptableObjects/DataContainers/SpecificStat", order = 1)]
public class SpecificStatDatabase : StatDatabase
{
    public StatDatabase allStats;
    public int specificIndex;

    public override void Initialize()
    {
        GetKeys();
        GetValues();
        #if UNITY_EDITOR
                EditorUtility.SetDirty(this);
        #endif
    }

    public override void GetKeys()
    {
        keys = new List<string>(allStats.allKeys.Split(allStats.keyDelimiter));
    }

    public override void GetValues()
    {
        List<string> tempValues = new List<string>(allStats.values);
        values.Clear();
        string[] specificValues = new string[0];
        for (int i = 0; i < tempValues.Count; i++)
        {
            specificValues = tempValues[i].Split(allStats.valueDelimiter);
            values.Add(specificValues[specificIndex]);
        }
    }
}
