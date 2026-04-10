using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleState", menuName = "ScriptableObjects/DataContainers/SavedData/BattleState", order = 1)]
public class BattleState : SavedState
{
    public bool subGame = false;
    public void UpdateBattleModifiers()
    {
        allyBattleModifiers = partyList.GetBattleModifiers();
        enemyBattleModifiers = enemyList.GetBattleModifiers();
    }
    public List<string> allyBattleModifiers;
    public List<string> GetAllyBattleModifiers()
    {
        return allyBattleModifiers;
    }
    public List<string> enemyBattleModifiers;
    public List<string> GetEnemyBattleModifiers()
    {
        return enemyBattleModifiers;
    }
    public int winningTeam = -1;
    public void SetWinningTeam(int newInfo)
    {
        winningTeam = newInfo;
        Save();
    }
    public void ResetWinningTeam()
    {
        winningTeam = -1;
        Save();
    }
    public int GetWinningTeam()
    {
        return winningTeam;
    }
    public CharacterList partyList;
    public CharacterList enemyList;
    public BattleMapEditorSaver savedBattles;
    public BattleMapFeatures battleMapFeatures;
    public string customBattleName;
    public void SetCustomBattleName(string newInfo)
    {
        customBattleName = newInfo;
    }
    public void ClearCustomBattleName()
    {
        customBattleName = "";
    }
    public string GetCustomBattleName()
    {
        return customBattleName;
    }
    public bool UsingCustomBattle()
    {
        return customBattleName.Length > 0;
    }
    public List<string> enemies;
    public void AddEnemyName(string newName){enemies.Add(newName);}
    public void SetEnemyNames(List<string> newEnemies){enemies = new List<string>(newEnemies);}
    public void UpdateEnemyNames(){enemies = new List<string>(enemyList.characters);}
    public string terrainType;
    public List<string> terrainTypes;
    public virtual void ForceTerrainType(string newInfo)
    {
        if (newInfo == ""){return;}
        terrainType = newInfo;
        battleMapFeatures.SetTerrainType(terrainType);
    }
    protected string practiceTerrainType = "Plains";
    public void SetPracticeTerrainType()
    {
        terrainType = practiceTerrainType;
        battleMapFeatures.SetTerrainType(terrainType);
    }
    public virtual void SetTerrainType()
    {
        terrainType = practiceTerrainType;
        battleMapFeatures.SetTerrainType(terrainType);
    }
    public virtual string GetTerrainType(){return terrainType;}
    public void UpdateTerrainType(){battleMapFeatures.SetTerrainType(terrainType);}
    public string time;
    public List<string> timeOfDayTypes;
    public void SetTime(string newInfo)
    {
        time = newInfo;
    }
    public virtual string GetTime()
    {
        if (time == "" || !timeOfDayTypes.Contains(time))
        {
            return timeOfDayTypes[UnityEngine.Random.Range(0, timeOfDayTypes.Count)];
        }
        return time;
    }
    public StatDatabase allWeather;
    public string weather;
    public List<string> weatherTypes;
    public void ResetWeather(){SetWeather();}
    public void SetWeather(string newInfo = "")
    {
        weather = newInfo;
    }
    public virtual string GetWeather()
    {
        if (weather.Length > 0)
        {
            return weather;
        }
        return weatherTypes[UnityEngine.Random.Range(0, weatherTypes.Count)];
    }
    public List<string> allStartingFormations;
    public string spawnPattern;
    public void SetSurroundedFormation()
    {
        SetStartingFormation("Surrounded");
    }
    public void SetSurroundingFormation()
    {
        SetStartingFormation("Surrounding");
    }
    public void SetStartingFormation(string newInfo)
    {
        spawnPattern = newInfo;
        int indexOf = allStartingFormations.IndexOf(spawnPattern);
        Debug.Log(indexOf);
        if (indexOf < 0)
        {
            ResetSpawnPatterns();
        }
        else
        {
            SetAllySpawnPattern(p1StartingFormations[indexOf]);
            SetEnemySpawnPattern(p2StartingFormations[indexOf]);
        }
        Save();
    }
    public List<string> p1StartingFormations;
    public List<string> p2StartingFormations;
    public string allySpawnPattern;
    public void SetAllySpawnPattern(string newInfo = "Left"){allySpawnPattern = newInfo;}
    public virtual string GetAllySpawnPattern()
    {
        if (allySpawnPattern.Length <= 1){return p1StartingFormations[0];}
        return allySpawnPattern;
    }
    public string enemySpawnPattern;
    public void SetEnemySpawnPattern(string newInfo = "Right"){enemySpawnPattern = newInfo;}
    public virtual string GetEnemySpawnPattern()
    {
        if (enemySpawnPattern.Length <= 1){return p2StartingFormations[0];}
        return enemySpawnPattern;
    }
    public void ResetSpawnPatterns()
    {
        SetAllySpawnPattern();
        SetEnemySpawnPattern();
    }
    public string alternateWinCondition;
    public void SetAltWinCon(string newInfo = "")
    {
        alternateWinCondition = newInfo;
    }
    public string alternateWinConditionSpecifics;
    public void SetAltWinConSpecifics(string newInfo = "")
    {
        alternateWinConditionSpecifics = newInfo;
    }
    public void SetNewAlternateWinCondition(string condition = "", string specifics = "")
    {
        alternateWinCondition = condition;
        alternateWinConditionSpecifics = specifics;
    }
    public string GetAltWinCon(){return alternateWinCondition;}
    public string GetAltWinConSpecifics(){return alternateWinConditionSpecifics;}
    public string AltWinConString()
    {
        string winCon = "Goal:\n";
        switch (alternateWinCondition)
        {
            default:
            winCon += "Defeat all enemies";
            break;
            case "Escape":
            winCon += "Allow " + alternateWinConditionSpecifics + " to escape";
            break;
            case "Defeat":
            winCon += "Defeat " + alternateWinConditionSpecifics;
            break;
            case "Capture":
            winCon += "Capture " + alternateWinConditionSpecifics;
            break;
        }
        return winCon;
    }

