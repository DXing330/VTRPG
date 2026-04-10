using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactionCityTester : MonoBehaviour
{
    public FactionCity dummyCity;
    public string testData;
    [ContextMenu("Test Load")]
    public void TestLoad()
    {
        dummyCity.SetStats(testData);
    }
    [ContextMenu("Validate Load")]
    public void ValidateLoad()
    {
        if (dummyCity == null)
        {
            Debug.LogWarning("DummyCity is null!");
            return;
        }
        // Load the test data first
        dummyCity.SetStats(testData);
        // 1. Top-level fields
        Debug.Assert(dummyCity.factionName == "IronLegion", "FactionName mismatch");
        Debug.Assert(dummyCity.factionColor == "Red", "FactionColor mismatch");
        Debug.Assert(dummyCity.capital == 3, "Capital mismatch");
        Debug.Assert(dummyCity.reputation == 42, "Reputation mismatch");
        Debug.Assert(dummyCity.location == 17, "Location mismatch");
        Debug.Assert(dummyCity.population == 1200, "Population mismatch");
        Debug.Assert(dummyCity.mana == 85, "Mana mismatch");
        Debug.Assert(dummyCity.gold == 340, "Gold mismatch");
        Debug.Assert(dummyCity.food == 560, "Food mismatch");
        Debug.Assert(dummyCity.materials == 290, "Materials mismatch");
        // 2. OwnedTiles list
        int[] expectedTiles = { 5, 6, 7, 12, 18 };
        Debug.Assert(dummyCity.ownedTiles.Count == expectedTiles.Length, "OwnedTiles count mismatch");
        for (int i = 0; i < expectedTiles.Length; i++)
        {
            Debug.Assert(dummyCity.ownedTiles[i] == expectedTiles[i], $"OwnedTile at index {i} mismatch");
        }
        // 3. Treasures list
        string[] expectedTreasures = { "AncientCoin", "RunedGem", "RoyalSeal" };
        Debug.Assert(dummyCity.treasures.Count == expectedTreasures.Length, "Treasures count mismatch");
        for (int i = 0; i < expectedTreasures.Length; i++)
        {
            Debug.Assert(dummyCity.treasures[i] == expectedTreasures[i], $"Treasure at index {i} mismatch");
        }
        Debug.Log("Validation completed successfully!");
    }
}
