using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GHBattleState", menuName = "ScriptableObjects/Gloomhaven/GHBattleState", order = 1)]
public class GHBattleState : SavedData
{
    public string delimiter2;
    // Event, Elite, Normal, Boss
    public string battleType;
    public void SetBattleType(string newInfo)
    {
        battleType = newInfo;
    }
    public List<string> enemies = new();
    public void SetEnemies(List<string> newInfo)
    {
        enemies = newInfo;
    }
    public List<string> rewards = new();
    public void SetRewards(List<string> newInfo)
    {
        rewards = newInfo;
    }
    public List<string> rewardSpecifics = new();
    public void SetRewardSpecifics(List<string> newInfo)
    {
        rewardSpecifics = newInfo;
    }
    public override void Save()
    {
        dataPath = GetSavePath();
        allData = "";
        allData += "BattleType=" + battleType + delimiter;
        allData += "Enemies=" + string.Join(delimiter2, enemies) + delimiter;
        allData += "Rewards=" + string.Join(delimiter2, rewards) + delimiter;
        allData += "RewardSpecifics=" + string.Join(delimiter2, rewardSpecifics) + delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = GetSavePath();
        if (!File.Exists(dataPath)){return;}
        allData = File.ReadAllText(dataPath);
        string[] savedData = allData.Split(new string[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string data in savedData)
        {
            string[] keyValue = data.Split(new char[] { '=' }, 2);
            if (keyValue.Length < 2){continue;}
            string key = keyValue[0];
            string value = keyValue[1];
            switch (key)
            {
                case "BattleType":
                    battleType = value;
                    break;
                case "Enemies":
                    enemies = string.IsNullOrEmpty(value) ? new List<string>() : new List<string>(value.Split(new string[] { delimiter2 }, StringSplitOptions.RemoveEmptyEntries));
                    break;
                case "Rewards":
                    rewards = string.IsNullOrEmpty(value) ? new List<string>() : new List<string>(value.Split(new string[] { delimiter2 }, StringSplitOptions.RemoveEmptyEntries));
                    break;
                case "RewardSpecifics":
                    rewardSpecifics = string.IsNullOrEmpty(value) ? new List<string>() : new List<string>(value.Split(new string[] { delimiter2 }, StringSplitOptions.RemoveEmptyEntries));
                    break;
            }
        }
    }
}
