using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MainCampaign", menuName = "ScriptableObjects/Story/MainCampaign", order = 1)]
public class MainCampaignState : SavedData
{
    public string delimiterTwo;
    public StatDatabase campaignData;
    // 0-prologue, 1-become strong, 2-conqueor, 3-demon army, 4-postgame
    //public int currentAct;
    /*
    0-0 : Introduce setting, clear dungeons for rewards and grow stronger
    0-1 : Clear dungeon as an initiation quest
    1-0 : Recruit party member / Train feat / buy equipment / learn spells
    1-1 : Rescue client from dungeon
    1-2 : Clear dungeon with escort
    1-3 : Pass battle check to ensure you're strong enough
    2-0 : Visit other cities and meet their leaders
    2-1 : Pick a city and help them sabotage their competitiors
    1. Destroy luxury resources by clearing dungeons (normal)
    2. Destroy villages by clearing dungeons (normal)
    3. Rob merchants by clearing dungeons (reverse rescue)
    4. Kidnap nobles by clearing dungeons (reverse rescue)
    5. Insert spies by clearing dungeons (escort)
    Each success will permanently weaken a city
    2-2 : Attack other cities
    Each city is a dungeon
    Each city you attack makes the other cities stronger (deeper dungeon, more buffs, stronger enemies)
    Build up your army each time and move to cities to conquer them
    Clear the city dungeon to conquer the city
    2-3 : Betray or submit to your chosen city
    2-4 : City building minigame
    1. Build outposts/villages through initial investments
    2. Protect them from bandits/monsters
    2a. Failure to protect means they will lose health and eventually be destroyed
    2b. Receive reports are random intervals to let you know if danger is near
    3. Collect tribute from them
    4. Hire underlings and expand your operations
    2-5 : Play for a few in game years?
    3-0 : New reports of demons appearing
    3-1 : Remove demons as they appear on overworld
    3-2 : Track down the demon general dungeons on the overworld
    2a. Either by yourself or through reports
    3-3 : Demon general dungeons
    3-3 : Demon lord dungeon
    4-0 : Post game super dungeons
    */
    // Track if you win or lose during each chapter, might affect the final ending?
    public bool CompletedStory()
    {
        return previousChapters.Count >= campaignData.GetAllKeys().Count;
    }
    public List<int> previousChapters;
    public List<int> GetPreviousChapters(){return previousChapters;}
    public void SetPreviousChapters(List<string> newInfo)
    {
        utility.RemoveEmptyListItems(newInfo);
        previousChapters = utility.ConvertStringListToIntList(newInfo, -1);
        utility.RemoveEmptyValues(previousChapters, -1);
    }
    public int currentChapter;
    public void CompleteChapter()
    {
        currentChapter = 1;
        Save();
    }
    public int GetCurrentChapter(){return currentChapter;}
    public void SetCurrentChapter(int newInfo){currentChapter = newInfo;}
    // Deliver on time, rescue on time, defeat on time, escort on time, explore first, defend til the end.
    public int chapterDeadline;
    public int GetCurrentDeadline(){return chapterDeadline;}
    public void SetCurrentDeadline(int newInfo){chapterDeadline = newInfo;}
    // Deliver, rescue, defeat, escort, explore, defend.
    public string currentRequest;
    public string GetCurrentRequest(){return currentRequest;}
    public void SetCurrentRequest(string newInfo){currentRequest = newInfo;}
    // Deliver what, rescue who, defeat who, escort who, explore where, defend what?
    public string requestSpecifics;
    public string GetRequestSpecifics(){return requestSpecifics;}
    public string GetRequestSpecificsName()
    {
        string[] nameQuantity = requestSpecifics.Split("*");
        return nameQuantity[0];
    }
    public void SetRequestSpecifics(string newInfo){requestSpecifics = newInfo;}
    // Drop off site, last seen location, last seen location, drop off location, dungeon location, defense location.
    public string requestLocation;
    public string GetRequestLocation(){return requestLocation;}
    public void SetRequestLocation(string newInfo){requestLocation = newInfo;}
    public string requestBattleDetails;
    public string GetRequestBattleDetails(){return requestBattleDetails;}
    public void SetRequestBattleDetails(string newInfo){requestBattleDetails = newInfo;}

    public void NextChapter(PartyDataManager partyData)
    {
        previousChapters.Add(currentChapter);
        if (currentChapter > 0)
        {
            partyData.guildCard.GainGuildRank();
        }
        NewChapter();
    }

    public void NewChapter()
    {
        currentChapter = 0;
        // Check if it's the end of the main story.
        if (campaignData.GetAllKeys().Count <= previousChapters.Count)
        {
            return;
        }
        string[] newInfo = campaignData.ReturnValueAtIndex(previousChapters.Count).Split("|");
        SetCurrentDeadline(int.Parse(newInfo[0]));
        SetCurrentRequest(newInfo[1]);
        SetRequestSpecifics(newInfo[2]);
        SetRequestLocation(newInfo[3]);
        SetRequestBattleDetails(newInfo[4]);
        Save();
    }

    public override void NewGame()
    {
        previousChapters.Clear();
        currentChapter = 0;
        chapterDeadline = 0;
        currentRequest = "";
        requestSpecifics = "";
        requestLocation = "";
        Save();
        Load();
    }

    public override void Save()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = "";
        allData += String.Join(delimiterTwo, previousChapters) + delimiter;
        allData += currentChapter + delimiter;
        allData += chapterDeadline + delimiter;
        allData += currentRequest + delimiter;
        allData += requestSpecifics + delimiter;
        allData += requestLocation + delimiter;
        File.WriteAllText(dataPath, allData);
    }

    public override void Load()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        if (File.Exists(dataPath)){allData = File.ReadAllText(dataPath);}
        else
        {
            NewGame();
            return;
        }
        dataList = allData.Split(delimiter).ToList();
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i], i);
        }
    }

    protected void LoadStat(string stat, int index)
    {
        switch (index)
        {
            default:
            break;
            case 0:
            SetPreviousChapters(stat.Split(delimiterTwo).ToList());
            break;
            case 1:
            SetCurrentChapter(int.Parse(stat));
            break;
            case 2:
            SetCurrentDeadline(int.Parse(stat));
            break;
            case 3:
            SetCurrentRequest(stat);
            break;
            case 4:
            SetRequestSpecifics(stat);
            break;
            case 5:
            SetRequestLocation(stat);
            break;
        }
    }
}