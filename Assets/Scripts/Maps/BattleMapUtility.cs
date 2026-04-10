using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleMapUtility", menuName = "ScriptableObjects/Utility/BattleMapUtility", order = 1)]
public class BattleMapUtility : ScriptableObject
{
    public GeneralUtility utility;
    public MapUtility mapUtility;
    // Finding Actor Utilities
    public TacticActor GetActorOnTile(BattleMap map, int tileNumber, bool includeInvisible = true)
    {
        if (tileNumber < 0){ return null; }
        string actorName = map.actorTiles[tileNumber];
        for (int i = 0; i < map.battlingActors.Count; i++)
        {
            if (map.battlingActors[i].GetSpriteName() == actorName && map.battlingActors[i].GetLocation() == tileNumber && map.battlingActors[i].GetHealth() > 0)
            {
                if (!includeInvisible && map.battlingActors[i].invisible){return null;}
                return map.battlingActors[i];
            }
        }
        return null;
    }
    public List<TacticActor> GetActorsOnTiles(BattleMap map, List<int> tiles)
    {
        List<TacticActor> actors = new List<TacticActor>();
        for (int i = 0; i < tiles.Count; i++)
        {
            TacticActor testActor = GetActorOnTile(map, tiles[i]);
            if (testActor != null)
            {
                actors.Add(testActor);
            }
        }
        return actors;
    }
    public List<TacticActor> AllActorsBySprite(string spriteName, BattleMap map)
    {
        List<TacticActor> actors = new List<TacticActor>();
        for (int i = 0; i < map.battlingActors.Count; i++)
        {
            if (map.battlingActors[i].GetSpriteName().Contains(spriteName))
            {
                actors.Add(map.battlingActors[i]);
            }
        }
        return actors;
    }
    public List<TacticActor> AllActorsBySpecies(string speciesName, BattleMap map)
    {
        List<TacticActor> actors = new List<TacticActor>();
        for (int i = 0; i < map.battlingActors.Count; i++)
        {
            if (map.battlingActors[i].GetSpecies().Contains(speciesName))
            {
                actors.Add(map.battlingActors[i]);
            }
        }
        return actors;
    }
    public int GetRandomEnemyLocation(TacticActor actor, List<int> targetedTiles, BattleMap map)
    {
        List<TacticActor> enemies = GetActorsOnTiles(map, targetedTiles);
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i].GetTeam() == actor.GetTeam())
            {
                enemies.RemoveAt(i);
            }
        }
        if (enemies.Count == 0)
        {
            return -1;
        }
        return enemies[UnityEngine.Random.Range(0, enemies.Count)].GetLocation();
    }
    public int GetRandomAllyLocation(TacticActor actor, List<int> targetedTiles, BattleMap map)
    {
        List<TacticActor> allies = GetActorsOnTiles(map, targetedTiles);
        for (int i = allies.Count - 1; i >= 0; i--)
        {
            if (allies[i].GetTeam() != actor.GetTeam())
            {
                allies.RemoveAt(i);
            }
        }
        if (allies.Count == 0)
        {
            return -1;
        }
        return allies[UnityEngine.Random.Range(0, allies.Count)].GetLocation();
    }
    // Calculation Utilities.
    public int AverageActorHealth(BattleMap map)
    {
        int health = 0;
        int count = map.battlingActors.Count;
        for (int i = 0; i < count; i++)
        {
            health += map.battlingActors[i].GetHealth();
        }
        return health / count;
    }
    // Finding Tiles Utilities.
    public int ReturnClosestTileOfType(BattleMap map, TacticActor actor, string tileType)
    {
        int tile = -1;
        int distance = map.mapSize * map.mapSize;
        for (int i = 0; i < map.mapInfo.Count; i++)
        {
            if (actor.GetMoveType() != "Flying" && map.excludedTileTypesForNonFlying.Contains(map.mapInfo[i]))
            {
                continue;
            }
            if (map.mapInfo[i].Contains(tileType) && GetActorOnTile(map, i) == null)
            {
                int newDistance = mapUtility.DistanceBetweenTiles(i, actor.GetLocation(), map.mapSize);
                if (newDistance < distance)
                {
                    distance = newDistance;
                    tile = i;
                }
            }
        }
        return tile;
    }
    public bool TileSandwiched(BattleMap map, TacticActor actor, string tileType)
    {
        if (actor.GetTarget() == null || actor.GetTarget().GetHealth() <= 0 || actor.GetTarget().invisible){return false;}
        // Already checked for alignment in earlier condition.
        // Get the tiles inbetween you and the target.
        List<int> tilesBetween = mapUtility.GetTileInLineBetweenPoints(actor.GetLocation(), actor.GetTarget().GetLocation(), map.mapSize);
        // Check if any of the tiles are of the tile type.
        for (int i = 0; i < tilesBetween.Count; i++)
        {
            if (map.mapInfo[tilesBetween[i]].Contains(tileType))
            {
                return true;
            }
        }
        return false;
    }
    public int ReturnClosestTileSandwiched(BattleMap map, TacticActor actor, string tileType)
    {
        int tile = -1;
        int distance = map.mapSize * map.mapSize;
        if (actor.GetTarget() == null || actor.GetTarget().GetHealth() <= 0 || actor.GetTarget().invisible){return tile;}
        int targetLocation = actor.GetTarget().GetLocation();
        List<int> adjacentTiles = mapUtility.AdjacentTiles(targetLocation, map.mapSize);
        for (int i = 0; i < adjacentTiles.Count; i++)
        {
            // Check if the target is adjacent to any of the requested tile types.
            if (map.mapInfo[adjacentTiles[i]].Contains(tileType))
            {
                // Get the direction and check if any point in a tile is valid.
                int direction = mapUtility.DirectionBetweenLocations(targetLocation, adjacentTiles[i], map.mapSize);
                List<int> lineTiles = mapUtility.GetTilesInLineDirection(adjacentTiles[i], direction, map.mapSize, map.mapSize);
                for (int j = 0; j < lineTiles.Count; j++)
                {
                    if (lineTiles[j] < 0 || map.TileExcluded(actor, map.mapInfo[lineTiles[j]]) || GetActorOnTile(map, lineTiles[j]) != null)
                    {
                        continue;
                    }
                    int newDistance = mapUtility.DistanceBetweenTiles(lineTiles[j], actor.GetLocation(), map.mapSize);
                    if (newDistance < distance)
                    {
                        distance = newDistance;
                        tile = lineTiles[j];
                        break;
                    }
                }
            }
        }
        return tile;
    }
}
