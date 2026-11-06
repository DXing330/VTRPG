using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public struct MultiKey : System.IEquatable<MultiKey>
{
    public string key1;
    public string key2;
    public string key3;
    public MultiKey(string key1, string key2, string key3)
    {
        this.key1 = key1;
        this.key2 = key2;
        this.key3 = key3;
    }
    public bool Equals(MultiKey other)
    {
        return key1 == other.key1 && key2 == other.key2 && key3 == other.key3;
    }
    public override bool Equals(object obj)
    {
        return obj is MultiKey other && Equals(other);
    }
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (key1 == null ? 0 : key1.GetHashCode());
            hash = hash * 31 + (key2 == null ? 0 : key2.GetHashCode());
            hash = hash * 31 + (key3 == null ? 0 : key3.GetHashCode());
            return hash;
        }
    }
}
[CreateAssetMenu(fileName = "MultiKeyStatData", menuName = "ScriptableObjects/DataContainers/MultiKeyStatData", order = 1)]
public class MultiKeyStatDatabase : StatDatabase
{
    public int keyCount;
    public string allSecondKeys;
    public string allThirdKeys;
    public List<string> secondKeys;
    public List<string> thirdKeys;
    protected Dictionary<MultiKey, string> multiKeyLookup;
    private void BuildMultiKeyLookup()
    {
        multiKeyLookup = new Dictionary<MultiKey, string>(keys.Count);
        for (int i = 0; i < keys.Count; i++)
        {
            string key2 = keyCount > 1 ? secondKeys[i] : "";
            string key3 = keyCount > 2 ? thirdKeys[i] : "";
            MultiKey key = new MultiKey(keys[i], key2, key3);
            // Preserve the existing behavior:
            // the first matching entry wins.
            if (!multiKeyLookup.ContainsKey(key))
            {
                multiKeyLookup.Add(key, values[i]);
            }
        }
    }
    // Input first key, get second key.
    public int GetHighestLevelFromPassive(string passiveName)
    {
        int level = 0;
        List<string> passiveLevels = new List<string>();
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] == passiveName)
            {
                passiveLevels.Add(secondKeys[i]);
            }
        }
        for (int i = 0; i < passiveLevels.Count; i++)
        {
            if (int.Parse(passiveLevels[i]) > level)
            {
                level = int.Parse(passiveLevels[i]);
            }
        }
        return level;
    }
    public void SetAllSecondKeys(string newKeys)
    {
        allSecondKeys = newKeys;
    }
    public void SetAllThirdKeys(string newKeys)
    {
        allThirdKeys = newKeys;
    }
    public override void GetKeys()
    {
        keys = allKeys.Split(keyDelimiter).ToList();
        if (keyCount > 1)
        {
            secondKeys = allSecondKeys.Split(keyDelimiter).ToList();
            if (keyCount > 2)
            {
                thirdKeys = allThirdKeys.Split(keyDelimiter).ToList();
            }
        }
        BuildMultiKeyLookup();
    }
    public string GetMultiKeyValue(string key1, string key2 = "", string key3 = "")
    {
        if (multiKeyLookup == null)
        {
            BuildMultiKeyLookup();
        }
        MultiKey key = new MultiKey(key1, keyCount > 1 ? key2 : "", keyCount > 2 ? key3 : "");
        return multiKeyLookup.TryGetValue(key, out string value) ? value : "";
    }
}
