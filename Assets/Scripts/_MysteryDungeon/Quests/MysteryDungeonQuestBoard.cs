using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Controls The Quest Board Scene.
public class MysteryDungeonQuestBoard : MonoBehaviour
{
    void Start()
    {
        DetermineExcludedQuests();
        GenerateQuests();
    }
    protected List<string> excludedStrings = new();
    protected void DetermineExcludedQuests()
    {
        excludedStrings = new();
        acceptedQuests = partyData.guildCard.GetQuests();
        for (int i = 0; i < acceptedQuests.Count; i++)
        {
            excludedStrings.Add(acceptedQuests[i].ReturnQuestAsString());
        }
        for (int i = 0; i < availableQuests.Count; i++)
        {
            excludedStrings.Add(availableQuests[i].ReturnQuestAsString());
        }
    }
    public GeneralUtility utility;
    public PartyDataManager partyData;
    public PopUpMessage popUp;
    public StatDatabase dungeonData;
    public List<string> VIPList;
    protected MysteryDungeonQuest GenerateQuest()
    {
        MysteryDungeonQuest newQuest = new MysteryDungeonQuest();
        List<string> allDungeons = dungeonData.GetAllKeys();
        string location = allDungeons[Random.Range(0, allDungeons.Count)];
        string[] locationDetails = dungeonData.ReturnValue(location).Split("|");
        int floor = -1;
        string goal = newQuest.GetRandomGoal();
        if (goal != "Escort")
        {
            int minFloor = 0;
            int maxFloor = int.Parse(locationDetails[0]);
            floor = Random.Range(minFloor, maxFloor + 1);
        }
        string specifics = "";
        switch (goal)
        {
            // Get A Random Boss From The Dungeon.
            case "Defeat":
            string[] possibleBosses = locationDetails[4].Split(",");
            specifics = possibleBosses[Random.Range(0, possibleBosses.Length)];
            break;
            // Generate A Random NPC Type.
            case "Escort":
            case "Rescue":
            specifics = VIPList[Random.Range(0, VIPList.Count)];
            break;
        }
        string reward = "Gold";
        int rewardQuantity = 20;
        newQuest.InitializeQuest(location, floor, goal, specifics, reward, rewardQuantity);
        if (excludedStrings.Contains(newQuest.ReturnQuestAsString()))
        {
            return GenerateQuest();
        }
        return newQuest;
    }
    protected int maxAvailableQuests = 6;
    public List<MysteryDungeonQuest> availableQuests = new();
    public List<MysteryDungeonQuest> acceptedQuests = new();
    public List<GameObject> availableQuestObjects;
    public List<TMP_Text> availableQuestLocationTexts;
    public List<GameObject> acceptedQuestObjects;
    public List<TMP_Text> acceptedQuestLocationTexts;
    public void UpdateQuestBoard()
    {
        utility.DisableGameObjects(availableQuestObjects);
        utility.DisableGameObjects(acceptedQuestObjects);
        for (int i = 0; i < Mathf.Min(availableQuests.Count, availableQuestObjects.Count); i++)
        {
            availableQuestObjects[i].SetActive(true);
            availableQuestLocationTexts[i].text = availableQuests[i].questLocation;
        }
        for (int i = 0; i < Mathf.Min(acceptedQuests.Count, acceptedQuestObjects.Count); i++)
        {
            acceptedQuestObjects[i].SetActive(true);
            acceptedQuestLocationTexts[i].text = acceptedQuests[i].questLocation;
        }
        //Reset Stuff Whenever Updating.
        availableQuestViewer.ResetView();
        acceptedQuestViewer.ResetView();
        acceptQuestButton.SetActive(false);
        abandonQuestButton.SetActive(false);
        submitQuestButton.SetActive(false);
        selectedAvailableQuestIndex = -1;
        selectedAcceptedQuestIndex = -1;
    }
    // Need To Generate/Add/Reroll Quests.
    public void GenerateQuests()
    {
        availableQuests.Clear();
        for (int i = 0; i < maxAvailableQuests; i++)
        {
            availableQuests.Add(GenerateQuest());
            DetermineExcludedQuests();
        }
        UpdateQuestBoard();
    }
    public MysteryDungeonQuestViewer availableQuestViewer;
    public MysteryDungeonQuestViewer acceptedQuestViewer;
    // Quests In Guild Card.
    protected int selectedAcceptedQuestIndex = -1;
    public void SelectAcceptedQuest(int index)
    {
        // Determine If The Abandon Or Submit Button Should Be Active.
        selectedAcceptedQuestIndex = index;
        acceptedQuestViewer.UpdateView(acceptedQuests[index]);
        if (acceptedQuests[index].completed == 0)
        {
            abandonQuestButton.SetActive(true);
        }
        else
        {
            submitQuestButton.SetActive(true);
        }
    }
    // Quests In Quest Board.
    protected int selectedAvailableQuestIndex = -1;
    public void SelectQuest(int index)
    {
        if (index < 0 || index >= availableQuests.Count){return;}
        selectedAvailableQuestIndex = index;
        availableQuestViewer.UpdateView(availableQuests[index]);
        acceptQuestButton.SetActive(true);
    }
    public GameObject acceptQuestButton;
    public void AcceptQuest()
    {
        if (selectedAvailableQuestIndex < 0 || selectedAvailableQuestIndex >= availableQuests.Count){return;}
        if (partyData.guildCard.AtQuestLimit())
        {
            popUp.SetMessage("You can't accept any more quests right now.");
            return;
        }
        partyData.guildCard.AcceptRequest(availableQuests[selectedAvailableQuestIndex]);
        availableQuests.RemoveAt(selectedAvailableQuestIndex);
        DetermineExcludedQuests();
        UpdateQuestBoard();
    }
    public GameObject abandonQuestButton;
    public void AbandonQuest()
    {
        if (selectedAcceptedQuestIndex < 0 || selectedAcceptedQuestIndex >= acceptedQuests.Count){return;}
        if (acceptedQuests[selectedAcceptedQuestIndex].completed != 0){return;}
        partyData.guildCard.RemoveQuestAtIndex(selectedAcceptedQuestIndex);
        DetermineExcludedQuests();
        UpdateQuestBoard();
    }
    public GameObject submitQuestButton;
    public void SubmitQuest()
    {
        if (selectedAcceptedQuestIndex < 0 || selectedAcceptedQuestIndex >= acceptedQuests.Count){return;}
        if (acceptedQuests[selectedAcceptedQuestIndex].completed == 0){return;}
        // For Now Assume That The Report Is Gold.
        partyData.inventory.GainGold(acceptedQuests[selectedAcceptedQuestIndex].rewardQuantity);
        partyData.guildCard.RemoveQuestAtIndex(selectedAcceptedQuestIndex);
        DetermineExcludedQuests();
        UpdateQuestBoard();
    }
}