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
    public int level;
    public int exp;
    public int gold;
    public int health;
    public int round;
    public List<string> benchActorData;
    public List<string> mapActorData;
    public int mapSize = 7;
    public List<string> mapTiles;
    public List<string> mapTerrain;
    // public string mode; // Normal/Hard/Hell/Endless?
    public override void NewGame()
    {
        level = 1;
        exp = 0;
        gold = 10;
        health = 100;
        round = 1;
        benchActorData.Clear();
        mapActorData.Clear();
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
        Save();
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        File.WriteAllText(dataPath, allData);
    }
}