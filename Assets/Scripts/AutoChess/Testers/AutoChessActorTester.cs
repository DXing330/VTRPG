using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessActorTester : MonoBehaviour
{
    public AutoActorRollUpData testActor = new();
    public StatDatabase autoActorData;
    [ContextMenu("Test Auto Actor Load Base Stats")]
    public void TestAutoActorLoadBaseStats()
    {
        List<string> testNames = autoActorData.GetAllKeys();
        for (int i = 0; i < testNames.Count; i++)
        {
            testActor = new();
            testActor.SetName(testNames[i]);
            testActor.LoadBaseStats(autoActorData);
        }
    }
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
            Debug.Log(allFactions[i] + " Exists: " + testAutoChessActor.AutoChessFaction(allFactions[i]));
        }
    }
}
