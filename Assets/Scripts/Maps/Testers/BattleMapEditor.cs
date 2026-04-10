using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class BattleMapEditor : MonoBehaviour
{
    void Start()
    {
        InitializeEnvironmentOptions();
        actorSelect.SetData(actorStats.GetAllKeys(), actorStats.GetAllKeys(), actorStats.GetAllValues());
        LoadBattle("TestBattle0");
    }
    public BattleState battleState;
    public SpinnerMenu weatherSelect;
    public SpinnerMenu timeSelect;
    public string battleWeather;
    public string battleTime;
    protected void InitializeEnvironmentOptions()
    {
        if (battleState == null){return;}
        battleTime = "";
        if (weatherSelect != null && battleState.weatherTypes.Count > 0)
        {
            weatherSelect.SetSelectables(battleState.weatherTypes);
            battleWeather = weatherSelect.GetSelected();
        }
        if (timeSelect != null && battleState.timeOfDayTypes.Count > 0)
        {
            timeSelect.SetSelectables(battleState.timeOfDayTypes);
            battleTime = timeSelect.GetSelected();
        }
    }
    public void ResetBattleEnvironment()
    {
        InitializeEnvironmentOptions();
    }
    protected void UpdateEnvironmentSelectors()
    {
        if (weatherSelect != null && weatherSelect.selectables != null && weatherSelect.selectables.Count > 0)
        {
            int weatherIndex = weatherSelect.selectables.IndexOf(battleWeather);
            weatherSelect.SetSelectedIndex(Mathf.Max(0, weatherIndex));
            battleWeather = weatherSelect.GetSelected();
        }
        if (timeSelect != null && timeSelect.selectables != null && timeSelect.selectables.Count > 0)
        {
            int timeIndex = timeSelect.selectables.IndexOf(battleTime);
            timeSelect.SetSelectedIndex(Mathf.Max(0, timeIndex));
            battleTime = timeSelect.GetSelected();
        }
    }
    public void ChangeWeather(bool right = true)
    {
        if (weatherSelect == null || weatherSelect.selectables.Count <= 0){return;}
        weatherSelect.ChangeIndex(right);
        battleWeather = weatherSelect.GetSelected();
    }
    public void ChangeTime(bool right = true)
    {
        if (timeSelect == null || timeSelect.selectables.Count <= 0){return;}
        timeSelect.ChangeIndex(right);
        battleTime = timeSelect.GetSelected();
    }
    public void SetBattleWeather(string newInfo)
    {
        battleWeather = newInfo;
        UpdateEnvironmentSelectors();
    }
    public string GetBattleWeather(){return battleWeather;}
    public void SetBattleTime(string newInfo)
    {
        battleTime = newInfo;
        UpdateEnvironmentSelectors();
    }
    public string GetBattleTime(){return battleTime;}
    public string battleID;
    public void SetBattleID(string newID)
    {
        battleID = newID;
        battleIDText.text = battleID;
    }
    public TMP_Text battleIDText;
    public BattleMapEditorSaver savedBattles;
    public void SaveBattle()
    {
        savedBattles.SaveBattle(this, battleID);
    }
    public void LoadBattle(string newID)
    {
        savedBattles.LoadBattle(this, newID);
        SetBattleID(newID);
    }
    public void DeleteBattle()
    {
        savedBattles.DeleteKey(battleID);
        LoadBattle("TestBattle0");
    }
    public GameObject nRObject;
    public NameRater nameRater;
    public StSEnemyTracker roguelikeEnemyTracker;
    public GameObject battleNameSelectObject;
    enum NameRaterState
    {
        copying,
        newing
    }
    // Battle Name Rater State.
    NameRaterState bNRS;
    protected List<string> GetAllRoguelikeBattleNames()
    {
        List<string> battleNames = new List<string>();
        if (roguelikeEnemyTracker == null){return battleNames;}
        for (int i = 0; i < roguelikeEnemyTracker.floorEnemies.Count; i++)
        {
            battleNames.AddRange(roguelikeEnemyTracker.floorEnemies[i].GetAllKeys());
        }
        for (int i = 0; i < roguelikeEnemyTracker.floorElites.Count; i++)
        {
            battleNames.AddRange(roguelikeEnemyTracker.floorElites[i].GetAllKeys());
        }
        for (int i = 0; i < roguelikeEnemyTracker.floorBosses.Count; i++)
        {
            battleNames.AddRange(roguelikeEnemyTracker.floorBosses[i].GetAllKeys());
        }
        return battleNames.Distinct().OrderBy(name => name).ToList();
    }
    protected bool RoguelikeBattleNameExists(string battleName)
    {
        if (roguelikeEnemyTracker == null){return true;}
        return GetAllRoguelikeBattleNames().Contains(battleName);
    }
    public void NewBattle()
    {
        bNRS = NameRaterState.newing;
        ShowBattleNameSelector();
    }
    public void CopyBattle()
    {
        bNRS = NameRaterState.copying;
        ShowBattleNameSelector();
    }
    public SelectList battleSelectList;
    protected void ShowBattleNameSelector()
    {
        List<string> roguelikeBattleNames = GetAllRoguelikeBattleNames();
        if (battleNameSelectObject == null || battleSelectList == null || roguelikeBattleNames.Count <= 0)
        {
            return;
        }
        battleSelectList.SetSelectables(roguelikeBattleNames);
        battleNameSelectObject.SetActive(true);
    }
    public void SelectBattleName()
    {
        if (battleSelectList == null || battleSelectList.GetSelected() < 0){return;}
        string newName = battleSelectList.GetSelectedString();
        switch (bNRS)
        {
            case NameRaterState.newing:
                LoadBattle(newName);
                break;
            case NameRaterState.copying:
                SetBattleID(newName);
                SaveBattle();
                break;
        }
        if (battleNameSelectObject != null)
        {
            battleNameSelectObject.SetActive(false);
        }
    }
    public void TryToLoadBattle()
    {
        battleSelectList.SetSelectables(savedBattles.savedKeys);
    }
    public void SelectLoadBattle()
    {
        if (battleSelectList.GetSelected() < 0){return;}
        LoadBattle(battleSelectList.GetSelectedString());
    }
    public MapEditorSaver savedMaps;
    public MapEditor mapEditor;
    public StatDatabase actorStats;
    public ActorSpriteHPList actorSelect;
    public void ResetSelectedEnemy()
    {
        selectedEnemy = "";
    }
    public void SelectEnemy()
    {
        if (actorSelect.GetSelected() < 0)
        {
            ResetSelectedEnemy();
            return;
        }
        selectedEnemy = actorSelect.GetSelectedName();
        //View stats/skills/passives when selecting actors.
    }
    public string selectedEnemy;
    public NameRater filter;
    public void ResetFilter()
    {
        filter.ResetNewName();
        actorSelect.SetData(actorStats.GetAllKeys(), actorStats.GetAllKeys(), actorStats.GetAllValues());
    }
    public void FilterActorSelect()
    {
        actorSelect.ResetSelected();
        List<string> filters = new List<string>();
        filters.Add(filter.ReturnNameWithFirstCharUpperCase());
        filters.Add(filter.ConfirmName().ToLower());
        actorSelect.SetData(actorStats.GetFilteredKeys(filters), actorStats.GetFilteredKeys(filters), actorStats.GetFilteredValues(filters));
    }
    public void InitializeNewMap()
    {
        mapEditor.InitializeNewMap();
        enemies.Clear();
        enemyLocations.Clear();
        InitializeEnvironmentOptions();
    }
    public List<string> enemies;
    public List<string> enemyLocations;
    public void ResetEnemies()
    {
        enemies.Clear();
        enemyLocations.Clear();
        UpdateMap();
    }
    public void AddEnemy()
    {
        if (selectedTile < 0 || selectedEnemy == ""){return;}
        if (enemyLocations.Contains(selectedTile.ToString())){return;}
        enemies.Add(selectedEnemy);
        enemyLocations.Add(selectedTile.ToString());
        UpdateMap();
    }
    public void RemoveEnemy()
    {
        if (selectedTile < 0){return;}
        for (int i = 0; i < enemyLocations.Count; i++)
        {
            if (int.Parse(enemyLocations[i]) == selectedTile)
            {
                enemyLocations.RemoveAt(i);
                enemies.RemoveAt(i);
            }
        }
        UpdateMap();
    }
    public int selectedTile = -1;
    public void ClickOnTile(int tileNumber)
    {
        selectedTile = tileNumber;
        mapEditor.HighlightTile(selectedTile);
    }
    public void UpdateMap()
    {
        mapEditor.UpdateMapWithActors(enemies, enemyLocations);   
    }
    // Enemy Equipment?
    // Enemy Buffs?
}
