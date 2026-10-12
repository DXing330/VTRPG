using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Very Similar To The Battle Map.
// Use Start/End For Tile/Effect/Weather/Etc. Each Turn.
public class MysteryDungeonBattleMap : BattleMap
{
    // Each Tile Is Either A Room (Room #) Or Not (-1). 
    public List<int> tileRooms;
    public override List<int> GetRoomTiles(int startTile)
    {
        int room = tileRooms[startTile];
        List<int> roomTiles = new List<int>();
        for (int i = 0; i < tileRooms.Count; i++)
        {
            if (tileRooms[i] == room)
            {
                roomTiles.Add(i);
            }
        }
        return roomTiles;
    }
    // Track Which Tiles Were Explored, So The Map Is Built While Exploring.
    public List<int> tileExplored;
    public List<int> visibleTiles;
    public void UpdateVisibleTiles()
    {
        visibleTiles = new List<int>();
        // TODO Get Tiles In A Squared Based On Grid Size / 2.
        int playerRow = mapUtility.GetRow(playerLocation, mapSize);
        int playerCol = mapUtility.GetColumn(playerLocation, mapSize);
        // 9x9 around player.
        for (int row = playerRow - 4; row <= playerRow + 4; row++)
        {
            for (int col = playerCol - 4; col <= playerCol + 4; col++)
            {
                if (row < 0 || row >= mapSize ||
                    col < 0 || col >= mapSize)
                {
                    continue;
                }
                int tile = row * mapSize + col;
                visibleTiles.Add(tile);
            }
        }
        int room = tileRooms[playerLocation];
        if (room >= 0)
        {
            visibleTiles.AddRange(GetRoomTiles(playerLocation));
        }
        visibleTiles = visibleTiles.Distinct().ToList();
        foreach (int tile in visibleTiles)
        {
            tileExplored[tile] = 1;
        }
    }
    // Track Player Location.
    public int playerLocation;
    // Track Stairs.
    public int stairsLocation;
    // Track Traps.
    public List<string> traps;
    public List<int> trapLocations;
    // Track Treasures.
    public List<string> items;
    public List<int> itemLocations;
    public void InitializeMapFromGenerator(MysteryDungeonGenerator generator, string dungeonType)
    {
        mapInfo.Clear();
        foreach (int tile in generator.allTiles)
        {
            if (tile == 0)
            {
                mapInfo.Add(GetRandomFloorTerrain(dungeonType));
            }
            else
            {
                mapInfo.Add(GetWallTerrain(dungeonType));
            }
        }
        UpdateMapSize();
        tileRooms = new List<int>(generator.tileRooms);
        playerLocation = generator.startLocation;
        stairsLocation = generator.stairsLocation;
        itemLocations = new List<int>(generator.itemLocations);
        trapLocations = new List<int>(generator.trapLocations);
        // TODO Decide What Items/Traps Exist.
        items = new List<string>();
        traps = new List<string>();
        tileExplored = Enumerable.Repeat(0, mapInfo.Count).ToList();
        UpdateVisibleTiles();
        // TODO Random Weather/Time Based On MapType.

    }
    protected string GetRandomFloorTerrain(string dungeonType)
    {
        switch (dungeonType)
        {
            case "Forest":
                return new[] { "Plains", "Forest", "Water" }[UnityEngine.Random.Range(0, 3)];
            case "Mountain":
                return new[] { "Plains", "Mountain", "Water" }[UnityEngine.Random.Range(0, 3)];
            case "Water":
                return new[] { "Plains", "Water" }[UnityEngine.Random.Range(0, 2)];
            case "Underground":
                return "Plains";
        }
        return "Plains";
    }
    protected string GetWallTerrain(string dungeonType)
    {
        switch (dungeonType)
        {
            case "Forest":
                return "DeepWater";
            case "Mountain":
                return "Pit";
            case "Water":
                return "DeepWater";
            case "Underground":
                return "Pit";
        }
        return "Pit";
    }
    public void LoadMapStateFromString(string savedState)
    {
        // TODO Clear Buildings + Etc.
        if (string.IsNullOrEmpty(savedState))
            return;
        string[] sections = savedState.Split('|');
        foreach (string section in sections)
        {
            if (string.IsNullOrEmpty(section))
                continue;
            string[] keyValue = section.Split(new char[] { '=' }, 2);
            if (keyValue.Length != 2)
                continue;
            string key = keyValue[0];
            string value = keyValue[1];
            switch (key)
            {
                case "Tiles":
                    mapInfo = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    break;
                case "TerrainEffects":
                    terrainEffectTiles = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    break;
                case "Weather":
                    weather = value;
                    break;
                case "Time":
                    time = value;
                    break;
                case "RoomInfo":
                    tileRooms = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                    break;
                case "Player":
                    playerLocation = int.Parse(value);
                    break;
                case "Stairs":
                    stairsLocation = int.Parse(value);
                    break;
                case "Traps":
                    traps = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    break;
                case "TrapLocations":
                    trapLocations = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                    break;
                case "Items":
                    items = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    break;
                case "ItemLocations":
                    itemLocations = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                    break;
            }
        }
    }
    public string ReturnMapStateString()
    {
        string mapState = "";
        mapState += "Tiles=" + String.Join(",", mapInfo) + "|";
        mapState += "TerrainEffects=" + String.Join(",", terrainEffectTiles) + "|";
        mapState += "Weather=" + weather + "|";
        mapState += "Time=" + time + "|";
        mapState += "RoomInfo=" + String.Join(",", tileRooms) + "|";
        mapState += "Player=" + playerLocation + "|";
        mapState += "Stairs=" + stairsLocation + "|";
        mapState += "Traps=" + String.Join(",", traps) + "|";
        mapState += "TrapLocations=" + String.Join(",", trapLocations) + "|";
        mapState += "Items=" + String.Join(",", items) + "|";
        mapState += "ItemLocations=" + String.Join(",", itemLocations) + "|";
        return mapState;
    }
    public MysteryDungeonMinimap miniMap;
    // TODO Locked Rooms?
    protected override void UpdateCurrentTiles()
    {
        currentTiles = currentTileManager.GetCurrentTilesFromCenter(playerLocation, mapSize, gridSize);
    }
    public override void UpdateMap()
    {
        // Tile Layer.
        BaseUpdateMap();
        // TODO Item Layer (Replaced Building Layer).
        // TODO Actor Layer.
        // TODO Terrain Layer.
        // TODO Highlight Layer.
        // TODO Aura Layer.
        // Update The Minimap Based On Explored/Visible Tiles.
    }
}
