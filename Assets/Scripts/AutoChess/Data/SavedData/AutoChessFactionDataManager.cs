using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AutoChessFactionDataManager", menuName = "ScriptableObjects/AutoChess/AutoChessFactionDataManager", order = 1)]
public class AutoChessFactionDataManager : SavedData
{
    public bool logDataManager = true;
    public void DisableLogs(){logDataManager = false;}
    public AutoChessLogDataManager logData; // Track actor trait timing + stacks.
    public void AddLog(string newLog)
    {
        if (!logDataManager || logData == null){return;}
        logData.AddLog(newLog);
    }
    public RNGUtility RNG;
    public string delimiter2;
    public List<string> mainFactions; // Require 3 field units to activate.
    public bool MainFaction(string factionName)
    {
        return mainFactions.Contains(factionName);
    }
    public List<string> econFactions; // Includes bench units.
    public bool EconFaction(string factionName)
    {
        return econFactions.Contains(factionName);
    }
    public List<string> allFactions;
    public void SetAllFactions(List<string> newFactions)
    {
        utility.RemoveEmptyListItems(newFactions);
        allFactions = new List<string>(newFactions);
    }
    public List<string> GetAllFactions()
    {
        return new List<string>(allFactions);
    }
    public string GetAllFactionStacksString()
    {
        string allFactionStacks = "";
        for (int i = 0; i < allFactions.Count; i++)
        {
            allFactionStacks += $"{allFactions[i]}:{GetStacksOfFaction(allFactions[i])}";
            if (i < allFactions.Count - 1)
            {
                allFactionStacks += ",";
            }
        }
        return allFactionStacks;
    }
    public List<string> allFactionStacks;
    public string HighestStackFaction()
    {
        int index = -1;
        int stacks = 0;
        for (int i = 0; i < allFactionStacks.Count; i++)
        {
            if (int.Parse(allFactionStacks[i]) > stacks)
            {
                stacks = int.Parse(allFactionStacks[i]);
                index = i;
            }
        }
        if (index < 0){return "";}
        return allFactions[index];
    }
    public void SetAllFactionStacks(List<string> newFactions)
    {
        utility.RemoveEmptyListItems(newFactions);
        allFactionStacks = new List<string>(newFactions);
    }
    public int GetMainFactionStacks()
    {
        int count = 0;
        for (int i = 0; i < mainFactions.Count; i++)
        {
            count += int.Parse(GetStacksOfFaction(mainFactions[i]));
        }
        return count;
    }
    public string GetStacksOfFaction(string factionName)
    {
        int indexOf = allFactions.IndexOf(factionName);
        if (indexOf < 0){return "0";}
        return allFactionStacks[indexOf];
    }
    public List<string> GetAllFactionStacks()
    {
        return new List<string>(allFactionStacks);
    }
    // This should only be called from a trait trigger during prep/battle.
    public void GainFactionStacks(string faction, int stackAmount)
    {
        if (faction.Length <= 0 || faction == "Harmony" || faction == "Assist"){return;}
        int indexOf = allFactions.IndexOf(faction);
        int oldStacks = 0;
        if (indexOf < 0)
        {
            allFactions.Add(faction);
            allFactionStacks.Add(stackAmount.ToString());
        }
        else
        {
            oldStacks = int.Parse(allFactionStacks[indexOf]);
            allFactionStacks[indexOf] = (oldStacks + stackAmount).ToString();
        }
        AddLog(faction + ": +" + stackAmount + " (" + oldStacks + "->" + (oldStacks + stackAmount) + ")");
    }
    public List<string> activeFactions;
    public string GetActiveFactionState()
    {
        string state = "";
        for (int i = 0; i < activeFactions.Count; i++)
        {
            state += $"{activeFactions[i]}:{GetStacksOfFaction(activeFactions[i])}";
            if (i < activeFactions.Count - 1)
            {
                state += ",";
            }
        }
        return state;
    }
    public void SetActiveFactions(List<string> newFactions)
    {
        utility.RemoveEmptyListItems(newFactions);
        activeFactions = new List<string>(newFactions);
    }
    public List<string> GetNonEconActiveFactions()
    {
        List<string> allActiveFactions = GetActiveFactions();
        for (int i = allActiveFactions.Count - 1; i >= 0; i--)
        {
            if (EconFaction(allActiveFactions[i]))
            {
                allActiveFactions.RemoveAt(i);
            }
        }
        return allActiveFactions;
    }
    public List<string> GetActiveFactions()
    {
        return new List<string>(activeFactions);
    }
    public bool FactionActive(string factionName)
    {
        return activeFactions.Contains(factionName);
    }
    public string HighestStackActiveFaction()
    {
        if (activeFactions.Count <= 0){return "";}
        int stackCount = -1;
        int index = -1;
        for (int i = 0; i < activeFactions.Count; i++)
        {
            int stacks = int.Parse(GetStacksOfFaction(activeFactions[i]));
            if (stacks > stackCount)
            {
                stackCount = stacks;
                index = i;
            }
        }
        return activeFactions[index];
    }
    public List<int> activeFactionCount;
    public void SetActiveFactionCount(List<int> newFactions)
    {
        activeFactionCount = new List<int>(newFactions);
    }
    public List<int> GetActiveFactionCount()
    {
        return new List<int>(activeFactionCount);
    }
    public List<int> GetActiveFactionStacks()
    {
        List<int> stacks = new List<int>();
        for (int i = 0; i < activeFactions.Count; i++)
        {
            stacks.Add(int.Parse(GetStacksOfFaction(activeFactions[i])));
        }
        return stacks;
    }
    public int GetCountOfFaction(string factionName)
    {
        int indexOf = activeFactions.IndexOf(factionName);
        if (indexOf < 0){return 0;}
        return activeFactionCount[indexOf];
    }
    public override void NewGame()
    {
        allFactions.Clear();
        allFactionStacks.Clear();
        activeFactions.Clear();
        activeFactionCount.Clear();
        Save();
    }
    public override void Save()
    {
        dataPath = GetSavePath();
        allData = "";
        allData += "Factions=" + String.Join(delimiter2, allFactions) + delimiter;
        allData += "Stacks=" + String.Join(delimiter2, allFactionStacks) + delimiter;
        allData += "Active=" + String.Join(delimiter2, activeFactions) + delimiter;
        allData += "ActiveFieldCount=" + String.Join(delimiter2, activeFactionCount) + delimiter;
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
        string[] blocks = data.Split("=");
        if (blocks.Length < 2){return;}
        string key = blocks[0];
        string value = blocks[1];
        switch (key)
        {
            default:
            return;
            case "Factions":
            SetAllFactions(value.Split(delimiter2).ToList());
            return;
            case "Stacks":
            SetAllFactionStacks(value.Split(delimiter2).ToList());
            return;
            case "Active":
            SetActiveFactions(value.Split(delimiter2).ToList());
            return;
            case "ActiveFieldCount":
            SetActiveFactionCount(utility.ConvertStringListToIntList(value.Split(delimiter2).ToList()));
            return;
        }
    }
    public void GainActiveStacks(List<string> actorFactions, int amount = 2)
    {
        for (int i = 0; i < actorFactions.Count; i++)
        {
            if (!activeFactions.Contains(actorFactions[i])){continue;}
            GainFactionStacks(actorFactions[i], amount);
        }
    }
    // TODO Change This An Actor? For Better Logging, Knowing Which Actor Caused Which Stack Gain.
    public void GainStacksFromTraitSwitch(AutoChessTrait trait, List<string> actorFactions, int amount = 1, List<string> frontFactions = null)
    {
        amount = Mathf.Max(amount, utility.SafeParseInt(trait.specifics));
        switch (trait.effect)
        {
            default:
            case "Self":
            for (int i = 0; i < actorFactions.Count; i++)
            {
                GainFactionStacks(actorFactions[i], amount);
            }
            break;
            case "SelfActive":
            GainActiveStacks(actorFactions, amount);
            break;
            case "FrontActive":
            GainActiveStacks(frontFactions, amount);
            break;
            case "SelfAndFrontActive":
            GainActiveStacks(actorFactions, amount);
            GainActiveStacks(frontFactions, amount);
            break;
            case "RandomActive":
            if (activeFactions.Count <= 0){return;}
            string randomFaction = activeFactions[RNG.SeedRange(0, activeFactions.Count)];
            GainFactionStacks(randomFaction, amount);
            break;
            case "HighestActive":
            if (activeFactions.Count <= 0){return;}
            GainFactionStacks(HighestStackActiveFaction(), amount);
            break;
        }
    }
}
