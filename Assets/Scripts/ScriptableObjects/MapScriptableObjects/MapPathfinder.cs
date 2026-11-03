using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Pathfinder", menuName = "ScriptableObjects/Utility/Pathfinder", order = 1)]
public class MapPathfinder : ScriptableObject
{
    public Heap heap;
    public MapUtility mapUtility;
    public int ClosestAdjacentTile(int tile)
    {
        List<int> adjacent = mapUtility.AdjacentTiles(tile, mapSize);
        int dist = heap.bigInt;
        int cTile = -1;
        for (int i = 0; i < adjacent.Count; i++)
        {
            int nDist = currentMoveCosts[adjacent[i]];
            if (nDist < dist)
            {
                dist = nDist;
                cTile = adjacent[i];
            }
        }
        return cTile;
    }
    public int mapSize;
    public void SetMapSize(int newSize)
    {
        mapSize = newSize;
        ResetMoveCosts();
    }
    public int GetMapSize(){return mapSize;}
    public StatDatabase buildingMoveCosts;
    private Dictionary<int, int> buildingMoveCostsByTile = new Dictionary<int, int>();
    public List<string> buildings;
    public List<int> buildingLocations;
    protected void ResetBuildings()
    {
        buildings.Clear();
        buildingLocations.Clear();
        buildingMoveCostsByTile.Clear();
    }
    public void SetBuildings(List<string> newBuildings, List<int> newBuildingLocations)
    {
        buildings = new List<string>(newBuildings);
        buildingLocations = new List<int>(newBuildingLocations);
        BuildBuildingMoveCostLookup();
    }
    protected void BuildBuildingMoveCostLookup()
    {
        buildingMoveCostsByTile.Clear();
        int count = Mathf.Min(buildings.Count, buildingLocations.Count);
        for (int i = 0; i < count; i++)
        {
            int tile = buildingLocations[i];
            if (buildingMoveCostsByTile.ContainsKey(tile)){continue;}
            buildingMoveCostsByTile.Add(tile, buildingMoveCosts.ReturnIntValue(buildings[i], 0));
        }
    }
    public int GetBuildingMoveCost(int into)
    {
        if (buildingMoveCostsByTile.TryGetValue(into, out int cost)){return cost;}
        return 0;
    }
    public List<int[]> borders;
    protected void ResetBorders()
    {
        borders.Clear();
    }
    public void SetBorders(List<string> newInfo)
    {
        borders = new List<int[]>(newInfo.Count);
        for (int i = 0; i < newInfo.Count; i++)
        {
            int[] costs = new int[6];
            string border = newInfo[i];
            if (!string.IsNullOrEmpty(border) && border != "0|0|0|0|0|0")
            {
                string[] values = border.Split('|');
                for (int j = 0; j < 6 && j < values.Length; j++)
                {
                    int.TryParse(values[j], out costs[j]);
                }
            }
            borders.Add(costs);
        }
    }
    public int GetBorderCost(int from, int into)
    {
        if (into < 0 || from < 0 || into >= borders.Count || from >= borders.Count)
        {
            return 0;
        }
        int direction = mapUtility.AdjacentDirectionBetweenLocations(into, from, mapSize);
        if (direction < 0 || direction >= 6)
        {
            return 0;
        }
        return borders[into][direction];
    }
    // Keep track of the elevation of each tile for additional move cost calculations.
    public List<int> elevations;
    protected void ResetElevations()
    {
        elevations.Clear();
    }
    public void DebugElevations()
    {
        for (int i = 0; i < elevations.Count; i++)
        {
            Debug.Log(i + " : " + elevations[i]);
        }
    }
    protected int GetElevation(int tileNumber)
    {
        if (tileNumber < 0 || tileNumber >= elevations.Count){return 0;}
        return elevations[tileNumber];
    }
    public void SetElevations(List<int> newElevations)
    {
        elevations = new List<int>(newElevations);
    }
    protected int GetElevationDifference(int tile1, int tile2)
    {
        return Mathf.Abs(elevations[tile1] - elevations[tile2]);
    }
    // Keep track of the movecost of each tile.
    public List<int> moveCosts;
    // Later we can also track borders?
    protected void ResetMoveCosts()
    {
        moveCosts.Clear();
    }
    protected int GetMoveCost(int tileNumber)
    {
        if (tileNumber < 0)
        {
            return heap.bigInt;
        }
        else if (tileNumber >= moveCosts.Count)
        {
            return 1;
        }
        return moveCosts[tileNumber];
    }
    public void SetMoveCosts(List<int> newMoveCosts)
    {
        moveCosts = new List<int>(newMoveCosts);
    }
    // Keep track of the distances to each tile.
    public List<int> distances;
    public List<int> currentMoveCosts;
    public List<int> GetCurrentMoveCosts(){return currentMoveCosts;}
    // Keep track of the tile that leads into each tile.
    public List<int> previousTiles;
    public int GetPreviousTile(int tileNumber)
    {
        if (tileNumber < 0 || tileNumber >= previousTiles.Count){return -1;}
        return previousTiles[tileNumber];
    }

