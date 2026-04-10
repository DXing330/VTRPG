using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexMapAdjustor : MonoBehaviour
{
    public bool showText = false;
    public bool pointyTop = false;
    public MapUtility mapUtility;
    public Sprite defaultSprite;
    public int gridSize = 9;
    public bool adjustElevation = false;
    public int minElevation;
    public int maxElevation;
    public List<RectTransform> hexTiles;
    public List<MapTile> mapTiles;

    [ContextMenu("InitializeElevations")]
    public virtual void InitializeElevations()
    {
        int tileIndex = 0;
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                mapTiles[tileIndex].SetElevation(tileIndex % (maxElevation + 1));
                tileIndex++;
            }
        }
    }

    [ContextMenu("ResetElevations")]
    public virtual void ResetElevations()
    {
        int tileIndex = 0;
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                mapTiles[tileIndex].SetElevation(0);
                tileIndex++;
            }
        }
    }

    protected virtual void InitializePointyTopTiles()
    {
        int tileIndex = 0;
        float xPivot = 0f;
        float xCenter = 1f - (1f/(2*gridSize));
        float yPivot = 1f;
        for (int i = 0; i < gridSize; i++)
        {
            // Start from the left every iteration.
            if (i % 2 == 0)
            {
                xPivot = 0f;
            }
            else
            {
                xPivot = 1f/(2*gridSize);
            }
            for (int j = 0; j < gridSize; j++)
            {
                // Set the pivot.
                hexTiles[tileIndex].pivot = new Vector2(xPivot, yPivot);
                mapTiles[tileIndex].SetTileNumber(tileIndex);
                mapTiles[tileIndex].UpdateLayerSprite(defaultSprite);
                if (adjustElevation)
                {
                    mapTiles[tileIndex].SetElevation(Random.Range(minElevation, maxElevation + 1));
                }
                if (showText)
                {
                    //mapTiles[tileIndex].UpdateText("("+mapUtility.GetHexQ(tileIndex, gridSize)+","+mapUtility.GetHexR(tileIndex, gridSize)+","+mapUtility.GetHexS(tileIndex, gridSize)+")");
                    mapTiles[tileIndex].UpdateText(tileIndex.ToString());
                }
                //mapTiles[tileIndex].UpdateText("("+mapUtility.GetRow(tileIndex, gridSize)+","+mapUtility.GetColumn(tileIndex, gridSize)+")");
                // Move right every step.
                tileIndex++;
                xPivot += 1f/(gridSize);
            }
            // Move down every iteration.
            yPivot -= 1f/(gridSize - 1);
        }
    }

    [ContextMenu("Initialize")]
    protected virtual void InitializeTiles()
    {
        if (pointyTop)
        {
            InitializePointyTopTiles();
            return;
        }
        int tileIndex = 0;
        float scale = 1f/(gridSize+1);
        float xPivot = 0f;
        float yCenter = 1f - (1f/(2*gridSize));
        float yPivot = 1f;
        for (int i = 0; i < gridSize; i++)
        {
            xPivot = 0f;
            for (int j = 0; j < gridSize; j++)
            {
                if (j%2 == 0)
                {
                    yPivot = yCenter + 1f/(4*gridSize);
                }
                else
                {
                    yPivot = yCenter - 1f/(4*gridSize);
                }
                hexTiles[tileIndex].pivot = new Vector2(xPivot, yPivot);
                mapTiles[tileIndex].SetTileNumber(tileIndex);
                mapTiles[tileIndex].UpdateLayerSprite(defaultSprite);
                if (adjustElevation)
                {
                    mapTiles[tileIndex].SetElevation(Random.Range(minElevation, maxElevation + 1));
                }
                //mapTiles[tileIndex].UpdateText(tileIndex.ToString());
                //tiles[tileIndex].SetTileText("("+GetHexQ(tileIndex)+","+GetHexR(tileIndex)+","+GetHexS(tileIndex)+")");
                tileIndex++;
                xPivot += 1f/(gridSize - 1);
            }
            yCenter -= 1f/(gridSize);
        }
    }

    [ContextMenu("ResetText")]
    public void ResetTileText()
    {
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].UpdateText();
        }
    }
}
