using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Generates requests. Displays available requests. Changes the overworld based on accepted quests.
public class RequestBoard : MonoBehaviour
{
    public GeneralUtility utility;
    public SavedOverworld overworldTiles;
    public OverworldState overworldState;
    public PartyDataManager partyData;
    public SavedCaravan caravan;
    public Inventory inventory;
    public GuildCard guildCard;
    public StatDatabase luxuryUnitPrices;
    public List<string> requestGoals;
    public Request dummyRequest;
    public RequestDisplay requestDisplay;
    public List<string> availableRequests;
    public SelectStatTextList questSelect;
    public TMP_Text questDetails;
    public void ResetQuestDetails(){questDetails.text = "";}
    public int baseRequestCount = 6;
    public int amountVariation = 9;
    public int distanceVariation = 3;
    //public int maxDefeatDistance = 6;
    public string deliverFeatureString = "Merchant";
    public string defeatFeatureString = "Cave";
    public string escortTempMember = "Scholar";
    public string escortFeatureString = "Ruins";
    public int escortFailPenalty = 50;

    void Start()
    {
        questSelect.ResetSelected();
        availableRequests = guildCard.availableQuests;
        if (guildCard.RefreshQuests()) { GenerateRequests(); }
        UpdateSelectableQuests();
    }

    public void TryToCompleteQuest()
    {
        // Check if a quest exists.
        if (guildCard.acceptedQuests.Count <= 0) { return; }
        int selectedQuest = requestDisplay.GetSelectedQuest();
        // Check if the currently selected quest is completed.
        if (guildCard.QuestCompleted(selectedQuest))
        {
            // Claim your reward.
            inventory.GainGold(guildCard.QuestReward(selectedQuest));
            // Remove the quest.
            guildCard.SubmitQuest(selectedQuest);
            requestDisplay.ResetSelectedQuest();
            requestDisplay.DisplayQuest();
            // Reset accepted quests.
            GenerateRequests();
            UpdateSelectableQuests();
        }
    }

    public void AcceptQuest()
    {
        int selected = questSelect.GetSelected();
        if (selected < 0) { return; }
        dummyRequest.Load(availableRequests[selected]);
        guildCard.AcceptQuest(availableRequests[selected]);
        // This could be a problem if you generate quests, then leave, spawn a feature then come back and accept the quest. Specifically this could lead to features overlapping on a single tile, in these situations the quest will be unable to be completed and automatically failed, git gud.
        availableRequests.Clear();
        UpdateSelectableQuests();
        ResetQuestDetails();
        guildCard.Save();
        // If it's a delivery request then add the specified cargo to your wagon.
        if (dummyRequest.GetGoal() == "Deliver")
        {
            caravan.AddCargo(dummyRequest.GetGoalSpecifics(), dummyRequest.GetGoalAmount());
        }
        // If it's an escort request, then add the temp party member.
        if (dummyRequest.GetGoal() == "Escort")
        {
            partyData.AddTempPartyMember(escortTempMember);
        }
        // Don't add a city, those are fixed locations at the start of the game, not normal features.
        if (dummyRequest.GetLocationSpecifics() == "City") { return; }
        overworldTiles.AddFeature(dummyRequest.GetLocationSpecifics(), dummyRequest.GetLocation().ToString());
    }

    public void UpdateSelectableQuests()
    {
        List<string> goals = new List<string>();
        List<string> rewards = new List<string>();
        for (int i = 0; i < availableRequests.Count; i++)
        {
            dummyRequest.Load(availableRequests[i]);
            goals.Add(dummyRequest.GetGoal());
            rewards.Add(dummyRequest.GetReward() + " Gold");
        }
        questSelect.SetStatsAndData(goals, rewards);
    }

    public void DisplayRequestDescription()
    {
        int selected = questSelect.GetSelected();
        string selectedRequest = availableRequests[selected];
        questDetails.text = requestDisplay.DisplayRequestDescription(selectedRequest);
        dummyRequest.Load(selectedRequest);
        int location = dummyRequest.GetLocation();
        int row = overworldTiles.mapUtility.GetRow(location, overworldTiles.GetSize());
        int col = overworldTiles.mapUtility.GetColumn(location, overworldTiles.GetSize());
        Debug.Log("Quest~Row: " + row + " Col: " + col);
        location = overworldState.GetLocation();
        row = overworldTiles.mapUtility.GetRow(location, overworldTiles.GetSize());
        col = overworldTiles.mapUtility.GetColumn(location, overworldTiles.GetSize());
        Debug.Log("(YOU)~Row: " + row + " Col: " + col);
        Debug.Log("Distance: "+overworldTiles.mapUtility.DistanceBetweenTiles(dummyRequest.GetLocation(), overworldState.GetLocation(), overworldTiles.GetSize()));
    }

    public void GenerateRequests()
    {
        // Remove any potential lingering merchants, caves and ruins can stay.
        overworldTiles.RemoveFeatures(deliverFeatureString);
        availableRequests.Clear();
        for (int i = 0; i < baseRequestCount; i++)
        {
            dummyRequest.Reset();
            // difficulty is based on a variety of factors, including the type of request
            //int difficulty = 1; // Do we even need a difficulty rating, just judge it for yourself kek?
            string requestGoal = requestGoals[Random.Range(0, requestGoals.Count)];
            switch (requestGoal)
            {
                case "Deliver":
                    GenerateDeliveryRequest();
                    break;
                case "Defeat":
                    GenerateDefeatRequest();
                    break;
                case "Escort":
                    GenerateEscortRequest();
                    break;
            }
            dummyRequest.SetGoal(requestGoal);
            availableRequests.Add(dummyRequest.ReturnDetails());
        }
        guildCard.Save();
    }

