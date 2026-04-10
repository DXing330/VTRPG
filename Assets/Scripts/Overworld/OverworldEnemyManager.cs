using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverworldEnemyManager : SavedData
{
    // Directly add characters/features to the overworld.
    public SavedOverworld savedOverworld;
    // Required for moving enemies in the overworld.
    public MapUtility mapUtility;
    // Enemies can only spawn on their designated terrain.
    public List<string> spawnableTerrain;
    // How often enemies spawn, maybe make this a setting or adjustable later.
    public int spawnRate;
    // If you get too close, the enemies will chase you.
    public int chaseRange = 3;
    // Different types of enemies have different overworld travelling speed.
    public int moveSpeed = 1;
    public override void NewDay(int dayCount)
    {
        ResetCurrentEnemies();
        if (dayCount % spawnRate == 0)
        {
            GenerateSpawner();
            SpawnEnemies();
        }
    }
    public string overworldEnemySprite;
    public int maxEnemies;
    public string overworldSpawnerSprite;
    public int maxSpawners;
    public List<string> enemyPool;
    public int minEnemyGroupSize;
    public int maxEnemyGroupSize;
    public List<string> currentEnemies;
    public void ResetCurrentEnemies()
    {
        currentEnemies.Clear();
    }
    public List<string> GetCurrentEnemies()
    {
        return currentEnemies;
    }
    public virtual bool EnemiesOnTile(int tileNumber)
    {
        if (savedOverworld.SpecificCharacterOnTile(overworldEnemySprite, tileNumber))
        {
            ResetCurrentEnemies();
            GenerateEnemies();
            RemoveEnemiesAtLocation(tileNumber);
            return true;
        }
        return false;
    }
    public virtual void GenerateEnemies()
    {
        int enemyGroupSize = Random.Range(minEnemyGroupSize, maxEnemyGroupSize + 1);
        for (int i = 0; i < enemyGroupSize; i++)
        {
            currentEnemies.Add(enemyPool[Random.Range(0, enemyPool.Count)]);
        }
    }
    public virtual void MoveEnemies(int except)
    {
        List<string> enemyLocations = savedOverworld.ReturnLocationsOfCharacter(overworldEnemySprite);
        for (int i = 0; i < enemyLocations.Count; i++)
        {
            if (enemyLocations[i] == except.ToString()){ continue; }
            if (savedOverworld.ReturnCharacterDistanceFromPlayer(enemyLocations[i]) <= chaseRange)
            {
                Debug.Log(savedOverworld.ReturnCharacterDistanceFromPlayer(enemyLocations[i]));
                for (int j = 0; j < moveSpeed; j++)
                {
                    int direction = savedOverworld.ReturnCharacterDirectionFromPlayer(enemyLocations[i]);
                    if (direction < 0) { continue; }
                    enemyLocations[i] = savedOverworld.MoveCharacterInDirection(enemyLocations[i], direction);
                    Debug.Log(savedOverworld.ReturnCharacterDistanceFromPlayer(enemyLocations[i]));
                }
            }
            else
            {
                // Move randomly.
                enemyLocations[i] = savedOverworld.MoveCharacterInDirection(enemyLocations[i]);
            }
        }
    }
    public virtual void GenerateSpawner()
    {
        // Check if the max amount of spawners has been reached.
        if (savedOverworld.ReturnFeatureCount(overworldSpawnerSprite) >= maxSpawners) { return; }
        // Try to find a good tile.
        int newLocation = savedOverworld.RandomTile();
        if (spawnableTerrain.Contains(savedOverworld.ReturnTerrain(newLocation)))
        {
            // Create a spawner feature on the tile.
            savedOverworld.AddFeature(overworldSpawnerSprite, newLocation.ToString());
        }
    }
    public virtual void SpawnEnemies()
    {
        // Check if the max amount of enemies has been reached.
        if (savedOverworld.ReturnCharacterCount(overworldEnemySprite) >= maxEnemies) { return; }
        // Create enemies at spawner locations.
        List<string> spawnerLocations = savedOverworld.ReturnLocationsOfFeature(overworldSpawnerSprite);
        for (int i = 0; i < spawnerLocations.Count; i++)
        {
            savedOverworld.AddCharacter(overworldEnemySprite, spawnerLocations[i]);
        }
    }
    public virtual void RemoveEnemiesAtLocation(int location)
    {
        savedOverworld.RemoveCharacterAtLocation(location, overworldEnemySprite);
    }
}
