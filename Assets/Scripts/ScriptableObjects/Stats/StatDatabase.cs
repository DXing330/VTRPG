using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CreateAssetMenu(fileName = "StatData", menuName = "ScriptableObjects/DataContainers/StatData", order = 1)]
public class StatDatabase : ScriptableObject
{
    public bool inputKeysAndValues = false;
    public bool inputBothv2 = false;
    public string allKeysAndValues;
    public virtual void SetAllData(string newData){allKeysAndValues = newData;}
    public string keyValueDelimiter;
    public string keyDelimiter;
    public string valueDelimiter;
    public string allKeys;
    public string allValues;
    public bool defaultsEnabled;
    public string defaultKey;
    public string defaultValue;
    public bool defaultPassivesEnabled;
    public string ReturnDefaultPassive(string passiveName)
    {
        // Timing, Condition, Specifics, Target, Effect, Specifics, Power
        // Default is adding a skill.
        string effect = "Skill";
        string specifics = passiveName;
        // Standardize the delimiter later if needed.
        if (passiveName.Contains("-"))
        {
            string[] blocks = passiveName.Split("-");
            effect = blocks[0];
            specifics = blocks[1];
        }
        return "BS|||Self|" + effect + "|" + specifics;
    }
    public List<string> keys;
    public List<string> values;

    public void DBSetDirty()
    {
        #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
        #endif
    }

    public virtual void Initialize()
    {
        if (inputKeysAndValues)
        {
            string[] keysAndValues = allKeysAndValues.Split(keyValueDelimiter);
            SetAllKeys(keysAndValues[0]);
            SetValues(keysAndValues[1]);
        }
        GetKeys();
        GetValues();
    }

    public void SetAllKeys(string newKeys)
    {
        allKeys = newKeys;
    }

    public void SetValues(string newValues)
    {
        allValues = newValues;
    }

    public virtual void GetKeys()
    {
        keys = allKeys.Split(keyDelimiter).ToList();
    }

    public virtual void GetValues()
    {
        values = allValues.Split(keyDelimiter).ToList();
    }

    public List<string> ReturnStats(string statName)
    {
        int indexOf = keys.IndexOf(statName);
        List<string> stats = new List<string>();
        if (indexOf < 0){return stats;}
        stats = values[indexOf].Split(valueDelimiter).ToList();
        return stats;
    }

    public bool KeyExists(string key)
    {
        return keys.Contains(key);
    }

    public string ReturnRandomKey(List<string> except = null)
    {
        int index = Random.Range(0, keys.Count);
        string rKey = keys[index];
        if (except != null && except.Count < keys.Count && except.Contains(rKey))
        {
            return ReturnRandomKey(except);
        }
        return rKey;
    }

    public string ReturnRandomWeightedKey(List<string> except = null)
    {
        int totalWeight = 0;
        for (int i = 0; i < values.Count; i++)
        {
            totalWeight += int.Parse(values[i]);
        }
        int roll = Random.Range(0, totalWeight);
        for (int i = 0; i < values.Count; i++)
        {
            if (roll < int.Parse(values[i]))
            {
                return keys[i];
            }
            else
            {
                roll -= int.Parse(values[i]);
            }
        }
        return ReturnRandomKey(except);
    }

    public List<string> ReturnRandomKeys(int count = 1)
    {
        List<string> rKeys = new List<string>();
        for (int i = 0; i < count; i++)
        {
            rKeys.Add(ReturnRandomKey(rKeys));
        }
        return rKeys;
    }

    // Used specifically for selecting random enemies for the roguelike.
    public string ReturnRandomKeyBasedOnIntValue(int value)
    {
        List<string> possibleKeys = new List<string>();
        for (int i = 0; i < keys.Count; i++)
        {
            if (int.Parse(values[i]) == value)
            {
                possibleKeys.Add(keys[i]);
            }
        }
        if (possibleKeys.Count == 0 && value > 0)
        {
            return ReturnRandomKeyBasedOnIntValue(value - 1);
        }
        else if (possibleKeys.Count > 0)
        {
            return possibleKeys[Random.Range(0, possibleKeys.Count)];
        }
        if (defaultsEnabled){return defaultKey;}
        return "";
    }

    public string ReturnKeyFromValue(string value)
    {
        int index = values.IndexOf(value);
        if (index < 0){return "";}
        return keys[index];
    }

    public string ReturnKeyAtIndex(int index)
    {
        if (index >= 0 && index < keys.Count)
        {
            return keys[index];
        }
        if (defaultsEnabled){return defaultKey;}
        return "";
    }

    public List<string> GetAllKeys()
    {
        return new List<string>(keys);
    }

    public List<string> GetFilteredKeys(List<string> filters)
    {
        List<string> filtered = new List<string>();
        for (int i = 0; i < keys.Count; i++)
        {
            for (int j = 0; j < filters.Count; j++)
            {
                if (keys[i].Contains(filters[j]))
                {
                    filtered.Add(keys[i]);
                    break;
                }
            }
        }
        return filtered;
    }

    public List<string> GetAllValues()
    {
        return new List<string>(values);
    }

    public List<string> GetFilteredValues(List<string> filters)
    {
        List<string> filtered = new List<string>();
        for (int i = 0; i < keys.Count; i++)
        {
            for (int j = 0; j < filters.Count; j++)
            {
                if (keys[i].Contains(filters[j]))
                {
                    filtered.Add(values[i]);
                    break;
                }
            }
        }
        return filtered;
    }

    public virtual string ReturnValue(string key)
    {
        int index = keys.IndexOf(key);
        if (index >= 0 && index < values.Count)
        {
            return values[index];
        }
        else
        {
            if (defaultPassivesEnabled){return ReturnDefaultPassive(key);}
            if (defaultsEnabled){return defaultValue;}
            return "";
        }
    }

    // No longer a dictionary if one key has multiple values.
    public List<string> ReturnAllValues(string key)
    {
        List<string> allValuesForKey = new List<string>();
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] == key)
            {
                allValuesForKey.Add(values[i]);
            }
        }
        return allValuesForKey;
    }

    public string ReturnValueAtIndex(int index)
    {
        if (index >= 0 && index < values.Count)
        {
            return values[index];
        }
        if (defaultsEnabled){return defaultValue;}
        return "";
    }

    public string ReturnRandomValue()
    {
        int index = Random.Range(0, values.Count);
        return values[index];
    }

    public string ReturnMiddleValue()
    {
        return values[values.Count / 2];
    }
}
