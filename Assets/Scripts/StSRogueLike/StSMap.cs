using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StSMap : SimpleMapManager
{
    // MAP STUFF
    public RectMapUtility mapUtility;
    public StSMapUtility rogueLikeMapUtility;
    public int columns = 17;
    public int rows = 9;
    public void UpdateCurrentTiles()
    {
        currentTiles.Clear();
        for (int i = 0; i < columns * rows; i++)
        {
            currentTiles.Add(i);
        }
    }
    public RNGUtility mapRNGSeed;
    public List<string> tileTypes;
    public List<string> nonRepeatableTileTypes;
    public List<int> tileWeights;
    public string RandomTileType(string except = "", bool exceptAll = false)
    {
        if (exceptAll)
        {
            int exceptRng = mapRNGSeed.Range(0, 2);
            if (exceptRng == 0){return tileTypes[0];}
            return tileTypes[1];
        }
        int rng = mapRNGSeed.Range(0, tileWeights.Sum());
        string tileType = "";
        for (int i = 0; i < tileTypes.Count; i++)
        {
            if (rng < tileWeights[i])
            {
                tileType = tileTypes[i];
                break;
            }
            else
            {
                rng -= tileWeights[i];
            }
        }
        if (tileType == except && nonRepeatableTileTypes.Contains(except))
        {
            return RandomTileType(except);
        }
        return tileType;
    }
    public List<MapDisplayer> mapDisplayers;
    public GameObject blackScreen;
    public SpriteContainer bossSprites;
    public Image bossImage;
    public Image ancientImage;
    public int GetFloor()
    {
        return manager.gameState.GetFloor();
    }
    public List<string> pathInfo; // For Displaying The Path.
    public List<string> pathTaken;
    public int GetCurrentTile()
    {
        if (pathTaken.Count < 1){return -1;}
        return utility.SafeParseInt(pathTaken[pathTaken.Count - 1], -1);
    }
    public void MoveToTile(int tileNumber)
    {
        pathTaken.Add(tileNumber.ToString());
        Save();
        manager.MoveToTile(mapInfo[tileNumber]);
    }
    // TODO Some tiles are quest/special tiles?
    public List<string> pathMarkings;
    // GAME LOGIC STUFF
    public GeneralUtility utility;
    public StSStateManager manager;
    public StSMapSaveData savedData;
    public string delimiter = "|";
    public string delimiter2 = ",";
    public string floorAncient;
    public void GetFloorAncient()
    {
        // Neow Or Nothing For Now?
        floorAncient = "Whale";
    }
    public string floorBoss;
    public void GetFloorBoss()
    {
        floorBoss = manager.enemyTracker.floorBoss;
    }
    public void NewGame()
    {
        GeneratePaths();
        Save();
    }
    public void Save()
    {
        savedData.SaveMap(this);
    }
    public void Load()
    {
        string loadedData = savedData.LoadMap();
        string[] dataBlocks = loadedData.Split(delimiter);
        for (int i = 0; i < dataBlocks.Length; i++)
        {
            LoadStat(dataBlocks[i]);
        }
        UpdateMap();
    }
    protected void LoadStat(string statData)
    {
        string[] data = statData.Split("=");
        if (data.Length < 2){return;}
        string key = data[0];
        string value = data[1];
        switch (key)
        {
            case "Map":
                mapInfo = value.Split(delimiter2).ToList();
                break;
            case "Path":
                pathTaken = value.Split(delimiter2).ToList();
                break;
            case "Ancient":
                floorAncient = value;
                break;
            case "Boss":
                floorBoss = value;
                break;
        }
    }
    // DISPLAY FUNCTIONS
    public void UpdateMap()
    {
        UpdateCurrentTiles();
        mapDisplayers[0].DisplayCurrentTiles(mapTiles, mapInfo, currentTiles);
        bossImage.sprite = bossSprites.SpriteDictionary(floorBoss);
        ancientImage.sprite = bossSprites.SpriteDictionary(floorAncient);
        UpdateDirectionalArrows();
        UpdatePathTaken();
        // Update Possible Next Tiles.
    }
    protected void UpdateDirectionalArrows()
    {
        int nextTile = -1;
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].ResetDirectionArrows();
            if (mapInfo[i] == ""){continue;}
            nextTile = mapUtility.GetUpRightDiagonal(i, rows, columns);
            if (nextTile != i && nextTile >= 0 && mapInfo[nextTile] != "")
            {
                mapTiles[i].ActivateDirectionArrow(1);
            }
            nextTile = mapUtility.GetDownRightDiagonal(i, rows, columns);
            if (nextTile != i && nextTile >= 0 && mapInfo[nextTile] != "")
            {
                mapTiles[i].ActivateDirectionArrow(2);
            }
        }
    }
    protected void UpdatePathTaken()
    {
        pathInfo = new List<string>();
        for (int i = 0; i < mapInfo.Count; i++)
        {
            pathInfo.Add("");
        }
        if (pathTaken.Count < 1 || GetCurrentTile() < 0)
        {
            mapDisplayers[1].DisplayCurrentTiles(mapTiles, pathInfo, currentTiles);
            return;
        }
        // Latest Tile Is Current Location.
        pathInfo[GetCurrentTile()] = "O";
        // Other Tiles Are Already Passed.
        for (int i = pathTaken.Count - 2; i >= 0; i--)
        {
            pathInfo[int.Parse(pathTaken[i])] = "X";
        }
        mapDisplayers[1].DisplayCurrentTiles(mapTiles, pathInfo, currentTiles);
    }
    // GENERATION
    protected void ResetAll()
    {
        UpdateCurrentTiles();
        mapInfo = new List<string>();
        for (int i = 0; i < currentTiles.Count; i++)
        {
            mapInfo.Add("");
        }
        pathTaken.Clear();
    }
    [ContextMenu("GeneratePaths")]
    public void GeneratePaths()
    {
        ResetAll();
        for (int i = 0; i < rows/2; i++)
        {
            GeneratePath(i);
        }
        // Make sure no two elites/shops/rests in a row.
        // Go Through Each Column, Right To Left.
        for (int i = columns - 2; i >= 1; i--)
        {
            // Go Through Each Row.
            for (int j = 0; j < rows; j++)
            {
                int tileNumber = mapUtility.ReturnTileNumberFromRowCol(j, i, columns);
                // Only look at those that might be problematic.
                if (!nonRepeatableTileTypes.Contains(mapInfo[tileNumber])){continue;}
                // Look To The Left.
                List<int> leftAdjacent = mapUtility.GetLeftDiagonalAdjacentTiles(tileNumber, rows, columns);
                for (int h = 0; h < leftAdjacent.Count; h++)
                {
                    if (mapInfo[leftAdjacent[h]] == mapInfo[tileNumber])
                    {
                        Debug.Log("Original:" + mapInfo[tileNumber]);
                        // Turn Into An Event Or Enemy.
                        mapInfo[leftAdjacent[h]] = RandomTileType("", true);
                        Debug.Log("New:" + mapInfo[leftAdjacent[h]]);
                    }
                }
            }
        }
        GetFloorBoss();
        GetFloorAncient();
        UpdateMap();
    }
    [ContextMenu("TestGeneratePath")]
    protected void TestGenPath()
    {
        ResetAll();
        GeneratePath();
        UpdateMap();
    }
    public void GeneratePath(int startRow = -1)
    {
        // Random start.
        if (startRow < 0)
        {
            startRow = mapRNGSeed.Range(0, rows);
        }
        else
        {
            // 0 - 1, 1 - 3, 2 - 5, 3 - 7
            startRow = startRow * 2 + 1;
        }
        // Get path.
        List<int> pathTiles = rogueLikeMapUtility.CreatePath(startRow, rows, columns);
        // Enable path.
        // End = Rest.
        mapInfo[pathTiles[pathTiles.Count - 1]] = "Rest";
        string tileType = RandomTileType();
        for (int i = pathTiles.Count - 2; i >= 0; i--)
        {
            if (mapInfo[pathTiles[i + 1]] == "Enemy" || mapInfo[pathTiles[i + 1]] == "Event")
            {
                tileType = RandomTileType();
            }
            else
            {
                tileType = RandomTileType(mapInfo[pathTiles[i + 1]]);
            }
            mapInfo[pathTiles[i]] = tileType;
            mapTiles[pathTiles[i]].EnableLayer();
        }
        // Some path values are fixed.
        // Middle = Treasure.
        mapInfo[pathTiles[pathTiles.Count / 2]] = "Treasure";
        // Start = Enemy.
        mapInfo[pathTiles[0]] = "Enemy";
    }
    // PLAYER INTERACTION
    public override void ClickOnTile(int tileNumber)
    {
        // Can't Click Empty Tiles.
        if (mapInfo[tileNumber] == ""){return;}
        int currentTile = GetCurrentTile();
        // Just Starting.
        if (currentTile == -1)
        {
            // Check If Tile Is In Column 0;
            if (mapUtility.GetColumn(tileNumber, rows, columns) != 0){return;}
            MoveToTile(tileNumber);
        }
        // Continuing Path.
        // Can only move to adjacent diagonal tiles.
        else if (mapUtility.GetRightDiagonalAdjacentTiles(currentTile, rows, columns).Contains(tileNumber))
        {
            MoveToTile(tileNumber);
        }
        UpdateMap();
    }
}
