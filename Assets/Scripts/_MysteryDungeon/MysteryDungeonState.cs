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
    public string dungeonType;
    public int floor;
    public int maxFloor;
    public string mapState;
    public void SaveFromMysteryDungeonMap(MysteryDungeonBattleMap map)
    {
        mapState = map.ReturnMapStateString();
        Save();
    }
    public void LoadToMysteryDungeonMap(MysteryDungeonBattleMap map)
    {
        Load();
        map.LoadMapStateFromString(mapState);
    }
    public override void NewGame()
    {
        floor = 0;
        maxFloor = 1;
        Save();
    }
    public override void Save()
    {
        dataPath = GetSavePath();
        allData = "";
        allData += "Floor=" + floor + delimiter;
        allData += "MaxFloor=" + maxFloor + delimiter;
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
            case "Floor":
            floor = int.Parse(data);
            break;
            case "MaxFloor":
            maxFloor = int.Parse(data);
            break;
            case "MapState":
            mapState = data;
            break;
        }
    }
}
