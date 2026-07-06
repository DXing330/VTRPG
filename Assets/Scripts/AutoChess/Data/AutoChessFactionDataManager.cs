using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AutoChessFactionDataManager", menuName = "ScriptableObjects/AutoChess/AutoChessFactionDataManager", order = 1)]
public class AutoChessFactionDataManager : SavedData
{
    public RNGUtility autoChessShopRNG;
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
        allFactions = newFactions;
    }
    public List<string> GetAllFactions(){return allFactions;}
    public List<string> allFactionStacks;
    public void SetAllFactionStacks(List<string> newFactions)
    {
        utility.RemoveEmptyListItems(newFactions);
        allFactionStacks = newFactions;
    }
    public string GetStacksOfFaction(string factionName)
    {
        int indexOf = allFactions.IndexOf(factionName);
        if (indexOf < 0){return "0";}
        return allFactionStacks[indexOf];
    }
    public List<string> GetAllFactionStacks(){return allFactionStacks;}
    public void GainFactionStacks(string faction, int stackAmount)
    {
        if (faction.Length <= 0){return;}
        int indexOf = allFactions.IndexOf(faction);
        if (indexOf < 0)
        {
            allFactions.Add(faction);
            allFactionStacks.Add(stackAmount.ToString());
            return;
        }
        allFactionStacks[indexOf] = (int.Parse(allFactionStacks[indexOf]) + stackAmount).ToString();
    }
    public List<string> activeFactions;
    public void SetActiveFactions(List<string> newFactions)
    {
        utility.RemoveEmptyListItems(newFactions);
        activeFactions = newFactions;
    }
    public List<string> GetActiveFactions(){return activeFactions;}
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
        activeFactionCount = newFactions;
    }
    public List<int> GetActiveFactionCount(){return activeFactionCount;}
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
        if (indexOf <= 0){return 0;}
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
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "Factions=" + String.Join(delimiter2, allFactions) + delimiter;
        allData += "Stacks=" + String.Join(delimiter2, allFactionStacks) + delimiter;
        allData += "Active=" + String.Join(delimiter2, activeFactions) + delimiter;
        allData += "ActiveFieldCount=" + String.Join(delimiter2, activeFactionCount) + delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
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
            for (int i = 0; i < actorFactions.Count; i++)
            {
                if (!activeFactions.Contains(actorFactions[i])){continue;}
                GainFactionStacks(actorFactions[i], amount);
            }
            break;
            case "FrontActive":
            for (int i = 0; i < frontFactions.Count; i++)
            {
                if (!activeFactions.Contains(frontFactions[i])){continue;}
                GainFactionStacks(frontFactions[i], amount);
            }
            break;
            case "SelfAndFrontActive":
            for (int i = 0; i < frontFactions.Count; i++)
            {
                if (!activeFactions.Contains(frontFactions[i])){continue;}
                GainFactionStacks(frontFactions[i], amount);
            }
            for (int i = 0; i < actorFactions.Count; i++)
            {
                if (!activeFactions.Contains(actorFactions[i])){continue;}
                GainFactionStacks(actorFactions[i], amount);
            }
            break;
            case "RandomActive":
            if (activeFactions.Count <= 0){return;}
            string randomFaction = activeFactions[autoChessShopRNG.SeedRange(0, activeFactions.Count)];
            GainFactionStacks(randomFaction, amount);
            break;
            case "HighestActive":
            if (activeFactions.Count <= 0){return;}
            GainFactionStacks(HighestStackActiveFaction(), amount);
            break;
        }
    }
}
