using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BanditManager", menuName = "ScriptableObjects/DataContainers/SavedData/BanditManager", order = 1)]
public class BanditManager : OverworldEnemyManager
{
    // Can't be too close to a city, it would be too easy for city guards to eliminate.
    public int minDistanceFromCity;

    public override void GenerateSpawner()
    {
        // Check if the max amount of spawners has been reached.
        if (savedOverworld.ReturnFeatureCount(overworldSpawnerSprite) >= maxSpawners) { return; }
        // Try to find a good tile.
        int newLocation = savedOverworld.RandomTile();
        if (spawnableTerrain.Contains(savedOverworld.ReturnTerrain(newLocation)) && savedOverworld.ReturnClosestCityDistance(newLocation) > minDistanceFromCity)
        {
            // Create a spawner feature on the tile.
            savedOverworld.AddFeature(overworldSpawnerSprite, newLocation.ToString());
        }
    }
}
