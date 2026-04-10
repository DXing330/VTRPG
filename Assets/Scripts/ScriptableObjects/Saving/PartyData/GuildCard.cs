using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GuildCard", menuName = "ScriptableObjects/DataContainers/SavedData/GuildCard", order = 1)]
public class GuildCard : SavedData
{
    public string delimiterTwo;
    // Higher rank -> larger party, bigger bag size(?), more equipment, harder quests
    protected int maxRank = 12;
    public int guildRank;
    public List<string> guildRankNames;
    public string GetGuildRankName()
    {
        if (guildRank < 0){return guildRankNames[0];}
        else if (guildRank >= maxRank){return guildRankNames[maxRank - 1];}
        return guildRankNames[guildRank];
    }
    public void SetGuildRank(int newRank) { guildRank = newRank; }
    public void GainGuildRank()
    {
        if (guildRank < maxRank)
        {
            IncreaseGuildRank();
        }
    }
    public Inventory partyBag;
    public DungeonBag partyDBag;
    public void IncreaseGuildRank()
    {
        guildRank++;
        IncreasePartyLimit();
        // Increase bag limit.
        partyBag.IncreaseItemLimit();
        // Increase dungeon bag limit.
        partyDBag.AddCapacity();
        // Increase storage limit.
    }
    public int GetGuildRank() { return guildRank; }
    // Gain exp = difficulty of quest squared?
    public int guildExp;
    public void SetGuildExp(int newExp) { guildExp = newExp; }
    public void GainGuildExp(int amount)
    {
        guildExp += Mathf.Max(1, amount / guildRank);
        if (guildExp > utility.Exponent(3, guildRank) && guildRank < maxRank)
        {
            IncreaseGuildRank();
        }
    }
    public int GetGuildExp() { return guildExp; }
    public int nextID;
    public void SetNextID(int newInfo){nextID = newInfo;}
    public int GetNextID(){return nextID;}
    public void IncrementNextID(){nextID++;}
    public int basePartyLimit;
    public void SetPartyLimit(int newInfo){basePartyLimit = newInfo;}
    public void IncreasePartyLimit(){basePartyLimit++;}
    public int GetPartyLimit(){return basePartyLimit;}
    // Separate dungeon vs overworld requests?
    // You can't fail a dungeon quest, you just keep trying til you succeed.
    // But there is a limit to how many quests you can accept at one time.
    public bool RequestLimit(){return dungeonLocations.Count >= guildRank;}
    public void ResetDungeonData()
    {
        dungeonLocations.Clear();
        dungeonQuestGoals.Clear();
        dungeonQuestFloors.Clear();
        dungeonQuestRewards.Clear();
    }
    public List<string> dungeonLocations;
    public List<string> GetQuestLocations(){return dungeonLocations;}
    public int QuestsAtLocation(string locationName)
    {
        return utility.CountStringsInList(dungeonLocations, locationName);
    }
    public List<int> GetQuestIndicesAtLocation(string location)
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < dungeonLocations.Count; i++)
        {
            if (dungeonLocations[i] == location){indices.Add(i);}
        }
        return indices;
    }
    public List<string> ReturnQuestGoalsAtLocation(string location)
    {
        List<int> indices = GetQuestIndicesAtLocation(location);
        List<string> goals = new List<string>();
        for (int i = 0; i < indices.Count; i++)
        {
            goals.Add(dungeonQuestGoals[indices[i]]);
        }
        return goals;
    }
    public void SetDungeonQuestLocations(List<string> newInfo)
    {
        dungeonLocations = newInfo;
        utility.RemoveEmptyListItems(dungeonLocations);
    }
    public List<string> dungeonQuestGoals;
    public void SetDungeonQuestGoals(List<string> newInfo)
    {
        dungeonQuestGoals = newInfo;
        utility.RemoveEmptyListItems(dungeonQuestGoals);
    }
    public List<string> GetQuestGoals(){return dungeonQuestGoals;}
    public List<int> dungeonQuestFloors;
    public void SetDungeonQuestFloors(List<string> newInfo)
    {
        dungeonQuestFloors = utility.ConvertStringListToIntList(newInfo);
        dungeonQuestFloors = utility.RemoveEmptyValues(dungeonQuestFloors);
    }
    public List<int> GetQuestFloors(){return dungeonQuestFloors;}
    public List<int> ReturnQuestFloorsAtLocation(string location)
    {
        List<int> indices = GetQuestIndicesAtLocation(location);
        List<int> floors = new List<int>();
        for (int i = 0; i < indices.Count; i++)
        {
            floors.Add(dungeonQuestFloors[indices[i]]);
        }
        return floors;
    }
    public List<int> dungeonQuestRewards;
    public void SetDungeonQuestRewards(List<string> newInfo)
    {
        dungeonQuestRewards = utility.ConvertStringListToIntList(newInfo);
        dungeonQuestRewards = utility.RemoveEmptyValues(dungeonQuestRewards);
    }
    public List<int> GetQuestRewards(){return dungeonQuestRewards;}
    public void AcceptDungeonRequest(string dungeonLocation, string goal, int floor, int reward)
    {
        dungeonLocations.Add(dungeonLocation);
        dungeonQuestGoals.Add(goal);
        dungeonQuestFloors.Add(floor);
        dungeonQuestRewards.Add(reward);
    }
    public List<string> ReturnGoalsAtDungeon(string dungeonName)
    {
        List<string> data = new List<string>();
        for (int i = 0; i < dungeonLocations.Count; i++)
        {
            if (dungeonLocations[i] == dungeonName)
            {
                data.Add(dungeonQuestGoals[i]);
            }
        }
        return data;
    }
    public List<int> ReturnFloorsAtDungeon(string dungeonName)
    {
        List<int> data = new List<int>();
        for (int i = 0; i < dungeonLocations.Count; i++)
        {
            if (dungeonLocations[i] == dungeonName)
            {
                data.Add(dungeonQuestFloors[i]);
            }
        }
        return data;
    }
    public void CompleteRequest(int index)
    {
        if (index < 0 || index >= dungeonLocations.Count){return;}
        dungeonLocations.RemoveAt(index);
        dungeonQuestGoals.RemoveAt(index);
        dungeonQuestFloors.RemoveAt(index);
        dungeonQuestRewards.RemoveAt(index);
    }

    public override void NewGame()
    {
        allData = newGameData;
        dataPath = Application.persistentDataPath + "/" + filename;
        File.WriteAllText(dataPath, allData);
        Load();
    }

    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "Rank=" + guildRank.ToString() + delimiter;
        allData += "Exp=" + guildExp.ToString() + delimiter;
        allData += "PartyLimit=" + GetPartyLimit() + delimiter;
        allData += "NextID=" + GetNextID() + delimiter;
        allData += "DLocations=" + String.Join(delimiterTwo, dungeonLocations) + delimiter;
        allData += "DGoals=" + String.Join(delimiterTwo, dungeonQuestGoals) + delimiter;
        allData += "DFloors=" + String.Join(delimiterTwo, dungeonQuestFloors) + delimiter;
        allData += "DRewards=" + String.Join(delimiterTwo, dungeonQuestRewards) + delimiter;
        File.WriteAllText(dataPath, allData);
    }

    public override void Load()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        if (File.Exists(dataPath)) { allData = File.ReadAllText(dataPath); }
        else { allData = newGameData; }
        if (allData.Length < newGameData.Length) { allData = newGameData; }
        dataList = allData.Split(delimiter).ToList();
        ResetDungeonData();
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
    }

    protected void LoadStat(string stat)
    {
        string[] blocks = stat.Split("=");
        if (blocks.Length < 2){return;}
        string data = blocks[1];
        switch (blocks[0])
        {
            default:
            break;
            case "Rank":
            guildRank = int.Parse(data);
            break;
            case "Exp":
            guildExp = int.Parse(data);
            break;
            case "PartyLimit":
            SetPartyLimit(int.Parse(data));
            break;
            case "NextID":
            SetNextID(int.Parse(data));
            break;
            case "DLocations":
            SetDungeonQuestLocations(data.Split(delimiterTwo).ToList());
            break;
            case "DGoals":
            SetDungeonQuestGoals(data.Split(delimiterTwo).ToList());
            break;
            case "DFloors":
            SetDungeonQuestFloors(data.Split(delimiterTwo).ToList());
            break;
            case "DRewards":
            SetDungeonQuestRewards(data.Split(delimiterTwo).ToList());
            break;
        }
    }

        // Overworld quest stuff.
    public List<string> acceptedQuests;
    public Request dummyRequest;
    public int failPenalty = 0;
    public int GetFailPenalty(){ return failPenalty; }
    public bool QuestCompleted(int index)
    {
        dummyRequest.Load(acceptedQuests[index]);
        return dummyRequest.GetCompletion();
    }
    public void AcceptQuest(string newQuest)
    {
        acceptedQuests.Add(newQuest);
        newQuests = 0;
        Save();
    }
    public bool QuestTile(int tileNumber)
    {
        for (int i = 0; i < acceptedQuests.Count; i++)
        {
            dummyRequest.Load(acceptedQuests[i]);
            if (dummyRequest.GetLocation() == tileNumber && !dummyRequest.GetCompletion()){ return true; }
        }
        return false;
    }
    public string QuestTypeFromTile(int tileNumber)
    {
        for (int i = 0; i < acceptedQuests.Count; i++)
        {
            dummyRequest.Load(acceptedQuests[i]);
            if (dummyRequest.GetLocation() == tileNumber) { return dummyRequest.GetGoal(); }
        }
        return "";
    }
    public int QuestIndexFromTile(int tileNumber)
    {
        for (int i = 0; i < acceptedQuests.Count; i++)
        {
            dummyRequest.Load(acceptedQuests[i]);
            if (dummyRequest.GetLocation() == tileNumber) { return i; }
        }
        return -1;
    }
    public bool CompleteDeliveryQuest(int tileNumber, SavedCaravan caravan)
    {
        int index = QuestIndexFromTile(tileNumber);
        dummyRequest.Load(acceptedQuests[index]);
        if (dummyRequest.GetCompletion()) { return false; } // Can't complete a completed request, obviously.
        if (caravan.EnoughCargo(dummyRequest.GetGoalSpecifics(), dummyRequest.GetGoalAmount()))
        {
            dummyRequest.Complete(); // Turn in the quest at the hub/city.
            caravan.UnloadCargo(dummyRequest.GetGoalSpecifics(), dummyRequest.GetGoalAmount());
            acceptedQuests[index] = dummyRequest.ReturnDetails();
            return true;
        }
        return false;
    }
    public void CompleteDefeatQuest(int tileNumber)
    {
        int index = QuestIndexFromTile(tileNumber);
        if (index == -1) { return; }
        dummyRequest.Load(acceptedQuests[index]);
        dummyRequest.Complete();
        acceptedQuests[index] = dummyRequest.ReturnDetails();
    }
    public bool CompleteEscortQuest(int tileNumber, PartyDataManager partyData)
    {
        int index = QuestIndexFromTile(tileNumber);
        dummyRequest.Load(acceptedQuests[index]);
        if (dummyRequest.GetCompletion()) { return false; }
        if (partyData.TempPartyMemberExists(dummyRequest.GetGoalSpecifics()))
        {
            partyData.RemoveTempPartyMember(dummyRequest.GetGoalSpecifics());
            dummyRequest.Complete();
            acceptedQuests[index] = dummyRequest.ReturnDetails();
            return true;
        }
        return false;
    }
    public int newQuests = 1;
    public void SubmitQuest(int index)
    {
        acceptedQuests.RemoveAt(index);
        newQuests = 1;
    }
    public bool RefreshQuests()
    {
        if (newQuests == 1)
        {
            newQuests = 0;
            return true;
        }
        return false;
    }
    public List<string> availableQuests;
    public int QuestReward(int index)
    {
        dummyRequest.Load(acceptedQuests[index]);
        return dummyRequest.GetReward();
    }
    public void SetAvailableQuests(List<string> newQuests)
    {
        availableQuests = new List<string>(newQuests);
    }
}
