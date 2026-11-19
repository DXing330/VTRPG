using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Tracks Everything The Player Has/Can Use.
[CreateAssetMenu(fileName = "GHPlayerState", menuName = "ScriptableObjects/Gloomhaven/GHPlayerState", order = 1)]
public class GHPlayerState : SavedData
{
    public string delimiter2;
    public int maxHealth;
    public void SetMaxHealth(int newInfo){maxHealth = newInfo;}
    public int health;
    public void SetHealth(int newInfo) { health = newInfo; }
    public int gold;
    public void SetGold(int newInfo) { gold = newInfo; }
    public int maxPotions;
    public void SetMaxPotions(int newInfo) { maxPotions = newInfo; }
    public List<string> potions = new();
    public void SetPotions(List<string> newInfo) { potions = newInfo; }
    public List<string> relics = new();
    public void SetRelics(List<string> newInfo) { relics = newInfo; }
    public override void Save()
    {
        dataPath = GetSavePath();
        allData = "";
        allData += "MaxHealth=" + maxHealth + delimiter;
        allData += "Health=" + health + delimiter;
        allData += "Gold=" + gold + delimiter;
        allData += "MaxPotions=" + maxPotions + delimiter;
        allData += "Potions=" + string.Join(delimiter2, potions ?? new List<string>()) + delimiter;
        allData += "Relics=" + string.Join(delimiter2, relics ?? new List<string>()) + delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = GetSavePath();
        if (!File.Exists(dataPath)) { return; }
        allData = File.ReadAllText(dataPath);
        string[] savedData = allData.Split(new string[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string data in savedData)
        {
            string[] keyValue = data.Split(new char[] { '=' }, 2);
            if (keyValue.Length < 2) { continue; }
            string key = keyValue[0];
            string value = keyValue[1];
            switch (key)
            {
                case "MaxHealth": int.TryParse(value, out maxHealth);
                break;
                case "Health": int.TryParse(value, out health);
                break;
                case "Gold": int.TryParse(value, out gold);
                break;
                case "MaxPotions": int.TryParse(value, out maxPotions);
                break;
                case "Potions": potions = string.IsNullOrEmpty(value) ? new List<string>() : new List<string>(value.Split(new string[] { delimiter2 }, StringSplitOptions.RemoveEmptyEntries));
                break;
                case "Relics": relics = string.IsNullOrEmpty(value) ? new List<string>() : new List<string>(value.Split(new string[] { delimiter2 }, StringSplitOptions.RemoveEmptyEntries));
                break;
            }
        }
    }
}