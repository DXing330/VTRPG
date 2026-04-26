using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SavedMaps", menuName = "ScriptableObjects/DataContainers/SavedData/SavedMaps", order = 1)]
public class MapEditorSaver : SavedData
{
    public StatDatabase customMapDB;
    public string testSavedMapName;
    [ContextMenu("Show Saved Map")]
    public void ShowSavedMap()
    {
        Debug.Log(customMapDB.ReturnValue(testSavedMapName));
    }
    public virtual void DeleteKey(string key)
    {
        if (!KeyExists(key)){return;}
        customMapDB.RemoveKey(key);
    }
    public virtual bool KeyExists(string key)
    {
        return customMapDB.KeyExists(key);
    }
    public virtual List<string> GetAllKeys()
    {
        return customMapDB.GetAllKeys();
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
        allData = ReturnSaveMapDataString(map);
        customMapDB.UpsertValue(currentMapName, allData);
    }
    public void LoadMap(MapEditor map)
    {
        allData = customMapDB.ReturnValue(currentMapName);
        if (allData.Length <= 0)
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
