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
    // Base Stat Mods
    public int enemyHPMod;
    public int enemyATKMod;
    public int enemyDEFMod;
    public int eliteHPMod;
    public int eliteATKMod;
    public int eliteDEFMod;
    public int bossHPMod;
    public int bossATKMod;
    public int bossDEFMod;
    public void SetStatMods(List<int> newMods)
    {
        int i = 0;
        // Enemy
        enemyHPMod = newMods[i++];
        enemyATKMod = newMods[i++];
        enemyDEFMod = newMods[i++];
        // Elite
        eliteHPMod = newMods[i++];
        eliteATKMod = newMods[i++];
        eliteDEFMod = newMods[i++];
        // Boss
        bossHPMod = newMods[i++];
        bossATKMod = newMods[i++];
        bossDEFMod = newMods[i++];
    }
    public List<int> ReturnStatMods()
    {
        List<int> statMods = new List<int>();
        // Enemy
        statMods.Add(enemyHPMod);
        statMods.Add(enemyATKMod);
        statMods.Add(enemyDEFMod);
        // Elite
        statMods.Add(eliteHPMod);
        statMods.Add(eliteATKMod);
        statMods.Add(eliteDEFMod);
        // Boss
        statMods.Add(bossHPMod);
        statMods.Add(bossATKMod);
        statMods.Add(bossDEFMod);
        return statMods;
    }
    // Bools
    public List<string> flags = new List<string>();
    public List<string> flagCharges = new List<string>();
    public List<string> sourceTypes = new List<string>();
    public List<string> sourceIds = new List<string>();
    public bool HasFlag(string flagName)
    {
        if (flagName == ""){return false;}
        InitializeLists();
        return flags.Contains(flagName);
    }
    // Should Have A List Somewhere Of Flags That Have Charges.
    public void ConsumeFlagCharge(string flagName)
    {
        if (!HasFlag(flagName)){return;}
        int indexOf = flags.IndexOf(flagName);
        int charges = utility.SafeParseInt(flagCharges[indexOf]);
        charges--;
        if (charges == 0)
        {
            DisableFlag(flagName);
        }
        else if (charges < 0)
        {
            return;
        }
        // Only update flags with positive charges.
        else
        {
            flagCharges[indexOf] = charges.ToString();
        }
    }
    public List<string> GetFlags()
    {
        InitializeLists();
        return new List<string>(flags);
    }
    public void EnableFlag(string flagName, string charges = "-1", string sourceType = "", string sourceId = "")
    {
        if (flagName == ""){return;}
        InitializeLists();
        int index = flags.IndexOf(flagName);
        if (index >= 0)
        {
            flagCharges[index] = charges;
            sourceTypes[index] = sourceType;
            sourceIds[index] = sourceId;
            return;
        }
        flags.Add(flagName);
        flagCharges.Add(charges);
        sourceTypes.Add(sourceType);
        sourceIds.Add(sourceId);
    }
    public void DisableFlag(string flagName)
    {
        InitializeLists();
        int index = flags.IndexOf(flagName);
        if (index < 0){return;}
        flags.RemoveAt(index);
        flagCharges.RemoveAt(index);
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
                flagCharges.RemoveAt(i);
                sourceTypes.RemoveAt(i);
                sourceIds.RemoveAt(i);
            }
        }
    }
    public void ClearFlags()
    {
        InitializeLists();
        flags.Clear();
        flagCharges.Clear();
        sourceTypes.Clear();
        sourceIds.Clear();
    }
    protected void InitializeDefaults()
    {
        enemyHPMod = 0;
        enemyATKMod = 0;
        enemyDEFMod = 0;
        eliteHPMod = 0;
        eliteATKMod = 0;
        eliteDEFMod = 0;
        bossHPMod = 0;
        bossATKMod = 0;
        bossDEFMod = 0;
        ClearFlags();
    }
    public override void NewGame()
    {
        InitializeDefaults();
        Save();
    }
    public override void Save()
    {
        InitializeLists();
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "EnemyHPMod=" + enemyHPMod + delimiter;
        allData += "EnemyATKMod=" + enemyATKMod + delimiter;
        allData += "EnemyDEFMod=" + enemyDEFMod + delimiter;
        allData += "EliteHPMod=" + eliteHPMod + delimiter;
        allData += "EliteATKMod=" + eliteATKMod + delimiter;
        allData += "EliteDEFMod=" + eliteDEFMod + delimiter;
        allData += "BossHPMod=" + bossHPMod + delimiter;
        allData += "BossATKMod=" + bossATKMod + delimiter;
        allData += "BossDEFMod=" + bossDEFMod + delimiter;
        allData += "Flags=" + utility.ConvertListToString(flags, delimiter2) + delimiter;
        allData += "FlagCharges=" + utility.ConvertListToString(flagCharges, delimiter2) + delimiter;
        allData += "SourceTypes=" + utility.ConvertListToString(sourceTypes, delimiter2) + delimiter;
        allData += "SourceIds=" + utility.ConvertListToString(sourceIds, delimiter2) + delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        InitializeDefaults();
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
    }
    protected void LoadStat(string stat)
    {
        string[] statData = stat.Split(new char[] { '=' }, 2);
        if (statData.Length < 2){return;}
        string key = statData[0];
        string value = statData[1];
        switch (key)
        {
            case "EnemyHPMod":
            enemyHPMod = utility.SafeParseInt(value);
            break;
            case "EnemyATKMod":
            enemyATKMod = utility.SafeParseInt(value);
            break;
            case "EnemyDEFMod":
            enemyDEFMod = utility.SafeParseInt(value);
            break;
            case "EliteHPMod":
            eliteHPMod = utility.SafeParseInt(value);
            break;
            case "EliteATKMod":
            eliteATKMod = utility.SafeParseInt(value);
            break;
            case "EliteDEFMod":
            eliteDEFMod = utility.SafeParseInt(value);
            break;
            case "BossHPMod":
            bossHPMod = utility.SafeParseInt(value);
            break;
            case "BossATKMod":
            bossATKMod = utility.SafeParseInt(value);
            break;
            case "BossDEFMod":
            bossDEFMod = utility.SafeParseInt(value);
            break;
            case "Flags":
            flags = utility.ConvertStringToList(value, delimiter2);
            break;
            case "FlagCharges":
            flagCharges = utility.ConvertStringToList(value, delimiter2);
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
        if (flagCharges == null){flagCharges = new List<string>();}
        if (sourceTypes == null){sourceTypes = new List<string>();}
        if (sourceIds == null){sourceIds = new List<string>();}
    }
}
