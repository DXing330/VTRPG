using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SavedBattles", menuName = "ScriptableObjects/DataContainers/SavedData/SavedBattles", order = 1)]
public class BattleMapEditorSaver : MapEditorSaver
{
    public string mapEnemyDelim;
    protected string BattleDataPath(string battleName)
    {
        return Application.persistentDataPath + "/" + filename + battleName;
    }
    public bool BattleExists(string battleName)
    {
        if (battleName.Length <= 0){return false;}
        return File.Exists(BattleDataPath(battleName));
    }
    public void SaveBattle(BattleMapEditor bMap, string battleName)
    {
        // Later can search and filter by key name.
        if (AddKey(battleName))
        {
            SaveKeys();
        }
        dataPath = BattleDataPath(battleName);
        allData = "";
        allData += ReturnSaveMapDataString(bMap.mapEditor);
        // Keep track of the different between the saved map and the saved enemies. 
        allData += mapEnemyDelim;
        allData += "Enemies=" + String.Join(delimiterTwo, bMap.enemies) + delimiter;
        allData += "EnemyLocations=" + String.Join(delimiterTwo, bMap.enemyLocations) + delimiter;
        allData += "Weather=" + bMap.GetBattleWeather() + delimiter;
        allData += "Time=" + bMap.GetBattleTime();
        File.WriteAllText(dataPath, allData);
    }
    public bool TryLoadBattleData(string battleName, out List<string> mapInfo, out List<string> terrainEffects, out List<int> elevations, out List<string> borders, out List<string> buildings, out List<string> enemies, out List<int> enemyLocations, out string weather, out string time)
    {
        mapInfo = new List<string>();
        terrainEffects = new List<string>();
        elevations = new List<int>();
        borders = new List<string>();
        buildings = new List<string>();
        enemies = new List<string>();
        enemyLocations = new List<int>();
        weather = "";
        time = "";

        dataPath = BattleDataPath(battleName);
        if (!File.Exists(dataPath)){return false;}
        allData = File.ReadAllText(dataPath);
        string[] mapEnemyData = allData.Split(mapEnemyDelim);
        if (mapEnemyData.Length < 2){return false;}

        string[] mapData = mapEnemyData[0].Split(delimiter);
        for (int i = 0; i < mapData.Length; i++)
        {
            if (!TryLoadMapDataStat(mapData[i], ref mapInfo, ref terrainEffects, ref elevations, ref borders, ref buildings))
            {
                return false;
            }
        }
        string[] enemyData = mapEnemyData[1].Split(delimiter);
        for (int i = 0; i < enemyData.Length; i++)
        {
            if (!TryLoadEnemyDataStat(enemyData[i], ref enemies, ref enemyLocations, ref weather, ref time))
            {
                return false;
            }
        }
        return mapInfo.Count > 0;
    }
    public void LoadBattle(BattleMapEditor bMap, string battleName)
    {
        bMap.ResetBattleEnvironment();
        dataPath = BattleDataPath(battleName);
        if (File.Exists(dataPath)){allData = File.ReadAllText(dataPath);}
        else
        {
            bMap.InitializeNewMap();
            return;
        }
        string[] mapEnemyData = allData.Split(mapEnemyDelim);
        string[] mapData = mapEnemyData[0].Split(delimiter);
        for (int i = 0; i < mapData.Length; i++)
        {
            if (!LoadMapStat(mapData[i], bMap.mapEditor))
            {
                bMap.InitializeNewMap();
                return;
            }
        }
        string[] enemyData = mapEnemyData[1].Split(delimiter);
        for (int i = 0; i < enemyData.Length; i++)
        {
            if (!LoadBattleStat(enemyData[i], bMap))
            {
                bMap.InitializeNewMap();
                return;
            }
        }
        bMap.mapEditor.UndoEdits();
        bMap.UpdateMap();
    }
    protected bool TryLoadMapDataStat(string data, ref List<string> mapInfo, ref List<string> terrainEffects, ref List<int> elevations, ref List<string> borders, ref List<string> buildings)
    {
        string[] blocks = data.Split("=");
        if (blocks.Length < 2){return false;}
        string value = blocks[1];
        switch (blocks[0])
        {
            default:
                return false;
            case "Tiles":
                mapInfo = value.Split(delimiterTwo).ToList();
                utility.RemoveEmptyListItems(mapInfo);
                return true;
            case "TEffects":
                terrainEffects = value.Split(delimiterTwo).ToList();
                return true;
            case "Elevations":
                elevations = utility.ConvertStringListToIntList(value.Split(delimiterTwo).ToList());
                return true;
            case "Buildings":
                buildings = value.Split(delimiterTwo).ToList();
                return true;
            case "Borders":
                borders = value.Split(delimiterTwo).ToList();
                return true;
        }
    }
    protected bool TryLoadEnemyDataStat(string data, ref List<string> enemies, ref List<int> enemyLocations)
    {
        string weather = "";
        string time = "";
        return TryLoadEnemyDataStat(data, ref enemies, ref enemyLocations, ref weather, ref time);
    }
    protected bool TryLoadEnemyDataStat(string data, ref List<string> enemies, ref List<int> enemyLocations, ref string weather, ref string time)
    {
        string[] blocks = data.Split("=");
        if (blocks.Length < 2){return false;}
        string value = blocks[1];
        switch (blocks[0])
        {
            default:
                return false;
            case "Enemies":
                enemies = value.Split(delimiterTwo).ToList();
                utility.RemoveEmptyListItems(enemies);
                return true;
            case "EnemyLocations":
                enemyLocations = utility.ConvertStringListToIntList(value.Split(delimiterTwo).ToList());
                return true;
            case "Weather":
                weather = value;
                return true;
            case "Time":
                time = value;
                return true;
        }
    }
    protected virtual bool LoadBattleStat(string data, BattleMapEditor map)
    {
        string[] blocks = data.Split("=");
        if (blocks.Length < 2){return false;}
        string value = blocks[1];
        switch (blocks[0])
        {
            default:
                return false;
            case "Enemies":
                map.enemies = value.Split(delimiterTwo).ToList();
                utility.RemoveEmptyListItems(map.enemies);
                return true;
            case "EnemyLocations":
                map.enemyLocations = value.Split(delimiterTwo).ToList();
                utility.RemoveEmptyListItems(map.enemyLocations);
                return true;
            case "Weather":
                map.SetBattleWeather(value);
                return true;
            case "Time":
                map.SetBattleTime(value);
                return true;
        }
    }
}
