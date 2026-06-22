using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AutoChessDataManager", menuName = "ScriptableObjects/AutoChess/AutoChessDataManager", order = 1)]
public class AutoChessDataManager : SavedData
{
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
    public bool SpendGold(int amount)
    {
        if (amount > gold){return false;}
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
    public List<string> benchActorData;
    public List<string> fieldActorData;
    public int mapSize = 7;
    public List<string> mapTiles;
    public List<string> mapTerrain;
    // public string mode; // Normal/Hard/Hell/Endless?
    [ContextMenu("New Game")]
    public override void NewGame()
    {
        level = 1;
        exp = 0;
        gold = 10;
        health = 100;
        round = 1;
        benchActorData.Clear();
        fieldActorData.Clear();
        mapTiles.Clear(); // All Plains.
        mapTerrain.Clear(); // All Blank.
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
        allData += "BenchActors=" + String.Join(delimiter2, benchActorData) + delimiter;
        allData += "FieldActors=" + String.Join(delimiter2, fieldActorData) + delimiter;
        allData += "MapTiles=" + String.Join(delimiter2, mapTiles) + delimiter;
        allData += "MapTerrain=" + String.Join(delimiter2, mapTerrain) + delimiter;
        File.WriteAllText(dataPath, allData);
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
    }
    public void LoadStat(string data)
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
        }
    }
}