    public void ResetStats()
    {
        SetNewAlternateWinCondition();
    }

    public void SetBattleDetailsFromDungeon(DungeonState dState)
    {
        SetWeather(dState.dungeon.GetWeather());
        ForceTerrainType(dState.dungeon.GenerateTerrain());
        string newInfo = dState.dungeon.GetQuestBattleInfo();
        string[] blocks = newInfo.Split(dState.dungeon.bossQuestBattleDelimiter);
        if (blocks.Length <= 6){return;}
        ForceTerrainType(blocks[1]);
        SetWeather(blocks[2]);
        SetTime(blocks[3]);
        SetStartingFormation(blocks[4]);
        SetAltWinCon(blocks[5]);
        SetAltWinConSpecifics(blocks[6]);
    }

    public override void NewGame()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = newGameData;
        File.WriteAllText(dataPath, allData);
        Load();
        Save();
    }

    public override void Save()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = "PrevScene=" + previousScene+delimiter;
        allData += "Enemies=" + String.Join(delimiterTwo, enemies);
        allData += delimiter;
        allData += "Terrain=" + terrainType;
        allData += delimiter;
        allData += "WinningTeam=" + winningTeam;
        allData += delimiter;
        UpdateBattleModifiers();
        allData += "AllyBM=" + String.Join(delimiterTwo, allyBattleModifiers);
        allData += delimiter;
        allData += "EnemyBM=" + String.Join(delimiterTwo, enemyBattleModifiers);
        allData += delimiter;
        allData += "Weather=" + weather;
        allData += delimiter;
        allData += "Time=" + time;
        allData += delimiter;
        allData += "AllySP=" + allySpawnPattern;
        allData += delimiter;
        allData += "EnemySP=" + enemySpawnPattern;
        allData += delimiter;
        allData += "AltWinCon=" + alternateWinCondition;
        allData += delimiter;
        allData += "AltWinConSpecs=" + alternateWinConditionSpecifics;
        allData += delimiter;
        allData += "CustomBattle=" + customBattleName;
        allData += delimiter;
        File.WriteAllText(dataPath, allData);
    }

    public override void Load()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = File.ReadAllText(dataPath);
        dataList = allData.Split(delimiter).ToList();
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
        sceneTracker.SetPreviousScene(previousScene);
        enemyList.ResetLists();
        enemyList.AddCharacters(enemies);
        battleMapFeatures.SetTerrainType(terrainType);
    }

    protected virtual void LoadStat(string data)
    {
        string[] blocks = data.Split("=");
        if (blocks.Length < 2){return;}
        string value = blocks[1];
        switch (blocks[0])
        {
            default:
                break;
            case "PrevScene":
                previousScene = value;
                break;
            case "Enemies":
                enemies = value.Split(delimiterTwo).ToList();
                break;
            case "Terrain":
                terrainType = value;
                break;
            case "WinningTeam":
                winningTeam = utility.SafeParseInt(value, -1);
                break;
            case "AllyBM":
                allyBattleModifiers = utility.RemoveEmptyListItems(value.Split(delimiterTwo).ToList());
                utility.RemoveEmptyListItems(allyBattleModifiers);
                partyList.SetBattleModifiers(allyBattleModifiers);
                break;
            case "EnemyBM":
                enemyBattleModifiers = utility.RemoveEmptyListItems(value.Split(delimiterTwo).ToList());
                enemyList.SetBattleModifiers(enemyBattleModifiers);
                break;
            case "Weather":
                SetWeather(value);
                break;
            case "Time":
                SetTime(value);
                break;
            case "AllySP":
                SetAllySpawnPattern(value);
                break;
            case "EnemySP":
                SetEnemySpawnPattern(value);
                break;
            case "AltWinCon":
                SetAltWinCon(value);
                break;
            case "AltWinConSpecs":
                SetAltWinConSpecifics(value);
                break;
            case "CustomBattle":
                SetCustomBattleName(value);
                break;
        }
    }
}
