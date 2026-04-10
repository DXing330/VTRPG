using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StSSettings", menuName = "ScriptableObjects/StS/StSSettings", order = 1)]
public class StSSettings : SavedData
{
    public int difficultySetting;
    public int maxDifficulty;
    public void SetDifficulty(int newInfo)
    {
        difficultySetting = newInfo;
        Save();
    }
    public void IncreaseDifficulty()
    {
        difficultySetting++;
        if (difficultySetting > maxDifficulty)
        {
            difficultySetting = 0;
        }
        Save();
    }
    public void DecreaseDifficulty()
    {
        difficultySetting--;
        if (difficultySetting < 0)
        {
            difficultySetting = maxDifficulty;
        }
        Save();
    }
    public int GetDifficulty()
    {
        Load();
        return difficultySetting;
    }
    public StatDatabase enemyModifiersPerDifficulty;
    public string ReturnEnemyModifiers()
    {
        // Handle some random buffs here.
        return enemyModifiersPerDifficulty.ReturnValueAtIndex(difficultySetting);
    }
    public StatDatabase eliteModifiersPerDifficulty;
    public string ReturnEliteModifiers()
    {
        // Handle some random buffs here.
        return eliteModifiersPerDifficulty.ReturnValueAtIndex(difficultySetting);
    }
    public StatDatabase bossModifiersPerDifficulty;
    public string ReturnBossModifiers()
    {
        // Handle some random buffs here.
        return bossModifiersPerDifficulty.ReturnValueAtIndex(difficultySetting);
    }
    
    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = difficultySetting.ToString();
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
            difficultySetting = 0;
            Save();
            return;
        }
        string[] blocks = allData.Split(delimiter);
        SetDifficulty(int.Parse(blocks[0]));
    }
}