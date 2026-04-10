using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DungeonRequests : MonoBehaviour
{
    public GeneralUtility utility;
    public PartyDataManager partyData;
    public StatDatabase dungeonData;
    public List<string> requestTypes;
    public List<int> requestTypeRewards;
    public List<string> requests;
    public int selected;
    public void Select(int index){selected = index;}
    public string selectedRequest;
    public List<TMP_Text> requestHeadlines;
    public List<TMP_Text> requestDetails;
    public PopUpMessage popUp;

    void Start()
    {
        RefreshRequests();
    }

    protected void ResetRequests()
    {
        selected = -1;
        selectedRequest = "";
        requests.Clear();
        for (int i = 0; i < requestHeadlines.Count; i++)
        {
            requestHeadlines[i].text = "";
        }
        for (int i = 0; i < requestDetails.Count; i++)
        {
            requestDetails[i].text = "";
        }
    }

    public void RefreshRequests()
    {
        ResetRequests();
        for (int i = 0; i < requestHeadlines.Count; i++)
        {
            requests.Add(GenerateRequest());
            // Display the request headlines.
            string[] blocks = requests[i].Split("|");
            requestHeadlines[i].text = blocks[0] + "-" + blocks[3];
        }
    }

    protected string GenerateRequest()
    {
        string location = DetermineLocation();
        string type = requestTypes[Random.Range(0, requestTypes.Count)];
        int floor = DetermineFloor(location, type);
        int reward = DetermineReward(location, type, floor);
        string requestDetails = location + "|" + type + "|" + floor + "|" + reward;
        return requestDetails;
    }

    protected string DetermineLocation()
    {
        List<string> possibleLocations = dungeonData.GetAllKeys();
        int min = Mathf.Min(1, possibleLocations.Count);
        int max = Mathf.Min(possibleLocations.Count, partyData.guildCard.GetGuildRank());
        return possibleLocations[Random.Range(min, max)];
    }

    protected int DetermineFloor(string location, string type)
    {
        int maxFloors = int.Parse(dungeonData.ReturnValue(location).Split("|")[0]);
        if (type == "Escort"){return maxFloors;}
        return Random.Range(Mathf.Min(1, maxFloors), maxFloors + 1);
    }

    protected int DetermineReward(string location, string type, int floor)
    {
        // Base reward.
        int reward = requestTypeRewards[requestTypes.IndexOf(type)];
        // Multiply by location plus floor.
        reward *= (floor * dungeonData.GetAllKeys().IndexOf(location));
        return reward;
    }

    public void SelectRequest()
    {
        if (selected < 0){return;}
        selectedRequest = requests[selected];
        string[] blocks = selectedRequest.Split("|");
        for (int i = 0; i < requestDetails.Count; i++)
        {
            requestDetails[i].text = blocks[i];
        }
    }

    public void AcceptRequest()
    {
        if (selected < 0){return;}
        if (partyData.guildCard.RequestLimit())
        {
            popUp.SetMessage("You aren't allowed to take any more requests right now. Rank up more in order to be trusted to handle more requests at once.");
            return;
        }
        selectedRequest = requests[selected];
        string[] blocks = selectedRequest.Split("|");
        partyData.guildCard.AcceptDungeonRequest(blocks[0], blocks[1], int.Parse(blocks[2]), int.Parse(blocks[3]));
        RefreshRequests();
    }
}
