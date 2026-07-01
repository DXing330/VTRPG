using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SavedData", menuName = "ScriptableObjects/DataContainers/SavedData/SavedData", order = 1)]
public class SavedData : ScriptableObject
{
    public GeneralUtility utility;
    protected string dataPath;
    public string filename;
    public string newGameData;
    public string allData;
    public List<string> dataList;
    public List<string> GetData(){return dataList;}
    public string delimiter;
    public virtual void NewDay(int dayCount)
    {

    }
    public virtual void NewRound()
    {
        
    }
    public virtual void AddHours(int hours)
    {

    }
    public virtual void Rest()
    {
        
    }
    public virtual void NewGame()
    {
        allData = newGameData;
        dataList = allData.Split(delimiter).ToList();
        Save();
    }
    public virtual void Save()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = "";
        for (int i = 0; i < dataList.Count; i++)
        {
            allData += dataList[i];
            if (i < dataList.Count - 1){allData += delimiter;}
        }
        File.WriteAllText(dataPath, allData);
    }
    public virtual void Load()
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
    public virtual void LoadStat(string data)
    {

    }
}
