using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MapShapeTester : MapManager
{
    protected override void Start()
    {
        ResetTest();
    }
    public List<string> shapes;
    public int shapeIndex;
    public string selectedShape;
    public TMP_Text selectedShapeText;
    public int span;
    public TMP_Text spanText;
    public int startTile;
    public int selectedTile;
    public List<int> shapeTiles;
    public void ResetTest()
    {
        startTile = -1;
        selectedTile = -1;
        selectedShapeText.text = selectedShape;
        spanText.text = span.ToString();
        shapeTiles.Clear();
        ResetHighlights();
    }
    public Color startColor;
    public Color endColor;
    public Color shapeColor;
    public void ChangeShape(bool right)
    {
        shapeIndex = utility.ChangeIndex(shapeIndex, right, shapes.Count - 1);
        selectedShape = shapes[shapeIndex];
        selectedShapeText.text = selectedShape;
        ResetTest();
    }
    public void ChangeSpan(bool right)
    {
        span = utility.ChangeIndex(span, right, mapSize - 1);
        spanText.text = span.ToString();
        ResetTest();
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
        for (int i = 0; i < shapeTiles.Count; i++)
        {
            mapTiles[shapeTiles[i]].HighlightTile(shapeColor);
        }
        if (startTile >= 0)
        {
            mapTiles[startTile].HighlightTile(startColor);
        }
        if (selectedTile >= 0)
        {
            mapTiles[selectedTile].HighlightTile(endColor);
        }
    }
    public override void ClickOnTile(int tileNumber)
    {
        if (startTile < 0)
        {
            startTile = tileNumber;
            HighlightPathTiles();
            return;
        }
        else if (selectedTile < 0)
        {
            selectedTile = tileNumber;
            shapeTiles = mapUtility.GetTilesByShapeSpan(selectedTile, selectedShape, span, mapSize, startTile);
            HighlightPathTiles();
            return;
        }
        else
        {
            ResetTest();
        }
    }
}
