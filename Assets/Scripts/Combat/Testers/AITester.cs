using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AITester : MonoBehaviour
{
    public BattleMap map;
    public MoveCostManager moveManager;
    public int testTurnIndex;
    public int testRuns;
    public ActorAI actorAI;

    [ContextMenu("Test Find Path")]
    public void FindPath()
    {
        List<int> path = actorAI.FindPathToTarget(map.battlingActors[testTurnIndex], map, moveManager);
        Debug.Log(path.Count);
        for (int i = 0; i < path.Count; i++)
        {
            Debug.Log(path[i]);
        }
    }
}
