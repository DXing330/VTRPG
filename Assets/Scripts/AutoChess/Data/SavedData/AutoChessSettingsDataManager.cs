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
    public int difficultyScaling = 0; // Start With 0 - 10.
    public int GetDifficulty(){return difficultyScaling;}
    public int GetTotalRounds()
    {
        return GetDifficulty() + 13;
    }
    public int maxDifficulty = 11;
    public void ChangeDifficulty(bool right)
    {
        int change = 1;
        if (!right)
        {
            change = (maxDifficulty - 1);
        }
        difficultyScaling = (difficultyScaling + change) % maxDifficulty;
    }
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
    public StatDatabase tacticianDatabase;
    public AutoChessTactician tactician;
    public string selectedTactician; // TODO Various Effects?
    public string GetTactician(){return selectedTactician;}
    public string GetTacticianEffect()
    {
        string[] tacticianData = tacticianDatabase.ReturnValue(selectedTactician).Split("|");
        if (tacticianData.Length < 2){return "";}
        return tacticianData[1];
    }
    public void ChangeTactician(bool right)
    {
        List<string> allTacticians = tacticianDatabase.GetAllKeys();
        int indexOf = allTacticians.IndexOf(selectedTactician);
        int change = 1;
        if (!right)
        {
            change = (allTacticians.Count - 1);
        }
        indexOf = (indexOf + change) % allTacticians.Count;
        selectedTactician = allTacticians[indexOf];
    }
    public override void NewGame()
    {
        newGame = 1;
        Save();
    }
    public override void Save()
    {
        dataPath = GetSavePath();
        allData = "";
        allData += "NewGame=" + newGame + delimiter;
        allData += "Difficulty=" + difficultyScaling + delimiter;
        allData += "Map=" + selectedMap + delimiter;
        allData += "Tactician=" + selectedTactician + delimiter;
        File.WriteAllText(dataPath, allData);
        if (tactician != null)
        {
            tactician.SetTactician(selectedTactician);
            tactician.Save();
        }
    }
    public override void Load()
    {
        dataPath = GetSavePath();
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
            difficultyScaling = int.Parse(value);
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