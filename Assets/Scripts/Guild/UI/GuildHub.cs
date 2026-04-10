using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GuildHub : MonoBehaviour
{
    public PartyDataManager partyData;
    public InventoryUI inventoryUI;
    public References references;
    public TMP_Text guildRank;
    public TMP_Text currentDay;
    public TMP_Text lastPayDay;
    protected void UpdateUI()
    {
        guildRank.text = partyData.guildCard.GetGuildRankName();
        currentDay.text = storyDay.GetDay().ToString();
        lastPayDay.text = storyDay.GetLastPayDay().ToString();
    }
    public List<string> questItems;
    public List<string> questTempMembers;

    public void Start()
    {
        RemoveDungeonData();
        NextStory();
        UpdateStory();
        partyData.Save();
        UpdateUI();
    }

    public void CollectPay()
    {
        if (storyDay.PayDateDifference() > 0)
        {
            int days = storyDay.PayDateDifference();
            storyDay.CollectPay();
            storyDay.Save();
            partyData.inventory.CollectPay(days, partyData.guildCard.GetGuildRank());
            partyData.Save();
            inventoryUI.UpdateKeyValues();
        }
    }

    // Don't let them keep the chests if they don't complete the dungeon.
    protected void RemoveDungeonData()
    {
        partyData.fullParty.ResetLists();
        partyData.dungeonBag.ReturnAllChests();
        for (int i = 0; i < questItems.Count; i++)
        {
            partyData.dungeonBag.RemoveAllItemsOfType(questItems[i]);
        }
        for (int i = 0; i < questTempMembers.Count; i++)
        {
            partyData.RemoveAllTempPartyMember(questTempMembers[i]);
        }
    }

    // This will also track main story stuff, since it updates whenever you return from the dungeon and it's a new day.
    public MainCampaignState mainStory;
    public GameObject storyOverObject;
    public StatDatabase storyQuestShortTexts;
    public StatDatabase storyQuestLongTexts;
    public DayTracker storyDay;
    public TMP_Text daysLeftText;
    public TMP_Text currentMission;
    public TMP_Text missionStatusText;
    public PopUpTalking newStoryPopUp;

    public void UpdateStory()
    {
        if (mainStory.GetCurrentChapter() == 1)
        {
            missionStatusText.text = "TRUE";
        }
        else{missionStatusText.text = "FALSE";}
        currentMission.text = storyQuestShortTexts.ReturnValueAtIndex(mainStory.GetPreviousChapters().Count);
        daysLeftText.text = storyDay.DaysLeft(mainStory.GetCurrentDeadline()).ToString();
    }

    [ContextMenu("Debug Next Story")]
    public void DebugNextStory()
    {
        if (mainStory.CompletedStory())
        {
            storyOverObject.SetActive(true);
            return;
        }
        mainStory.NextChapter(partyData);
        storyDay.NewQuest();
        ShowNextChapter();
    }

    public void SubmitQuest()
    {
        if (mainStory.GetCurrentChapter() == 1 && !mainStory.CompletedStory())
        {
            mainStory.NextChapter(partyData);
            storyDay.NewQuest();
            ShowNextChapter();
            UpdateUI();
        }
    }

    public void NextStory()
    {
        // Begin the story.
        if (mainStory.GetPreviousChapters().Count == 0 && mainStory.GetCurrentDeadline() <= 0)
        {
            mainStory.NewChapter();
            ShowNextChapter();
            return;
        }
        // End the story.
        if (mainStory.CompletedStory())
        {
            storyOverObject.SetActive(true);
            return;
        }
        // Continue the story.
        if (storyDay.DeadlineReached(mainStory.GetCurrentDeadline()))
        {
            mainStory.NextChapter(partyData);
            storyDay.NewQuest();
            ShowNextChapter();
            return;
        }
    }

    protected void ShowNextChapter()
    {
        newStoryPopUp.StartTalking(storyQuestLongTexts.ReturnValueAtIndex(mainStory.GetPreviousChapters().Count), "Guild Owner", "Noble");
        UpdateStory();
    }
}
