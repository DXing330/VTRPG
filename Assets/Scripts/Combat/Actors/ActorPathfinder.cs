using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ActorPathfinder", menuName = "ScriptableObjects/BattleLogic/ActorPathfinder", order = 1)]
public class ActorPathfinder : MapPathfinder
{
    public TacticActor currentActor;
    public void SetCurrentActor(TacticActor actor)
    {
        currentActor = actor;
    }
    public List<int> path;
    public List<int> FindPaths(int startIndex, List<int> moveCosts, bool extraCosts = true, TacticActor actor = null)
    {
        SetCurrentActor(actor);
        ResetDistances(startIndex);
        for (int i = 0; i < moveCosts.Count - 1; i++)
        {
            DeepCheckClosestTile(moveCosts, extraCosts);
        }
        return new List<int>(distances);
    }

    public List<int> GetPrecomputedPath(int startIndex, int endIndex)
    {
        path = new List<int>();
        path.Add(endIndex);
        if (startIndex == endIndex){return path;}
        int nextTile = -1;
        for (int i = 0; i < distances.Count; i++)
        {
            if (path[i] < 0 || previousTiles[path[i]] < 0)
            {
                path.Clear();
                break;
            }
            // previousTiles[path[i]] is -1 sometimes.
            nextTile = previousTiles[path[i]];
            if (nextTile == startIndex){break;}
            path.Add(nextTile);
        }
        return path;
    }

    protected int DeepCheckClosestTile(List<int> moveCosts, bool extraCosts = false)
    {
        int closestTile = heap.Pull();
        if (closestTile < 0){return -1;}
        List<int> adjacentTiles = mapUtility.AdjacentTiles(closestTile, mapSize);
        int moveCost = 1;
        for (int i = 0; i < adjacentTiles.Count; i++)
        {
            // Deal with elevation/border costs here.
            moveCost = moveCosts[adjacentTiles[i]];
            if (extraCosts)
            {
                // Fliers half elevation differences, because they aren't op enough already.
                if (currentActor != null && currentActor.GetMoveType() == "Flying")
                {
                    moveCost += GetElevationDifference(closestTile, adjacentTiles[i]) / 2;
                }
                else
                {
                    moveCost += GetElevationDifference(closestTile, adjacentTiles[i]);
                }
                int borderCost = GetBorderCost(closestTile, adjacentTiles[i]);
                moveCost += borderCost;
                // This could be moved to the movecost manager like tiles and teffects. Although I do enjoy roads being able to decrease move cost to alleviate elevation/border costs.
                int buildingCost = GetBuildingMoveCost(adjacentTiles[i]);
                moveCost += buildingCost;
            }
            if (moveCost < 1){moveCost = 1;}
            if (distances[closestTile] + moveCost < distances[adjacentTiles[i]])
            {
                distances[adjacentTiles[i]] = distances[closestTile] + moveCost;
                currentMoveCosts[adjacentTiles[i]] = moveCost;
                previousTiles[adjacentTiles[i]] = closestTile;
                heap.AddNodeWeight(adjacentTiles[i], distances[adjacentTiles[i]]);
            }
        }
        return closestTile;
    }

    public List<int> FindTilesInMoveRange(int start, int moveRange, List<int> moveCosts)
    {
        List<int> tiles = new List<int>();
        ResetDistances(start);
        int distance = 0;
        for (int i = 0; i < moveCosts.Count-1; i++)
        {
            distance = heap.PeekWeight();
            if (distance > moveRange){break;}
            tiles.Add(DeepCheckClosestTile(moveCosts, true));
        }
        tiles.RemoveAt(0);
        tiles.Sort();
        return tiles;
    }

    public List<int> GetTilesInAttackRange(TacticActor actor, BattleMap map, List<int> moveCosts, bool current = true)
    {
        int start = actor.GetLocation();
        int moveRange = actor.GetMoveRangeWhileAttacking(current);
        int attackRange = actor.GetAttackRange();
        List<int> attackableTiles = new List<int>();
        List<int> tiles = new List<int>();
        if (attackRange <= 0){return tiles;}
        ResetDistances(start);
        // Check what tiles you can move to.
        int distance = 0;
        for (int i = 0; i < moveCosts.Count-1; i++)
        {
            distance = heap.PeekWeight();
            if (distance > moveRange){break;}
            // Attacking ignores elevation move costs, but has its own elevation calculation separately.
            tiles.Add(DeepCheckClosestTile(moveCosts));
        }
        // Check what tiles you can attack based on the tiles you can move to.
        // O(n).
        for (int i = 0; i < tiles.Count; i++)
        {
            attackableTiles.AddRange(map.GetAttackableTiles(actor, false, tiles[i]));
        }
        attackableTiles.Remove(start);
        return attackableTiles;
    }
}