    public List<int> ShortestPathToTile(int start, int end)
    {
        return mapUtility.ShortestLineBetweenPoints(start, end, mapSize);
    }

    public List<int> StraightPathToTile(int start, int end)
    {
        List<int> path = new List<int>();
        if (!mapUtility.StraightLineBetweenPoints(start, end, mapSize))
        {
            return path;
        }
        int direction = mapUtility.DirectionBetweenLocations(start, end, mapSize);
        int distance = mapUtility.DistanceBetweenTiles(start, end, mapSize);
        int nextTile = start;
        for (int i = 0; i < distance; i++)
        {
            nextTile = mapUtility.PointInDirection(nextTile, direction, mapSize);
            path.Add(nextTile);
        }
        return path;
    }
    
    public List<int> BasicPathToTile(int start, int end)
    {
        List<int> path = new List<int>();
        ResetDistances(start);
        for (int i = 0; i < mapSize * mapSize - 1; i++)
        {
            CheckClosestTile();
        }
        path.Add(end);
        if (start == end){return path;}
        int nextTile = -1;
        for (int i = 0; i < distances.Count; i++)
        {
            nextTile = previousTiles[path[i]];
            if (nextTile == start){break;}
            path.Add(nextTile);
        }
        return path;
    }

    protected void ResetHeap()
    {
        heap.ResetHeap();
        heap.InitializeHeap(mapSize*mapSize);
    }

    protected void ResetDistances(int startTile)
    {
        ResetHeap();
        previousTiles.Clear();
        distances.Clear();
        currentMoveCosts.Clear();
        for (int i = 0; i < mapSize*mapSize; i++)
        {
            previousTiles.Add(-1);
            if (i == startTile)
            {
                distances.Add(0);
                currentMoveCosts.Add(0);
                heap.AddNodeWeight(startTile, 0);
                continue;
            }
            distances.Add(heap.bigInt);
            currentMoveCosts.Add(heap.bigInt);
        }
    }

    protected virtual int CheckClosestTile()
    {
        int closestTile = heap.Pull();
        List<int> adjacentTiles = mapUtility.AdjacentTiles(closestTile, mapSize);
        int moveCost = 1;
        for (int i = 0; i < adjacentTiles.Count; i++)
        {
            moveCost = GetMoveCost(adjacentTiles[i]);
            if (distances[closestTile]+moveCost < distances[adjacentTiles[i]])
            {
                distances[adjacentTiles[i]] = distances[closestTile]+moveCost;
                previousTiles[adjacentTiles[i]] = closestTile;
                heap.AddNodeWeight(adjacentTiles[i], distances[adjacentTiles[i]]);
            }
        }
        return closestTile;
    }

    public List<int> FindTilesInRange(int startTile, int range = 1)
    {
        List<int> tiles = new List<int>();
        ResetDistances(startTile);
        int distance = 0;
        for (int i = 0; i < mapSize*mapSize; i++)
        {
            distance = heap.PeekWeight();
            if (distance > range){break;}
            tiles.Add(CheckClosestTile());
        }
        if (tiles.Count <= 0){return tiles;}
        tiles.RemoveAt(0);
        return tiles;
    }

    public int GetTileByDirectionDistance(int startTile, int direction, int distance = 1)
    {
        int current = startTile;
        for (int i = 0; i < distance; i++)
        {
            if (mapUtility.DirectionCheck(current, direction, mapSize))
            {
                current = mapUtility.PointInDirection(current, direction, mapSize);
            }
            else
            {
                return startTile;
            }
        }
        return current;
    }

    public List<int> GetTilesInLineDirection(int startTile, int direction, int range)
    {
        return mapUtility.GetTilesInLineDirection(startTile, direction, range, mapSize);
    }

    public int DirectionBetweenLocations(int start, int end)
    {
        return mapUtility.DirectionBetweenLocations(start, end, mapSize);
    }

    public int DistanceBetweenTiles(int start, int end)
    {
        return mapUtility.DistanceBetweenTiles(start, end, mapSize);
    }

    public int PointInDirection(int start, int direction)
    {
        return mapUtility.PointInDirection(start, direction, mapSize);
    }

    public List<int> GetTilesInLineRange(int startTile, int range, List<int> directions = null)
    {
        List<int> tiles = new List<int>();
        int start = startTile;
        if (directions == null)
        {
            for (int i = 0; i < 6; i++)
            {
                tiles.AddRange(GetTilesInLineDirection(start, i, range));
            }
        }
        else
        {
            for (int i = 0; i < directions.Count; i++)
            {
                tiles.AddRange(GetTilesInLineDirection(start, directions[i], range));
            }
        }
        return tiles;
    }
}
