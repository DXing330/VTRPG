using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Controls The Start Of The Game, After That Not Needed.
[CreateAssetMenu(fileName = "AutoChessSettingsDataManager", menuName = "ScriptableObjects/AutoChess/AutoChessSettingsDataManager", order = 1)]
public class AutoChessSettingsDataManager : SavedData
{
    public int newGame = 1; // Start Always Do A New Game.
    public int difficultScaling; // Start With 1 - 10.
    public List<AutoChessMapAsset> allMaps;
    public AutoChessMapAsset GetSelectedMap()
    {
        return allMaps[selectedMap];
    }
    public int selectedMap;
    public void ChangeMap(bool right)
    {
        int change = 1;
        if (!right)
        {
            change = (allMaps.Count - 1);
        }
        selectedMap = (selectedMap + change) % allMaps.Count;
    }
    public string selectedTactician; // TODO Various Effects?
    public override void NewGame()
    {
        newGame = 1;
        Save();
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "NewGame=" + newGame + delimiter;
        allData += "Difficulty=" + difficultScaling + delimiter;
        allData += "Map=" + selectedMap + delimiter;
        allData += "Tactician=" + selectedTactician + delimiter;
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
            case "NewGame":
            newGame = int.Parse(value);
            return;
            case "Difficulty":
            difficultScaling = int.Parse(value);
            return;
            case "Map":
            selectedMap = int.Parse(value);
            return;
            case "Tactician":
            selectedTactician = value;
            return;
        }
    }
}