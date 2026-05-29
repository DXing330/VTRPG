using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StSTreasureScene : MonoBehaviour
{
    public PartyDataManager partyData;
    public StSStateManager stsManager;
    public StSRewardSaveData rewardTracker;
    public RewardDisplayMenu rewardDisplay;
    public bool clickedChest = false;
    public Image chestImage;
    public Sprite chestClosedSprite;
    public Sprite chestOpenSprite;
    void Start()
    {
        clickedChest = false;
        chestImage.sprite = chestClosedSprite;
    }
    public void OpenTreasure()
    {
        if (clickedChest){return;}
        // Have A Bool Or Something So Clicking More Does Nothing.
        clickedChest = true;
        // Rewards Should Already Be Generated, Or Not, Since It's Seeded It'll Always Generate The Same.
        // Generate/Pull The Rewards.
        rewardTracker.GenerateChestRewards();
        // Change The Chest Image To Opened Chest.
        chestImage.sprite = chestOpenSprite;
        // Have The Reward Menu PopUp So You Can Claim The Gold + Relic.
        rewardDisplay.OpenTreasureRewards(partyData, stsManager, rewardTracker.GetRewards(), rewardTracker.GetRewardSpecifics());
    }
}
