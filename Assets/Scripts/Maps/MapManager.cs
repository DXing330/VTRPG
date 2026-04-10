using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : SimpleMapManager
{
    public GeneralUtility utility;
    public LoadingScreen loadingScreen;
    public MapUtility mapUtility;
    public MapCurrentTiles currentTileManager;
    public MapMaker mapMaker;
    public List<MapDisplayer> mapDisplayers;
    [ContextMenu("Reset Tile Numbers")]
    public void ResetTileNumbers()
    {
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].SetTileNumber(i);
        }
    }
    [ContextMenu("Reset Text")]
    public void ResetText()
    {
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].UpdateText();
        }
    }
    [ContextMenu("Show Tile Numbers")]
    public void ShowTileNumbers()
    {
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].UpdateText(i.ToString());
        }
    }
    public bool showElevationSprite = false;
    public StatDatabase tileElevationMappings;
    public SpriteContainer elevationSprites;
    public List<int> mapElevations;
    // If the elevation difference is too large then basic melee attacks don't work.
    public int ReturnElevation(int tileNumber)
    {
        if (tileNumber < 0 || tileNumber >= mapElevations.Count)
        {
            return 0;
        }
        return mapElevations[tileNumber];
    }
    public int ReturnElevationDifference(int tileOne, int tileTwo)
    {
        return Mathf.Abs(mapTiles[tileOne].GetElevation() - mapTiles[tileTwo].GetElevation());
    }
    public int ReturnPosNegElvDiff(int tileOne, int tileTwo)
    {
        return mapTiles[tileOne].GetElevation() - mapTiles[tileTwo].GetElevation();
    }
    public virtual int ReturnClosestTileWithinElevationDifference(int start, int end, int maxElvDiff, List<int> moveCosts)
    {
        List<int> adjacentTiles = mapUtility.AdjacentTiles(end, mapSize);
        int target = end;
        int dist = mapSize * mapSize;
        for (int i = 0; i < adjacentTiles.Count; i++)
        {
            if (ReturnElevationDifference(adjacentTiles[i], end) > maxElvDiff)
            {
                continue;
            }
            if (mapUtility.DistanceBetweenTiles(adjacentTiles[i], start, mapSize) < dist)
            {
                dist = mapUtility.DistanceBetweenTiles(adjacentTiles[i], start, mapSize);
                target = adjacentTiles[i];
            }
        }
        return target;
    }
    public List<int> possibleElevation;
    public int RandomElevation(string tileType)
    {
        List<string> possibleElevations = tileElevationMappings.ReturnValue(tileType).Split("|").ToList();
        if (possibleElevations.Count < 2){return 0;}
        return int.Parse(possibleElevations[Random.Range(0, possibleElevations.Count)]);
    }
    public int MinElevation(string tileType)
    {
        int min = 3;
        List<string> possibleElevations = tileElevationMappings.ReturnValue(tileType).Split("|").ToList();
        for (int i = 0; i < possibleElevations.Count; i++)
        {
            if (int.Parse(possibleElevations[i]) < min)
            {
                min = int.Parse(possibleElevations[i]);
            }
        }
        return min;
    }
    public int MaxElevation(string tileType)
    {
        int max = -3;
        List<string> possibleElevations = tileElevationMappings.ReturnValue(tileType).Split("|").ToList();
        for (int i = 0; i < possibleElevations.Count; i++)
        {
            if (int.Parse(possibleElevations[i]) > max)
            {
                max = int.Parse(possibleElevations[i]);
            }
        }
        return max;
    }
    public int MiddleElevation(string tileType)
    {
        List<string> possibleElevations = tileElevationMappings.ReturnValue(tileType).Split("|").ToList();
        if (possibleElevations.Count < 2){return 0;}
        return int.Parse(possibleElevations[possibleElevations.Count / 2]);
    }
    public void InitializeElevations()
    {
        mapElevations = new List<int>();
        // Maybe make a pattern for elevations later.
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapElevations.Add(RandomElevation(mapInfo[i]));
            mapTiles[i].SetElevation(mapElevations[i]);
            if (showElevationSprite)
            {
                mapTiles[i].UpdateElevationSprite(elevationSprites.SpriteDictionary("E"+mapTiles[i].GetElevation().ToString()));
            }
        }
    }
    public SpriteContainer borderSprites;
    public void UpdateTileBorderSprites(int tileNumber)
    {
        List<string> borders = mapTiles[tileNumber].GetBorders();
        for (int i = 0; i < borders.Count; i++)
        {
            mapTiles[tileNumber].UpdateBorderImage(i, borderSprites.SpriteDictionary(borders[i]));
        }
    }
    public List<string> borderDetails;
    public void InitializeBorders()
    {
        borderDetails = new List<string>();
        for (int i = 0; i < mapTiles.Count; i++)
        {
            // For now just reset borders, we need to deal with making borders later.
            mapTiles[i].ResetBorders();
            borderDetails.Add(mapTiles[i].ReturnBorderString());
        }
    }
    public void UpdateBorders()
    {
        borderDetails = new List<string>();
        for (int i = 0; i < mapTiles.Count; i++)
        {
            // For now just reset borders, we need to deal with making borders later.
            borderDetails.Add(mapTiles[i].ReturnBorderString());
            UpdateTileBorderSprites(i);
        }
    }
    public string ReturnBorderFromTileDirection(int tile, int direction)
    {
        return mapTiles[tile].GetBorderInDirection(direction);
    }
    public int bordersPerTile = 2;
    [ContextMenu("Randomize Borders")]
    public void RandomizeBorders()
    {
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].ResetBorders();
            for (int j = 0; j < bordersPerTile; j++)
            {
                mapTiles[i].AddBorder(Random.Range(0, 6));
            }
        }
        UpdateBorders();
    }
    protected virtual void ResetAllLayers()
    {
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].UpdateText();
            mapTiles[i].DisableLayers();
        }
    }
    public List<string> GetMapInfo(){return mapInfo;}
    public void SetMapInfo(List<string> newInfo)
    {
        mapInfo = new List<string>(newInfo);
    }
    public virtual void InitializeMapInfo()
    {
        InitializeEmptyList();
        mapInfo = new List<string>(emptyList);
    }
    public List<int> ReturnTileNumbersOfTileType(string type)
    {
        List<int> tileNumbers = new List<int>();
        for (int i = 0; i < mapInfo.Count; i++)
        {
            if (mapInfo[i] == type)
            {
                tileNumbers.Add(i);
            }
        }
        return tileNumbers;
    }
    public List<int> ReturnTileNumbersOfTileTypes(List<string> tileTypes)
    {
        List<int> tileNumbers = new List<int>();
        for (int i = 0; i < mapInfo.Count; i++)
        {
            if (tileTypes.Contains(mapInfo[i]))
            {
                tileNumbers.Add(i);
            }
        }
        return tileNumbers;
    }
    public virtual int ReturnRandomTileOfTileTypes(List<string> tileTypes)
    {
        List<int> possibleNumbers = ReturnTileNumbersOfTileTypes(tileTypes);
        if (possibleNumbers.Count < 0)
        {
            return Random.Range(0, mapSize * mapSize);
        }
        return possibleNumbers[Random.Range(0, possibleNumbers.Count)];
    }
    public void SwitchTile(int tile1, int tile2)
    {
        string temp = mapInfo[tile1];
        mapInfo[tile1] = mapInfo[tile2];
        mapInfo[tile2] = temp;
        UpdateMap();
    }
    [System.NonSerialized]
    public List<string> emptyList;
    public List<string> ReturnEmptyList()
    {
        InitializeEmptyList();
        return new List<string>(emptyList);
    }
    protected virtual void InitializeEmptyList()
    {
        emptyList = new List<string>();
        for (int i = 0; i < mapSize * mapSize; i++)
        {
            emptyList.Add("");
        }
    }
    public int mapSize;
    public int MapMaxPartyCapacity()
    {
        return (mapSize * mapSize) / 3;
    }
    public int gridSize;
    public int centerTile;
    // Make hex to up-down-left-right
    // This is for pointy top only currently.
    public void RectangularMoveCenterTile(int direction)
    {
        int prevCenter = centerTile;
        // UP
        if (direction == 0 || direction == 5)
        {
            centerTile -= mapSize * 2;
        }
        // DOWN
        else if (direction == 2 || direction == 3)
        {
            centerTile += mapSize * 2;
        }
        // LEFT
        else if (direction == 1)
        {
            centerTile += 1;
        }
        // RIGHT
        else if (direction == 4)
        {
            centerTile -= 1;
        }
        if (centerTile < 0 || centerTile >= mapSize * mapSize)
        {
            centerTile = prevCenter;
            return;
        }
    }
    public void UpdateCenterTile(int newInfo, bool up = false)
    {
        centerTile = newInfo;
        // Can't move left or right by 1, have to move left/right by 2.
        if (mapUtility.flatTop && mapUtility.GetColumn(centerTile, mapSize) % 2 != 1)
        {
            centerTile += 1;
        }
        // Can't move up or down by 1, have to move up or down by 2.
        else if (!mapUtility.flatTop && mapUtility.GetRow(centerTile, mapSize) % 2 != 1)
        {
            if (up)
            {
                centerTile -= mapSize;
            }
            else
            {
                centerTile += mapSize;
            }
        }
    }

    protected virtual void Start()
    {
        UpdateMap();
    }

    public List<string> MakeRandomMap()
    {
        return mapMaker.MakeRandomMap(mapSize);
    }

    [ContextMenu("Get New Map")]
    public virtual void GetNewMap()
    {
        // Change this later.
        ResetAllLayers();
        mapInfo = MakeRandomMap();
        centerTile = mapUtility.DetermineCenterTile(mapSize);
        UpdateMap();
    }

    public virtual void GetNewMapFeatures(List<string> featuresAndPatterns, string baseTileType = "Plains")
    {
        mapInfo = mapMaker.MakeBasicMap(mapSize, baseTileType);
        for (int i = 0; i < featuresAndPatterns.Count; i++)
        {
            string[] fPSplit = featuresAndPatterns[i].Split(">>");
            if (fPSplit.Length < 2){continue;}
            mapInfo = mapMaker.AddFeature(mapInfo, fPSplit[0], fPSplit[1]);
        }
        UpdateMap();
    }

    public List<int> AllConnectedTilesOfSameType(int tileNumber)
    {
        List<int> connected = new List<int>();
        if (mapInfo[tileNumber] == "")
        {
            return connected;
        }
        string tileType = mapInfo[tileNumber];
        List<int> viewedTiles = new List<int>();
        List<int> queuedTiles = new List<int>();
        viewedTiles.Add(tileNumber);
        connected.Add(tileNumber);
        queuedTiles.Add(tileNumber);
        while (queuedTiles.Count > 0)
        {
            int currentTile = queuedTiles[0];
            List<int> adjacentTiles = mapUtility.AdjacentTiles(currentTile, mapSize);
            for (int i = 0; i < adjacentTiles.Count; i++)
            {
                // Only look at new tiles.
                if (viewedTiles.Contains(adjacentTiles[i])){continue;}
                viewedTiles.Add(adjacentTiles[i]);
                if (mapInfo[adjacentTiles[i]] == tileType)
                {
                    connected.Add(adjacentTiles[i]);
                    queuedTiles.Add(adjacentTiles[i]);
                }
            }
            queuedTiles.RemoveAt(0);
        }
        return connected;
    }

    // Probably never use this. Moving the map should happen automatically as the player icon moves.
    // This is used to move the map up/down/left/right.
    public void MoveMap(int direction)
    {
        RectangularMoveCenterTile(direction);
        UpdateMap();
    }

    protected virtual void UpdateCurrentTiles()
    {
        currentTiles = currentTileManager.GetCurrentTilesFromCenter(centerTile, mapSize, gridSize);
    }

    public List<string> ReturnMapInfoPlusElevation()
    {
        List<string> infoAndElevation = new List<string>(mapInfo);
        for (int i = 0; i < mapInfo.Count; i++)
        {
            infoAndElevation[i] += "E"+mapTiles[i].GetElevation();
        }
        return infoAndElevation;
    }

    [ContextMenu("Test Update Map")]
    public void TestUpdateMap()
    {
        UpdateMap();
    }

    [ContextMenu("Test Show Elevation")]
    public void TestShowElevation()
    {
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].UpdateElevationSprite(elevationSprites.SpriteDictionary("E"+mapTiles[i].GetElevation().ToString()));
        }
    }

    public virtual void UpdateMap()
    {
        UpdateCurrentTiles();
        mapDisplayers[0].DisplayCurrentTiles(mapTiles, mapInfo, currentTiles);
    }

    /*[ContextMenu("Move 0")]
    public void Move0(){MoveMap(0);}

    [ContextMenu("Move 1")]
    public void Move1(){MoveMap(1);}

    [ContextMenu("Move 2")]
    public void Move2(){MoveMap(2);}

    [ContextMenu("Move 3")]
    public void Move3(){MoveMap(3);}

    [ContextMenu("Move 4")]
    public void Move4(){MoveMap(4);}

    [ContextMenu("Move 5")]
    public void Move5(){MoveMap(5);}*/
}
