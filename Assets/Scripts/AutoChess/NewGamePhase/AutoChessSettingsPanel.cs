using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AutoChessSettingsPanel : MonoBehaviour
{
    public string autoChessSceneName;
    public SceneMover sceneMover;
    public void StartGame()
    {
        settingsData.Save();
        dataManager.NewGame();
        if (pvpDataManager != null)
        {
            pvpDataManager.NewGameAllDataManagers();
            // TODO Assign Genomes To Each Data Manager From The Champion Database.
        }
        sceneMover.LoadScene(autoChessSceneName);
    }
    public AutoChessDataManager dataManager;
    public AutoChessPVPDataManager pvpDataManager;
    public AutoChessSettingsDataManager settingsData;
    // Map Display + Select.
    public AutoChessMapDisplay mapDisplay;
    void Start()
    {
        settingsData.Load();
        mapDisplay.DisplayMap(settingsData.GetSelectedMap());
        UpdateDifficultyText();
        UpdateTacticianText();
    }
    public void ChangeMap(bool right = true)
    {
        settingsData.ChangeMap(right);
        mapDisplay.DisplayMap(settingsData.GetSelectedMap());
    }
    public TMP_Text difficultyText;
    public void UpdateDifficultyText()
    {
        difficultyText.text = settingsData.difficultyScaling.ToString();
    }
    public void ChangeDifficulty(bool right = true)
    {
        settingsData.ChangeDifficulty(right);
        UpdateDifficultyText();
    }
    public TMP_Text tacticianName;
    public TMP_Text tacticianEffect;
    public void UpdateTacticianText()
    {
        tacticianName.text = settingsData.GetTactician();
        tacticianEffect.text = settingsData.GetTacticianEffect();
    }
    public void ChangeTactician(bool right)
    {
        settingsData.ChangeTactician(right);
        UpdateTacticianText();
    }
}
