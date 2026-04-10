using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RequestDisplay : MonoBehaviour
{
    public GeneralUtility utility;
    public GuildCard guildCard;
    public SavedOverworld overworldTiles;
    public OverworldState overworldState;
    public Request dummyRequest;
    public TMP_Text requestText;
    public GameObject requestCompletedStamp;
    public int selectedQuest = 0;
    public int GetSelectedQuest() { return selectedQuest; }
    public void ResetSelectedQuest()
    {
        selectedQuest = 0;
        requestCompletedStamp.SetActive(false);
    }
    public void ChangeSelectedQuest(bool right = true)
    {
        selectedQuest = utility.ChangeIndex(selectedQuest, right, guildCard.acceptedQuests.Count - 1);
        DisplayQuest();
    }
    public void DisplayQuest()
    {
        // If no quests then return.
        if (guildCard.acceptedQuests.Count <= 0)
        {
            requestText.text = "";
            return;
        }
        requestText.text = DisplayRequestDescription(guildCard.acceptedQuests[selectedQuest]);
        // If its completed them red stamp it.
        if (guildCard.QuestCompleted(selectedQuest))
        {
            requestCompletedStamp.SetActive(true);
        }
    }

    public string DisplayRequestDescription(string requestInfo)
    {
        dummyRequest.Load(requestInfo);
        switch (dummyRequest.GetGoal())
        {
            case "Deliver":
                return UpdateDeliveryDescription();
            case "Defeat":
                return UpdateDefeatDescription();
            case "Escort":
                return UpdateEscortDescription();
        }
        return "";
    }

    protected string UpdateDeliveryDescription()
    {
        string description = "I need you to deliver these " + dummyRequest.GetGoalAmount() + " shipments of " + dummyRequest.GetGoalSpecifics();
        string cityName = overworldTiles.GetCityNameFromDemandedLuxury(dummyRequest.GetGoalSpecifics());
        int direction = overworldTiles.mapUtility.DirectionBetweenLocations(overworldState.GetLocation(), dummyRequest.GetLocation(), overworldTiles.GetSize());
        string directionName = overworldTiles.mapUtility.IntDirectionToString(direction);
        switch (dummyRequest.GetLocationSpecifics())
        {
            case "City":
                description += " to " + cityName;
                break;
            case "Merchant":
                description += " about halfway to " + cityName;
                break;
        }
        description += ", to the " + directionName + " of here,";
        description += " within " + dummyRequest.GetDeadline() + " days.";
        int failPenalty = dummyRequest.GetFailPenalty();
        if (failPenalty > 0)
        {
            description += "\n" + "Note: " + dummyRequest.GetFailPenalty() + " GOLD fine if the delivery is not completed.";
        }
        return description;
    }

    protected string UpdateDefeatDescription()
    {
        string description = "There have been reports of monsters in a cave";
        int direction = overworldTiles.mapUtility.DirectionBetweenLocations(overworldState.GetLocation(), dummyRequest.GetLocation(), overworldTiles.GetSize());
        string directionName = overworldTiles.mapUtility.IntDirectionToString(direction);
        description += ", to the " + directionName + " of here.";
        description += "\n" + "Eliminate them within " + dummyRequest.GetDeadline() + " days.";
        return description;
    }

    protected string UpdateEscortDescription()
    {
        string description = "There is a research expedition";
        int direction = overworldTiles.mapUtility.DirectionBetweenLocations(overworldState.GetLocation(), dummyRequest.GetLocation(), overworldTiles.GetSize());
        string directionName = overworldTiles.mapUtility.IntDirectionToString(direction);
        description += ", to the " + directionName + " of here.";
        description += "\n" + "Please escort me there within " + dummyRequest.GetDeadline() + " days.";
        int failPenalty = dummyRequest.GetFailPenalty();
        if (failPenalty > 0)
        {
            description += "\n" + "Note: " + dummyRequest.GetFailPenalty() + " GOLD fine if I am injured or late.";
        }
        return description;
    }
}
