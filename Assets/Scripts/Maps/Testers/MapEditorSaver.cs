using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SavedMaps", menuName = "ScriptableObjects/DataContainers/SavedData/SavedMaps", order = 1)]
public class MapEditorSaver : SavedData
{
    public List<string> savedKeys;
    public bool AddKey(string newKey)
    {
        if (savedKeys.Contains(newKey)){return false;}
        savedKeys.Add(newKey);
        return true;
    }
    public void DeleteKey(string key)
    {
        if (!KeyExists(key)){return;}
        savedKeys.Remove(key);
        SaveKeys();
        // Delete the file.
        dataPath = Application.persistentDataPath + "/" + filename + currentMapName;
        if (File.Exists(dataPath))
        {
            File.Delete(dataPath);
            Debug.Log("File deleted!");
        }
    }
    public bool KeyExists(string key)
    {
        return savedKeys.Contains(key);
    }
    public void SaveKeys()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = String.Join(delimiter, savedKeys);
        File.WriteAllText(dataPath, allData);
    }
    public void LoadKeys()
    {
        savedKeys.Clear();
        dataPath = Application.persistentDataPath + "/" + filename;
        if (File.Exists(dataPath)){allData = File.ReadAllText(dataPath);}
        else{return;}
        savedKeys = allData.Split(delimiter).ToList();
        utility.RemoveEmptyListItems(savedKeys);
    }
    public string currentMapName;
    public string delimiterTwo;
    public void SetCurrentMap(MapEditor map, string newInfo)
    {
        currentMapName = newInfo;
        LoadMap(map);
    }
    public void SaveMapToName(MapEditor map, string newInfo)
    {
        currentMapName = newInfo;
        SaveMap(map);
    }
    public string ReturnSaveMapDataString(MapEditor map)
    {
        string data = "";
        data += "Tiles=" + String.Join(delimiterTwo, map.cMapInfo) + delimiter;
        data += "TEffects=" + String.Join(delimiterTwo, map.cTerrainEffects) + delimiter;
        data += "Elevations=" + String.Join(delimiterTwo, map.cTileElevations) + delimiter;
        data += "Buildings=" + String.Join(delimiterTwo, map.cBuildings) + delimiter;
        data += "Borders=" + String.Join(delimiterTwo, map.cBorders);
        return data;
    }
    public void SaveMap(MapEditor map)
    {
        currentMapName = map.cMap;
        if (AddKey(currentMapName))
        {
            SaveKeys();
        }
        dataPath = Application.persistentDataPath + "/" + filename + currentMapName;
        allData = ReturnSaveMapDataString(map);
        File.WriteAllText(dataPath, allData);
    }
    public void LoadMap(MapEditor map)
    {
        dataPath = Application.persistentDataPath + "/" + filename + currentMapName;
        if (File.Exists(dataPath)){allData = File.ReadAllText(dataPath);}
        else
        {
            map.InitializeNewMap();
            return;
        }
        dataList = allData.Split(delimiter).ToList();
        for (int i = 0; i < dataList.Count; i++)
        {
            if (!LoadMapStat(dataList[i], map))
            {
                map.InitializeNewMap();
                return;
            }
        }
        map.UndoEdits();
    }
    protected virtual bool LoadMapStat(string data, MapEditor map)
    {
        string[] blocks = data.Split("=");
        if (blocks.Length < 2){return false;}
        string value = blocks[1];
        switch (blocks[0])
        {
            default:
                return false;
            case "Tiles":
                map.mapInfo = value.Split(delimiterTwo).ToList();
                return true;
            case "TEffects":
                map.terrainEffects = value.Split(delimiterTwo).ToList();
                return true;
            case "Elevations":
                map.tileElevations = value.Split(delimiterTwo).ToList();
                return true;
            case "Buildings":
                map.buildings = value.Split(delimiterTwo).ToList();
                return true;
            case "Borders":
                map.borders = value.Split(delimiterTwo).ToList();
                return true;
        }
    }
}
