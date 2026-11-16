using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MysteryDungeonMapUtility", menuName = "ScriptableObjects/Utility/MysteryDungeonMapUtility", order = 1)]
public class MysteryDungeonMapUtility : ScriptableObject
{
    // Save A Map
    public string ReturnMapStateString(MysteryDungeonBattleMap map)
    {
        string mapState = "";
        mapState += "Floor=" + map.floor + "|";
        mapState += "Type=" + map.dungeonType + "|";
        mapState += "Tiles=" + String.Join(",", map.mapInfo) + "|";
        mapState += "MapSize=" + map.mapSize + "|";
        mapState += "TileRooms=" + String.Join(",", map.tileRooms) + "|";
        mapState += "TerrainEffects=" + String.Join(",", map.terrainEffectTiles) + "|";
        mapState += "Weather=" + map.weather + "|";
        mapState += "Time=" + map.time + "|";
        mapState += "Player=" + map.playerLocation + "|";
        mapState += "Stairs=" + map.stairsLocation + "|";
        mapState += "Traps=" + String.Join(",", map.traps) + "|";
        mapState += "TrapLocations=" + String.Join(",", map.trapLocations) + "|";
        mapState += "Items=" + String.Join(",", map.items) + "|";
        mapState += "ItemLocations=" + String.Join(",", map.itemLocations) + "|";
        mapState += "PossibleEnemies=" + String.Join(",", map.possibleEnemies) + "|";
        mapState += "StartingEnemies=" + String.Join(",", map.startingEnemies) + "|";
        mapState += "EnemyLocations=" + String.Join(",", map.startingEnemyLocations) + "|";
        mapState += "MonsterHouseRoom=" + map.monsterHouseRoom + "|";
        mapState += "StoreRoom=" + map.storeRoom + "|";
        mapState += "StoreTile=" + map.GetStoreOwnerTile() + "|";
        mapState += "StoreItems=" + String.Join(",", map.GetStoreItems()) + "|";
        mapState += "NPCs=" + String.Join(",", map.GetNPCs()) + "|";
        mapState += "NPCLocations=" + String.Join(",", map.GetNPCLocations()) + "|";
        return mapState;
    }
    public void CreateRoomBiome(string dungeonType, List<int> roomTiles, MysteryDungeonBattleMap map)
    {
        string[] dungeonTypeBiomes = map.dungeonTypeSubTerrains.ReturnValue(dungeonType).Split("|");
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
            GrowTerrainPatch(seed, mainBiome, subBiome, targetSize, roomSet, changedTiles, map);
        }
    }
    protected void GrowTerrainPatch(int seed, string terrain, string subTerrain, int targetSize, HashSet<int> roomTiles, HashSet<int> changedTiles, MysteryDungeonBattleMap map)
    {
        if (!roomTiles.Contains(seed) || changedTiles.Contains(seed)){return;}
        int mapSize = map.mapSize;
        List<int> mainTerrainTiles = new List<int>{seed};
        List<int> borders = map.mapUtility.AdjacentBorders(mainTerrainTiles, mapSize);
        // Only Get Tiles Inside The Room.
        borders = borders.Intersect(roomTiles).ToList();
        if (borders.Count <= 0){return;}
        // Randomly Add A Tile And Keep Spreading.
        for (int i = 0; i < targetSize; i++)
        {
            mainTerrainTiles.Add(borders[UnityEngine.Random.Range(0, borders.Count)]);
            borders = map.mapUtility.AdjacentBorders(mainTerrainTiles, mapSize);
            borders = borders.Intersect(roomTiles).ToList();
            if (borders.Count <= 0){return;}
        }
        for (int i = 0; i < mainTerrainTiles.Count; i++)
        {
            map.mapInfo[mainTerrainTiles[i]] = terrain;
            changedTiles.Add(mainTerrainTiles[i]);
        }
        // Surround the main terrain with subterrain.
        borders = map.mapUtility.AdjacentBorders(mainTerrainTiles, mapSize);
        borders = borders.Intersect(roomTiles).ToList();
        for (int i = 0; i < borders.Count; i++)
        {
            map.mapInfo[borders[i]] = subTerrain;
        }
    }
    public string GetWallTerrain(string dungeonType)
    {
        switch (dungeonType)
        {
            case "Forest":
                return "Wall";
            case "Mountain":
                return "Pit";
            case "Water":
                return "DeepWater";
            case "Underground":
                return "Pit";
            case "Snow":
                return "Wall";
            case "Desert":
                return "Wall";
        }
        return "Wall";
    }
    public bool TilePassable(string terrain)
    {
        switch (terrain)
        {
            case "Plains":
            case "Forest":
            case "Mountain":
            case "Water":
            case "Desert":
            case "Volcano":
            case "Snow":
            case "SnowForest":
            case "SnowMountain":
            case "Oasis":
                return true;
            case "Pit":
            case "Wall":
            case "DeepWater":
            case "Lava":
                return false;
        }
        return false;
    }
    public bool AllRoomTiles(List<int> tiles, MysteryDungeonBattleMap map)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            if (map.GetRoomOfTile(tiles[i]) < 0){return false;}
        }
        return true;
    }
    public List<int> GetEmptyTiles(List<int> tiles, MysteryDungeonBattleMap map)
    {
        for (int i = tiles.Count - 1; i >= 0; i--)
        {
            if (!TilePassable(map.mapInfo[tiles[i]]) || map.GetActorOnTile(tiles[i]) != null)
            {
                tiles.RemoveAt(i);
            }
        }
        return tiles;
    }
    public List<int> GetEmptyTilesInCircleRange(int start, MysteryDungeonBattleMap map, int span = 1)
    {
        List<int> empty = map.mapUtility.GetTilesInCircleShape(start, span, map.mapSize);
        empty.Remove(start);
        return GetEmptyTiles(empty, map);
    }
    public List<int> GetVisibleTiles(int actorLocation, MysteryDungeonBattleMap map)
    {
        int mapSize = map.mapSize;
        List<int> actorVisible = new List<int>();
        int room = map.GetRoomOfTile(actorLocation);
        int aRow = map.mapUtility.GetRow(actorLocation, mapSize);
        int aCol = map.mapUtility.GetColumn(actorLocation, mapSize);
        // Room, See Whole Room + Adjacent Tiles.
        if (room >= 0)
        {
            actorVisible.AddRange(map.GetRoomTiles(actorLocation));
            List<int> borders = map.mapUtility.AdjacentBorders(actorVisible, mapSize);
            actorVisible.AddRange(borders);
        }
        // Hallway, See 9x9 + Hide Rooms
        else
        {
            for (int row = aRow - 4; row <= aRow + 4; row++)
            {
                for (int col = aCol - 4; col <= aCol + 4; col++)
                {
                    if (row < 0 || row >= mapSize || col < 0 || col >= mapSize)
                    {
                        continue;
                    }
                    int tile = row * mapSize + col;
                    actorVisible.Add(tile);
                }
            }
            // Hide Rooms
            for (int i = actorVisible.Count - 1; i >= 0; i--)
            {
                if (map.GetRoomOfTile(actorVisible[i]) >= 0)
                {
                    actorVisible.RemoveAt(i);
                }
            }
            // Reveal Room Tiles Close To Center.
            List<int> borders = map.mapUtility.AdjacentBorders(actorVisible, mapSize);
            foreach (int tile in borders)
            {
                if (map.GetRoomOfTile(tile) >= 0)
                {
                    actorVisible.Add(tile);
                }
            }
        }
        return actorVisible;
    }
    // Priority 1: Same Room.
    // 2: Visible Tiles.
    // 3: Fail
    public TacticActor GetClosestEnemy(TacticActor actor, MysteryDungeonBattleMap map)
    {
        List<int> targetTiles = new List<int>();
        targetTiles = map.GetActorVisibleTiles(actor.GetLocation());
        List<TacticActor> enemies = map.ReturnEnemiesInTiles(actor, targetTiles);
        if (enemies.Count <= 0){return null;}
        int distance = int.MaxValue;
        int index = -1;
        for (int i = 0; i < enemies.Count; i++)
        {
            int newDist = map.DistanceBetweenActors(enemies[i], actor);
            if (newDist < distance)
            {
                index = i;
                distance = newDist;
            }
        }
        return enemies[index];
    }
    public List<int> GetPassableTilesInDirection(TacticActor actor, MysteryDungeonBattleMap map, int direction, int maxRange)
    {
        List<int> passableTiles = new List<int>();
        int currentTile = actor.GetLocation();
        int nextTile = -1;
        for (int i = 0; i < maxRange; i++)
        {
            nextTile = map.mapUtility.PointInDirection(currentTile, direction, map.mapSize);
            if (nextTile < 0 || nextTile == currentTile || !TilePassable(map.mapInfo[nextTile]) || map.GetActorOnTile(nextTile) != null){return passableTiles;}
            passableTiles.Add(nextTile);
            currentTile = nextTile;
        }
        return passableTiles;
    }
    // When Throwing Something Give The First Actor Tile Or The Tile Before The First Wall Tile.
    public int GetThrownToTile(TacticActor actor, MysteryDungeonBattleMap map)
    {
        int start = actor.GetLocation();
        int direction = actor.GetDirection();
        int currentTile = start;
        int nextTile = -1;
        for (int i = 0; i < map.mapSize; i++)
        {
            nextTile = map.mapUtility.PointInDirection(currentTile, direction, map.mapSize);
            // Out Of Bounds Or Edge Of Map.
            if (nextTile < 0 || nextTile == currentTile || !TilePassable(map.mapInfo[nextTile]))
            {
                return currentTile;
            }
            // Hit The First Actor.
            else if (map.GetActorOnTile(nextTile) != null)
            {
                return nextTile;
            }
            currentTile = nextTile;
        }
        return nextTile;
    }
}
