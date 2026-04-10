using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StSEnemyTracker", menuName = "ScriptableObjects/StS/StSEnemyTracker", order = 1)]
public class StSEnemyTracker : SavedData
{
    public string delimiterTwo;
    public StSState stsState;
    public StSMapSaveData mapState;
    public RNGUtility enemyRNGSeed;
    public List<StatDatabase> floorEnemies;
    public List<StatDatabase> floorEnemyDifficulties;
    public List<StatDatabase> floorElites;
    public List<StatDatabase> floorBosses;
    public List<string> enemyPool;
    public List<string> elitePool;
    public List<string> defaultAllies;
    public string previousElite;
    public string floorBoss;

    public override void NewGame()
    {
        previousElite = "";
        int floor = stsState.GetFloor();
        enemyPool = floorEnemies[floor - 1].GetAllKeys();
        elitePool = floorElites[floor - 1].GetAllKeys();
        floorBoss = floorBosses[floor - 1].ReturnKeyAtIndex(enemyRNGSeed.Range(0, floorBosses[floor - 1].keys.Count));
        Save();
    }

    public void NewFloor()
    {
        int floor = stsState.GetFloor();
        previousElite = "";
        enemyPool = floorEnemies[floor - 1].GetAllKeys();
        elitePool = floorElites[floor - 1].GetAllKeys();
        floorBoss = floorBosses[floor - 1].ReturnKeyAtIndex(enemyRNGSeed.Range(0, floorBosses[floor - 1].keys.Count));
        Save();
    }

    public string RandomNewBoss()
    {
        int floor = stsState.GetFloor();
        string newBoss = floorBosses[floor - 1].ReturnKeyAtIndex(enemyRNGSeed.Range(0, floorBosses[floor - 1].keys.Count));
        if (newBoss != floorBoss)
        {
            return newBoss;
        }
        return RandomNewBoss();
    }

    public List<string> GetBossData(bool additional = false)
    {
        int floor = stsState.GetFloor();
        if (additional)
        {
            // Generate another boss except the one that was just fought.
            string newBoss = RandomNewBoss();
            return floorBosses[floor - 1].ReturnValue(newBoss).Split("-").ToList();
        }
        return floorBosses[floor - 1].ReturnValue(floorBoss).Split("-").ToList();
    }

    public string GetEliteData(string eliteName)
    {
        int floor = stsState.GetFloor();
        return floorElites[floor - 1].ReturnValue(eliteName);
    }

    public string GetEliteName()
    {
        string eliteName = elitePool[enemyRNGSeed.Range(0, elitePool.Count)];
        if (eliteName == previousElite)
        {
            return GetEliteName();
        }
        previousElite = eliteName;
        Save();
        return eliteName;
    }

    public string GetEnemyData(string enemyName)
    {
        int floor = stsState.GetFloor();
        return floorEnemies[floor - 1].ReturnValue(enemyName);
    }

    public string GetEnemyName()
    {
        int floor = stsState.GetFloor();
        // Determine the difficultly based on how many fights you've taken.
        // First two fights are easy, then start getting harder fights.
        int battleCount = mapState.ReturnBattleCount();
        int difficulty = 0;
        if (battleCount > 2){difficulty = 1;}
        List<string> possibleEnemies = new List<string>();
        for (int i = 0; i < enemyPool.Count; i++)
        {
            if (int.Parse(floorEnemyDifficulties[floor - 1].ReturnValue(enemyPool[i])) == difficulty)
            {
                possibleEnemies.Add(enemyPool[i]);
            }
        }
        if (possibleEnemies.Count <= 0 && difficulty <= 0)
        {
            return enemyPool[enemyRNGSeed.Range(0, enemyPool.Count)];
        }
        string enemyName = possibleEnemies[enemyRNGSeed.Range(0, possibleEnemies.Count)];
        enemyPool.Remove(enemyName);
        Save();
        return enemyName;
    }

    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += String.Join(delimiterTwo, enemyPool) + delimiter;
        allData += String.Join(delimiterTwo, elitePool) + delimiter;
        allData += previousElite + delimiter;
        allData += floorBoss + delimiter;
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
            // Pretend you're entering a new floor.
            NewFloor();
            return;
        }
        string[] blocks = allData.Split(delimiter);
        enemyPool = blocks[0].Split(delimiterTwo).ToList();
        elitePool = blocks[1].Split(delimiterTwo).ToList();
        previousElite = blocks[2];
        floorBoss = blocks[3];
    }
}
