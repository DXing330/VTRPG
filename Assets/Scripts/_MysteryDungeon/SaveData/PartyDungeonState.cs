using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

// Tracks Hunger/Health Inside Dungeons + Assigned AI To Party Members, Updates + Saves On New Floor To Allow SaveScum.
[CreateAssetMenu(fileName = "PartyDungeonState", menuName = "ScriptableObjects/DataContainers/SavedData/PartyDungeonState", order = 1)]
public class PartyDungeonState : SavedData
{
    // Stored Until Changed.
    public Dictionary<int, string> partyAIAssignments = new();
    public void ChangePartyAI(int newID, string newAI)
    {
        partyAIAssignments[newID] = newAI;
    }
    public string GetAIOfPartyMember(int partyID)
    {
        if(partyAIAssignments.TryGetValue(partyID, out string value))
        {
            return value;
        }
        return "None";
    }
    // Only 1 Member Can Be The Leader Inside A Dungeon At A Time.
    public int leaderID;
    public void SetLeaderID(int newID){leaderID = newID;}
    public int maxFullness;
    // Restores Starting Each Dungeon + Eating.
    public int currentFullness;
    // Changes Each Dungeon Based On Who You Pick To Enter + Health Updates Each Floor/Move.
    public Dictionary<int, string> currentPartyStats = new();
    public string GetCurrentStats(int ID)
    {
        if(currentPartyStats.TryGetValue(ID, out string value))
        {
            return value;
        }
        // Default Is Full Health And No Mana.
        return "-1||0";
    }
    public void EnterDungeon(int newLeader, List<int> partyIDs, PartyDataManager partyData)
    {
        leaderID = newLeader;
        maxFullness = 600;
        currentFullness = maxFullness;
        currentPartyStats.Clear();
        for (int i = 0; i < partyIDs.Count; i++)
        {
            currentPartyStats[partyIDs[i]] = partyData.ReturnActorFromID(partyIDs[i]).ReturnPersistentStats();
        }
        Save(); 
    }
    // Take The Party As Input, Update Health + Fullness + Remove Defeated Members, This Implies Leader Is Alive Else The Dungeon Would Be Over, No New Floor.
    public void NewFloor(List<TacticActor> actors)
    {
        currentPartyStats.Clear();
        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i].GetTeam() != 0 || actors[i].GetHealth() <= 0){continue;}
            if (actors[i].GetID() >= 0)
            {
                currentPartyStats[actors[i].GetID()] = actors[i].ReturnPersistentStats();
            }
            if (actors[i].GetID() == leaderID)
            {
                currentFullness = actors[i].GetFullness();
            }
        }
        Save();
    }
    public override void Save()
    {
        dataPath = GetSavePath();
        allData = "";
        allData += "AI=" + utility.IntStringDictionaryToString(partyAIAssignments) + delimiter;
        allData += "Leader=" + leaderID + delimiter;
        allData += "MaxFullness=" + maxFullness + delimiter;
        allData += "CurrentFullness=" + currentFullness + delimiter;
        allData += "CurrentParty=" + utility.IntStringDictionaryToString(currentPartyStats) + delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = GetSavePath();
        if (File.Exists(dataPath))
        {
            allData = File.ReadAllText(dataPath);
        }
        else
        {
            NewGame();
            return;
        }
        string[] blocks = allData.Split(delimiter);
        for (int i = 0; i < blocks.Length; i++)
        {
            LoadStat(blocks[i]);
        }
    }
    public override void LoadStat(string data)
    {
        string[] blocks = data.Split(new[] { '=' }, 2);
        if (blocks.Length < 2){return;}
        string key = blocks[0];
        string value = blocks[1];
        switch (key)
        {
            default:
            break;
            case "AI":
                partyAIAssignments = utility.StringToIntStringDictionary(value);
                break;
            case "Leader":
                int.TryParse(value, out leaderID);
                break;
            case "MaxFullness":
                int.TryParse(value, out maxFullness);
                break;
            case "CurrentFullness":
                int.TryParse(value, out currentFullness);
                break;
            case "CurrentParty":
                currentPartyStats = utility.StringToIntStringDictionary(value);
                break;
        }
    }
}
