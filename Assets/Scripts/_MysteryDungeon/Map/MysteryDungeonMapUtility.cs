using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MysteryDungeonMapUtility", menuName = "ScriptableObjects/Utility/MysteryDungeonMapUtility", order = 1)]
public class MysteryDungeonMapUtility : ScriptableObject
{
    public bool IsTilePassable(string terrain)
    {
        switch (terrain)
        {
            case "Plains":
            case "Forest":
            case "Mountain":
            case "Water":
            case "Desert":
            case "Volcano":
            case "Snow":
            case "SnowForest":
            case "SnowMountain":
            case "Oasis":
                return true;
            case "Pit":
            case "Wall":
            case "DeepWater":
                return false;
        }
        return false;
    }
    public List<int> GetEmptyTiles(List<int> tiles, MysteryDungeonBattleMap map)
    {
        for (int i = tiles.Count - 1; i >= 0; i--)
        {
            if (!IsTilePassable(map.mapInfo[tiles[i]]) || map.GetActorOnTile(tiles[i]) != null)
            {
                tiles.RemoveAt(i);
            }
        }
        return tiles;
    }
    public List<int> GetEmptyTilesInCircleRange(int start, MysteryDungeonBattleMap map, int span = 1)
    {
        List<int> empty = map.mapUtility.GetTilesInCircleShape(start, span, map.mapSize);
        empty.Remove(start);
        return GetEmptyTiles(empty, map);
    }
}
