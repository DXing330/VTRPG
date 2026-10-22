using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MysteryDungeonMapUtility", menuName = "ScriptableObjects/Utility/MysteryDungeonMapUtility", order = 1)]
public class MysteryDungeonMapUtility : ScriptableObject
{
    public bool IsTilePassable(string terrain)
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
                return false;
        }
        return false;
    }
    public List<int> GetEmptyTiles(List<int> tiles, MysteryDungeonBattleMap map)
    {
        for (int i = tiles.Count - 1; i >= 0; i--)
        {
            if (!IsTilePassable(map.mapInfo[tiles[i]]) || map.GetActorOnTile(tiles[i]) != null)
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
    // Priority 1: Same Room.
    // 2: Visible Tiles.
    // 3: Fail
    public TacticActor GetClosestEnemy(TacticActor actor, MysteryDungeonBattleMap map)
    {
        List<int> targetTiles = new List<int>();
        int currentRoom = map.GetRoomOfTile(actor.GetLocation());
        if (currentRoom >= 0)
        {
            targetTiles = map.GetRoomTiles(currentRoom);
        }
        else
        {
            targetTiles = map.GetVisibleTiles();
        }
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
            if (nextTile < 0 || nextTile == currentTile || !IsTilePassable(map.mapInfo[nextTile]) || map.GetActorOnTile(nextTile) != null){return passableTiles;}
            passableTiles.Add(nextTile);
            currentTile = nextTile;
        }
        return passableTiles;
    }
}
