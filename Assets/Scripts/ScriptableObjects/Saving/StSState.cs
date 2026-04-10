using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StSState", menuName = "ScriptableObjects/StS/StSState", order = 1)]
public class StSState : SavedState
{
    public int newGame;
    public bool StartingNewGame()
    {
        return newGame == 1;
    }
    public override void NewGame()
    {
        newGame = 1;
        floor = 1;
        state = "";
        previousState = "";
        Save();
    }
    public void SetUpNewGame()
    {
        newGame = 0;
        floor = 1;
        state = "Map";
        previousState = "";
        Save();
    }
    public int floor;
    public int GetFloor()
    {
        return floor;
    }
    public void SetFloor(int cFloor)
    {
        floor = cFloor;
        Save();
    }
    // Related To Scenes.
    // Battle/Shop/Rest/Map/Boss/Ancient/Reward
    public string state;
    public string previousState;
    public void UpdateState(string newState)
    {
        previousState = state;
        state = newState;
        Save();
    }
    public string GetState()
    {
        return state;
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "NG=" + newGame + delimiter;
        allData += "Floor=" + floor + delimiter;
        allData += "State=" + state + delimiter;
        allData += "PrevState=" + previousState + delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = File.ReadAllText(dataPath);
        string[] dataBlocks = allData.Split(delimiter);
        for (int i = 0; i < dataBlocks.Length; i++)
        {
            LoadStat(dataBlocks[i]);
        }
    }
    public void LoadStat(string stat)
    {
        string[] statData = stat.Split("=");
        if (statData.Length < 2){return;}
        string key = statData[0];
        string value = statData[1];
        switch (key)
        {
            case "NG":
            newGame = int.Parse(value);
            break;
            case "Floor":
            floor = int.Parse(value);
            break;
            case "State":
            state = value;
            break;
            case "PrevState":
            previousState = value;
            break;
        }
    }
}
