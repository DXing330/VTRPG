using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassiveEffectTester : MonoBehaviour
{
    public PassiveSkill passive;
    public BattleMap map;
    public string dummyTime;
    public string dummyWeather;
    public PassiveOrganizer passiveOrganizer;
    public TacticActor dummyActor;
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
        passive.ApplyPassive(dummyActor, map, testPassive);
        Debug.Log(dummyActor.GetCurseString() == "");
    }
}
