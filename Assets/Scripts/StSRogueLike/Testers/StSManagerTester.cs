using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StSManagerTester : MonoBehaviour
{
    public StSStateManager stsManager;
    public int testGoldAmount = 1;
    [ContextMenu("Test Gain Gold")]
    public void TestGainGold()
    {
        // Check The Party Gold Before.
        int startGold = stsManager.stsParty.inventory.GetGold();
        stsManager.GainGold(testGoldAmount);
        // Check The Party Gold After.
        int finalGold = stsManager.stsParty.inventory.GetGold();
        if (startGold + testGoldAmount != finalGold)
        {
            Debug.Log("Gain Gold Failed");
        }
    }
    public string testGoldPickUpRelic = "Gold Bar";
    [ContextMenu("Test Gain Relic")]
    public void TestGainRelic()
    {
        // Test A Pickup Effect Relic Which Should Grant Gold.
        if (stsManager.stsParty.dungeonBag.RelicExists(testGoldPickUpRelic))
        {
            stsManager.stsParty.dungeonBag.RemoveRelic(testGoldPickUpRelic);
        }
        int startGold = stsManager.stsParty.inventory.GetGold();
        stsManager.GainRelic(testGoldPickUpRelic);
        if (!stsManager.stsParty.dungeonBag.RelicExists(testGoldPickUpRelic))
        {
            Debug.Log("Gain Gold PickUp Relic Failed");
        }
        int finalGold = stsManager.stsParty.inventory.GetGold();
        if (startGold >= finalGold)
        {
            Debug.Log("Gain Gold From PickUp Relic Failed");
        }
    }
    public string testBattleEventName = "Vampire Coven";
    [ContextMenu("Test Generate Event Battle")]
    public void TestGenerateEventBattle()
    {
        StSEventData testEvent = stsManager.eventTracker.GetEventByName(testBattleEventName);
        if (testEvent == null)
        {
            Debug.LogError("Event Not Found.");
            return;
        }
        StSEventEffect battleEffect = null;
        for (int i = 0; i < testEvent.choices.Count; i++)
        {
            for (int j = 0; j < testEvent.choices[i].choiceEffects.Count; j++)
            {
                StSEventEffect effect = testEvent.choices[i].choiceEffects[j];
                if (effect.target == "Battle")
                {
                    battleEffect = effect;
                    break;
                }
            }
            if (battleEffect != null)
            {
                break;
            }
        }
        if (battleEffect == null)
        {
            Debug.LogError("No Battle Effect Found.");
            return;
        }
        stsManager.battleState.SetCustomBattleName("Old Battle");
        stsManager.enemyList.ResetLists();
        stsManager.enemyList.AddCharacters(new List<string>() { "Old Enemy" });
        stsManager.PrepareEventBattle(battleEffect.effect, battleEffect.effectSpecifics);
        if (stsManager.battleState.GetCustomBattleName() != "")
        {
            Debug.LogError("Custom Battle Name Was Not Cleared");
        }
        if (stsManager.battleState.GetTerrainType() != battleEffect.effect)
        {
            Debug.LogError("Terrain Mismatch");
        }
        List<string> enemies = stsManager.enemyList.GetCharacterSprites();
        if (enemies.Count != 4)
        {
            Debug.LogError("Enemy Count Mismatch");
        }
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != "Vampire")
            {
                Debug.LogError("Enemy Mismatch");
            }
        }
    }
}
