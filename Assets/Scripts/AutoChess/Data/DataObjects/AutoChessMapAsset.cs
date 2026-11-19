using UnityEngine;
using System.Collections.Generic;

public enum StartingMapTile
{
    Plains,
    Forest,
    Water,
    Mountain,
    Desert,
    Snow,
    DeepWater,
    Pit
}

public enum TerrainEffect
{
    None,
    Thorns,
    Fire,
    ToxicMist,
    Fog,
    Ice,
    Oil,
    OilFire,
    Water,
    ElectrifiedWater
}

[CreateAssetMenu(fileName = "AutoChessMapAsset", menuName = "ScriptableObjects/AutoChessDataObjects/AutoChessMapAsset", order = 1)]
public class AutoChessMapAsset : ScriptableObject
{
    public int gridSize = 7;
    public string mapName;
    public StartingMapTile[] tiles;
    public TerrainEffect[] terrainEffects;
    // 0 = Invalid, 1 = Valid
    public bool[] playerSpawnZones;
    public bool[] enemySpawnZones;
    protected void OnValidate()
    {
        gridSize = Mathf.Max(1, gridSize);
        int size = gridSize * gridSize;
        if (tiles == null || tiles.Length != size)
            System.Array.Resize(ref tiles, size);
        if (terrainEffects == null || terrainEffects.Length != size)
            System.Array.Resize(ref terrainEffects, size);
        if (playerSpawnZones == null || playerSpawnZones.Length != size)
            System.Array.Resize(ref playerSpawnZones, size);
        if (enemySpawnZones == null || enemySpawnZones.Length != size)
            System.Array.Resize(ref enemySpawnZones, size);
    }
    // Convert terrain tiles into strings
    public List<string> GetTilesAsStrings()
    {
        List<string> result = new List<string>(tiles.Length);

        foreach (StartingMapTile tile in tiles)
            result.Add(tile.ToString());

        return result;
    }

    // Convert terrain effects into strings
    public List<string> GetTerrainEffectsAsStrings()
    {
        List<string> result = new List<string>(terrainEffects.Length);

        foreach (TerrainEffect effect in terrainEffects)
            result.Add(effect.ToString());

        return result;
    }

    // Convert player spawn zones into 0/1 strings
    public List<string> GetPlayerSpawnZonesAsStrings()
    {
        List<string> result = new List<string>(playerSpawnZones.Length);

        foreach (bool valid in playerSpawnZones)
            result.Add(valid ? "1" : "0");

        return result;
    }

    // Convert enemy spawn zones into 0/1 strings
    public List<string> GetEnemySpawnZonesAsStrings()
    {
        List<string> result = new List<string>(enemySpawnZones.Length);

        foreach (bool valid in enemySpawnZones)
            result.Add(valid ? "1" : "0");

        return result;
    }
}
