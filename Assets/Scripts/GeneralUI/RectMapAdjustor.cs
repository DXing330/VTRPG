using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RectMapAdjustor : MonoBehaviour
{
    public Sprite defaultSprite;
    public List<RectTransform> rectTiles;
    public List<MapTile> mapTiles;
    public int gridWidth = 16;
    public int offsetWidth;
    public int gridHeight = 6;
    public int offsetHeight;

    [ContextMenu("Initialize")]
    protected virtual void Initialize()
    {
        int tileIndex = 0;
        float xPivot = 0f + (float)offsetWidth/(gridWidth + offsetWidth + offsetWidth - 1);
        float yPivot = 1f - (float)offsetHeight/(gridHeight + offsetHeight + offsetHeight - 1);;
        for (int i = 0; i < gridHeight; i++)
        {
            // Start from the left every iteration.
            for (int j = 0; j < gridWidth; j++)
            {
                // Set the pivot.
                rectTiles[tileIndex].pivot = new Vector2(xPivot, yPivot);
                mapTiles[tileIndex].SetTileNumber(tileIndex);
                mapTiles[tileIndex].UpdateLayerSprite(defaultSprite);
                tileIndex++;
                xPivot += 1f/(gridWidth + offsetWidth + offsetWidth - 1);
            }
            // Move down every iteration.
            yPivot -= 1f/(gridHeight + offsetHeight + offsetHeight - 1);
            xPivot = 0f + (float)offsetWidth/(gridWidth + offsetWidth + offsetWidth - 1);
        }
    }
}
