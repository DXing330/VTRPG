using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CreateAssetMenu(fileName = "TerrainPassivesList", menuName = "ScriptableObjects/BattleLogic/TerrainPassivesList", order = 1)]
public class TerrainPassivesList : StatDatabase
{
    public string delimiterTwo;
    public string passiveDelimiter = "+";
    public override void Initialize()
    {
        if (inputKeysAndValues)
        {
            string[] keysAndValues = allKeysAndValues.Split(keyValueDelimiter);
            SetAllKeys(keysAndValues[0]);
            SetValues(keysAndValues[1]);
            GetKeys();
            GetValues();
            #if UNITY_EDITOR
                EditorUtility.SetDirty(this);
            #endif
        }
    }
    public List<string> ReturnPassivesByTypeTiming(string type, string timing)
    {
        string key = type + timing;
        return GetStrictFilteredValues(key);
    }
    public List<string> ReturnAttackingPassive(string key)
    {
        return ReturnPassivesByTypeTiming(key, "Attack");
    }
    public List<string> ReturnDefendingPassive(string key)
    {
        return ReturnPassivesByTypeTiming(key, "Defend");
    }
    public List<string> ReturnMovingPassive(string key)
    {
        return ReturnPassivesByTypeTiming(key, "Moving");
    }
    public List<string> ReturnStartPassive(string key)
    {
        return ReturnPassivesByTypeTiming(key, "Start");
    }
    public List<string> ReturnEndPassive(string key)
    {
        return ReturnPassivesByTypeTiming(key, "End");
    }
}
