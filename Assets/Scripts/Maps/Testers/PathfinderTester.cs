using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfinderTester : MonoBehaviour
{
    public bool debugThis;
    public MapPathfinder pathfinder;
    public MapUtility mapUtility;
    public List<MapTile> mapTiles;
    public List<int> highlightedTiles;
    public string highlightColor;
    public MapDisplayer highlightDisplayer;
    [ContextMenu("ResetHighlights")]
    public void ResetHighlights()
    {
        highlightedTiles.Clear();
        for (int i = 0; i < mapTiles.Count; i++)
        {
            highlightedTiles.Add(-1);
            highlightDisplayer.HighlightTilesInSetColor(mapTiles, highlightedTiles, "");
        }
    }
    public void HighlightTiles()
    {
        highlightDisplayer.HighlightTilesInSetColor(mapTiles, highlightedTiles, highlightColor);
    }
    public int testConeStart;
    public int testTile;
    public int testDirection;
    public int testSize;
    public int testRange;

    [ContextMenu("Test All Tiles")]
    public void MultipleTests()
    {
        StartCoroutine(TestAllTiles());
    }

    IEnumerator TestAllTiles()
    {
        pathfinder.SetMapSize(testSize);
        for (int i = 0; i < mapTiles.Count; i++)
        {
            ResetHighlights();
            highlightedTiles = pathfinder.FindTilesInRange(i, testRange);
            HighlightTiles();
            yield return new WaitForSeconds(0.3f);
        }
    }

    [ContextMenu("Test Find Tiles")]
    public void TestFindTiles()
    {
        ResetHighlights();
        pathfinder.SetMapSize(testSize);
        highlightedTiles = pathfinder.FindTilesInRange(testTile, testRange);
        HighlightTiles();
        if (!debugThis){return;}
        highlightedTiles.Sort();
        for (int i = 0; i < highlightedTiles.Count; i++)
        {
            Debug.Log(highlightedTiles[i]);
        }
    }

    [ContextMenu("Test Direction Check")]
    public void TestDirectionCheck()
    {
        if (!debugThis){return;}
        for (int i = 0; i < 6; i++)
        {
            Debug.Log(pathfinder.mapUtility.PointInDirection(testTile, i, testSize));
        }
    }

    [ContextMenu("Test Beam Range")]
    public void TestBeamRange()
    {
        ResetHighlights();
        highlightedTiles = mapUtility.GetTilesInBeamShape(testTile, testDirection, testRange, testSize);
        HighlightTiles();
        if (!debugThis){return;}
        pathfinder.SetMapSize(testSize);
    }

    [ContextMenu("Test Cone Range")]
    public void TestConeRange()
    {
        ResetHighlights();
        highlightedTiles = mapUtility.GetTilesInConeShape(mapUtility.PointInDirection(testTile, testDirection, testSize), testRange, testTile, testSize);
        HighlightTiles();
        if (!debugThis){return;}
        pathfinder.SetMapSize(testSize);
    }
}
