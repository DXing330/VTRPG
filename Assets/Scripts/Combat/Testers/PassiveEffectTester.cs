using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassiveEffectTester : MonoBehaviour
{
    // For Better Map Initialization.
    public AttackManagerTester attackTester;
    public StatDatabase allPassives;
    public PassiveSkill passive;
    public BattleMap map;
    public string dummyTime;
    public string dummyWeather;
    public PassiveOrganizer passiveOrganizer;
    public TacticActor dummyActor;
    public int testDamage = 20;
    public string actorStats;
    public int actorLocation;
    public int actorDirection;
    public string actorTile;
    public string actorTEffect;
    public List<string> actorBorders;
    public string testPassive;
    protected void InitializeMap()
    {
        map.ForceStart();
        map.combatLog.ForceStart();
        map.combatLog.AddNewLog();
        // Set up the actors.
        dummyActor.SetInitialStatsFromString(actorStats);
        dummyActor.InitializeStats();
        passiveOrganizer.OrganizeActorPassives(dummyActor);
        dummyActor.SetLocation(actorLocation);
        dummyActor.SetDirection(actorDirection);
        dummyActor.SetCurrentHealth(dummyActor.GetHealth() - testDamage);
        // Set up the attack conditions.
        map.SetTime(dummyTime);
        map.SetWeather(dummyWeather);
        map.ChangeTile(actorLocation, "Tile", actorTile, true);
        map.ChangeTile(actorLocation, "TerrainEffect", actorTEffect, true);
        map.ChangeTile(actorLocation, "Borders", String.Join("|", actorBorders), true);
        dummyActor.SetTeam(0);
        map.AddActorToBattle(dummyActor);
    }
    [ContextMenu("Test Passive")]
    public void TestPassive()
    {
        InitializeMap();
        string original = dummyActor.ReturnTestStatsString();
        passive.ForceApplyPassiveEffect(dummyActor, map, testPassive);
        string final = dummyActor.ReturnTestStatsString();
        Debug.Log(original + "\n" + final);
    }
    // For Testing Passives That Scale Off Passive Levels.
    public void TestPassiveWithAddedPassive(string addedPassiveName, string addedPassiveLevel = "1")
    {
        InitializeMap();
        string original = dummyActor.ReturnTestStatsString();
        dummyActor.AddPassiveSkill(addedPassiveName, addedPassiveLevel);
        passive.ForceApplyPassiveEffect(dummyActor, map, testPassive);
        string final = dummyActor.ReturnTestStatsString();
        Debug.Log(original + "\n" + final);
    }
    [ContextMenu("Test Scaling Passives")]
    public void TestAllScalingPassives()
    {
        List<string> allPassiveNames = allPassives.GetAllKeys();
        for (int i = 0; i < allPassiveNames.Count; i++)
        {
            string scalingPassiveData = allPassives.ReturnValue(allPassiveNames[i]);
            if (scalingPassiveData.Contains("ScalingEquals"))
            {
                // Test Only Start/End For Now.
                if (!scalingPassiveData.StartsWith("Start") && !scalingPassiveData.StartsWith("End")){continue;}
                string[] blocks = scalingPassiveData.Split("ScalingEquals");
                Debug.Log(allPassiveNames[i] + " " + blocks[1]);
                //Debug.Log(scalingPassiveData);
                testPassive = scalingPassiveData;
                TestPassiveWithAddedPassive(allPassiveNames[i], "2");
            }
        }
    }
}
