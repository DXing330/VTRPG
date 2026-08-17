using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// In charge of storing key events for logging/debugging, similar to the combatlog in battles.
// This is persistent for the entire run so save and load it with the rest of the data.
[CreateAssetMenu(fileName = "AutoChessLogDataManager", menuName = "ScriptableObjects/AutoChess/AutoChessLogDataManager", order = 1)]
public class AutoChessLogDataManager : SavedData
{
    public List<string> logs = new();
    public void SetLogs(List<string> newInfo)
    {
        logs = new List<string>(newInfo);
    }
    public void AddLog(string newLog)
    {
        logs.Add(newLog);
    }
    public List<string> GetLogs()
    {
        return new List<string>(logs);
    }
    public override void NewGame()
    {
        logs.Clear();
    }
    public override void Save()
    {
        dataPath = GetSavePath();
        allData = String.Join(delimiter, logs);
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = GetSavePath();
        if (File.Exists(dataPath))
        {
            allData = File.ReadAllText(dataPath);
            SetLogs(allData.Split(delimiter).ToList());
        }
        else
        {
            NewGame();
            return;
        }
    }
}
