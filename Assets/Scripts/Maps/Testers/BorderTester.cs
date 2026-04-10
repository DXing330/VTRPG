using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BorderTester : MonoBehaviour
{
    public List<MapTile> mapTiles;
    public string testBorder;
    public SpriteContainer borderSprites;
    [ContextMenu("Set Borders")]
    public void SetBorders()
    {
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].SetAllBorders(testBorder, borderSprites.SpriteDictionary(testBorder));
        }
    }
}
