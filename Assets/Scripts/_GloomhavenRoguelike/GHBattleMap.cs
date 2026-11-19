using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Stores Map State
public class GHBattleMap : BattleMap
{
    // List Of Map Data, Should Be 9x9 Maps.
    public List<AutoChessMapAsset> mapInfoList;
    public AutoChessMapAsset currentMap = null;
    public void SetCurrentMap(AutoChessMapAsset newMap)
    {
        currentMap = newMap;
        LoadCurrentMapInfo();
    }
    public void LoadMapByName(string newMapName = "Basic")
    {
        for (int i = 0; i < mapInfoList.Count; i++)
        {
            if (mapInfoList[i].mapName == newMapName)
            {
                SetCurrentMap(mapInfoList[i]);
                return;
            }
        }
        // Default Map.
        SetCurrentMap(mapInfoList[0]);
    }
    // Map Includes Tiles/Terrain/PlayerSpawn/EnemySpawn
    // Weather/Time Is Randomized.
    protected void LoadCurrentMapInfo()
    {
        mapInfo = currentMap.GetTilesAsStrings();
        UpdateMap();
    }
}
