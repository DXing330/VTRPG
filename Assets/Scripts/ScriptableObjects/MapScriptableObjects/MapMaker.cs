using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapMaker", menuName = "ScriptableObjects/Utility/MapMaker", order = 1)]
public class MapMaker : ScriptableObject
{
    public MapUtility mapUtility;
    public SpriteContainer possibleSprites;
    public void SetPossibleSprites(SpriteContainer newSprites) { possibleSprites = newSprites; }
    public List<string> possibleTiles;
    protected int defaultSize = 36;
    public int mapSize = 36;

    [ContextMenu("Make Map")]
    // Start by making the base layer.
    public List<string> MakeRandomMap(int newSize = -1)
    {
        UpdatePossibleTiles();
        if (newSize > 0) { mapSize = newSize; }
        else { mapSize = defaultSize; }
        List<string> mapTiles = new List<string>();
        // Add a bunch of tiles.
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                mapTiles.Add(possibleTiles[Random.Range(0, possibleTiles.Count)]);
            }
        }
        return mapTiles;
    }

    protected void UpdatePossibleTiles()
    {
        possibleTiles.Clear();
        for (int i = 0; i < possibleSprites.sprites.Count; i++)
        {
            possibleTiles.Add(possibleSprites.sprites[i].name);
        }
    }

    public List<string> MakeBasicMap(int newSize = -1, string baseTerrain = "")
    {
        UpdatePossibleTiles();
        if (newSize > 0) { mapSize = newSize; }
        else { mapSize = defaultSize; }
        List<string> mapTiles = new List<string>();
        if (baseTerrain == "") { baseTerrain = possibleTiles[0]; }
        // Add a bunch of tiles.
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                mapTiles.Add(baseTerrain);
            }
        }
        return mapTiles;
    }

    public List<string> AddFeature(List<string> originalMap, string featureType, string pattern, string patternSpecifics = "")
    {
        switch (pattern)
        {
            case "All":
                return AddAllTiles(originalMap, featureType, patternSpecifics);
            case "River":
                return AddRiver(originalMap, featureType, patternSpecifics);
            case "Single":
                return AddPoint(originalMap, featureType, patternSpecifics);
            case "Forest":
                return AddForest(originalMap, featureType, patternSpecifics);
            case "Wall":
                return AddWall(originalMap, featureType, patternSpecifics);
            case "CenterWall":
                return AddCenterWall(originalMap, featureType, patternSpecifics);
            case "Border":
                return AddBorder(originalMap, featureType, patternSpecifics);
            case "CenterForest":
                return AddCenterForest(originalMap, featureType, patternSpecifics);
            case "Valley":
                return AddValley(originalMap, featureType);
            case "MountainValley":
                return AddValley(originalMap, featureType, "Mountain");
            case "ForestValley":
                return AddValley(originalMap, featureType, "Forest");
            case "WaterValley":
                return AddValley(originalMap, featureType, "Water");
        }
        return originalMap;
    }

    protected List<string> AddAllTiles(List<string> originalMap, string featureType, string specifics)
    {
        for (int i = 0; i < originalMap.Count; i++)
        {
            originalMap[i] = featureType;
        }
        return originalMap;
    }

    protected List<string> AddPoint(List<string> originalMap, string featureType, string specifics)
    {
        int startTile = (Random.Range(1, mapSize - 1) * mapSize) + Random.Range(1, mapSize - 1);
        originalMap[startTile] = featureType;
        return originalMap;
    }

    protected List<string> AddForest(List<string> originalMap, string featureType, string specifics)
    {
        int startTile = (Random.Range(1, mapSize - 1) * mapSize) + Random.Range(1, mapSize - 1);
        List<int> allTiles = mapUtility.AdjacentTiles(startTile, mapSize);
        allTiles.Add(startTile);
        for (int i = 0; i < allTiles.Count; i++)
        {
            originalMap[allTiles[i]] = featureType;
        }
        return originalMap;
    }

    protected List<string> AddCenterForest(List<string> originalMap, string featureType, string specifics)
    {
        int startTile = (mapSize/2 * mapSize) + mapSize/2;
        List<int> allTiles = mapUtility.AdjacentTiles(startTile, mapSize);
        allTiles.Add(startTile);
        for (int i = 0; i < allTiles.Count; i++)
        {
            originalMap[allTiles[i]] = featureType;
        }
        return originalMap;
    }

    // Rivers flow from left to right.
    protected List<string> AddRiver(List<string> originalMap, string featureType, string specifics)
    {
        // Pick a starting point.
        int currentPoint = Random.Range(1, mapSize - 1) * mapSize;
        int newPoint = -1;
        for (int i = 0; i < mapSize; i++)
        {
            originalMap[currentPoint] = featureType;
            newPoint = mapUtility.RandomPointRight(currentPoint, mapSize);
            if (newPoint == currentPoint) { break; }
            currentPoint = newPoint;
        }
        return originalMap;
    }
    
    // Valleys are rivers surrounded by flat ground.
    protected List<string> AddValley(List<string> originalMap, string featureType, string specifics = "Plains")
    {
        // Pick a starting point.
        int currentPoint = Random.Range(1, mapSize - 1) * mapSize;
        int newPoint = -1;
        int upperSide = -1;
        int lowerSide = -1;
        for (int i = 0; i < mapSize; i++)
        {
            originalMap[currentPoint] = featureType;
            // Get the point above and below and change them into plains or something.
            upperSide = mapUtility.PointInDirection(currentPoint, 0, mapSize);
            if (upperSide >= 0 && upperSide != currentPoint)
            {
                originalMap[upperSide] = specifics;
            }
            lowerSide = mapUtility.PointInDirection(currentPoint, 3, mapSize);
            if (lowerSide >= 0 && lowerSide != currentPoint)
            {
                originalMap[lowerSide] = specifics;
            }
            newPoint = mapUtility.RandomPointRight(currentPoint, mapSize);
            if (newPoint == currentPoint) { break; }
            currentPoint = newPoint;
        }
        return originalMap;
    }

    // Walls can go straight from top to bottom or they can try to curve.
    protected List<string> AddWall(List<string> originalMap, string featureType, string specifics)
    {
        int currentPoint = Random.Range(1, mapSize - 1);
        int newPoint = -1;
        for (int i = 0; i < 2 * mapSize; i++)
        {
            originalMap[currentPoint] = featureType;
            newPoint = mapUtility.RandomPointDown(currentPoint, mapSize);
            if (newPoint == currentPoint) { break; }
            currentPoint = newPoint;
        }
        return originalMap;
    }

    public List<int> GetCenterWallTiles()
    {
        List<int> centerPoints = new List<int>();
        int currentPoint = mapSize / 2;
        for (int i = 0; i < mapSize; i++)
        {
            if (currentPoint >= mapSize * mapSize){break;}
            centerPoints.Add(currentPoint);
            currentPoint += mapSize;
        }
        return centerPoints;
    }

    // Wall through the center.
    protected List<string> AddCenterWall(List<string> originalMap, string featureType, string specifics)
    {
        int currentPoint = mapSize / 2;
        for (int i = 0; i < mapSize; i++)
        {
            if (currentPoint >= mapSize * mapSize){break;}
            originalMap[currentPoint] = featureType;
            currentPoint += mapSize;
        }
        return originalMap;
    }

    // Borders are along the edges of the map.
    protected List<string> AddBorder(List<string> originalMap, string featureType, string specifics)
    {
        int currentPoint = 0;
        originalMap[currentPoint] = featureType;
        // Top
        for (int i = 0; i < mapSize - 1; i++)
        {
            currentPoint++;
            originalMap[currentPoint] = featureType;
        }
        // Right
        for (int i = 0; i < mapSize - 1; i++)
        {
            currentPoint += mapSize;
            originalMap[currentPoint] = featureType;
        }
        // Bottom
        for (int i = 0; i < mapSize - 1; i++)
        {
            currentPoint--;
            originalMap[currentPoint] = featureType;
        }
        // Left
        for (int i = 0; i < mapSize - 1; i++)
        {
            currentPoint -= mapSize;
            originalMap[currentPoint] = featureType;
        }
        return originalMap;
    }

    public List<int> CreatePath(int startPoint, int endPoint, int size, bool right = true)
    {
        List<int> path = new List<int>();
        path.Add(startPoint);
        // Get the horizontal/vertical distance between the points.
        int horiDist = size - 1;//Mathf.Abs(mapUtility.HorizontalDistanceBetweenTiles(startPoint, endPoint, size));
        int vertDist = mapUtility.VerticalDistanceBetweenTiles(startPoint, endPoint, size);
        int nextPoint = startPoint;
        for (int i = horiDist - 1; i >= 1; i--)
        {
            // Make sure you don't go too far up or down.
            // Check if your verticality is too much.
            vertDist = mapUtility.VerticalDistanceBetweenTiles(startPoint, endPoint, size);
            if (Mathf.Abs(vertDist) > i / 2)
            {
                if (vertDist < 0)
                {
                    // Go up.
                    if (right)
                    {
                        nextPoint = mapUtility.PointInDirection(nextPoint, 1, size);
                    }
                    else
                    {
                        nextPoint = mapUtility.PointInDirection(nextPoint, 5, size);
                    }
                }
                else if (vertDist > 0)
                {
                    // Go down.
                    if (right)
                    {
                        nextPoint = mapUtility.PointInDirection(nextPoint, 2, size);
                    }
                    else
                    {
                        nextPoint = mapUtility.PointInDirection(nextPoint, 4, size);
                    }
                }
                else
                {
                    if (right)
                    {
                        nextPoint = mapUtility.RandomPointRight(nextPoint, size);
                    }
                    else
                    {
                        nextPoint = mapUtility.RandomPointLeft(nextPoint, size);
                    }
                }
            }
            else
            {
                // If not too far then distance go rightup or rightdown.
                if (right)
                {
                    nextPoint = mapUtility.RandomPointRight(nextPoint, size);
                }
                else
                {
                    nextPoint = mapUtility.RandomPointLeft(nextPoint, size);
                }
            }
            if (nextPoint < 0)
            {
                if (right)
                {
                    nextPoint = mapUtility.RandomPointRight(nextPoint, size);
                }
                else
                {
                    nextPoint = mapUtility.RandomPointLeft(nextPoint, size);
                }
            }
            // Make sure the next point is adjacent to the previous point or restart.
            if (mapUtility.DistanceBetweenTiles(nextPoint, path[path.Count - 1], size) > 1)
            {
                return CreatePath(startPoint, endPoint, size, right);
            }
            path.Add(nextPoint);
        }
        // Make sure the end point is adjacent to the next point or restart.
        if (mapUtility.DistanceBetweenTiles(nextPoint, endPoint, size) > 1)
        {
            return CreatePath(startPoint, endPoint, size, right);
        }
        path.Add(endPoint);
        return path;
    }
}
