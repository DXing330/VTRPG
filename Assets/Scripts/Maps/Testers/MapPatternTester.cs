using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPatternTester : MonoBehaviour
{
    public MapPatternLocations mapPatterns;
    public int mapSize;
    public int testSize;
    public string testPattern;
    public List<MapTile> mapTiles;

    [ContextMenu("Test Pattern")]
    public void TestPattern()
    {
        List<int> test = mapPatterns.ReturnTilesOfPattern(testPattern, testSize, mapSize);
        for (int i = 0; i < test.Count; i++)
        {
            mapTiles[test[i]].UpdateText(i.ToString());
        }
    }
}
