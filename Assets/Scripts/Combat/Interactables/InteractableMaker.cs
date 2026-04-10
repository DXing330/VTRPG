using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableMaker : MonoBehaviour
{
    public MapMaker mapMaker;
    public Interactable interactablePrefab;
    public StatDatabase featureToInteractables;
    public StatDatabase interactableStats;

    public Interactable CreateInteractable(string newName, string newStats = "")
    {
        Interactable newInter = Instantiate(interactablePrefab, transform.position, new Quaternion(0, 0, 0, 0));
        newInter.ResetStats();
        if (newStats != "")
        {
            newInter.SetStats(newStats);
            return newInter;
        }
        else
        {
            // Get the stats from the name.
            newInter.SetStats(interactableStats.ReturnValue(newName));
        }
        return newInter;
    }

    // Basically only used for placing traps.
    // Later make sure traps only trigger for the opposite team?
    public void PlaceInteractable(BattleMap map, string newInfo, int location)
    {
        Interactable newInter = CreateInteractable(newInfo);
        newInter.SetLocation(location);
        newInter.AddTargetLocation(location);
        map.AddInteractable(newInter);
    }

    public void PlaceTrap(BattleMap map, string newInfo, int location, TacticActor placer)
    {
        Interactable newInter = CreateInteractable(newInfo);
        newInter.SetLocation(location);
        newInter.AddTargetLocation(location);
        newInter.ForceCondition("Team<>", placer.GetTeam().ToString());
        map.AddInteractable(newInter);
    }

    public void GetNewInteractables(BattleMap map, List<string> newInfo)
    {
        for (int i = 0; i < newInfo.Count; i++)
        {
            // Split?
            AddInteractableFeature(map, newInfo[i]);
        }
    }

    protected int RandomAdjacentEmptyTile(int tile, List<int> except, BattleMap map)
    {
        List<int> adjacent = map.mapUtility.AdjacentTiles(tile, map.mapSize);
        int rTile = adjacent[Random.Range(0, adjacent.Count)];
        if (except.Contains(rTile))
        {
            return RandomAdjacentEmptyTile(tile, except, map);
        }
        else if (map.InteractableOnTile(rTile))
        {
            return RandomAdjacentEmptyTile(tile, except, map);
        }
        return rTile;
    }

    protected void AddInteractableFeature(BattleMap map, string newFeature)
    {
        List<int> featureTiles = new List<int>();
        int interactableLocation = -1;
        int adjacentToLocation = -1;
        switch (newFeature)
        {
            // Empty, don't make any interactables.
            default:
            return;
            case "WaterBridgeLever":
            //Add a river of deepwater going through the middle of the map.
            map.mapInfo = mapMaker.AddFeature(map.mapInfo, "DeepWater", "CenterWall");
            //Add a lever to a random adjacent point.
            featureTiles = mapMaker.GetCenterWallTiles();
            adjacentToLocation = featureTiles[Random.Range(0, featureTiles.Count)];
            //Make sure no interactable is already on the tile?
            interactableLocation = RandomAdjacentEmptyTile(adjacentToLocation, featureTiles, map);
            break;
            case "GateLever":
            map.mapInfo = mapMaker.AddFeature(map.mapInfo, "Wall", "CenterWall");
            featureTiles = mapMaker.GetCenterWallTiles();
            adjacentToLocation = featureTiles[Random.Range(0, featureTiles.Count)];
            interactableLocation = RandomAdjacentEmptyTile(adjacentToLocation, featureTiles, map);
            break;
        }
        // Make the interactable and add it to the map.
        Interactable newInter = CreateInteractable(newFeature);
        newInter.SetLocation(interactableLocation);
        newInter.AddTargetLocation(adjacentToLocation);
        map.AddInteractable(newInter);
    }
}
