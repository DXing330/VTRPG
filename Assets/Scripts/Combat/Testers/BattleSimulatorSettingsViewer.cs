using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleSimulatorSettingsViewer : MonoBehaviour
{
    public BattleSimulatorState simulatorState;
    public GeneralUtility utility;
    public List<GameObject> pageObjects;
    public List<GameObject> pageObjects1;
    public void SetPage(int page)
    {
        utility.DisableGameObjects(pageObjects);
        utility.DisableGameObjects(pageObjects1);
        if (page == 0)
        {
            utility.EnableGameObjects(pageObjects);
        }
        else
        {
            utility.EnableGameObjects(pageObjects1);
        }
    }
    public void UpdateViewer()
    {
        UpdateBattleSettings();
        UpdateSelectedTerrain();
        UpdateSelectedWeather();
        UpdateSelectedTime();
        UpdateSelectedP1BattleMods();
        UpdateSelectedP2BattleMods();
        UpdateSelectedFormations();
    }
    public TMP_Text multiBattleEnabledText;
    public TMP_Text multiBattleCountText;
    public TMP_Text autoBattleEnabledText;
    public TMP_Text controlAIEnabledText;
    public void UpdateBattleSettings()
    {
        if (simulatorState.multiBattle == 0)
        {
            multiBattleEnabledText.text = "False";
        }
        else
        {
            multiBattleEnabledText.text = "True";
        }
        multiBattleCountText.text = simulatorState.multiBattleCount.ToString();
        if (simulatorState.autoBattle == 0)
        {
            autoBattleEnabledText.text = "False";
        }
        else
        {
            autoBattleEnabledText.text = "True";
        }
        if (simulatorState.controlAI == 0)
        {
            controlAIEnabledText.text = "False";
        }
        else
        {
            controlAIEnabledText.text = "True";
        }
    }
    public SelectStatTextList terrainSelect;
    public void SelectAllTerrain()
    {
        simulatorState.SelectAllTerrain();
        UpdateViewer();
    }
    public void SelectNoTerrain()
    {
        simulatorState.ResetSelectedTerrain();
        UpdateViewer();
    }
    public void SelectTerrain()
    {
        simulatorState.SelectTerrainType(terrainSelect.GetSelected());
        UpdateViewer();
    }
    public void UpdateSelectedTerrain()
    {
        List<string> all = new List<string>(simulatorState.allTerrainTypes);
        List<string> active = new List<string>();
        for (int i = 0; i < all.Count; i++)
        {
            if (simulatorState.selectedTerrainTypes.Contains(all[i]))
            {
                active.Add("Allowed");
            }
            else
            {
                active.Add("Not Allowed");
            }
        }
        int page = terrainSelect.GetPage();
        terrainSelect.SetStatsAndData(all, active);
        terrainSelect.SetPage(page);
    }
    public SelectStatTextList weatherSelect;
    public void SelectAllWeather()
    {
        simulatorState.SelectAllWeather();
        UpdateViewer();
    }
    public void SelectNoWeather()
    {
        simulatorState.ResetSelectedWeather();
        UpdateViewer();
    }
    public void SelectWeather()
    {
        simulatorState.SelectWeather(weatherSelect.GetSelected());
        UpdateViewer();
    }
    public void UpdateSelectedWeather()
    {
        List<string> all = new List<string>(simulatorState.allWeathers);
        List<string> active = new List<string>();
        for (int i = 0; i < all.Count; i++)
        {
            if (simulatorState.selectedWeathers.Contains(all[i]))
            {
                active.Add("Allowed");
            }
            else
            {
                active.Add("Not Allowed");
            }
        }
        int page = weatherSelect.GetPage();
        weatherSelect.SetStatsAndData(all, active);
        weatherSelect.SetPage(page);
    }
    public SelectStatTextList timeSelect;
    public void SelectTime()
    {
        simulatorState.SelectTime(timeSelect.GetSelected());
        UpdateViewer();
    }
    public void UpdateSelectedTime()
    {
        List<string> all = new List<string>(simulatorState.allTimes);
        List<string> active = new List<string>();
        for (int i = 0; i < all.Count; i++)
        {
            if (simulatorState.selectedTimes.Contains(all[i]))
            {
                active.Add("Allowed");
            }
            else
            {
                active.Add("Not Allowed");
            }
        }
        int page = timeSelect.GetPage();
        timeSelect.SetStatsAndData(all, active);
        timeSelect.SetPage(page);
    }
    public SelectStatTextList p1BattleModSelect;
    public void SelectP1BattleMod()
    {
        simulatorState.SelectP1BattleMod(p1BattleModSelect.GetSelectedStat());
        UpdateViewer();
    }
    public void UpdateSelectedP1BattleMods()
    {
        List<string> all = new List<string>(simulatorState.allBattleModifiers);
        List<string> active = new List<string>();
        for (int i = 0; i < all.Count; i++)
        {
            if (simulatorState.selectedP1BattleMods.Contains(all[i]))
            {
                active.Add("Enabled");
            }
            else
            {
                active.Add("Not Enabled");
            }
        }
        int page = p1BattleModSelect.GetPage();
        p1BattleModSelect.SetStatsAndData(all, active);
        p1BattleModSelect.SetPage(page);
    }
    public SelectStatTextList p2BattleModSelect;
    public void SelectP2BattleMod()
    {
        simulatorState.SelectP2BattleMod(p2BattleModSelect.GetSelectedStat());
        UpdateViewer();
    }
    public void UpdateSelectedP2BattleMods()
    {
        List<string> all = new List<string>(simulatorState.allBattleModifiers);
        List<string> active = new List<string>();
        for (int i = 0; i < all.Count; i++)
        {
            if (simulatorState.selectedP2BattleMods.Contains(all[i]))
            {
                active.Add("Enabled");
            }
            else
            {
                active.Add("Not Enabled");
            }
        }
        int page = p2BattleModSelect.GetPage();
        p2BattleModSelect.SetStatsAndData(all, active);
        p2BattleModSelect.SetPage(page);
    }
    public SelectStatTextList formationSelect;
    public void SelectFormation()
    {
        simulatorState.SelectFormation(formationSelect.GetSelectedStat());
        UpdateViewer();
    }
    public void UpdateSelectedFormations()
    {
        List<string> all = new List<string>(simulatorState.allStartingFormations);
        List<string> active = new List<string>();
        for (int i = 0; i < all.Count; i++)
        {
            if (simulatorState.selectedStartingFormations.Contains(all[i]))
            {
                active.Add("Enabled");
            }
            else
            {
                active.Add("Not Enabled");
            }
        }
        int page = formationSelect.GetPage();
        formationSelect.SetStatsAndData(all, active);
        formationSelect.SetPage(page);
    }
}
