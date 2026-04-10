using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FactionMapData", menuName = "ScriptableObjects/FactionObjects/FactionMapData", order = 1)]
public class FactionMapData : SavedData
{
    public MapUtility mapUtility;
    public string delimiterTwo;

    public void SaveMap(FactionMap map)
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = "";
        allData += String.Join(delimiterTwo, map.GetMapInfo()) + delimiter;
        allData += String.Join(delimiterTwo, map.GetTileBuildings()) + delimiter;
        allData += String.Join(delimiterTwo, map.GetLuxuryTiles()) + delimiter;
        allData += String.Join(delimiterTwo, map.GetTileOutputs()) + delimiter;
        allData += String.Join(delimiterTwo, map.GetActorTiles()) + delimiter;
        allData += String.Join(delimiterTwo, map.GetHighlights()) + delimiter;
        File.WriteAllText(dataPath, allData);
    }

    public StatDatabase zoneTileMappings;
    public StatDatabase luxuryTileMappings;
    public StatDatabase tileMoveCosts;
    public int cutInto;
    public string defaultZoneLayout;

    protected void GenerateZone(List<string> tiles, int sRow, int sCol, int size, int mapSize, string zoneType)
    {
        int initialCol = sCol;
        int currentRow = sRow;
        int currentCol = sCol;
        string[] zoneTiles = zoneTileMappings.ReturnValue(zoneType).Split("|");
        for (int i = 0; i < size * size; i++)
        {
            tiles[mapUtility.ReturnTileNumberFromRowCol(currentRow, currentCol, mapSize)] = zoneTiles[UnityEngine.Random.Range(0, zoneTiles.Length)];
            currentCol += 1;
            if (currentCol == initialCol + size)
            {
                currentCol = initialCol;
                currentRow += 1;
            }
        }
    }

    protected void GenerateLuxury(FactionMap map, string luxury)
    {
        List<string> tileTypes = luxuryTileMappings.ReturnValue(luxury).Split("|").ToList();
        List<int> possibleTiles = map.ReturnTileNumbersOfTileTypes(tileTypes);
        if (possibleTiles.Count <= 0){return;}
        map.SetLuxuryTile(possibleTiles[UnityEngine.Random.Range(0, possibleTiles.Count)], luxury);
    }

    public void GenerateNewMap(FactionMap map)
    {
        int mapSize = map.mapSize;
        int row = 0;
        int col = 0;
        int shift = mapSize / cutInto;
        // Generate tiles.
        string[] defaultZoneOrder = defaultZoneLayout.Split("|");
        List<string> newMapInfo = map.ReturnEmptyList();
        for (int i = 0; i < cutInto * cutInto; i++)
        {
            GenerateZone(newMapInfo, row, col, shift, mapSize, defaultZoneOrder[i]);
            col += shift;
            if (col >= mapSize)
            {
                col = 0;
                row += shift;
            }
        }
        map.SetMapInfo(newMapInfo);
        UpdateMapMoveCosts(map);
        // Generate luxuries.
        map.ResetLuxuryTiles();
        for (int i = 0; i < luxuryTileMappings.GetAllKeys().Count; i++)
        {
            GenerateLuxury(map, luxuryTileMappings.ReturnKeyAtIndex(i));
        }
        // Reset buildings.
        map.ResetTileBuildings();
        // Reset highlights.
        map.ResetHighlights();
        // Reset actors.
        map.ResetActorTiles();
        // Make the starting cities.
        map.cities.GenerateStartingCities();
        map.RefreshAllTileOutputs();
    }

    public void LoadMap(FactionMap map)
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        if (File.Exists(dataPath)){allData = File.ReadAllText(dataPath);}
        else
        {
            GenerateNewMap(map);
            return;
        }
        dataList = allData.Split(delimiter).ToList();
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadMapStat(map, dataList[i], i);
        }
    }

    protected void UpdateMapMoveCosts(FactionMap map)
    {
        map.ResetMoveCosts();
        for (int i = 0; i < map.mapInfo.Count; i++)
        {
            map.moveCosts.Add(int.Parse(tileMoveCosts.ReturnValue(map.mapInfo[i])));
        }
    }

    protected void LoadMapStat(FactionMap map, string stat, int index)
    {
        switch (index)
        {
            default:
            break;
            case 0:
            map.SetMapInfo(stat.Split(delimiterTwo).ToList());
            UpdateMapMoveCosts(map);
            break;
            case 1:
            map.SetTileBuildings(stat.Split(delimiterTwo).ToList());
            break;
            case 2:
            map.SetLuxuryTiles(stat.Split(delimiterTwo).ToList());
            break;
            case 3:
            map.SetTileOutputs(stat.Split(delimiterTwo).ToList());
            break;
            case 4:
            map.SetActorTiles(stat.Split(delimiterTwo).ToList());
            break;
            case 5:
            map.SetHighlightedTiles(stat.Split(delimiterTwo).ToList());
            break;
        }
    }
}
