using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EquipSlots
{
    None,
    Weapon,
    Armor,
    Charm,
    Helmet,
    Boots,
    Gloves
}
public enum EquipmentRune
{
    None,
    FoolRune,
    BrahmaRune,
    ChariotRune,
    DevilRune,
    EmperorRune,
    FireRune,
    GoldRune,
    HangedManRune,
    IntelligenceRune,
    JudgementRune,
    KuberaRune,
    LoveRune,
    MoonRune,
    NullRune,
    ObliterationRune,
    PriestessRune,
    QueenRune,
    RetreatRune,
    ShivaRune,
    TemperanceRune,
    UnicornRune,
    VishnuRune,
    WheelofFortuneRune,
    KaliRune,
    YamaRune,
    WorldRune
}
[System.Serializable]
public class Equipment
{
    public Equipment()
    {
        ResetStats();
    }
    public string delimiter = "|";
    public string allStats;
    public string GetStats(){ return allStats; }
    public void RefreshStats()
    {
        allStats = "";
        allStats += equipName + delimiter;
        allStats += slot + delimiter;
        allStats += type + delimiter;
        allStats += String.Join(",", passives) + delimiter;
        allStats += String.Join(",", passiveLevels) + delimiter;
        allStats += maxUpgrades + delimiter;
        allStats += rarity + delimiter;
        allStats += runeSlots + delimiter;
        allStats += String.Join(",", GetRunes()) + delimiter;
        allStats += equipSet + delimiter;
    }
    // Used to preserved ordering of slots in prefabs.
    public void ResetStatsExceptSlot()
    {
        equipName = "None";
        type = "";
        passives = new List<string>();
        passiveLevels = new List<string>();
        maxUpgrades = 0;
        rarity = "";
        runeSlots = 0;
        runes = new List<EquipmentRune>();
        equipSet = "";
    }
    public void ResetStats()
    {
        equipName = "None";
        slot = EquipSlots.None;
        type = "";
        passives = new List<string>();
        passiveLevels = new List<string>();
        maxUpgrades = 0;
        rarity = "";
        runeSlots = 0;
        runes = new List<EquipmentRune>();
        equipSet = "";
    }
    public void SetAllStats(string newStats)
    {
        ResetStats();
        allStats = newStats;
        string[] dataBlocks = allStats.Split("|");
        if (dataBlocks.Length < 9)
        {
            return;
        }
        for (int i = 0; i < dataBlocks.Length; i++)
        {
            SetStat(dataBlocks[i], i);
        }
    }
    protected void SetStat(string stat, int index)
    {
        switch (index)
        {
            case 0:
            SetName(stat);
            break;
            case 1:
            SetSlot(stat);
            break;
            case 2:
            SetType(stat);
            break;
            case 3:
            SetPassives(stat.Split(",").ToList());
            break;
            case 4:
            SetPassiveLevels(stat.Split(",").ToList());
            break;
            case 5:
            SetMaxUpgrades(SafeParseInt(stat));
            break;
            case 6:
            SetRarity(stat);
            break;
            case 7:
            SetRuneSlots(SafeParseInt(stat));
            break;
            case 8:
            SetRunes(stat.Split(",").ToList());
            break;
            case 9:
            SetEquipSet(stat);
            break;
        }
    }
    public string equipName;
    public void SetName(string newInfo)
    {
        equipName = newInfo;
    }
    public string GetName(){return equipName;}
    public EquipSlots slot;
    public void SetSlot(string newInfo)
    {
        if (newInfo == "-1" || newInfo.Length <= 0)
        {
            slot = EquipSlots.None;
            return;
        }
        slot = Enum.Parse<EquipSlots>(newInfo);
    }
    public string GetSlot(){return slot.ToString();}
    public string type;
    public void SetType(string newInfo)
    {
        type = newInfo;
    }
    public string GetEquipType(){return type;}
    public List<string> GetPassivesAndLevels()
    {
        List<string> passivesAndLevels = new List<string>();
        for (int i = 0; i < passives.Count; i++)
        {
            passivesAndLevels.Add(passives[i] + ":" + passiveLevels[i]);
        }
        return passivesAndLevels;
    }
    public List<string> passives;
    public void SetPassives(List<string> newInfo)
    {
        passives = newInfo;
    }
    public List<string> GetPassives()
    {
        return passives;
    }
    public void AddPassive(string passiveName)
    {
        if (passiveName.Length <= 1){return;}
        int indexOf = passives.IndexOf(passiveName);
        int cLevel = GetLevelOfPassive(passiveName);
        if (cLevel > 0)
        {
            passiveLevels[indexOf] = (cLevel + 1).ToString();
        }
        else
        {
            passives.Add(passiveName);
            passiveLevels.Add("1");
        }
    }
    public int GetLevelOfPassive(string passiveName)
    {
        int indexOf = passives.IndexOf(passiveName);
        if (indexOf >= 0)
        {
            return int.Parse(passiveLevels[indexOf]);
        }
        return 0;
    }
    public int GetTotalLevel()
    {
        int level = 0;
        for(int i = 0; i < passiveLevels.Count; i++)
        {
            level += int.Parse(passiveLevels[i]);
        }
        return level;
    }
    public void DebugPassives()
    {
        for (int i = 0; i < passives.Count; i++)
        {
            Debug.Log(passives[i] + ":" + passiveLevels[i]);
        }
    }
    public List<string> passiveLevels;
    public void SetPassiveLevels(List<string> newInfo)
    {
        passiveLevels = newInfo;
    }
    public List<string> GetPassiveLevels()
    {
        return passiveLevels;
    }
    public int GetCurrentLevel()
    {
        int level = 0;
        for (int i = 0; i < passiveLevels.Count; i++)
        {
            level += int.Parse(passiveLevels[i]);
        }
        return level;
    }
    public int maxUpgrades;
    public bool UpgradesAvailable()
    {
        return maxUpgrades > 0;
    }
    public void ConsumeUpgrade()
    {
        maxUpgrades--;
    }
    public void SetMaxUpgrades(int newInfo)
    {
        maxUpgrades = newInfo;
    }
    public int GetMaxUpgrades()
    {
        return maxUpgrades;
    }
    public void UpgradeEquipment(string passiveUpgrade, int level = 1)
    {
        for (int i = 0; i < level; i++)
        {
            if (!UpgradesAvailable()){return;}
            AddPassive(passiveUpgrade);
            ConsumeUpgrade();
            RefreshStats();
        }   
    }
    public int runeSlots;
    public int GetRuneSlots(){return runeSlots;}
    public void SetRuneSlots(int newInfo){runeSlots = newInfo;}
    public bool RuneSlotsAvailable()
    {
        return runeSlots > 0;
    }
    public void ConsumeRuneSlot()
    {
        runeSlots--;
    }
    public List<EquipmentRune> runes;
    public void ResetRunes(){runes.Clear();}
    // For testing.
    public void DebugAddRune(string newInfo)
    {
        if (newInfo.Length <= 1){return;}
        runes.Add(Enum.Parse<EquipmentRune>(newInfo));
    }
    public void AddRune(string newInfo)
    {
        if (newInfo.Length <= 1){return;}
        runes.Add(Enum.Parse<EquipmentRune>(newInfo));
        ConsumeRuneSlot();
    }
    public void SetRunes(List<string> newInfo)
    {
        runes.Clear();
        for (int i = 0; i < newInfo.Count; i++)
        {
            if (newInfo[i].Length <= 0){continue;}
            runes.Add(Enum.Parse<EquipmentRune>(newInfo[i]));
        }
    }
    public List<string> GetRunes()
    {
        List<string> runeNames = new List<string>();
        for (int i = 0; i < runes.Count; i++)
        {
            string runeName = runes[i].ToString();
            // Better have at least "Rune" in the name + something else.
            if (runeName.Length <= 4){continue;}
            runeNames.Add(runeName);
        }
        return runeNames;
    }
    public string rarity;
    public void SetRarity(string newInfo)
    {
        rarity = newInfo;
    }
    public int GetRarity()
    {
        return int.Parse(rarity);
    }
    public string equipSet;
    public void SetEquipSet(string newInfo)
    {
        equipSet = newInfo;
    }
    public string GetEquipSet()
    {
        return equipSet;
    }
    public void EquipToActor(TacticActor actor)
    {
        if (allStats.Length < 6) { return; }
        for (int i = 0; i < passives.Count; i++)
        {
            actor.AddPassiveSkill(passives[i], passiveLevels[i]);
        }
    }
    public int SafeParseInt(string intString, int defaultValue = 0)
    {
        try
        {
            return int.Parse(intString);
        }
        catch
        {
            return defaultValue;
        }
    }
}
