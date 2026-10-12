using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleMapManager : ClickTileManager
{
    public List<MapTile> mapTiles;
    public List<int> currentTiles;
    public int mapSize;
    public void UpdateMapSize()
    {
        mapSize = Mathf.RoundToInt(Mathf.Sqrt(mapInfo.Count));
    }
    public List<string> mapInfo;
    public virtual List<int> GetRoomTiles(int startTile)
    {
        List<int> roomTiles = new List<int>();
        for (int i = 0; i < mapInfo.Count; i++)
        {
            roomTiles.Add(i);
        }
        return roomTiles;
    }
    public virtual void SetMapInfo(List<string> newMapInfo)
    {
        mapInfo = new List<string>(newMapInfo);
        UpdateMapSize();
    }
}
