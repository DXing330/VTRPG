using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DungeonSelectList : MonoBehaviour
{
    public GeneralUtility utility;
    public MainCampaignState mainStory;
    public PartyDataManager partyData;
    public List<GameObject> dungeonSelectObjects;
    public int page = 0;
    public void ChangePage(bool right)
    {
        page = utility.ChangePage(page, right, dungeonSelectObjects, allDungeons);
        UpdateCurrentPage();
    }
    public int selected;
    public void Select(int index)
    {
        selected = index;
    }
    public void SelectDungeon()
    {
        dungeon.SetDungeonName(currentDungeons[selected]);
        // If it's a story quest then do things differently.
        if (currentDungeons[selected] == mainStory.GetRequestLocation() && mainStory.GetCurrentChapter() != 1)
        {
            dungeon.SetStoryQuest();
            if (mainStory.GetCurrentRequest() == "Escort")
            {
                partyData.AddTempPartyMember(mainStory.GetRequestSpecifics());
            }
        }
        // Otherwise get quests from the guild card.
        else
        {
            dungeon.ResetQuests();
            // Set Quest Info.
            List<string> questGoals = partyData.guildCard.ReturnQuestGoalsAtLocation(dungeon.GetDungeonName());
            dungeon.SetQuestGoals(questGoals);
            dungeon.SetQuestFloors(partyData.guildCard.ReturnQuestFloorsAtLocation(dungeon.GetDungeonName()));
            // Add any temp party members.
            for (int i = 0; i < questGoals.Count; i++)
            {
                if (questGoals[i] == "Escort")
                {
                    partyData.AddTempPartyMember(dungeon.GetEscortName());
                }
            }
        }
        partyData.Save();
        dungeon.MakeDungeon();
        // Move Scene.
        sceneMover.MoveToDungeon();
    }
    public StatDatabase dungeonData;
    public List<string> allDungeons;
    public List<string> currentDungeons;
    public List<TMP_Text> difficultyTexts;
    public List<TMP_Text> questCountTexts;
    public List<TMP_Text> dungeonNames;
    public Dungeon dungeon;
    public SceneMover sceneMover;
    void Start()
    {
        allDungeons = new List<string>(dungeonData.GetAllKeys());
        UpdateCurrentPage();
    }

    protected void ResetPage()
    {
        for (int i = 0; i < dungeonNames.Count; i++)
        {
            difficultyTexts[i].text = "";
            questCountTexts[i].text = "";
            dungeonNames[i].text = "";
        }
        utility.DisableGameObjects(dungeonSelectObjects);
    }

    protected void UpdateCurrentPage()
    {
        ResetPage();
        currentDungeons = utility.GetCurrentPageStrings(page, dungeonSelectObjects, allDungeons);
        // Get the difficulty;
        for (int i = 0; i < currentDungeons.Count; i++)
        {
            dungeonSelectObjects[i].SetActive(true);
            dungeonNames[i].text = currentDungeons[i];
            difficultyTexts[i].text = utility.Root(int.Parse(dungeonData.ReturnValue(currentDungeons[i]).Split("|")[0])).ToString();
            if (currentDungeons[i] == mainStory.GetRequestLocation() && mainStory.GetCurrentChapter() != 1)
            {
                questCountTexts[i].text = "*";
            }
            else
            {
                questCountTexts[i].text = partyData.guildCard.QuestsAtLocation(currentDungeons[i]).ToString();
            }
        }
    }
}
