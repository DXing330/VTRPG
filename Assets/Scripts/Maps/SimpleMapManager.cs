using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleMapManager : ClickTileManager
{
    public List<MapTile> mapTiles;
    public List<int> currentTiles;
    public int mapSize;
    public List<string> mapInfo;
    public virtual void SetMapInfo(List<string> newMapInfo)
    {
        mapInfo = new List<string>(newMapInfo);
        // Determine Map Size.
        for (int i = 1; i < mapInfo.Count / 2; i++)
        {
            if (i * i >= mapInfo.Count)
            {
                mapSize = i;
                break;
            }
        }
    }
}
