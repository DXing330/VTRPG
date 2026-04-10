using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveTester : MonoBehaviour
{
    public BattleMap map;
    public MoveCostManager moveCostManager;
    public ActiveManager activeManager;
    public int startTile;
    public string skillName;
    public int skillRange;
    public List<int> targetableTiles;

    [ContextMenu("Get Targetable Tiles")]
    public void GetTargetableTiles()
    {
        moveCostManager.SetMapInfo(map.mapInfo);
        activeManager.SetSkillFromName(skillName, null);
        targetableTiles = activeManager.GetTargetableTiles(startTile, moveCostManager.actorPathfinder);
        // Pick a random tile.
        activeManager.GetTargetedTiles(targetableTiles[Random.Range(0, targetableTiles.Count)], moveCostManager.actorPathfinder);
        map.UpdateHighlights(targetableTiles);
        //map.UpdateHighlights(activeManager.targetedTiles, "", 4);
    }
}
