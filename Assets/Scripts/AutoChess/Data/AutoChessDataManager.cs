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
    public void LevelUp()
    {
        if (MaxLevel()){return;}
        // Check If EXP Is Sufficient
        int expToLevel = ExpToLevelUp();
        if (exp >= expToLevel)
        {
            level++;
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
        return (level + 1) * (level + 1) * (level + 1);
    }
    public void GainExp(int amount)
    {
        exp += amount;
        LevelUp();
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
        return true;
    }
    public int health;
    public int GetHealth(){return health;}
    public void SetHealth(int newInfo)
    {
        health = newInfo;
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
        roundGainedActors = 0;
        roundSpentGold = 0;
        GainGold(GetNextRoundGold());
        SetNextRoundGold(0);
        for (int i = 0; i < subDataManagers.Count; i++)
        {
            subDataManagers[i].NewRound();
        }
    }
    public List<string> benchActorData;
    public List<string> fieldActorData;
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
    public List<string> mapTerrain;
    public List<string> equipment;
    public List<string> GetEquipment()
    {
        for (int i = equipment.Count - 1; i >= 0; i--)
        {
            if (equipment[i].Length <= 0){equipment.RemoveAt(i);}
        }
        return equipment;
    }
    public void GainEquipment(string equipName)
    {
        if (equipName.Length <= 0){return;}
        equipment.Add(equipName);
    }
    // public string mode; // Normal/Hard/Hell/Endless?
    [ContextMenu("New Game")]
    public override void NewGame()
    {
        level = 1;
        exp = 0;
        gold = 10;
        health = 100;
        round = 1;
        nextRoundGold = 0;
        roundSpentGold = 0;
        roundGainedActors = 0;
        benchActorData.Clear();
        fieldActorData.Clear();
        mapTiles.Clear(); // All Plains.
        mapTerrain.Clear(); // All Blank.
        equipment.Clear();
        for (int i = 0; i < mapSize * mapSize; i++)
        {
            mapTiles.Add("Plains");
            mapTerrain.Add("");
        }
        Save();
        for (int i = 0; i < subDataManagers.Count; i++)
        {
            subDataManagers[i].NewGame();
        }
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