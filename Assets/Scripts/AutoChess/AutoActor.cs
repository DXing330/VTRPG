using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AutoChessFaction
{
    None,
    // Core alliances
    Laterano, // Light
    Yan, // Fire
    Kjerag, // Ice
    Aegir, // Water
    Sargon, // Earth
    Victoria, // Lightning
    // Additional alliances
    Precision,
    Agile,
    Swift,
    Resilient,
    Durable,
    Aid,
    Raid,
    Marvel,
    Foresight,
    Investor,
    Assist,
    Harmony
}
[System.Serializable]
public class AutoChessTrait
{
    string delimiter = "aCTDelim";
    // Can't use "=" as a delimiter in a nested object.
    string equals = "aCTEquals";
    public string timing;
    public string effect;
    public string specifics;
    public void ResetTraitStats()
    {
        timing = "None";
        effect = "None";
        specifics = "None";
    }
    public void LoadBaseTrait(string newTiming, string newEffect, string newSpecifics)
    {
        timing = newTiming;
        effect = newEffect;
        specifics = newSpecifics;
    }
    public string ReturnTrait()
    {
        string traitDetails = "";
        traitDetails += "Timing" + equals + timing + delimiter;
        traitDetails += "Effect" + equals + effect + delimiter;
        traitDetails += "Specific" + equals + specifics + delimiter;
        return traitDetails;
    }
    public void SetTrait(string newTraitInfo)
    {
        ResetTraitStats();
        string[] traitBlocks = newTraitInfo.Split(delimiter);
        for (int i = 0; i < traitBlocks.Length; i++)
        {
            LoadStat(traitBlocks[i]);
        }
    }
    public void LoadStat(string data)
    {
        string[] blocks = data.Split(equals);
        if (blocks.Length < 2){return;}
        string key = blocks[0];
        string value = blocks[1];
        switch (key)
        {
            default:
            return;
            case "Timing":
            timing = value;
            return;
            case "Effect":
            effect = value;
            return;
            case "Specific":
            specifics = value;
            return;
        }
    }
}
// For Saving/Loading And Easy Management During The Prep Phase.
// The Prep Phase Will Only Store A List Of These.
[System.Serializable]
public class AutoActorRollUpData
{
    string delimiter = "aCADelim";
    string factionDelimiter = "aCAFDelim";
    string equipDelimiter = "aCAEDelim";
    string equals = "aCAEquals";
    // Name + Level Contains All Base Stat Data
    public string autoChessName;
    public string GetName(){return autoChessName;}
    public void SetName(string newData){autoChessName = newData;}
    public int autoChessLevel;
    public int GetLevel(){return autoChessLevel;}
    public void SetLevel(int newData){autoChessLevel = newData;}
    public List<string> factions;
    public bool FactionExists(string factionName){return factions.Contains(factionName);}
    public List<string> RAWGetFactions(){return factions;}
    public List<string> GetFactions(){return new List<string>(factions);}
    public void SetFactions(List<string> newFactions){factions = newFactions;}
    public int health;
    public int GetHealth(){return health;}
    public void SetHealth(int newData){health = newData;}
    public int attack;
    public int GetAttack(){return attack;}
    public void SetAttack(int newData){attack = newData;}
    public int defense;
    public int GetDefense(){return defense;}
    public void SetDefense(int newData){defense = newData;}
    public int resist; // Magic Resist.
    public int GetResist(){return resist;}
    public void SetResist(int newData){resist = newData;}
    public int attackRange;
    public int GetAttackRange(){return attackRange;}
    public void SetAttackRange(int newData){attackRange = newData;}
    public string attackShape;
    public string GetAttackShape(){return attackShape;}
    public void SetAttackShape(string newData){attackShape = newData;}
    public bool healer = false;
    public bool AOE = false;
    public string GetBaseStatString()
    {
        string baseStatString = GetName();
        if (GetLevel() > 1)
        {
            baseStatString += "+";
        }
        /*baseStatString += "\n" + "HP:" + GetHealth() + " ATK:" + GetAttack() + " DEF:" + GetDefense();*/
        return baseStatString;
    }
    public List<string> equipmentNames = new List<string>();
    public List<string> GetEmblems()
    {
        List<string> emblems = new List<string>();
        for (int i = 0; i < equipmentNames.Count; i++)
        {
            if (equipmentNames[i].Contains(" Emblem"))
            {
                emblems.Add(equipmentNames[i].Replace(" Emblem", ""));
            }
        }
        return emblems;
    }
    public bool EmblemExists(string emblem)
    {
        return GetEmblems().Contains(emblem);
    }
    public bool EquipmentExists(string equipName)
    {
        return equipmentNames.Contains(equipName);
    }
    public List<string> GetEquipmentNames()
    {
        for (int i = equipmentNames.Count - 1; i >= 0; i--)
        {
            if (equipmentNames[i].Length <= 0)
            {
                equipmentNames.RemoveAt(i);
            }
        }
        return equipmentNames;
    }
    public void RemoveLatestEquipment()
    {
        if (equipmentNames.Count <= 0){return;}
        equipmentNames.RemoveAt(equipmentNames.Count - 1);
    }
    public int GetOpenEquipmentSlots()
    {
        for (int i = equipmentNames.Count - 1; i >= 0; i--)
        {
            if (equipmentNames[i].Length <= 0)
            {
                equipmentNames.RemoveAt(i);
            }
        }
        return 3 - equipmentNames.Count;
    }
    public void EquipEquipment(string equipName)
    {
        equipmentNames.Add(equipName);
    }
    public string GetLatestEquipment()
    {
        if (equipmentNames.Count <= 0){return "";}
        return equipmentNames[equipmentNames.Count - 1];
    }
    // Need The Trait Since Some Traits Activate During Prep Phase.
    public AutoChessTrait trait;
    public AutoChessTrait GetTrait(){return trait;}
    public void LoadBaseStats(StatDatabase autoActorData, int newLevel = 1)
    {
        SetLevel(newLevel);
        string data = autoActorData.ReturnValue(autoChessName);
        string[] blocks = data.Split("|");
        trait = new AutoChessTrait();
        trait.LoadBaseTrait(blocks[1], blocks[2], blocks[3]);
        SetFactions(blocks[0].Split(",").ToList());
        SetHealth(int.Parse(blocks[7]) + (int.Parse(blocks[7]) * (newLevel - 1) * 3 / 10));
        SetAttack(int.Parse(blocks[8]) + (int.Parse(blocks[8]) * (newLevel - 1) * 3 / 10));
        SetDefense(int.Parse(blocks[9]) + (int.Parse(blocks[9]) * (newLevel - 1) * 3 / 10));
        SetResist(int.Parse(blocks[10]));
        SetAttackRange(int.Parse(blocks[11]));
        SetAttackShape(blocks[14]);
        if (int.Parse(blocks[15]) == 1)
        {
            healer = true;
        }
        if (int.Parse(blocks[17]) == 1)
        {
            AOE = true;
        }
    }
    public void LoadBaseTrait(StatDatabase autoActorData)
    {
        string data = autoActorData.ReturnValue(autoChessName);
        string[] blocks = data.Split("|");
        trait = new AutoChessTrait();
        trait.LoadBaseTrait(blocks[1], blocks[2], blocks[3]);
    }
    // Seat/Tile
    public int location;
    public int GetLocation(){return location;}
    public void SetLocation(int newInfo){location = newInfo;}
    public int direction;
    public int GetDirection(){return direction;}
    public void SetDirection(int newInfo){direction = newInfo;}
    public string ReturnRollUpData()
    {
        string data = "";
        data += "Name" + equals + autoChessName + delimiter;
        data += "Level" + equals + autoChessLevel + delimiter;
        data += "Factions" + equals + String.Join(factionDelimiter, factions) + delimiter;
        data += "Health" + equals + health + delimiter;
        data += "Attack" + equals + attack + delimiter;
        data += "Defense" + equals + defense + delimiter;
        data += "Resist" + equals + resist + delimiter;
        data += "Range" + equals + attackRange + delimiter;
        data += "RangeShape" + equals + attackShape + delimiter;
        data += "Healer" + equals + healer + delimiter;
        data += "AOE" + equals + AOE + delimiter;
        data += "Equipment" + equals + String.Join(equipDelimiter, equipmentNames) + delimiter;
        data += "Location" + equals + location + delimiter;
        data += "Direction" + equals + direction + delimiter;
        return data;
    }
    public void LoadRollUpData(string newData)
    {
        string[] blocks = newData.Split(delimiter);
        for (int i = 0; i < blocks.Length; i++)
        {
            LoadStat(blocks[i]);
        }
    }
    public void LoadStat(string data)
    {
        string[] blocks = data.Split(equals);
        if (blocks.Length < 2){return;}
        string key = blocks[0];
        string value = blocks[1];
        switch (key)
        {
            default:
            return;
            case "Name":
            autoChessName = value;
            return;
            case "Level":
            SetLevel(int.Parse(value));
            return;
            case "Factions":
            SetFactions(value.Split(factionDelimiter).ToList());
            return;
            case "Health":
            SetHealth(int.Parse(value));
            return;
            case "Attack":
            SetAttack(int.Parse(value));
            return;
            case "Defense":
            SetDefense(int.Parse(value));
            return;
            case "Resist":
            SetResist(int.Parse(value));
            return;
            case "Range":
            SetAttackRange(int.Parse(value));
            return;
            case "RangeShape":
            SetAttackShape(value);
            return;
            case "Healer":
            if (bool.TryParse(value, out bool healerValue))
                healer = healerValue;
            return;
            case "AOE":
            if (bool.TryParse(value, out bool aoeValue))
                AOE = aoeValue;
            return;
            case "Equipment":
            equipmentNames = new List<string>(value.Split(equipDelimiter).ToList());
            return;
            case "Location":
            location = int.Parse(value);
            return;
            case "Direction":
            direction = int.Parse(value);
            return;
        }
    }
}

