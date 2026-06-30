using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessActorTester : MonoBehaviour
{
    public ActorSubGameStats testAutoChessActor;
    public List<string> allFactions;
    public string testFaction;
    public string testFactions;
    [ContextMenu("Test Adding Factions")]
    public void RunAddingTests()
    {
        testAutoChessActor.ResetFactions();
        Debug.Log("Adding Faction: " + testFaction);
        testAutoChessActor.AddFaction(testFaction);
        TestFactionsExist();
        Debug.Log("Setting Factions: " + testFactions);
        testAutoChessActor.SetFactionsFromString(testFactions);
        TestFactionsExist();
    }
    protected void TestFactionsExist()
    {
        for (int i = 0; i < allFactions.Count; i++)
        {
            Debug.Log(allFactions[i] + " Exists: " + testAutoChessActor.Faction(allFactions[i]));
        }
    }
}
