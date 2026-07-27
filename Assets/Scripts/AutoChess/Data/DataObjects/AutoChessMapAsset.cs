using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StartingMapTile
{
    Plains,
    Forest,
    Water,
    Mountain,
    Pit
}

[CreateAssetMenu(fileName = "AutoChessMapAsset", menuName = "ScriptableObjects/AutoChessDataObjects/AutoChessMapAsset", order = 1)]
public class AutoChessMapAsset : ScriptableObject
{
    public int gridSize = 7;
    public string mapName;
    public StartingMapTile[] tiles;
    protected void OnValidate()
    {
        int size = gridSize * gridSize;
        if (tiles == null || tiles.Length != size)
        {
            System.Array.Resize(ref tiles, size);
        }
    }
}
