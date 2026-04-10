using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MapPathfinderTester : MapManager
{
    public MapPathfinder pathfinder;
    public TMP_Text testingText;
    public List<string> testingTexts;
    public int state = 0;
    public void SetState(int newInfo)
    {
        state = newInfo;
        testingText.text = testingTexts[state];
        ResetAll();
    }
    protected override void Start()
    {
        pathfinder.SetMapSize(mapSize);
        SetState(0);
    }
    protected void ResetAll()
    {
        ResetPath();
        ResetBorders();
    }
    protected void ResetPath()
    {
        startPathTile = -1;
        endPathTile = -1;
        path.Clear();
        ResetHighlights();
    }
    protected void ResetBorders()
    {
        ownedTiles.Clear();
        ResetHighlights();
    }
    public int startPathTile;
    public Color startPathColor;
    public int endPathTile;
    public Color endPathColor;
    public List<int> path;
    public List<int> ownedTiles;
    public List<int> borders;
    public Color pathColor;
    protected void PathClickOnTile(int tileNumber)
    {
        if (startPathTile < 0)
        {
            startPathTile = tileNumber;
            HighlightPathTiles();
            return;
        }
        if (endPathTile < 0)
        {
            endPathTile = tileNumber;
            // Make the path.
            if (state == 0)
            {
                path = pathfinder.BasicPathToTile(startPathTile, endPathTile);
            }
            else if (state == 3)
            {
                path = pathfinder.StraightPathToTile(startPathTile, endPathTile);
            }
            else if (state == 4)
            {
                path = pathfinder.ShortestPathToTile(startPathTile, endPathTile);
            }
            HighlightPathTiles();
            return;
        }
        else
        {
            ResetPath();
        }
    }
    protected void BordersClickOnTile(int tileNumber)
    {
        if (ownedTiles.Contains(tileNumber))
        {
            ownedTiles.Remove(tileNumber);
        }
        else
        {
            ownedTiles.Add(tileNumber);
        }
        HighlightBorderTiles();
    }
    public override void ClickOnTile(int tileNumber)
    {
        if (state == 0 || state == 3 || state == 4)
        {
            PathClickOnTile(tileNumber);
        }
        else if (state == 1 || state == 2)
        {
            BordersClickOnTile(tileNumber);
        }
    }
    public void ResetHighlights()
    {
        InitializeEmptyList();
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].ResetHighlight();
        }
    }
    public void HighlightPathTiles()
    {
        if (startPathTile >= 0)
        {
            mapTiles[startPathTile].HighlightLayer(4, startPathColor);
        }
        if (endPathTile >= 0)
        {
            mapTiles[endPathTile].HighlightLayer(4, endPathColor);
        }
        for (int i = 0; i < path.Count; i++)
        {
            mapTiles[path[i]].HighlightLayer(4, pathColor);
        }
    }
    public void HighlightBorderTiles()
    {
        ResetHighlights();
        for (int i = 0; i < ownedTiles.Count; i++)
        {
            mapTiles[ownedTiles[i]].HighlightLayer(4, startPathColor);
        }
        if (state == 1)
        {
            borders = mapUtility.AdjacentBorders(ownedTiles, mapSize);
        }
        else if (state == 2)
        {
            borders = mapUtility.BorderTileSet(ownedTiles, mapSize);
        }
        for (int i = 0; i < borders.Count; i++)
        {
            mapTiles[borders[i]].HighlightLayer(4, endPathColor);
        }
    }
}
