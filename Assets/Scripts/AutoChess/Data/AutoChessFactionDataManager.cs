using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AutoChessFactionDataManager", menuName = "ScriptableObjects/AutoChess/AutoChessFactionDataManager", order = 1)]
public class AutoChessFactionDataManager : SavedData
{
    public string delimiter2;
    public List<string> mainFactions; // Require 3 field units to activate.
    public List<string> econFactions; // Includes bench units.
    public List<string> allFactions;
    public List<string> GetAllFactions(){return allFactions;}
    public List<string> allFactionStacks;
    public List<string> GetAllFactionStacks(){return allFactionStacks;}
    public void GainFactionStacks(string faction, int stackAmount)
    {
        int indexOf = allFactions.IndexOf(faction);
        if (indexOf < 0)
        {
            allFactions.Add(faction);
            allFactionStacks.Add(stackAmount.ToString());
            return;
        }
        allFactionStacks[indexOf] = (int.Parse(allFactionStacks[indexOf]) + stackAmount).ToString();
        utility.QuickSortByIntStringList(allFactions, allFactionStacks, 0, allFactions.Count - 1);
    }
    public override void NewGame()
    {
        allFactions.Clear();
        allFactionStacks.Clear();
        Save();
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "Factions=" + String.Join(delimiter2, allFactions) + delimiter;
        allData += "Stacks=" + String.Join(delimiter2, allFactionStacks) + delimiter;
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
    public void LoadStat(string data)
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
            allFactions = value.Split(delimiter2).ToList();
            return;
            case "Stacks":
            allFactionStacks = value.Split(delimiter2).ToList();
            return;
        }
    }
}
