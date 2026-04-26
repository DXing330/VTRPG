using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StSRunModifierSources
{
    public const string Relic = "Relic";
    public const string Ascension = "Ascension";
    public const string Event = "Event";
}

public static class StSRunFlags
{
    public const string ChooseStartingPositions = "ChooseStartingPositions";
    public const string ShopRestock = "ShopRestock";
    public const string NoGold = "NoGold";
    public const string NoConsumables = "NoConsumables";
    public const string SacrificeRewards = "SacrificeRewards";
}

[CreateAssetMenu(fileName = "StSRunModifiers", menuName = "ScriptableObjects/StS/StSRunModifiersSaveData", order = 1)]
public class StSRunModifiersSaveData : SavedData
{
    public string delimiter2 = ",";
    public List<string> flags = new List<string>();
    public List<string> sourceTypes = new List<string>();
    public List<string> sourceIds = new List<string>();

    public bool HasFlag(string flagName)
    {
        if (flagName == ""){return false;}
        InitializeLists();
        return flags.Contains(flagName);
    }

    public List<string> GetFlags()
    {
        InitializeLists();
        return new List<string>(flags);
    }

    public void EnableFlag(string flagName, string sourceType = "", string sourceId = "")
    {
        if (flagName == ""){return;}
        InitializeLists();
        int index = flags.IndexOf(flagName);
        if (index >= 0)
        {
            sourceTypes[index] = sourceType;
            sourceIds[index] = sourceId;
            return;
        }
        flags.Add(flagName);
        sourceTypes.Add(sourceType);
        sourceIds.Add(sourceId);
    }

    public void DisableFlag(string flagName)
    {
        InitializeLists();
        int index = flags.IndexOf(flagName);
        if (index < 0){return;}
        flags.RemoveAt(index);
        sourceTypes.RemoveAt(index);
        sourceIds.RemoveAt(index);
    }

    public void RemoveFlagsFromSource(string sourceType, string sourceId)
    {
        InitializeLists();
        for (int i = flags.Count - 1; i >= 0; i--)
        {
            if (sourceTypes[i] == sourceType && sourceIds[i] == sourceId)
            {
                flags.RemoveAt(i);
                sourceTypes.RemoveAt(i);
                sourceIds.RemoveAt(i);
            }
        }
    }

    public void ClearFlags()
    {
        InitializeLists();
        flags.Clear();
        sourceTypes.Clear();
        sourceIds.Clear();
    }

    public override void NewGame()
    {
        ClearFlags();
        Save();
    }

    public override void Save()
    {
        InitializeLists();
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "Flags=" + utility.ConvertListToString(flags, delimiter2) + delimiter;
        allData += "SourceTypes=" + utility.ConvertListToString(sourceTypes, delimiter2) + delimiter;
        allData += "SourceIds=" + utility.ConvertListToString(sourceIds, delimiter2) + delimiter;
        File.WriteAllText(dataPath, allData);
    }

    public override void Load()
    {
        InitializeLists();
        dataPath = Application.persistentDataPath + "/" + filename;
        if (!File.Exists(dataPath))
        {
            ClearFlags();
            return;
        }
        allData = File.ReadAllText(dataPath);
        dataList = allData.Split(delimiter).ToList();
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
        NormalizeListAlignment();
    }

    protected void LoadStat(string stat)
    {
        string[] statData = stat.Split(new char[] { '=' }, 2);
        if (statData.Length < 2){return;}
        string key = statData[0];
        string value = statData[1];
            switch (key)
        {
            case "Flags":
            flags = utility.ConvertStringToList(value, delimiter2);
            break;
            case "SourceTypes":
            sourceTypes = utility.ConvertStringToList(value, delimiter2);
            break;
            case "SourceIds":
            sourceIds = utility.ConvertStringToList(value, delimiter2);
            break;
        }
    }

    protected void InitializeLists()
    {
        if (flags == null){flags = new List<string>();}
        if (sourceTypes == null){sourceTypes = new List<string>();}
        if (sourceIds == null){sourceIds = new List<string>();}
        NormalizeListAlignment();
    }

    protected void NormalizeListAlignment()
    {
        if (flags == null){flags = new List<string>();}
        if (sourceTypes == null){sourceTypes = new List<string>();}
        if (sourceIds == null){sourceIds = new List<string>();}
        for (int i = flags.Count - 1; i >= 0; i--)
        {
            if (flags[i] == "")
            {
                flags.RemoveAt(i);
                if (i < sourceTypes.Count){sourceTypes.RemoveAt(i);}
                if (i < sourceIds.Count){sourceIds.RemoveAt(i);}
            }
        }
        while (sourceTypes.Count < flags.Count){sourceTypes.Add("");}
        while (sourceIds.Count < flags.Count){sourceIds.Add("");}
        if (sourceTypes.Count > flags.Count){sourceTypes = sourceTypes.GetRange(0, flags.Count);}
        if (sourceIds.Count > flags.Count){sourceIds = sourceIds.GetRange(0, flags.Count);}
    }
}
