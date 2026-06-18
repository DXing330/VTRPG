using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StSEventTester : MonoBehaviour
{
    public StSEventScene eventScene;
    [ContextMenu("Run Event Tests")]
    public void RunEventTests()
    {
        eventScene.testMode = true;
        // Run All Tests.
        TestGainGoldEvent();
        TestBattleEvent();
        TestAllActorEvent();
        TestRandomActorEvent();
        eventScene.testMode = false;
    }
    public string testGoldEventName = "Shrine of Wealth";
    public void TestGainGoldEvent()
    {
        eventScene.partyData.Load();
        eventScene.currentEvent = eventScene.eventData.GetEventByName(testGoldEventName);
        eventScene.DisplayEvent();
        int startGold = eventScene.partyData.inventory.GetGold();
        eventScene.SelectChoice(0);
        int finalGold = eventScene.partyData.inventory.GetGold();
        if (finalGold != startGold + 50)
        {
            Debug.LogError(testGoldEventName + " Test Failed");
        }
    }
    public string testBattleEventName = "Vampire Coven";
    public void TestBattleEvent()
    {
        eventScene.currentEvent = eventScene.eventData.GetEventByName(testBattleEventName);
        eventScene.DisplayEvent();
        eventScene.SelectChoice(2);
        if (eventScene.stsManager.battleState.GetTerrainType() != "Plains")
        {
            Debug.LogError(testBattleEventName + " Terrain Test Failed");
        }
        List<string> enemies = eventScene.stsManager.enemyList.GetCharacterSprites();
        if (enemies.Count != 4)
        {
            Debug.LogError(testBattleEventName + " Enemy Count Test Failed");
        }
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != "Vampire")
            {
                Debug.LogError(
                    testBattleEventName +
                    " Enemy Test Failed At Index " + i
                );
            }
        }
    }
    public string testAllActorEventName = "Vampire Coven";
    public void TestAllActorEvent()
    {
        eventScene.partyData.Load();
        eventScene.currentEvent = eventScene.eventData.GetEventByName(testAllActorEventName);
        int partyCount = eventScene.partyData.ReturnTotalPartyCount();
        List<int> startingHealth = new List<int>();
        List<bool> startingDrainLife = new List<bool>();
        for (int i = 0; i < partyCount; i++)
        {
            TacticActor actor = eventScene.partyData.ReturnActorAtIndex(i);
            startingHealth.Add(actor.GetBaseHealth());
            startingDrainLife.Add(actor.SkillExists("Drain Life"));
        }
        eventScene.DisplayEvent();
        eventScene.SelectChoice(1);
        for (int i = 0; i < partyCount; i++)
        {
            TacticActor actor = eventScene.partyData.ReturnActorAtIndex(i);
            int finalHealth = actor.GetBaseHealth();
            bool finalDrainLife = actor.SkillExists("Drain Life");
            int expectedHealth = startingHealth[i] - (startingHealth[i] * 30 / 100);
            if (finalHealth != expectedHealth)
            {
                Debug.LogError(testAllActorEventName + " Actor " + i + " Health Mismatch. Expected " + expectedHealth + ", Got " + finalHealth);
            }
            if (!startingDrainLife[i] && !finalDrainLife)
            {
                Debug.LogError(testAllActorEventName + " Actor " + i + " Did Not Gain Drain Life");
            }
        }
    }
    public string testRandomActorEventName = "Vampire Coven";
    public void TestRandomActorEvent()
    {
        eventScene.partyData.Load();
        eventScene.currentEvent = eventScene.eventData.GetEventByName(testRandomActorEventName);
        int partyCount = eventScene.partyData.ReturnTotalPartyCount();
        List<int> startingHealth = new List<int>();
        List<bool> startingDrainLife = new List<bool>();
        for (int i = 0; i < partyCount; i++)
        {
            TacticActor actor = eventScene.partyData.ReturnActorAtIndex(i);
            startingHealth.Add(actor.GetBaseHealth());
            startingDrainLife.Add(actor.SkillExists("Drain Life"));
        }
        eventScene.DisplayEvent();
        eventScene.SelectChoice(0);
        int selectedIndex = eventScene.randomActorIndex;
        if (selectedIndex < 0 || selectedIndex >= partyCount)
        {
            Debug.LogError(testRandomActorEventName + " Random Actor Index Failed");
            return;
        }
        int healthChangedCount = 0;
        int drainLifeAddedCount = 0;
        for (int i = 0; i < partyCount; i++)
        {
            TacticActor actor = eventScene.partyData.ReturnActorAtIndex(i);
            int finalHealth = actor.GetBaseHealth();
            bool finalDrainLife = actor.SkillExists("Drain Life");
            bool healthChanged = finalHealth != startingHealth[i];
            bool drainLifeAdded = !startingDrainLife[i] && finalDrainLife;
            if (healthChanged)
            {
                healthChangedCount++;
            }
            if (drainLifeAdded)
            {
                drainLifeAddedCount++;
            }
            if (i == selectedIndex)
            {
                if (!healthChanged)
                {
                    Debug.LogError(testRandomActorEventName + " Selected Actor Health Did Not Change");
                }
                if (!drainLifeAdded)
                {
                    Debug.LogError(testRandomActorEventName + " Selected Actor Did Not Gain Drain Life");
                }
            }
            else
            {
                if (healthChanged)
                {
                    Debug.LogError(testRandomActorEventName + " Non-Selected Actor " + i + " Health Changed");
                }
                if (drainLifeAdded)
                {
                    Debug.LogError(testRandomActorEventName + " Non-Selected Actor " + i + " Gained Drain Life");
                }
            }
        }
        if (healthChangedCount != 1)
        {
            Debug.LogError(testRandomActorEventName + " Expected 1 Health Change, Got " + healthChangedCount);
        }
        if (drainLifeAddedCount != 1)
        {
            Debug.LogError(testRandomActorEventName + " Expected 1 Drain Life Gain, Got " + drainLifeAddedCount);
        }
    }
}
