using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StSMapSaveData", menuName = "ScriptableObjects/StS/StSMapSaveData", order = 1)]
public class StSMapSaveData : SavedData
{
    public string delimiter2 = ",";
    // Need to know the current and the current tile info.
    public List<string> mapInfo;
    public List<string> pathInfo;
    public int ReturnBattleCount()
    {
        Load();
        int count = 0;
        if (pathInfo.Count < 1){return 0;}
        for (int i = 0; i < pathInfo.Count; i++)
        {
            string tileType = mapInfo[int.Parse(pathInfo[i])];
            if (tileType == "Enemy" || tileType == "Elite" || tileType == "Boss" || tileType == "Battle")
            {
                count++;
            }
        }
        return count;
    }
    public string GetLatestTile()
    {
        Load();
        if (pathInfo.Count < 1){return "";}
        return mapInfo[int.Parse(pathInfo[pathInfo.Count - 1])];
    }
    public override void Save()
    {
        // Don't bother saving if blank?
        if (allData.Length < 1){return;}
        dataPath = Application.persistentDataPath + "/" + filename;
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        if (!File.Exists(dataPath))
        {
            allData = "";
            mapInfo = new List<string>();
            pathInfo = new List<string>();
            return;
        }
        allData = File.ReadAllText(dataPath);
        dataList = allData.Split(delimiter).ToList();
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
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
            case "Map":
            mapInfo = value.Split(delimiter2).ToList();
            break;
            case "Path":
            pathInfo = value.Split(delimiter2).ToList();
            break;
        }
    }
    public void SaveMap(StSMap mapToSave)
    {
        allData = "";
        allData += "Map=" + String.Join(delimiter2, mapToSave.mapInfo) + delimiter;
        allData += "Path=" + String.Join(delimiter2, mapToSave.pathTaken) + delimiter;
        allData += "Ancient=" + mapToSave.floorAncient + delimiter;
        allData += "Boss=" + mapToSave.floorBoss + delimiter;
        Save();
    }
    public string LoadMap()
    {
        Load();
        return allData;
    }
}
