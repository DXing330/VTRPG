using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CreateAssetMenu(fileName = "StatData2", menuName = "ScriptableObjects/DataContainers/StatData2", order = 1)]
public class StatDatabaseLevelTwo : StatDatabase
{
    // This database only stores values.
    // Keys are stored in a separate database.
    public StatDatabase levelOneData;

    public override void Initialize()
    {
        SetValues(allKeysAndValues);
        GetValues();
        #if UNITY_EDITOR
                EditorUtility.SetDirty(this);
        #endif
    }

    public override string ReturnValue(string key)
    {
        int indexOf = levelOneData.keys.IndexOf(key);
        if (indexOf < 0 || indexOf >= values.Count){return "";}
        return values[indexOf];
    }
}
