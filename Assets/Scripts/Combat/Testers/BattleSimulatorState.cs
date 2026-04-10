using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleSimulatorState", menuName = "ScriptableObjects/Debug/BattleSimulatorState", order = 1)]
public class BattleSimulatorState : BattleState
{
    public string delimiterThree;
    public CharacterList partyOneList;
    public CharacterList partyTwoList;
    // Select which terrains that battle may take place one, then randomly select one of the selected.
    public List<string> allTerrainTypes;
    public List<string> selectedTerrainTypes;
    public void ResetSelectedTerrain()
    {
        selectedTerrainTypes.Clear();
    }
    public void SelectAllTerrain()
    {
        selectedTerrainTypes = new List<string>(allTerrainTypes);
    }
    public void SelectTerrainType(int index)
    {
        string newInfo = allTerrainTypes[index];
        if (selectedTerrainTypes.Contains(newInfo))
        {
            selectedTerrainTypes.Remove(newInfo);
        }
        else
        {
            selectedTerrainTypes.Add(newInfo);
        }
    }
    public string selectedTerrain;
    public override void SetTerrainType()
    {
        battleMapFeatures.SetTerrainType(GetTerrainType());
    }
    public override string GetTerrainType()
    {
        if (selectedTerrainTypes.Count > 0)
        {
            selectedTerrain = selectedTerrainTypes[UnityEngine.Random.Range(0, selectedTerrainTypes.Count)];
        }
        else
        {
            selectedTerrain = "Random";
        }
        if (selectedTerrain == "Random")
        {
            return terrainTypes[UnityEngine.Random.Range(0, terrainTypes.Count)];
        }
        return selectedTerrain;
    }
    public List<string> allWeathers;
    public List<string> selectedWeathers;
    public void ResetSelectedWeather()
    {
        selectedWeathers.Clear();
    }
    public void SelectAllWeather()
    {
        selectedWeathers = new List<string>(allWeathers);
    }
    public void SelectWeather(int index)
    {
        string newInfo = allWeathers[index];
        if (selectedWeathers.Contains(newInfo))
        {
            selectedWeathers.Remove(newInfo);
        }
        else
        {
            selectedWeathers.Add(newInfo);
        }
    }
    public string selectedWeather;
    public override string GetWeather()
    {
        if (selectedWeathers.Count > 0)
        {
            selectedWeather = selectedWeathers[UnityEngine.Random.Range(0, selectedWeathers.Count)];
        }
        else
        {
            selectedWeather = "Random";
        }
        if (selectedWeather == "Random")
        {
            return weatherTypes[UnityEngine.Random.Range(0, weatherTypes.Count)];
        }
        return selectedWeather;
    }
    public List<string> allTimes;
    public List<string> selectedTimes;
    public void SelectTime(int index)
    {
        string newInfo = allTimes[index];
        if (selectedTimes.Contains(newInfo))
        {
            selectedTimes.Remove(newInfo);
        }
        else
        {
            selectedTimes.Add(newInfo);
        }
    }
    public string selectedTime;
    public override string GetTime()
    {
        if (selectedTimes.Count > 0)
        {
            selectedTime = selectedTimes[UnityEngine.Random.Range(0, selectedTimes.Count)];
        }
        else
        {
            selectedTime = "Random";
        }
        if (selectedTime == "Random")
        {
            return allTimes[UnityEngine.Random.Range(0, allTimes.Count)];
        }
        return selectedTime;
    }
    public List<string> selectedStartingFormations;
    public string selectedStartingFormation;
    public void GetStartingFormation()
    {
        if (selectedStartingFormations.Count > 0)
        {
            selectedStartingFormation = selectedStartingFormations[UnityEngine.Random.Range(0, selectedStartingFormations.Count)];
        }
        else
        {
            selectedStartingFormation = "Random";
        }
        if (selectedStartingFormation == "Random")
        {
            selectedStartingFormation = allStartingFormations[UnityEngine.Random.Range(0, allStartingFormations.Count)];
        }
    }
    public void SelectFormation(string newInfo)
    {
        int indexOf = selectedStartingFormations.IndexOf(newInfo);
        if (indexOf >= 0)
        {
            selectedStartingFormations.RemoveAt(indexOf);
            return;
        }
        selectedStartingFormations.Add(newInfo);
    }
    public override string GetAllySpawnPattern()
    {
        GetStartingFormation();
        int indexOf = allStartingFormations.IndexOf(selectedStartingFormation);
        if (indexOf < 0){indexOf = 0;}
        return p1StartingFormations[indexOf];
    }
    public override string GetEnemySpawnPattern()
    {
        int indexOf = allStartingFormations.IndexOf(selectedStartingFormation);
        if (indexOf < 0){indexOf = 0;}
        return p2StartingFormations[indexOf];
    }
    public StatDatabase battleModifierData;
    public List<string> allBattleModifiers;
    public List<string> selectedP1BattleMods;
    public void SelectP1BattleMod(string newInfo)
    {
        int indexOf = selectedP1BattleMods.IndexOf(newInfo);
        if (indexOf >= 0)
        {
            selectedP1BattleMods.RemoveAt(indexOf);
            return;
        }
        selectedP1BattleMods.Add(newInfo);
    }
    public List<string> selectedP2BattleMods;
    public void SelectP2BattleMod(string newInfo)
    {
        int indexOf = selectedP2BattleMods.IndexOf(newInfo);
        if (indexOf >= 0)
        {
            selectedP2BattleMods.RemoveAt(indexOf);
            return;
        }
        selectedP2BattleMods.Add(newInfo);
    }
    public void ApplyBattleModifiers()
    {
        partyOneList.SetBattleModifiers(selectedP1BattleMods);
        partyTwoList.SetBattleModifiers(selectedP2BattleMods);
    }
    public int multiBattle = 0;
    public int prevMultiBattle = 0;
    public bool MultiBattlePreviouslyEnabled()
    {
        return prevMultiBattle == 1;
    }
    public int multiBattleCount = 2;
    public void ChangeMultiBattleCount(bool right = true)
    {
        multiBattleCurrent = 0;
        multiBattleCount = utility.ChangeIndex(multiBattleCount, right, maxMultiBattle, minMultiBattle);
    }
    public int minMultiBattle = 2;
    public int maxMultiBattle = 30;
    public int multiBattleCurrent = 0;
    public void ResetBattleIteration()
    {
        multiBattle = 0;
        multiBattleCurrent = 0;
        Save();
    }
    public int GetCurrentMultiBattleIteration()
    {
        return multiBattleCurrent;
    }
    public void IncrementMultiBattle()
    {
        multiBattleCurrent++;
        Save();
    }
    public void EnableMultiBattle()
    {
        multiBattleCurrent = 0;
        multiBattle = (multiBattle + 1) % 2;
        prevMultiBattle = multiBattle;
    }
    public bool MultiBattleEnabled()
    {
        return multiBattle == 1;
    }
    public bool MultiBattleFinished()
    {
        return (multiBattleCurrent >= multiBattleCount);
    }
    public int autoBattle = 1;
    public void EnableAutoBattle()
    {
        autoBattle = (autoBattle + 1) % 2;
    }
    public bool AutoBattleEnabled()
    {
        return autoBattle == 1;
    }
    public int controlAI = 0;
    public void EnableControlAI()
    {
        controlAI = (controlAI + 1) % 2;
    }
    public bool ControlAIEnabled()
    {
        return controlAI == 1;
    }
    public override void NewGame()
    {
        partyOneList.ResetLists();
        partyTwoList.ResetLists();
        selectedTerrainTypes.Clear();
        selectedWeathers.Clear();
        selectedTimes.Clear();
        winningTeam = -1;
        multiBattle = 0;
        prevMultiBattle = 1;
        multiBattleCount = 2;
        multiBattleCurrent = 0;
        autoBattle = 1;
        controlAI = 0;
        Save();
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "P1=" + partyOneList.ReturnData() + delimiter;
        allData += "P2=" + partyTwoList.ReturnData() + delimiter;
        allData += "Terrain=" + String.Join(delimiterThree, selectedTerrainTypes) + delimiter;
        allData += "Weather=" + String.Join(delimiterThree, selectedWeathers) + delimiter;
        allData += "Time=" + String.Join(delimiterThree, selectedTimes) + delimiter;
        allData += "MultiBattle=" + multiBattle + delimiter;
        allData += "MultiBattleCount=" + multiBattleCount + delimiter;
        allData += "MultiBattleCurrent=" + multiBattleCurrent + delimiter;
        allData += "MultiBattlePrev=" + prevMultiBattle + delimiter;
        allData += "Auto=" + autoBattle + delimiter;
        allData += "ControlAI=" + controlAI + delimiter;
        allData += "P1BattleMods=" + String.Join(delimiterThree, selectedP1BattleMods) + delimiter;
        allData += "P2BattleMods=" + String.Join(delimiterThree, selectedP2BattleMods) + delimiter;
        allData += "StartingFormations=" + String.Join(delimiterThree, selectedStartingFormations) + delimiter;
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
            return;
        }
        // ---------- defaults ----------
        selectedTerrainTypes = new List<string>();
        selectedWeathers = new List<string>();
        selectedTimes = new List<string>();
        selectedP1BattleMods = new List<string>();
        selectedP2BattleMods = new List<string>();
        selectedStartingFormations = new List<string>();
        multiBattle = 0;
        autoBattle = 0;
        controlAI = 0;
        multiBattleCount = 0;
        multiBattleCurrent = 0;
        prevMultiBattle = 0;
        winningTeam = -1;
        // ------------------------------
        dataList = allData.Split(delimiter).ToList();
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
        utility.RemoveEmptyListItems(selectedTerrainTypes);
        utility.RemoveEmptyListItems(selectedWeathers);
        utility.RemoveEmptyListItems(selectedTimes);
        utility.RemoveEmptyListItems(selectedP1BattleMods);
        utility.RemoveEmptyListItems(selectedP2BattleMods);
        utility.RemoveEmptyListItems(selectedStartingFormations);
    }
    protected override void LoadStat(string data)
    {
        string[] blocks = data.Split("=");
        if (blocks.Length < 2){return;}
        string value = blocks[1];
        switch (blocks[0])
        {
            default:
            break;
            case "P1":
                partyOneList.LoadData(value);
                break;
            case "P2":
                partyTwoList.LoadData(value);
                break;
            case "Terrain":
                selectedTerrainTypes = value.Split(delimiterThree).ToList();
                break;
            case "Weather":
                selectedWeathers = value.Split(delimiterThree).ToList();
                break;
            case "Time":
                selectedTimes = value.Split(delimiterThree).ToList();
                break;
            case "MultiBattle":
                multiBattle = int.Parse(value);
                break;
            case "MultiBattleCount":
                multiBattleCount = int.Parse(value);
                break;
            case "MultiBattleCurrent":
                multiBattleCurrent = int.Parse(value);
                break;
            case "MultiBattlePrev":
                prevMultiBattle = int.Parse(value);
                break;
            case "Auto":
                autoBattle = int.Parse(value);
                break;
            case "ControlAI":
                controlAI = int.Parse(value);
                break;
            case "P1BattleMods":
                selectedP1BattleMods = value.Split(delimiterThree).ToList();
                break;
            case "P2BattleMods":
                selectedP2BattleMods = value.Split(delimiterThree).ToList();
                break;
            case "StartingFormations":
                selectedStartingFormations = value.Split(delimiterThree).ToList();
                break;
        }
    }
}
