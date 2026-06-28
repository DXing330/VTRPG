using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AutoChessEnemyDataManager", menuName = "ScriptableObjects/AutoChess/AutoChessEnemyDataManager", order = 1)]
public class AutoChessEnemyDataManager : SavedData
{
    public string delimiter2;
    public StatDatabase enemyData;
    public StatDatabase enemyGroups;
    public StatDatabase enemyDifficulty;
    public RNGUtility autoChessEnemyRNG;
    public int round;
    public List<string> availableEnemyGroups;
    public List<string> nextRoundEnemies;
    public List<string> GetNextRoundEnemies(){return nextRoundEnemies;}
    public List<string> GetEnemiesOfGroup(string group)
    {
        List<string> groupEnemies = new List<string>();
        List<string> allEnemies = enemyData.GetAllKeys();
        for (int i = 0; i < allEnemies.Count; i++)
        {
            if (enemyGroups.ReturnValue(allEnemies[i]) == group)
            {
                groupEnemies.Add(allEnemies[i]);
            }
        }
        return groupEnemies;
    }
    public List<string> GetDifficultyOfEnemies(List<string> enemies)
    {
        List<string> enemyDifficulties = new List<string>();
        for (int i = 0; i < enemies.Count; i++)
        {
            enemyDifficulties.Add(enemyDifficulty.ReturnValue(enemies[i]));
        }
        return enemyDifficulties;
    }
    public List<string> GenerateEnemiesOfDifficulty(List<string> enemies, List<string> difficulties, int amount, bool normal = true)
    {
        if (amount <= 0){return new List<string>();}
        List<string> generatedEnemies = new List<string>();
        List<string> possibleEnemies = new List<string>();
        for (int i = 0; i < enemies.Count; i++)
        {
            if (difficulties[i] == "Normal" && normal)
            {
                possibleEnemies.Add(enemies[i]);
            }
            else if (difficulties[i] != "Normal" && !normal)
            {
                possibleEnemies.Add(enemies[i]);
            }
        }
        for (int i = 0; i < amount; i++)
        {
            generatedEnemies.Add(possibleEnemies[autoChessEnemyRNG.SeedRange(0, possibleEnemies.Count)]);
        }
        return generatedEnemies;
    }
    public void GenerateNextRoundEnemies()
    {
        nextRoundEnemies.Clear();
        int normalCount = autoChessEnemyRNG.SeedRange(round, round * round);
        int eliteCount = 0;
        // First few round have no elites.
        if (round > 4)
        {
            eliteCount = autoChessEnemyRNG.SeedRange(round, round * 2);
        }
        // Determine The Group Being Fought.
        string group = availableEnemyGroups[autoChessEnemyRNG.SeedRange(0, availableEnemyGroups.Count)];
        // Generate Enemies From That Group.
        List<string> enemiesOfGroup = GetEnemiesOfGroup(group);
        List<string> enemyDiff = GetDifficultyOfEnemies(enemiesOfGroup);
        nextRoundEnemies.AddRange(GenerateEnemiesOfDifficulty(enemiesOfGroup, enemyDiff, normalCount));
        nextRoundEnemies.AddRange(GenerateEnemiesOfDifficulty(enemiesOfGroup, enemyDiff, eliteCount, false));
    }
    public override void NewGame()
    {
        round = 1;
        nextRoundEnemies.Clear();
        availableEnemyGroups = enemyGroups.GetAllValues().Distinct().ToList();
        // TODO Determine What Enemy Groups Are Available By Removing Some.
        GenerateNextRoundEnemies();
        Save();
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "Round=" + round + delimiter;
        allData += "EnemyGroups=" + String.Join(delimiter2, availableEnemyGroups) + delimiter;
        allData += "NextRound=" + String.Join(delimiter2, nextRoundEnemies) + delimiter;
        File.WriteAllText(dataPath, allData);
        autoChessEnemyRNG.Save();
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
        autoChessEnemyRNG.Load();
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
            case "Round":
            round = int.Parse(value);
            return;
            case "EnemyGroups":
            availableEnemyGroups = value.Split(delimiter2).ToList();
            return;
            case "NextRound":
            nextRoundEnemies = value.Split(delimiter2).ToList();
            return;
        }
    }
}