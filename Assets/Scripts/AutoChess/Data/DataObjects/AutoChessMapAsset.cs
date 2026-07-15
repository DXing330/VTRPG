using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StartingMapTile
{
    Plains,
    Forest,
    Water,
    Mountain
}

[CreateAssetMenu(fileName = "AutoChessMapAsset", menuName = "ScriptableObjects/AutoChessDataObjects/AutoChessMapAsset", order = 1)]
public class AutoChessMapAsset : ScriptableObject
{
    public string mapName;
    public StartingMapTile[] tiles = new StartingMapTile[49];
}
