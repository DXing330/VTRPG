using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RectTransformAdjustor : MonoBehaviour
{
    public List<RectTransform> rectTiles;
    public void SetRectTiles(List<RectTransform> newRectTiles)
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
        Initialize();
    }
    public int maxGridWidth;
    public int gridWidth = 16;
    public int offsetWidth;
    public int gridHeight = 6;
    public int offsetHeight;
    [ContextMenu("Initialize")]
    public virtual void Initialize()
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
}
