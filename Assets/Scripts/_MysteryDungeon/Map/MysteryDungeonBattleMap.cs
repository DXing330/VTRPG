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
    public MysteryDungeonManager MDGeneralManager;
    public MysteryDungeonMapUtility MDMapUtility;
    public StatDatabase dungeonTypeSubTerrains;
    public StatDatabase dungeonTypeWeathers;
    public StatDatabase dungeonItems;
    // Each Tile Is Either A Room (Room #) Or Not (-1).
    [System.NonSerialized]
    public List<int> tileRooms = new();
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
    [System.NonSerialized]
    public List<int> tileExplored = new();
    public List<int> visibleTiles;
    public List<int> GetVisibleTiles(){return visibleTiles;}
    public void UpdateVisibleTiles()
    {
        visibleTiles = new List<int>();
        if (tileExplored.Count < mapInfo.Count)
        {
            tileExplored = Enumerable.Repeat(0, mapInfo.Count).ToList();
        }
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
    public void UpdatePlayerLocation(int newLocation)
    {
        playerLocation = newLocation;
        if (playerLocation == stairsLocation)
        {
            MDGeneralManager.NewFloor();
            return;
        }
        UpdateMap();
    }
    public int GetPlayerLocation(){return playerLocation;}
    // Track Stairs.
    public int stairsLocation;
    public int GetStairsLocations(){return stairsLocation;}
    // Track Traps.
    public List<string> traps;
    public List<int> trapLocations;
    // Track Treasures.
    public List<string> items;
    public List<int> itemLocations;
    public void InitializeMapFromGenerator(MysteryDungeonGenerator generator, string dungeonType)
    {
        mapInfo.Clear();
        tileRooms = new List<int>(generator.tileRooms);
        for (int i = 0; i < generator.allTiles.Count; i++)
        {
            if (generator.allTiles[i] == 1)
            {
                mapInfo.Add(GetWallTerrain(dungeonType));
            }
            else
            {
                // Start With A Plains Default.
                mapInfo.Add("Plains");
            }
        }
        UpdateMapSize();
        // Resets Terrain/Elevations/Borders/Buildings
        ResetAll();
        playerLocation = generator.startLocation;
        stairsLocation = generator.stairsLocation;
        itemLocations = new List<int>(generator.itemLocations);
        trapLocations = new List<int>(generator.trapLocations);
        // TODO Decide What Items/Traps Exist.
        items = new List<string>();
        traps = new List<string>();
        tileExplored = Enumerable.Repeat(0, mapInfo.Count).ToList();
        int timeRNG = UnityEngine.Random.Range(0, 24);
        if (timeRNG < 16)
        {
            SetTime("Day");
        }
        else
        {
            SetTime("Night");
        }
        string[] possibleWeather = dungeonTypeWeathers.ReturnValue(dungeonType).Split("|");
        SetWeather(possibleWeather[UnityEngine.Random.Range(0, possibleWeather.Length)]);
        // Group only walkable tiles that belong to a room.
        Dictionary<int, List<int>> roomFloorTiles =
        new Dictionary<int, List<int>>();
        for (int tile = 0; tile < generator.allTiles.Count; tile++)
        {
            if (generator.allTiles[tile] != 0)
                continue;
            int room = tileRooms[tile];
            // Room ID -1 means hallway or other non-room floor.
            if (room < 0)
                continue;
            if (!roomFloorTiles.ContainsKey(room))
            {
                roomFloorTiles[room] = new List<int>();
            }
            roomFloorTiles[room].Add(tile);
        }
        // Second pass: give each room its own biome and generate clustered terrain inside that room.
        foreach (var room in roomFloorTiles)
        {
            CreateRoomBiome(dungeonType, room.Value);
        }
        UpdateMoveCostManager();
    }
    protected void CreateRoomBiome(string dungeonType, List<int> roomTiles)
    {
        string[] dungeonTypeBiomes = dungeonTypeSubTerrains.ReturnValue(dungeonType).Split("|");
        string mainBiome = dungeonTypeBiomes[UnityEngine.Random.Range(0, dungeonTypeBiomes.Length)];
        string subBiome = dungeonTypeBiomes[UnityEngine.Random.Range(0, dungeonTypeBiomes.Length)];
        // TODO Make The Biome Clusters.
        HashSet<int> roomSet = new HashSet<int>(roomTiles);
        HashSet<int> changedTiles = new HashSet<int>();
        int patchCount = Mathf.Max(1, roomTiles.Count / 12);
        for (int patch = 0; patch < patchCount; patch++)
        {
            // Choose a seed that has not already been changed.
            List<int> availableSeeds = roomTiles.Where(tile => !changedTiles.Contains(tile)).ToList();
            if (availableSeeds.Count == 0)
                break;
            int seed = availableSeeds[UnityEngine.Random.Range(0, availableSeeds.Count)];
            // Keep patches irregular by varying their size.
            int targetSize = UnityEngine.Random.Range(4, 9);
            GrowTerrainPatch(seed, mainBiome, subBiome, targetSize, roomSet, changedTiles);
        }
    }
    protected void GrowTerrainPatch(int seed, string terrain, string subTerrain, int targetSize, HashSet<int> roomTiles, HashSet<int> changedTiles)
    {
        if (!roomTiles.Contains(seed) || changedTiles.Contains(seed)){return;}
        List<int> mainTerrainTiles = new List<int>{seed};
        List<int> borders = mapUtility.AdjacentBorders(mainTerrainTiles, mapSize);
        // Only Get Tiles Inside The Room.
        borders = borders.Intersect(roomTiles).ToList();
        if (borders.Count <= 0){return;}
        // Randomly Add A Tile And Keep Spreading.
        for (int i = 0; i < targetSize; i++)
        {
            mainTerrainTiles.Add(borders[UnityEngine.Random.Range(0, borders.Count)]);
            borders = mapUtility.AdjacentBorders(mainTerrainTiles, mapSize);
            borders = borders.Intersect(roomTiles).ToList();
            if (borders.Count <= 0){return;}
        }
        for (int i = 0; i < mainTerrainTiles.Count; i++)
        {
            mapInfo[mainTerrainTiles[i]] = terrain;
            changedTiles.Add(mainTerrainTiles[i]);
        }
        // Surround the main terrain with subterrain.
        borders = mapUtility.AdjacentBorders(mainTerrainTiles, mapSize);
        borders = borders.Intersect(roomTiles).ToList();
        for (int i = 0; i < borders.Count; i++)
        {
            mapInfo[borders[i]] = subTerrain;
        }
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
            case "Snow":
                return "DeepWater";
            case "Desert":
                return "Pit";
        }
        return "Pit";
    }
    // This Is Basically Start()
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
                    mapInfo = value.Split(",").ToList();
                    break;
                case "MapSize":
                    mapSize = int.Parse(value);
                    break;
                case "TileRooms":
                    tileRooms = string.IsNullOrEmpty(value) ? new List<int>() : value.Split(',').Select(int.Parse).ToList();
                    break;
                case "TerrainEffects":
                    terrainEffectTiles = value.Split(",").ToList();
                    break;
                case "Weather":
                    weather = value;
                    break;
                case "Time":
                    time = value;
                    break;
                case "RoomInfo":
                    trapLocations = value.Split(",").Select(int.Parse).ToList();
                    break;
                case "Player":
                    playerLocation = int.Parse(value);
                    break;
                case "Stairs":
                    stairsLocation = int.Parse(value);
                    break;
                case "Traps":
                    traps = string.IsNullOrEmpty(value) ? new List<string>() : value.Split(',').ToList();
                    break;
                case "TrapLocations":
                    trapLocations = string.IsNullOrEmpty(value) ? new List<int>() : value.Split(',').Select(int.Parse).ToList();
                    break;
                case "Items":
                    items = string.IsNullOrEmpty(value) ? new List<string>() : value.Split(',').ToList();
                    break;
                case "ItemLocations":
                    itemLocations = string.IsNullOrEmpty(value) ? new List<int>() : value.Split(',').Select(int.Parse).ToList();
                    break;
            }
        }
        ResetAll();
        UpdateVisibleTiles();
        UpdateMoveCostManager();
    }
    public string ReturnMapStateString()
    {
        string mapState = "";
        mapState += "Tiles=" + String.Join(",", mapInfo) + "|";
        mapState += "MapSize=" + mapSize + "|";
        mapState += "TileRooms=" + String.Join(",", tileRooms) + "|";
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
    public override void UpdateActors()
    {
        GetActorTiles();
        if (!DisplayLayerExists(actorLayer)){return;}
        actorTiles[stairsLocation] = "Stairs";
        mapDisplayers[actorLayer].DisplayCurrentStyledTiles(mapTiles, actorTiles, currentTiles, true, actorDirections);
    }
    public override void UpdateMap()
    {
        // Tile Layer.
        BaseUpdateMap();
        // TODO Item Layer (Replaced Building Layer).
        // TODO Actor Layer (Includes Stairs).
        UpdateActors();
        // TODO Terrain Layer.
        UpdateTerrain();
        // TODO Highlight Layer.
        // TODO Aura Layer.
        // Update The Minimap Based On Explored/Visible Tiles.
        miniMap.CreateMinimap();
    }
}
