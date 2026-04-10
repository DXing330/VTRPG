using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Request : MonoBehaviour
{
    // Might create some features depending on the request.
    /*public SavedOverworld overworld;
    public SavedCaravan caravan;
    public List<string> requestGoals;
    public List<StatDatabase> goalDatabases;*/
    public string delimiter = "|";
    public int difficulty;
    public int GetDifficulty() { return difficulty; }
    public int reward;
    public void SetReward(int newInfo) { reward = newInfo; }
    public int GetReward() { return reward; }
    public string goal;
    public void SetGoal(string newInfo) { goal = newInfo; }
    public string GetGoal() { return goal; }
    public string goalSpecifics;
    public void SetGoalSpecifics(string newInfo) { goalSpecifics = newInfo; }
    public string GetGoalSpecifics() { return goalSpecifics; }
    public int goalAmount;
    public void SetGoalAmount(int newInfo) { goalAmount = newInfo; }
    public int GetGoalAmount() { return goalAmount; }
    // # days left before failure
    public int deadline;
    public void SetDeadline(int newInfo) { deadline = newInfo; }
    public void NewDay()
    {
        if (completed == 1){ return; }
        deadline--;
    }
    public int GetDeadline() { return deadline; }
    // 0 = false, 1 = true
    public int completed;
    public void Complete(){ completed = 1; }
    public bool GetCompletion() { return completed == 1; }
    public int location;
    public void SetLocation(int newInfo) { location = newInfo; }
    public int GetLocation() { return location; }
    // city/bandits/village/person/etc.
    public string locationSpecifics;
    public void SetLocationSpecifics(string newInfo) { locationSpecifics = newInfo; }
    public string GetLocationSpecifics() { return locationSpecifics; }
    public int failPenalty; // If you would go negative then derank.
    public void SetFailPenalty(int newInfo) { failPenalty = newInfo; }
    public int GetFailPenalty() { return failPenalty; }

    public void Reset()
    {
        difficulty = 0;
        reward = 0;
        goal = "";
        goalSpecifics = "";
        goalAmount = 0;
        deadline = 0;
        completed = 0;
        location = -1; // 0 is a real location, so don't use that as the default.
        locationSpecifics = "";
        failPenalty = 0;
    }

    public void Load(string requestDetails)
    {
        string[] data = requestDetails.Split(delimiter);
        if (data.Length < 6){ Reset(); return; }
        difficulty = int.Parse(data[0]);
        reward = int.Parse(data[1]);
        goal = data[2];
        goalSpecifics = data[3];
        goalAmount = int.Parse(data[4]);
        deadline = int.Parse(data[5]);
        completed = int.Parse(data[6]);
        location = int.Parse(data[7]);
        locationSpecifics = data[8];
        failPenalty = int.Parse(data[9]);
    }

    public string ReturnDetails()
    {
        string details = difficulty + delimiter + reward + delimiter + goal + delimiter + goalSpecifics + delimiter + goalAmount + delimiter + deadline + delimiter + completed + delimiter + location + delimiter + locationSpecifics + delimiter + failPenalty + delimiter;
        return details;
    }
}
