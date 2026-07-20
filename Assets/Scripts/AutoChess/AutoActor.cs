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
    Laterano,
    Yan,
    Kjerag,
    Aegir,
    Sargon,
    Victoria,
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
    public List<string> GetFactions(){return factions;}
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
    public void LoadBaseStats(StatDatabase autoActorData, int newLevel = 1)
    {
        SetLevel(newLevel);
        string data = autoActorData.ReturnValue(autoChessName);
        string[] blocks = data.Split("|");
        trait = new AutoChessTrait();
        trait.LoadBaseTrait(blocks[1], blocks[2], blocks[3]);
        SetFactions(blocks[0].Split(",").ToList());
        SetHealth(int.Parse(blocks[7]) + (10 * (newLevel - 1)));
        SetAttack(int.Parse(blocks[8]) + (2 * (newLevel - 1)));
        SetDefense(int.Parse(blocks[9]));
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
    public void AutoChessSetInitialStatsFromString(string newStats, int level = 1)
    {
        // Initialize Regular Stats.
        baseCrit = 0;
        baseCritPower = 200;
        baseHitChance = 100;
        baseDodge = 0;
        ResetEquipment();
        ResetPassives();
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
        SetBaseHealth(int.Parse(statBlocks[7]) + (10 * (level - 1)));
        SetCurrentHealth(int.Parse(statBlocks[7]) + (10 * (level - 1)));
        SetBaseAttack(int.Parse(statBlocks[8]) + (2 * (level - 1)));
        SetBaseDefense(int.Parse(statBlocks[9]));
        SetAttackRange(int.Parse(statBlocks[10]));
        autoChessAttackRangeShape =  statBlocks[13];
        SetPassiveSkills(statBlocks[11].Split(passiveDelimiter).ToList());
        SetPassiveLevels(statBlocks[12].Split(passiveDelimiter).ToList());
        akHealer = int.Parse(statBlocks[14]);
        baseRespawnTimer = int.Parse(statBlocks[15]);
        akAOE = int.Parse(statBlocks[16]);
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
        SetAttackRange(int.Parse(statBlocks[6]));
        SetPassiveSkills(statBlocks[7].Split(passiveDelimiter).ToList());
        SetPassiveLevels(statBlocks[8].Split(passiveDelimiter).ToList());
        SetMoveType(statBlocks[9]);
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
