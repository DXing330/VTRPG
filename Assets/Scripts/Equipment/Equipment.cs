using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Equipment : MonoBehaviour
{
    public GeneralUtility utility;
    public StatDatabase weaponReach;
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
        allStats += String.Join(",", runes) + delimiter;
    }
    public void ResetStats()
    {
        equipName = "";
        slot = "-1";
        type = "-1";
        passives.Clear();
        passiveLevels.Clear();
        maxUpgrades = 0;
        rarity = "";
        runeSlots = 0;
        runes.Clear();
    }
    public void SetAllStats(string newStats)
    {
        allStats = newStats;
        string[] dataBlocks = allStats.Split("|");
        if (allStats.Length < 6 || dataBlocks.Length < 7)
        {
            equipName = "None";
            slot = "-1";
            type = "-1";
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
            SetMaxUpgrades(utility.SafeParseInt(stat, 0));
            break;
            case 6:
            SetRarity(stat);
            break;
            case 7:
            SetRuneSlots(utility.SafeParseInt(stat, 0));
            break;
            case 8:
            SetRunes(stat.Split(",").ToList());
            break;
        }
    }
    public string equipName;
    public void SetName(string newInfo)
    {
        equipName = newInfo;
    }
    public string GetName(){return equipName;}
    public string slot;
    public void SetSlot(string newInfo)
    {
        slot = newInfo;
    }
    public string GetSlot(){return slot;}
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
    public List<string> runes;
    public void ResetRunes(){runes.Clear();}
    public void AddRune(string newInfo)
    {
        if (newInfo.Length <= 1){return;}
        runes.Add(newInfo);
        ConsumeRuneSlot();
    }
    public void SetRunes(List<string> newInfo)
    {
        runes.Clear();
        for (int i = 0; i < newInfo.Count; i++)
        {
            AddRune(newInfo[i]);
        }
    }
    public StatDatabase runeLetters;
    public StatDatabase runeWords;
    public List<string> ReturnRunePassives()
    {
        List<string> rPassives = new List<string>();
        // Get the letter mapping.
        string word = "";
        for (int i = 0; i < runes.Count; i++)
        {
            word += runeLetters.ReturnValue(runes[i]);
        }
        string runeWord = runeWords.ReturnValue(word);
        // Check if you have a real word.
        if (runeWord.Length > 1)
        {
            rPassives.Add(runeWord);
            return rPassives;
        }
        // Else just return the runes.
        return GetRunes();
    }
    public List<string> GetRunes(){return runes;}
    public string rarity;
    public void SetRarity(string newInfo)
    {
        rarity = newInfo;
    }
    public int GetRarity()
    {
        return int.Parse(rarity);
    }
    public void EquipToActor(TacticActor actor)
    {
        if (allStats.Length < 6) { return; }
        if (slot == "Weapon")
        {
            actor.SetWeaponType(type);
            actor.SetWeaponName(equipName);
            actor.SetWeaponStats(allStats);
            actor.SetWeaponReach(int.Parse(weaponReach.ReturnValue(type)));
        }
        for (int i = 0; i < passives.Count; i++)
        {
            actor.AddPassiveSkill(passives[i], passiveLevels[i]);
        }
        List<string> rPassives = ReturnRunePassives();
        for (int i = 0; i < rPassives.Count; i++)
        {
            actor.AddRunePassive(rPassives[i]);
        }
    }
    public void EquipWeapon(TacticActor actor)
    {
        if (allStats.Length < 6){return;}
        if (slot == "Weapon")
        {
            actor.SetWeaponType(type);
        }
    }
}
