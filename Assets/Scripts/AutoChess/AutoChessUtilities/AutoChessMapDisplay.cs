using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessMapDisplay : MapManager
{
    public bool PVP = false;
    public int GetCastleTile()
    {
        int column = 0; // Left Side.
        int row = mapSize / 2; // Middle.
        int castleTile = mapUtility.ReturnTileNumberFromRowCol(row, column, mapSize);
        return castleTile;
    }
    public void DisplayMap(AutoChessMapAsset newMap)
    {
        // Display All Tiles.
        mapInfo.Clear();
        for (int i = 0; i < newMap.tiles.Length; i++)
        {
            mapInfo.Add(newMap.tiles[i].ToString());
        }
        UpdateMap();
        if (!PVP)
        {
            mapTiles[GetCastleTile()].UpdateText("Castle");
        }
    }
}
