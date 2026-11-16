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
    public List<MysteryDungeonQuest> quests = new();
    public List<MysteryDungeonQuest> GetQuests(){return quests;}
    public void QuestStartDungeon(PartyDataManager partyData, string location)
    {
        for (int i = 0; i < quests.Count; i++)
        {
            quests[i].StartDungeon(partyData, location);
        }
    }
    public void QuestStartFloor(int floor, MysteryDungeonBattleMap map)
    {
        for (int i = 0; i < quests.Count; i++)
        {
            quests[i].EnterFloor(floor, map);
        }
    }
    public void CheckQuestCompletion(PartyDataManager partyData)
    {
        for (int i = 0; i < quests.Count; i++)
        {
            quests[i].CheckCompletion(partyData);
        }
    }
    public void CheckQuestCompletion(int floor, MysteryDungeonBattleMap map)
    {
        for (int i = 0; i < quests.Count; i++)
        {
            quests[i].CheckCompletion(floor, map);
        }
    }
    public void RemoveQuestAtIndex(int index)
    {
        if (index < 0 || index >= quests.Count){return;}
        quests.RemoveAt(index);
    }
    public void SetQuests(string allQuestString)
    {
        quests = new();
        string[] questStrings = allQuestString.Split(",");
        for (int i = 0; i < questStrings.Length; i++)
        {
            if (string.IsNullOrEmpty(questStrings[i])){continue;}
            MysteryDungeonQuest newQuest = new MysteryDungeonQuest();
            if (newQuest.LoadQuestFromString(questStrings[i]))
            {
                quests.Add(newQuest);
            }
        }
    }
    // Limit to how many quests you can accept at one time.
    public bool AtQuestLimit()
    {
        return quests.Count >= 6;
    }
    public void AcceptRequest(MysteryDungeonQuest quest)
    {
        quests.Add(quest);
    }
    public void CompleteRequest(int index)
    {
    }
    public override void NewGame()
    {
        allData = newGameData;
        dataPath = GetSavePath();
        File.WriteAllText(dataPath, allData);
        Load();
    }
    public override void Save()
    {
        dataPath = GetSavePath();
        allData = "";
        allData += "Rank=" + guildRank.ToString() + delimiter;
        allData += "Exp=" + guildExp.ToString() + delimiter;
        allData += "PartyLimit=" + GetPartyLimit() + delimiter;
        allData += "NextID=" + GetNextID() + delimiter;
        allData += "Quests=";
        for (int i = 0; i < quests.Count; i++)
        {
            allData += quests[i].ReturnQuestAsString();
            if (i < quests.Count - 1){allData += ",";}
        }
        allData += delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = GetSavePath();
        if (File.Exists(dataPath)) { allData = File.ReadAllText(dataPath); }
        else { allData = newGameData; }
        if (allData.Length < newGameData.Length) { allData = newGameData; }
        dataList = allData.Split(delimiter).ToList();
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
    }
    public override void LoadStat(string stat)
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
            case "Quests":
            SetQuests(data);
            break;
        }
    }
}
