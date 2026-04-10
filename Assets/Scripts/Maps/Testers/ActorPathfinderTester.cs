using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorPathfinderTester : MapManager
{
    protected override void Start()
    {
        RefreshData();
        ShowTileNumbers();
    }
    public ActorPathfinder pathfinder;
    public string defaultTile = "Plains";
    [ContextMenu("Refresh Data")]
    public void RefreshData()
    {
        // Set it all to the default tiles.
        for (int i = 0; i < mapInfo.Count; i++)
        {
            mapInfo[i] = defaultTile;
        }
        moveManager.SetMapInfo(mapInfo);
        // Randomize Elevations.
        InitializeElevations();
        moveManager.SetMapElevations(mapElevations);
        // Randomize Borders.
        RandomizeBorders();
        moveManager.SetBorders(borderDetails);
        // Set the move manager which will set the pathfinder.
        UpdateMap();
    }
    public MoveCostManager moveManager;
    public int startPathTile;
    public Color startPathColor;
    public int endPathTile;
    public Color endPathColor;
    public List<int> path;
    public Color pathColor;
    protected void ResetPath()
    {
        startPathTile = -1;
        endPathTile = -1;
        path.Clear();
        ResetHighlights();
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
            mapTiles[startPathTile].HighlightTile(startPathColor);
        }
        if (endPathTile >= 0)
        {
            mapTiles[endPathTile].HighlightTile(endPathColor);
        }
        for (int i = 0; i < path.Count; i++)
        {
            mapTiles[path[i]].HighlightTile(pathColor);
        }
    }
    public override void ClickOnTile(int tileNumber)
    {
        if (startPathTile < 0)
        {
            startPathTile = tileNumber;
            moveManager.ClickOnStartTile(startPathTile);
            HighlightPathTiles();
            return;
        }
        if (endPathTile < 0)
        {
            endPathTile = tileNumber;
            // Make the path.
            path = moveManager.GetPrecomputedPath(startPathTile, endPathTile);
            for (int i = path.Count - 1; i >= 1; i--)
            {
                //Debug.Log("Border Cost from: " + path[i] + " -> " + path[i-1] + " = " + pathfinder.GetBorderCost(path[i], path[i-1]));
            }
            // Show the path cost.
            Debug.Log(moveManager.moveCost);
            HighlightPathTiles();
            return;
        }
        else
        {
            ResetPath();
        }
    }
}
