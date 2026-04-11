using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CustomBattleTester : MonoBehaviour
{
    public PartyDataManager partyDataManager;
    public BattleState battleState;
    public BattleMapEditorSaver savedBattles;
    public CharacterList enemyList;
    public SceneMover sceneMover;
    public string testBattleName;
    public TMP_Text statusText;

    void Start()
    {
        statusText.text = testBattleName;
    }

    public void SetTestBattleName(string newInfo)
    {
        testBattleName = newInfo;
        UpdateStatus("Selected: " + testBattleName);
    }

    [ContextMenu("Check Custom Battle Exists")]
    public void CheckBattleExists()
    {
        if (testBattleName.Length <= 0)
        {
            UpdateStatus("No test battle selected.");
            return;
        }
        if (!savedBattles.BattleExists(testBattleName))
        {
            UpdateStatus("Saved battle not found: " + testBattleName);
            return;
        }
        else
        {
            UpdateStatus("Saved battle found: " + testBattleName);
            return;
        }
    }

    [ContextMenu("Test Custom Battle")]
    public void TestBattle()
    {
        if (partyDataManager == null || battleState == null || savedBattles == null || enemyList == null || sceneMover == null)
        {
            UpdateStatus("Missing tester references.");
            return;
        }
        if (testBattleName.Length <= 0)
        {
            UpdateStatus("No test battle selected.");
            return;
        }
        if (!savedBattles.BattleExists(testBattleName))
        {
            battleState.ClearCustomBattleName();
            UpdateStatus("Saved battle not found: " + testBattleName);
            return;
        }

        List<string> mapInfo;
        List<string> terrainEffects;
        List<int> elevations;
        List<string> borders;
        List<string> buildings;
        List<string> enemies;
        List<int> enemyLocations;
        string weather;
        string time;

        if (!savedBattles.TryLoadBattleData(
            testBattleName,
            out mapInfo,
            out terrainEffects,
            out elevations,
            out borders,
            out buildings,
            out enemies,
            out enemyLocations,
            out weather,
            out time))
        {
            battleState.ClearCustomBattleName();
            UpdateStatus("Failed to load saved battle: " + testBattleName);
            return;
        }

        partyDataManager.Load();
        battleState.Load();
        battleState.SetCustomBattleName(testBattleName);
        battleState.SetWeather(weather);
        battleState.SetTime(time);
        enemyList.ResetLists();
        enemyList.AddCharacters(enemies);
        battleState.UpdateEnemyNames();
        battleState.Save();

        UpdateStatus("Testing battle: " + testBattleName);
        sceneMover.MoveToBattle();
    }

    protected void UpdateStatus(string newInfo)
    {
        Debug.Log(newInfo);
        if (statusText != null)
        {
            statusText.text = newInfo;
        }
    }
}