[System.Serializable]
public class AutoActor : TacticActor
{
    public int GetLevel(){return autoChessLevel;}
    public void AutoChessSetInitialStatsFromString(string newStats, int level = 1)
    {
        // Initialize Regular Stats.
        baseCrit = 0;
        baseCritPower = 200;
        baseHitChance = 100;
        baseDodge = 0;
        ResetEquipment();
        ResetPassives();
        autoChessLevel = level;
        // Deal With AutoChess Stats
        autoChessTrait = new AutoChessTrait();
        autoChessEquipment.Clear();
        string[] statBlocks = newStats.Split("|");
        // From A Database So Hardcoded.
        SetFactionsFromString(statBlocks[0]);
        autoChessTrait.LoadBaseTrait(statBlocks[1], statBlocks[2], statBlocks[3]);
        autoSkill = statBlocks[4];
        SetBaseEnergy(int.Parse(statBlocks[5]));
        SetCurrentEnergy(int.Parse(statBlocks[6]));
        SetBaseHealth(int.Parse(statBlocks[7]));
        SetCurrentHealth(int.Parse(statBlocks[7]));
        SetBaseAttack(int.Parse(statBlocks[8]));
        SetBaseDefense(int.Parse(statBlocks[9]));
        SetBaseMagicResist(int.Parse(statBlocks[10]));
        // Should Scale Based On Base Stats.
        if (level > 1)
        {
            int scaling = (level - 1) * 3;
            UpdateBaseHealth(GetBaseHealth() * scaling / 10, false);
            SetCurrentHealth(GetBaseHealth());
            UpdateBaseAttack(GetBaseAttack() * scaling / 10);
            UpdateBaseDefense(GetBaseDefense() * scaling / 10);
        }
        SetAttackRange(int.Parse(statBlocks[11]));
        autoChessAttackRangeShape = statBlocks[14];
        SetPassiveSkills(statBlocks[12].Split(passiveDelimiter).ToList());
        SetPassiveLevels(statBlocks[13].Split(passiveDelimiter).ToList());
        akHealer = int.Parse(statBlocks[15]);
        baseRespawnTimer = int.Parse(statBlocks[16]);
        akAOE = int.Parse(statBlocks[17]);
    }
    public void AutoChessEnemySetInitialStatsFromString(string newStats, int difficultyScaling = 0)
    {
        // Initialize Regular Stats.
        baseCrit = 0;
        baseCritPower = 200;
        baseHitChance = 100;
        baseDodge = 0;
        ResetEquipment();
        ResetPassives();
        // Load AutoStats
        string[] statBlocks = newStats.Split("|");
        autoSkill = statBlocks[0];
        SetBaseEnergy(int.Parse(statBlocks[1]));
        SetCurrentEnergy(int.Parse(statBlocks[2]));
        int health = int.Parse(statBlocks[3]);
        int attack = int.Parse(statBlocks[4]);
        int defense = int.Parse(statBlocks[5]);
        int resist = int.Parse(statBlocks[6]);
        if (difficultyScaling > 0)
        {
            health += (health * difficultyScaling) / 100;
            attack += (attack * difficultyScaling) / 100;
            defense += (defense * difficultyScaling) / 100;
        }
        SetBaseHealth(health);
        SetCurrentHealth(health);
        SetBaseAttack(attack);
        SetBaseDefense(defense);
        SetBaseMagicResist(resist);
        SetAttackRange(int.Parse(statBlocks[7]));
        SetPassiveSkills(statBlocks[8].Split(passiveDelimiter).ToList());
        SetPassiveLevels(statBlocks[9].Split(passiveDelimiter).ToList());
        SetMoveType(statBlocks[10]);
    }
    public override int GetMaxMoveRange()
    {
        return 333;
    }
    public override int GetMoveRange(bool current = true)
    {
        return 333;
    }
}
