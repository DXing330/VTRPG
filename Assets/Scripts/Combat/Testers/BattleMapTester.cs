using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMapTester : MonoBehaviour
{
    public BattleMap testMap;
    public StatDatabase tileTileChanges;
    [ContextMenu("Test Tile > Tile Changes")]
    public void TestTileToTileChanges()
    {
        bool allPass = true;
        List<string> possibleChanges = tileTileChanges.keys;
        List<string> possibleResults = tileTileChanges.values;
        for (int i = 0; i < possibleChanges.Count; i++)
        {
            string[] t_t = possibleChanges[i].Split("-");
            testMap.ChangeTerrain(0, t_t[0], true);
            testMap.ChangeTerrain(0, t_t[1]);
            string result = possibleResults[i];
            string actualResult = testMap.mapInfo[0];
            if (result == "" && actualResult != t_t[1])
            {
                Debug.Log(possibleChanges[i] + " failed.");
                Debug.Log("Expected = " + t_t[1] + " / Actual = " + actualResult);
                allPass = false;
            }
            else if (result != "" && actualResult != result)
            {
                Debug.Log(possibleChanges[i] + " failed.");
                Debug.Log("Expected = " + result + " / Actual = " + actualResult);
                allPass = false;
            }
        }
        Debug.Log(allPass);
    }

    public StatDatabase effectEffectChanges;
    [ContextMenu("Test TEffect > TEffect Changes")]
    public void TestTEffectToTEffectChanges()
    {
        bool allPass = true;
        List<string> possibleChanges = effectEffectChanges.keys;
        List<string> possibleResults = effectEffectChanges.values;
        for (int i = 0; i < possibleChanges.Count; i++)
        {
            string[] t_t = possibleChanges[i].Split("-");
            testMap.ChangeTEffect(0, t_t[0], true);
            testMap.ChangeTEffect(0, t_t[1]);
            string result = possibleResults[i];
            string actualResult = testMap.terrainEffectTiles[0];
            if (result == "" && actualResult != t_t[1])
            {
                Debug.Log(possibleChanges[i] + " failed.");
                Debug.Log("Expected = " + t_t[1] + " / Actual = " + actualResult);
                allPass = false;
            }
            else if (result.Contains("ChainReplace"))
            {
                string[] cRS = result.Split(">>");
                if (cRS.Length < 2 && actualResult != t_t[1])
                {
                    Debug.Log(possibleChanges[i] + " failed.");
                    Debug.Log("Expected = " + t_t[1] + " / Actual = " + actualResult);
                    allPass = false;
                }
                else if (cRS.Length >= 2 && actualResult != cRS[1])
                {
                    Debug.Log(possibleChanges[i] + " failed.");
                    Debug.Log("Expected = " + cRS[1] + " / Actual = " + actualResult);
                    allPass = false;
                }
            }
            else if (result != "" && actualResult != result)
            {
                Debug.Log(possibleChanges[i] + " failed.");
                Debug.Log("Expected = " + result + " / Actual = " + actualResult);
                allPass = false;
            }
        }
        Debug.Log(allPass);
    }
    public List<int> testTEffectTiles;
    public int changedTEffectTile;
    public string startingTEffect;
    public string changedTEffect;
    [ContextMenu("Test TEffect Change")]
    public void TestTEffectChange()
    {
        testMap.ForceStart();
        for (int i = 0; i < testTEffectTiles.Count; i++)
        {
            testMap.ChangeTEffect(testTEffectTiles[i], startingTEffect, true);
        }
        testMap.ChangeTEffect(changedTEffectTile, changedTEffect);
        testMap.UpdateMap();
    }
}
