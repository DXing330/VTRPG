using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapTester : MapManager
{
    public bool debugThis;
    public List<string> directions;
    public int testTile;
    public int testTileTwo;
    public int testSize;
    public int testRange;
    public int testQ;
    public int testR;
    public int testS;

    [ContextMenu("Test Inward Spiral")]
    public void TestInwardSpiral()
    {
        string spiral = "";
        for (int i = 0 ; i < mapSize * mapSize; i++)
        {
            spiral += mapUtility.SpiralInward(i, mapSize);
            mapTiles[mapUtility.SpiralInward(i, mapSize)].UpdateText(i.ToString());
            if (i < mapSize * mapSize - 1)
            {
                spiral += ",";
            }
        }
        Debug.Log(spiral);
    }

    [ContextMenu("Test Outward Spiral")]
    public void TestOutwardSpiral()
    {
        string spiral = "";
        for (int i = 0 ; i < mapSize * mapSize; i++)
        {
            spiral += mapUtility.SpiralOutward(i, mapSize);
            mapTiles[mapUtility.SpiralOutward(i, mapSize)].UpdateText(i.ToString());
            if (i < mapSize * mapSize - 1)
            {
                spiral += ",";
            }
        }
        Debug.Log(spiral);
    }

    [ContextMenu("Test Distance")]
    public void TestDistance()
    {
        Debug.Log(mapUtility.DistanceBetweenTiles(testTile, testTileTwo, testSize));
        if (debugThis)
        {
            for (int i = 0; i < testSize*testSize; i++)
            {
                Debug.Log(mapUtility.DistanceBetweenTiles(testTile, i, testSize));
            }
        }
    }

    [ContextMenu("Test Adjacent")]
    public List<int> TestAdjacent()
    {
        List<int> testAdjacent = mapUtility.AdjacentTiles(testTile, testSize);
        for (int i = 0; i < testAdjacent.Count; i++)
        {
            if (!debugThis){break;}
            Debug.Log(directions[i]);
            Debug.Log(testAdjacent[i]);
        }
        return testAdjacent;
    }

    [ContextMenu("Test Tile To QRS")]
    public void TestTileToQRS()
    {
        Debug.Log(mapUtility.GetHexQ(testTile, testSize));
        Debug.Log(mapUtility.GetHexR(testTile, testSize));
        Debug.Log(mapUtility.GetHexS(testTile, testSize));
    }

    [ContextMenu("Test QRS To Tile")]
    public void TestQRSToTile()
    {
        Debug.Log(mapUtility.ReturnTileNumberFromQRS(testQ, testR, testS, testSize));
    }

    [ContextMenu("Test QRS To Col")]
    public void TestQRSToCol()
    {
        Debug.Log(mapUtility.GetColFromQRS(testQ, testR, testS, testSize));
    }

    [ContextMenu("Test QRS To Row")]
    public void TestQRSToRow()
    {
        Debug.Log(mapUtility.GetRowFromQRS(testQ, testR, testS, testSize));
    }

    [ContextMenu("Test Directions")]
    public void TestDirections()
    {
        for (int i = 0; i < testSize*testSize; i++)
        {
            Debug.Log("Tile Number: "+i+", Direction: "+mapUtility.DirectionBetweenLocations(testTile, i, testSize));
        }
    }

    public int sandwicherLocation;
    public int sandwichedLocation;
    public string sandwichedTileType;
    [ContextMenu("Test Sandwich Check")]
    public void DebugSandwichCheck()
    {
        Debug.Log(SandwichCheck());
    }
    public bool SandwichCheck()
    {
        int direction = mapUtility.DirectionBetweenLocations(sandwicherLocation, sandwichedLocation, mapSize);
        int sandwichingPoint = mapUtility.PointInDirection(sandwichedLocation, direction, mapSize);
        Debug.Log(sandwichingPoint);
        if (sandwichingPoint < 0){return false;}
        return mapInfo[sandwichingPoint].Contains(sandwichedTileType);
    }
    [ContextMenu("Test Closest Sandwich")]
    public void DebugClosestSandwich()
    {
        Debug.Log(ClosestSandwichTile());
    }
    public int ClosestSandwichTile()
    {
        int tile = -1;
        int distance = mapSize * mapSize;
        List<int> adjacentTiles = mapUtility.AdjacentTiles(sandwichedLocation, mapSize);
        for (int i = 0; i < adjacentTiles.Count; i++)
        {
            if (mapInfo[adjacentTiles[i]].Contains(sandwichedTileType))
            {
                int sandwichingPoint = mapUtility.PointInOppositeDirection(sandwichedLocation, adjacentTiles[i], mapSize);
                if (sandwichingPoint < 0)
                {
                    continue;
                }
                int newDistance = mapUtility.DistanceBetweenTiles(sandwichingPoint, sandwicherLocation, mapSize);
                if (newDistance < distance)
                {
                    distance = newDistance;
                    tile = sandwichingPoint;
                }
            }
        }
        return tile;
    }

    public int connectedStartPoint;
    [ContextMenu("Test Connected")]
    public void DebugConnectedSameTiles()
    {
        List<int> connectedTiles = AllConnectedTilesOfSameType(connectedStartPoint);
        Debug.Log(String.Join(",", connectedTiles));
    }
}
