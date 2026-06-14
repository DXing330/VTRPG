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
}
