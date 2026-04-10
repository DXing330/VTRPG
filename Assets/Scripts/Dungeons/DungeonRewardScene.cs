using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DungeonRewardScene : MonoBehaviour
{
    public GeneralUtility utility;
    // Check treasures obtained, enemies defeated and quests completed.
    public Dungeon dungeon;
    // Obtain gold/items.
    public Inventory inventory;
    // Obtain equipment.
    public EquipmentInventory equipmentInventory;
    // Obtain skills.
    public PartyDataManager partyData;
    public StatDatabase rewardData;
    public StatTextList allItemRewards;
    public StatTextList allEquipmentRewards;
    public TMP_Text goldReward;
    public QuestSuccessChecker questSuccessChecker;
    public int gold;
    public List<string> equipmentRewards;
    public TreasureChestManager treasureChestManager;

    void Start()
    {
        CalculateRewards();
        DisplayRewards();
    }

    protected void CalculateRewards()
    {
        treasureChestManager.OpenAllChests();
        gold = treasureChestManager.gold;
        CalculateQuestRewards();
        if(questSuccessChecker.StoryQuestSuccessful(partyData, dungeon))
        {
            dungeon.mainStory.CompleteChapter();
            partyData.Save();
        }
    }

    protected void CalculateQuestRewards()
    {
        // Check which quests were active in the dungeon.
        List<int> indices = partyData.guildCard.GetQuestIndicesAtLocation(dungeon.GetDungeonName());
        for (int i = indices.Count - 1; i >= 0; i--)
        {
            if (questSuccessChecker.QuestSuccessful(partyData, i, dungeon))
            {
                partyData.guildCard.CompleteRequest(i);
            }
        }
    }

    protected void DisplayRewards()
    {
        goldReward.text = gold.ToString();
        allItemRewards.SetStatsAndData(treasureChestManager.GetItemsFound(), treasureChestManager.GetQuantitiesFound());
        allEquipmentRewards.SetStatsAndData(treasureChestManager.GetEquipmentFound());
    }
}
