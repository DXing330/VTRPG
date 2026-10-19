using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MysteryDungeonState", menuName = "ScriptableObjects/MysteryDungeon/MysteryDungeonState", order = 1)]
public class MysteryDungeonState : SavedData
{
    public MysteryDungeonGenerator generator;
    // Set And Save The Dungeon Name Before Entering The Dungeon.
    public string dungeonName;
    // Effectively The NewGame Function, Used In Hub When Selecting A Dungeon.
    public void InitializeDungeonByName(string newName)
    {
        SetDungeonName(newName);
        floor = 0;
        mapState = "";
    }
    // Used While Loading.
    public void SetDungeonName(string newName)
    {
        dungeonName = newName;
        UpdateDungeonSpecifics();
    }
    // Max Floors|Terrain Type|Enemy Pool|Average Size|Variance|Difficulty Modifiers|
    public List<string> dungeonSpecifics;
    public void UpdateDungeonSpecifics()
    {
        dungeonSpecifics = dungeonData.ReturnValue(dungeonName).Split("|").ToList();
    }
    public StatDatabase dungeonData;
    public string GetDungeonType()
    {
        UpdateDungeonSpecifics();
        if (dungeonSpecifics.Count < 2)
        {
            return "Forest";
        }
        return dungeonSpecifics[1];
    }
    public int floor;
    public bool NewFloor()
    {
        floor++;
        return floor <= GetMaxFloor();
    }
    public int GetMaxFloor()
    {
        UpdateDungeonSpecifics();
        if (dungeonSpecifics.Count < 1)
        {
            return 0;
        }
        return int.Parse(dungeonSpecifics[0]);
    }
    public string mapState;
    public void SaveFromMysteryDungeonMap(MysteryDungeonBattleMap map)
    {
        mapState = map.ReturnMapStateString();
        Save();
    }
    public bool LoadToMysteryDungeonMap(MysteryDungeonBattleMap map)
    {
        Load();
        if (mapState.Length <= 0){return false;}
        map.LoadMapStateFromString(mapState);
        return true;
    }
    public override void NewGame()
    {
        floor = 0;
        dungeonName = "";
        Save();
    }
    public override void Save()
    {
        dataPath = GetSavePath();
        allData = "";
        allData += "Dungeon=" + dungeonName + delimiter;
        allData += "Floor=" + floor + delimiter;
        allData += "MapState=" + mapState + delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = GetSavePath();
        if (File.Exists(dataPath)) { allData = File.ReadAllText(dataPath); }
        else
        {
            NewGame();
            return;
        }
        if (allData.Contains(delimiter))
        {
            dataList = allData.Split(delimiter).ToList();
        }
        else
        {
            NewGame();
            return;
        }
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
    }
    public override void LoadStat(string stat)
    {
        string[] blocks = stat.Split(new[] { '=' }, 2);
        if (blocks.Length < 2){return;}
        string data = blocks[1];
        switch (blocks[0])
        {
            default:
            break;
            case "Dungeon":
            SetDungeonName(data);
            break;
            case "Floor":
            floor = int.Parse(data);
            break;
            case "MapState":
            mapState = data;
            break;
        }
    }
}
