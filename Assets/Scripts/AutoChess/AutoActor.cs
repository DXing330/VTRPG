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
    protected void LoadStat(string data)
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
    string equipDelimiter = "aCAEDelim";
    string equals = "aCAEquals";
    // Name + Level Contains All Base Stat Data
    public string autoChessName;
    public string GetName(){return autoChessName;}
    public void SetName(string newData){autoChessName = newData;}
    public int autoChessLevel;
    public int GetLevel(){return autoChessLevel;}
    public void SetLevel(int newData){autoChessLevel = newData;}
    public List<string> equipmentNames = new List<string>();
    // Need The Trait Since Some Traits Activate During Prep Phase.
    public AutoChessTrait trait;
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

public class AutoActor : TacticActor
{
    // Unique Stats For The Gamemode.
    public List<AutoChessEquipment> autoChessEquipment;
    // Automatically Uses A Skill When You Have Resources.
    public string autoSkill;
    public List<AutoChessFaction> factions;
    public void SetFactionsFromString(string data)
    {
        factions.Clear();
        string[] blocks = data.Split(",");
        for (int i = 0; i < blocks.Length; i++)
        {
            factions.Add(Enum.Parse<AutoChessFaction>(blocks[i]));
        }
    }
    public AutoChessTrait trait;
    public string attackRangeShape;
    public int healer = 0;
    // Defeated Allies Respawn After Some Rounds In This Mode.
    public int baseRespawnTimer;
    public int currentRespawnTimer = 0;
    // Granted By Other Actors At The Start Of Battle.
    public List<AutoChessTrait> temporaryTraits;
    public override void SetInitialStatsFromString(string newStats)
    {
        // Initialize Regular Stats.
        baseCrit = 0;
        baseCritPower = 200;
        baseHitChance = 100;
        baseDodge = 0;
        ResetEquipment();
        ResetPassives();
        // Deal With AutoChess Stats
        trait = new AutoChessTrait();
        autoChessEquipment.Clear();
        string[] statBlocks = newStats.Split("|");
        // From A Database So Hardcoded.
        SetFactionsFromString(statBlocks[0]);
        trait.LoadBaseTrait(statBlocks[1], statBlocks[2], statBlocks[3]);
        autoSkill = statBlocks[4];
        SetBaseEnergy(int.Parse(statBlocks[5]));
        SetCurrentEnergy(int.Parse(statBlocks[6]));
        SetBaseHealth(int.Parse(statBlocks[7]));
        SetCurrentHealth(int.Parse(statBlocks[7]));
        SetBaseAttack(int.Parse(statBlocks[8]));
        SetBaseDefense(int.Parse(statBlocks[9]));
        attackRangeShape =  statBlocks[10];
        baseRespawnTimer = int.Parse(statBlocks[11]);
        SetPassiveSkills(statBlocks[12].Split(passiveDelimiter).ToList());
        SetPassiveLevels(statBlocks[13].Split(passiveDelimiter).ToList());
    }
}
