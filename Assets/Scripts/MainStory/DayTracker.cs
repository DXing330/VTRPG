using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DayTracker", menuName = "ScriptableObjects/Story/DayTracker", order = 1)]
public class DayTracker : SavedData
{
    // Keep track of the day obviously.
    public int day;
    public int GetDay(){return day;}
    // Get paid every day?
    public int lastPayDay;
    public int GetLastPayDay(){return lastPayDay;}
    public int PayDateDifference()
    {
        return day - lastPayDay;
    }
    public void CollectPay()
    {
        lastPayDay = day;
    }
    // Keep track of any time limited game stuff.
    public int storyQuestStartDay;

    public void DayTrackerNewDay()
    {
        day++;
        Save();
    }

    public void NewQuest()
    {
        storyQuestStartDay = day;
        Save();
    }

    public bool DeadlineReached(int deadline)
    {
        return day >= storyQuestStartDay + deadline;
    }

    public int DaysLeft(int deadline)
    {
        return storyQuestStartDay + deadline - day;
    }

    public override void NewGame()
    {
        day = 1;
        lastPayDay = 1;
        storyQuestStartDay = 1;
        Save();
    }

    public override void Save()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = "";
        allData += day + delimiter;
        allData += storyQuestStartDay + delimiter;
        allData += lastPayDay + delimiter;
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
            day = utility.SafeParseInt(stat, 1);
            break;
            case 1:
            storyQuestStartDay = utility.SafeParseInt(stat, 1);
            break;
            case 2:
            lastPayDay = utility.SafeParseInt(stat, 1);
            break;
        }
    }
}
