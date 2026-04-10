using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleStatsTrackerSaving", menuName = "ScriptableObjects/Debug/BattleStatsTrackerSaving", order = 1)]
public class BattleStatsTrackerSaving : SavedData
{
    // Keep track of how many iterations were ran to return the average stats at the very end.
    public BattleSimulatorState simulatorState;
    public string delimiterTwo;
    public int fieldsTracked = 6;
    public List<int> winningTeams;
    public List<string> actorNames;
    public List<string> GetActorNames()
    {
        return actorNames;
    }
    public List<string> actorSprites;
    public List<string> GetActorSprites()
    {
        return actorSprites;
    }
    public List<int> actorTeams;
    public List<int> GetActorTeams()
    {
        return actorTeams;
    }
    public List<int> actorsDamageDealt;
    public List<int> GetDamageDealt()
    {
        return actorsDamageDealt;
    }
    public List<int> actorsDamageTaken;
    public List<int> GetDamageTaken()
    {
        return actorsDamageTaken;
    }
    public override void NewGame()
    {
        ResetTracker();
        Save();
    }
    public void ResetTracker()
    {
        actorNames = new List<string>();
        actorSprites = new List<string>();
        actorTeams = new List<int>();
        actorsDamageDealt = new List<int>();
        actorsDamageTaken = new List<int>();
        winningTeams = new List<int>();
    }
    public void LoadToTracker(BattleStatsTracker battleTracker)
    {
        // Divide the damage dealt in order to get the average damage per battle.
        int divider = simulatorState.multiBattleCount;
        for (int i = 0; i < actorsDamageDealt.Count; i++)
        {
            actorsDamageDealt[i] = actorsDamageDealt[i] / divider;
            actorsDamageTaken[i] = actorsDamageTaken[i] / divider;
        }
        battleTracker.actorNames = new List<string>(actorNames);
        battleTracker.actorSprites = new List<string>(actorSprites);
        battleTracker.actorTeams = new List<int>(actorTeams);
        battleTracker.actorsDamageDealt = new List<int>(actorsDamageDealt);
        battleTracker.actorsDamageTaken = new List<int>(actorsDamageTaken);
        battleTracker.winningTeams = new List<int>(winningTeams);
    }
    public void AddNewActorToTracker(string aName, string sprite, int team, int dd, int dt)
    {
        actorNames.Add(aName);
        actorSprites.Add(sprite);
        actorTeams.Add(team);
        actorsDamageDealt.Add(dd);
        actorsDamageTaken.Add(dt);
    }
    public void UpdateActorInTracker(int index, int dd, int dt)
    {
        actorsDamageDealt[index] += dd;
        actorsDamageTaken[index] += dt;
    }
    public void AddToTracker(BattleStatsTracker battleTracker)
    {
        winningTeams.Add(battleTracker.winningTeam);
        List<string> newNames = battleTracker.GetActorNames();
        List<string> newSprites = battleTracker.GetActorSprites();
        List<int> newTeams = battleTracker.GetActorTeams();
        List<int> newDamages = battleTracker.GetDamageDealt();
        List<int> newDamageTaken = battleTracker.GetDamageTaken();
        for (int i = 0; i < newNames.Count; i++)
        {
            int index = ReturnMatchingIndexByName(newNames[i]);
            if (index < 0)
            {
                AddNewActorToTracker(newNames[i], newSprites[i], newTeams[i], newDamages[i], newDamageTaken[i]);
            }
            else
            {
                UpdateActorInTracker(index, newDamages[i], newDamageTaken[i]);
            }
        }
        Save();
    }
    public int ReturnMatchingIndexByName(string newInfo)
    {
        return actorNames.IndexOf(newInfo);
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += String.Join(delimiterTwo, actorNames) + delimiter;
        allData += String.Join(delimiterTwo, actorSprites) + delimiter;
        allData += String.Join(delimiterTwo, actorTeams) + delimiter;
        allData += String.Join(delimiterTwo, actorsDamageDealt) + delimiter;
        allData += String.Join(delimiterTwo, actorsDamageTaken) + delimiter;
        allData += String.Join(delimiterTwo, winningTeams) + delimiter;
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
        if (blocks.Length < fieldsTracked)
        {
            NewGame();
            return;
        }
        actorNames = utility.RemoveEmptyListItems(blocks[0].Split(delimiterTwo).ToList());
        actorSprites = utility.RemoveEmptyListItems(blocks[1].Split(delimiterTwo).ToList());
        actorTeams = utility.ConvertStringListToIntList(utility.RemoveEmptyListItems(blocks[2].Split(delimiterTwo).ToList()));
        actorsDamageDealt = utility.ConvertStringListToIntList(utility.RemoveEmptyListItems(blocks[3].Split(delimiterTwo).ToList()));
        actorsDamageTaken = utility.ConvertStringListToIntList(utility.RemoveEmptyListItems(blocks[4].Split(delimiterTwo).ToList()));
        winningTeams = utility.ConvertStringListToIntList(utility.RemoveEmptyListItems(blocks[5].Split(delimiterTwo).ToList()));
    }
}
