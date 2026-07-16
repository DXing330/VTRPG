using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessSettingsPanel : MonoBehaviour
{
    public string autoChessSceneName;
    public SceneMover sceneMover;
    public void StartGame()
    {
        settingsData.Save();
        dataManager.NewGame();
        sceneMover.LoadScene(autoChessSceneName);
    }
    public AutoChessDataManager dataManager;
    public AutoChessSettingsDataManager settingsData;
    // Map Display + Select.
    public AutoChessMapDisplay mapDisplay;
    void Start()
    {
        mapDisplay.DisplayMap(settingsData.GetSelectedMap());
    }
    public void ChangeMap(bool right = true)
    {
        settingsData.ChangeMap(right);
        mapDisplay.DisplayMap(settingsData.GetSelectedMap());
    }
}
