using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RectTransformAdjustor : MonoBehaviour
{
    public List<RectTransform> rectTiles;
    public void SetRectTilesToRow(List<RectTransform> newRectTiles)
    {
        rectTiles = newRectTiles;
        gridWidth = rectTiles.Count;
        // Want it to always be centered.
        if (rectTiles.Count < maxGridWidth)
        {
            gridHeight = 1;
        }
        else
        {
            gridHeight = 1 + rectTiles.Count / maxGridWidth;
        }
        InitializeRow();
    }
    public void SetRectTilesToColumn(List<RectTransform> newRectTiles)
    {
        rectTiles = newRectTiles;
        gridHeight = rectTiles.Count;
        if (rectTiles.Count < maxGridHeight)
        {
            gridWidth = 1;
        }
        else
        {
            gridWidth = 1 + rectTiles.Count / maxGridHeight;
        }
        InitializeColumn();
    }
    public int maxGridWidth;
    public int gridWidth = 16;
    public int offsetWidth;
    public int maxGridHeight;
    public int gridHeight = 6;
    public int offsetHeight;
    [ContextMenu("Initialize")]
    public virtual void Initialize()
    {
        int tileIndex = 0;
        float xPivot = 0f + (float)offsetWidth/(gridWidth + offsetWidth + offsetWidth - 1);
        float yPivot = 0.5f;
        if (gridHeight != 1)
        {
            yPivot = 1f - (float)offsetHeight/(gridHeight + offsetHeight + offsetHeight - 1);
        }
        for (int i = 0; i < gridHeight; i++)
        {
            // Start from the left every iteration.
            for (int j = 0; j < gridWidth; j++)
            {
                // Set the pivot.
                rectTiles[tileIndex].pivot = new Vector2(xPivot, yPivot);
                tileIndex++;
                xPivot += 1f/(gridWidth + offsetWidth + offsetWidth - 1);
                if (tileIndex > rectTiles.Count){return;}
            }
            // Move down every iteration.
            yPivot -= 1f/(gridHeight + offsetHeight + offsetHeight - 1);
            xPivot = 0f + (float)offsetWidth/(gridWidth + offsetWidth + offsetWidth - 1);
        }
    }
    [ContextMenu("InitializeRow")]
    public virtual void InitializeRow()
    {
        // Should Always Try To Center Them If Possible.
        gridWidth = rectTiles.Count;
        if (gridWidth < maxGridWidth)
        {
            offsetWidth = (maxGridWidth - gridWidth) / 2;
        }
        else
        {
            offsetWidth = 0;
        }
        int tileIndex = 0;
        float xPivot = 0f + (float)offsetWidth/(gridWidth + offsetWidth + offsetWidth - 1);
        float yPivot = 0.5f;
        if (gridHeight != 1)
        {
            yPivot = 1f - (float)offsetHeight/(gridHeight + offsetHeight + offsetHeight - 1);
        }
        for (int i = 0; i < gridHeight; i++)
        {
            // Start from the left every iteration.
            for (int j = 0; j < gridWidth; j++)
            {
                // Set the pivot.
                rectTiles[tileIndex].pivot = new Vector2(xPivot, yPivot);
                tileIndex++;
                xPivot += 1f/(gridWidth + offsetWidth + offsetWidth - 1);
                if (tileIndex > rectTiles.Count){return;}
            }
            // Move down every iteration.
            yPivot -= 1f/(gridHeight + offsetHeight + offsetHeight - 1);
            xPivot = 0f + (float)offsetWidth/(gridWidth + offsetWidth + offsetWidth - 1);
        }
    }
    [ContextMenu("InitializeColumn")]
    public virtual void InitializeColumn()
    {
        // Should Always Try To Center Them If Possible.
        gridHeight = rectTiles.Count;
        if (gridHeight < maxGridHeight)
        {
            offsetHeight = (maxGridHeight - gridHeight) / 2;
        }
        else
        {
            offsetHeight = 0;
        }
        int tileIndex = 0;
        float xPivot = 0.5f;
        float yPivot = 1f - (float)offsetHeight / (gridHeight + offsetHeight + offsetHeight - 1);
        if (gridWidth != 1)
        {
            xPivot = 0f;
        }
        for (int i = 0; i < gridWidth; i++)
        {
            // Start from the top every iteration.
            for (int j = 0; j < gridHeight; j++)
            {
                // Set the pivot.
                rectTiles[tileIndex].pivot = new Vector2(xPivot, yPivot);
                tileIndex++;
                yPivot -= 1f / (gridHeight + offsetHeight + offsetHeight - 1);
                if (tileIndex >= rectTiles.Count) { return; }
            }
            // Move right every iteration.
            xPivot += 1f / (gridWidth + offsetWidth + offsetWidth - 1);
            yPivot = 1f - (float)offsetHeight / (gridHeight + offsetHeight + offsetHeight - 1);
        }
    }
}
