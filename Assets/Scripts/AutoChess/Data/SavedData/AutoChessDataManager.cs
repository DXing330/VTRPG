using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AutoChessDataManager", menuName = "ScriptableObjects/AutoChess/AutoChessDataManager", order = 1)]
public class AutoChessDataManager : SavedData
{
    public List<SavedData> subDataManagers;
    public AutoChessFactionDataManager factionData;
    public AutoChessLogDataManager logData; // Track Round / Gold / Exp / Etc.
    public void AddLog(string newLog)
    {
        logData.AddLog(newLog);
    }
    public void GainFactionStacks(string faction, int stackAmount)
    {
        factionData.GainFactionStacks(faction, stackAmount);
    }
    public List<string> GetAllFactions(){return factionData.GetAllFactions();}
    public List<string> GetAllFactionStacks(){return factionData.GetAllFactionStacks();}
    public string delimiter2;
    protected int maxLevel = 6;
    public bool MaxLevel(){return level >= maxLevel;}
    public int level;
    public int GetLevel(){return level;}
    public void SetLevel(int newInfo)
    {
        level = newInfo;
    }
    public override void LevelUp()
    {
        if (MaxLevel()){return;}
        // Check If EXP Is Sufficient
        int expToLevel = ExpToLevelUp();
        if (exp >= expToLevel)
        {
            level++;
            for (int i = 0; i < subDataManagers.Count; i++)
            {
                subDataManagers[i].LevelUp();
            }
            AddLog("Leveled Up! (" + (level - 1) + "->" + level + ")");
            exp -= expToLevel;
        }
    }
    public int exp;
    public int GetExp(){return exp;}
    public void SetExp(int newInfo)
    {
        exp = newInfo;
    }
    public int ExpToLevelUp()
    {
        return (2) * (level + 1) * (level + 1);
    }
    public void GainExp(int amount)
    {
        exp += amount;
        AddLog("Gained " + amount + " EXP");
        LevelUp();
    }
    public void GainExpAfterBattle()
    {
        GainExp(6 + (round / 2));
    }
    public int gold;
    public int GetGold(){return gold;}
    public void SetGold(int newInfo)
    {
        gold = newInfo;
    }
    public void GainGold(int amount)
    {
        gold += amount;
        AddLog("+" + amount + " Gold");
    }
    public void GainGoldAfterBattle()
    {
        // Gain Interest.
        int gain = 0;
        gain += Mathf.Min(5, gold / 10);
        // Gain More Gold As The Rounds Go On.
        gain += 6 + (round / 2);
        gain += GetNextRoundGold();
        SetNextRoundGold(0);
        GainGold(gain);
    }
    public int nextRoundGold;
    public void GainNextRoundGold(int amount)
    {
        nextRoundGold += amount;
    }
    public void SetNextRoundGold(int amount)
    {
        nextRoundGold = amount;
    }
    public int startRoundForesightStacks;
    public void SetForesightStacks(int newInfo){startRoundForesightStacks = newInfo;}
    public int startRoundMarvelStacks;
    public void SetMarvelStacks(int newInfo){startRoundMarvelStacks = newInfo;}
    public int GetStartRoundStacksOfFaction(string factionName)
    {
        if (factionName == "Foresight"){return startRoundForesightStacks;}
        else if (factionName == "Marvel"){return startRoundMarvelStacks;}
        return 0;
    }
    public void SetStartRoundStacksOfFaction(string factionName, int amount)
    {
        if (factionName == "Foresight")
        {
            SetForesightStacks(amount);
        }
        else if (factionName == "Marvel")
        {
            SetMarvelStacks(amount);
        }
    }
    public void GainGoldFromFaction(string faction = "Foresight", int breakpoint = 10, int gold = 2)
    {
        int newStacks = int.Parse(factionData.GetStacksOfFaction(faction));
        int startStacks = GetStartRoundStacksOfFaction(faction);
        int oldBreakpoints = startStacks / breakpoint;
        int newBreakpoints = newStacks / breakpoint;
        int crossed = newBreakpoints - oldBreakpoints;
        if (crossed > 0)
        {
            GainGold(crossed * gold);
            AddLog("Gained " + (crossed * gold) + " gold from " + faction + " faction stacks. (" + startStacks + "->" + newStacks + ")");
        }
        SetStartRoundStacksOfFaction(faction, newStacks);
    }
    public int GetNextRoundGold(){return nextRoundGold;}
    public int roundSpentGold;
    public void SetRoundGold(int amount)
    {
        roundSpentGold = amount;
    }
    public int GetRoundGold(){return roundSpentGold;}
    public bool SpendGold(int amount)
    {
        if (amount > gold){return false;}
        roundSpentGold += amount;
        gold -= amount;
        AddLog("Spent " + amount + " Gold. " + gold + " Remaining.");
        return true;
    }
    public int health;
    public int GetHealth(){return health;}
    public void SetHealth(int newInfo)
    {
        health = newInfo;
    }
    public void LoseHealth(int amount)
    {
        health -= amount;
        AddLog("Lost " + amount + " health. " + health + " Remaining.");
    }
    public int round;
    public int GetRound(){return round;}
    public void SetRound(int newInfo)
    {
        round = newInfo;
    }
    public override void NewRound()
    {
        round++;
        AddLog("--- Round " + round + " Begins ---");
        GainGoldAfterBattle();
        GainExpAfterBattle();
        GainGoldFromFaction(); // Foresight.
        GainGoldFromFaction("Marvel", 50, 10); // Marvel.
        roundGainedActors = 0;
        roundSpentGold = 0;
        GainEquipmentDrops();
        for (int i = 0; i < subDataManagers.Count; i++)
        {
            subDataManagers[i].NewRound();
        }
    }
    public List<string> benchActorData;
    public List<string> fieldActorData;
    public List<string> GetFieldActorData()
    {
        return fieldActorData;
    }
    public int roundGainedActors;
    public void SetRoundActors(int amount)
    {
        roundGainedActors = amount;
    }
    public int GetRoundActors(){return roundGainedActors;}
    public void GainActor(AutoActorRollUpData newActor)
    {
        roundGainedActors++;
    }
    public int mapSize = 7;
    public List<string> mapTiles;
    public List<string> GetMapTiles(){return mapTiles;}
    public List<string> mapTerrain;
    public List<string> GetMapTerrain(){return mapTerrain;}
    public StatDatabase equipmentRarity;
    public RNGUtility shopRNG;
    // Get 2 Random Equipment At The Start Of The Game.
    public List<string> GenerateEquipmentOfRarity(int rarity = 1, int quantity = 1)
    {
        List<string> equipmentOfRarity = equipmentRarity.GetKeysFilteringByValues(rarity.ToString());
        List<string> generatedEquipment = new List<string>();
        for (int i = 0; i < Mathf.Max(1, quantity); i++)
        {
            generatedEquipment.Add(equipmentOfRarity[shopRNG.SeedRange(0, equipmentOfRarity.Count)]);
        }
        return generatedEquipment;
    }
    public void GenerateStarterEquipment()
    {
        List<string> starterEquipment = GenerateEquipmentOfRarity(1, 2);
        for (int i = 0; i < starterEquipment.Count; i++)
        {
            GainEquipment(starterEquipment[i]);
        }
    }
    public void GainEquipmentDrops()
    {
        // Formula Based On Round #.
        // 1 2 3 4 5 6 7 8 9 0 1 2 3 4 - Round
        // 1 1 2 2 2 3 3 1 1 1 2 2 2 3 - Quantity
        // 1 1 1 1 1 1 1 4 4 4 4 4 4 4 - Rarity
        int quantity = 1 + ((round % 8) / 3);
        // Rounds (1-7) -> Rarity 1, 7+ -> Rarity 4. 
        int rarity = 1;
        if (round >= 8)
        {
            rarity = 4;
        }
        List<string> equipmentDrops = GenerateEquipmentOfRarity(rarity, quantity);
        for (int i = 0; i < equipmentDrops.Count; i++)
        {
            GainEquipment(equipmentDrops[i]);
        }
    }
    public List<string> equipment;
    public List<string> GetEquipment()
    {
        for (int i = equipment.Count - 1; i >= 0; i--)
        {
            if (equipment[i].Length <= 0){equipment.RemoveAt(i);}
        }
        return equipment;
    }
    public void UseEquipment(string equipName)
    {
        int indexOf = equipment.IndexOf(equipName);
        if (indexOf >= 0)
        {
            equipment.RemoveAt(indexOf);
        }
    }
    public int GetEquipmentCount(string equipName)
    {
        return utility.CountStringsInList(equipment, equipName);
    }
    public void GainEquipment(string equipName)
    {
        if (equipName.Length <= 0){return;}
        AddLog("Gained Equipment: " + equipName);
        equipment.Add(equipName);
    }
    public void ReclaimEquipmentFromActor(AutoActorRollUpData actor)
    {
        List<string> actorEquipment = actor.GetEquipmentNames();
        for (int i = 0; i < actorEquipment.Count; i++)
        {
            GainEquipment(actorEquipment[i]);
        }
    }
    // public string mode; // Normal/Hard/Hell/Endless?
    [ContextMenu("New Game")]
    public override void NewGame()
    {
        // Reset Everything, Log First.
        logData.NewGame();
        AddLog("--- Round 1 Begins ---");
        for (int i = 0; i < subDataManagers.Count; i++)
        {
            subDataManagers[i].NewGame();
        }
        level = 1;
        exp = 0;
        gold = 10;
        health = 100;
        round = 1;
        nextRoundGold = 0;
        startRoundForesightStacks = 0;
        startRoundMarvelStacks = 0;
        roundSpentGold = 0;
        roundGainedActors = 0;
        benchActorData.Clear();
        fieldActorData.Clear();
        mapTiles.Clear(); // All Plains.
        mapTerrain.Clear(); // All Blank.
        equipment.Clear();
        GenerateStarterEquipment();
        for (int i = 0; i < mapSize * mapSize; i++)
        {
            mapTiles.Add("Plains");
            mapTerrain.Add("");
        }
        Save();
    }
    public void SaveFromPrepManager(AutoChessPrepManager prepManager)
    {
        // Copy The Data From The PrepManager.
        benchActorData.Clear();
        fieldActorData.Clear();
        for (int i = 0; i < prepManager.benchSlots.Count; i++)
        {
            benchActorData.Add(prepManager.benchSlots[i].ReturnRollUpData());
        }
        for (int i = 0; i < prepManager.fieldSlots.Count; i++)
        {
            fieldActorData.Add(prepManager.fieldSlots[i].ReturnRollUpData());
        }
        Save();
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "Level=" + level + delimiter;
        allData += "Exp=" + exp + delimiter;
        allData += "Gold=" + gold + delimiter;
        allData += "Health=" + health + delimiter;
        allData += "Round=" + round + delimiter;
        allData += "NextRoundGold=" + nextRoundGold + delimiter;
        allData += "Foresight=" + startRoundForesightStacks + delimiter;
        allData += "Marvel=" + startRoundMarvelStacks + delimiter;
        allData += "RoundGold=" + roundSpentGold + delimiter;
        allData += "RoundActors=" + roundGainedActors + delimiter;
        allData += "BenchActors=" + String.Join(delimiter2, benchActorData) + delimiter;
        allData += "FieldActors=" + String.Join(delimiter2, fieldActorData) + delimiter;
        allData += "MapTiles=" + String.Join(delimiter2, mapTiles) + delimiter;
        allData += "MapTerrain=" + String.Join(delimiter2, mapTerrain) + delimiter;
        allData += "Equipment=" + String.Join(delimiter2, equipment) + delimiter;
        File.WriteAllText(dataPath, allData);
        for (int i = 0; i < subDataManagers.Count; i++)
        {
            subDataManagers[i].Save();
        }
        logData.Save();
    }
    public override void Load()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        if (File.Exists(dataPath))
        {
            allData = File.ReadAllText(dataPath);
        }
        else
        {
            NewGame();
            return;
        }
        string[] blocks = allData.Split(delimiter);
        for (int i = 0; i < blocks.Length; i++)
        {
            LoadStat(blocks[i]);
        }
        for (int i = 0; i < subDataManagers.Count; i++)
        {
            subDataManagers[i].Load();
        }
        logData.Load();
    }
    public override void LoadStat(string data)
    {
        string[] blocks = data.Split("=");
        if (blocks.Length < 2){return;}
        string key = blocks[0];
        string value = blocks[1];
        switch (key)
        {
            default:
            return;
            case "Level":
            SetLevel(int.Parse(value));
            return;
            case "Exp":
            SetExp(int.Parse(value));
            return;
            case "Gold":
            SetGold(int.Parse(value));
            return;
            case "Health":
            SetHealth(int.Parse(value));
            return;
            case "Round":
            SetRound(int.Parse(value));
            return;
            case "NextRoundGold":
            SetNextRoundGold(int.Parse(value));
            return;
            case "Foresight":
            SetForesightStacks(int.Parse(value));
            return;
            case "Marvel":
            SetMarvelStacks(int.Parse(value));
            return;
            case "RoundGold":
            SetRoundGold(int.Parse(value));
            return;
            case "RoundActors":
            SetRoundActors(int.Parse(value));
            return;
            case "BenchActors":
            benchActorData = value.Split(delimiter2).ToList();
            return;
            case "FieldActors":
            fieldActorData = value.Split(delimiter2).ToList();
            return;
            case "MapTiles":
            mapTiles = value.Split(delimiter2).ToList();
            return;
            case "MapTerrain":
            mapTerrain = value.Split(delimiter2).ToList();
            return;
            case "Equipment":
            equipment = value.Split(delimiter2).ToList();
            return;
        }
    }
}