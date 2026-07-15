using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum MapTileTypes
{
    Plains,
    Forest,
    Mountain,
    Water,
    Pit,
    Wall,
    DeepWater,
    Desert,
    Volcano,
    Snow,
    SnowForest,
    SnowMountain,
    Oasis
}
public enum MapEffectTypes
{
    None,
    Thorns,
    Fire,
    ToxicMist,
    Fog,
    Ice,
    Oil,
    OilFire,
    Water,
    ElectrifiedWater
}
public enum MapBuildingTypes
{
    None,
    Road,
    Castle,
    Tower,
    Bridge
}
public enum MapBorderTypes
{
    None,
    Wall,
    Thorns,
    Fire,
    ToxicMist,
    Fog,
    Ice,
    Oil,
    OilFire,
    Water,
    ElectrifiedWater
}
public enum BorderDirection
{
    NorthEast,
    East,
    SouthEast,
    SouthWest,
    West,
    NorthWest
}
[System.Serializable]
public class TileBorders
{
    public MapBorderTypes northEast = MapBorderTypes.None;
    public MapBorderTypes east = MapBorderTypes.None;
    public MapBorderTypes southEast = MapBorderTypes.None;
    public MapBorderTypes southWest = MapBorderTypes.None;
    public MapBorderTypes west = MapBorderTypes.None;
    public MapBorderTypes northWest = MapBorderTypes.None;
    public void UpdateBorder(string border, string direction)
    {
        MapBorderTypes newBorder = Enum.Parse<MapBorderTypes>(border);
        switch (direction)
        {
            case "All":
            northEast = newBorder;
            east = newBorder;
            southEast = newBorder;
            southWest = newBorder;
            west = newBorder;
            northWest = newBorder;
            break;
            case "AllWest":
            southWest = newBorder;
            west = newBorder;
            northWest = newBorder;
            break;
            case "AllEast":
            northEast = newBorder;
            east = newBorder;
            southEast = newBorder;
            break;
            case "AllNorth":
            northEast = newBorder;
            northWest = newBorder;
            break;
            case "AllSouth":
            southEast = newBorder;
            southWest = newBorder;
            break;
            case "NorthEast":
            northEast = newBorder;
            break;
            case "East":
            east = newBorder;
            break;
            case "SouthEast":
            southEast = newBorder;
            break;
            case "SouthWest":
            southWest = newBorder;
            break;
            case "West":
            west = newBorder;
            break;
            case "NorthWest":
            northWest = newBorder;
            break;
        }
    }
    public string ReturnBorderString()
    {
        return northEast.ToString() + "|" + east.ToString() + "|" + southEast.ToString() + "|" + southWest.ToString() + "|" + west.ToString() + "|" + northWest.ToString();
    }
}
[CreateAssetMenu(fileName = "CustomMapAsset", menuName = "ScriptableObjects/DataObjects/CustomMapAsset", order = 1)]
public class CustomMapAsset : ScriptableObject
{
    public int width = 15;
    public int height = 15;
    public string mapName;
    public MapTileTypes[] tiles = new MapTileTypes[225];
    public MapEffectTypes[] effects = new MapEffectTypes[225];
    public MapBuildingTypes[] buildings = new MapBuildingTypes[225];
    public TileBorders[] borders = new TileBorders[225];
    public int[] elevations = new int[225];
}