    protected void GenerateDeliveryRequest()
    {
        // Either go all the way or around halfway to a city.
        // Going all the way pays ~1.5x, since if you go all the way you can sell goods at the city yourself
        // Pick a luxury at random.
        string requestedLuxury = overworldTiles.RandomLuxury();
        dummyRequest.SetGoalSpecifics(requestedLuxury);
        dummyRequest.SetGoalAmount(Random.Range(1, amountVariation + 1));
        // The failure fee is the cost of the goods.
        dummyRequest.SetFailPenalty(dummyRequest.GetGoalAmount()*int.Parse(luxuryUnitPrices.ReturnValue(dummyRequest.GetGoalSpecifics())));
        // Double it since you probably need to travel back eventually.
        dummyRequest.SetReward(2 * (int) Mathf.Sqrt(dummyRequest.GetGoalAmount()));
        int distance = Random.Range(0, 3); // 0 = full, else half
        if (distance == 0)
        {
            dummyRequest.SetLocation(overworldTiles.GetCityLocationFromLuxuryDemanded(requestedLuxury));
            dummyRequest.SetLocationSpecifics("City");
            dummyRequest.SetDeadline(overworldTiles.mapUtility.DistanceBetweenTiles(dummyRequest.GetLocation(), overworldState.GetLocation(), overworldTiles.GetSize())+distanceVariation);
            dummyRequest.SetReward(dummyRequest.GetReward() * dummyRequest.GetDeadline() * 3 / 4);
        }
        else
        {
            // Go roughly in the middle of the target city and the current location.
            int cityLocation = overworldTiles.GetCityLocationFromLuxuryDemanded(requestedLuxury);
            int currentLocation = overworldState.GetLocation();
            int row = (overworldTiles.mapUtility.GetRow(cityLocation, overworldTiles.GetSize()) + overworldTiles.mapUtility.GetRow(currentLocation, overworldTiles.GetSize())) / 2;
            int col = (overworldTiles.mapUtility.GetColumn(cityLocation, overworldTiles.GetSize()) + overworldTiles.mapUtility.GetColumn(currentLocation, overworldTiles.GetSize())) / 2;
            int questLocation = GenerateRandomEmptyLocationAroundCenter(row, col);
            dummyRequest.SetLocation(questLocation);
            dummyRequest.SetLocationSpecifics(deliverFeatureString);
            // Deadline is assuming you travel 1 tile/day + buffer.
            dummyRequest.SetDeadline(overworldTiles.mapUtility.DistanceBetweenTiles(dummyRequest.GetLocation(), overworldState.GetLocation(), overworldTiles.GetSize())+distanceVariation);
            dummyRequest.SetReward(dummyRequest.GetReward() * dummyRequest.GetDeadline());
            // Add some slight RNG to the reward amount.
            dummyRequest.SetReward(dummyRequest.GetReward() + Random.Range(-amountVariation, amountVariation + 1));
        }
    }

    protected int GenerateRandomEmptyLocationAroundCenter(int row, int col, int distance = -1)
    {
        if (distance <= 0){ distance = distanceVariation; }
        row += Random.Range(-distance, distance + 1); // Add a small variation
        col += Random.Range(-distance, distance + 1);
        int tile = overworldTiles.mapUtility.ReturnTileNumberFromRowCol(row, col, overworldTiles.GetSize());
        if (!overworldTiles.FeatureExist(tile)){ return tile; }
        return GenerateRandomEmptyLocationAroundCenter(row, col);
    }

    protected void GenerateEscortRequest()
    {
        // Escort people to key features (dungeons).
        // Used to generate features.
        int currentLocation = overworldState.GetLocation();
        int row = overworldTiles.mapUtility.GetRow(currentLocation, overworldTiles.GetSize());
        int col = overworldTiles.mapUtility.GetColumn(currentLocation, overworldTiles.GetSize());
        int questLocation = GenerateRandomEmptyLocationAroundCenter(row, col, distanceVariation * 2);
        dummyRequest.SetLocation(questLocation);
        dummyRequest.SetLocationSpecifics(escortFeatureString);
        dummyRequest.SetDeadline(overworldTiles.mapUtility.DistanceBetweenTiles(dummyRequest.GetLocation(), overworldState.GetLocation(), overworldTiles.GetSize()) + distanceVariation);
        dummyRequest.SetReward(dummyRequest.GetDeadline() + Random.Range(1, amountVariation + 1));
        // Fail penalty is the cost of hiring someone.
        dummyRequest.SetFailPenalty(escortFailPenalty);
        dummyRequest.SetGoalSpecifics(escortTempMember);
    }

    protected void GenerateDefeatRequest()
    {
        // Monster Cave (Goblins/Wolves).
        // Pick a random location close by.
        int currentLocation = overworldState.GetLocation();
        int row = overworldTiles.mapUtility.GetRow(currentLocation, overworldTiles.GetSize());
        int col = overworldTiles.mapUtility.GetColumn(currentLocation, overworldTiles.GetSize());
        int questLocation = GenerateRandomEmptyLocationAroundCenter(row, col);
        dummyRequest.SetLocation(questLocation);
        dummyRequest.SetLocationSpecifics(defeatFeatureString);
        dummyRequest.SetDeadline(overworldTiles.mapUtility.DistanceBetweenTiles(dummyRequest.GetLocation(), overworldState.GetLocation(), overworldTiles.GetSize()) + distanceVariation);
        dummyRequest.SetReward(dummyRequest.GetDeadline() + Random.Range(1, amountVariation + 1));
    }
}