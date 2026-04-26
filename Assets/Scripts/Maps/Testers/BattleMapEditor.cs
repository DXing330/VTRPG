using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class BattleMapEditor : MonoBehaviour
{
    [Header("Debug Loading")]
    public string debugBattleNameToLoad;
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
        if (battleIDText != null)
        {
            battleIDText.text = battleID;
        }
    }
    public TMP_Text battleIDText;
    public BattleMapEditorSaver savedBattles;
    [ContextMenu("Debug Load Battle By Name")]
    public void DebugLoadBattleByName()
    {
        if (savedBattles == null)
        {
            Debug.LogWarning("BattleMapEditor debug load failed: missing savedBattles reference.");
            return;
        }
        if (string.IsNullOrWhiteSpace(debugBattleNameToLoad))
        {
            Debug.LogWarning("BattleMapEditor debug load failed: debugBattleNameToLoad is empty.");
            return;
        }
        if (!savedBattles.KeyExists(debugBattleNameToLoad))
        {
            Debug.LogWarning("BattleMapEditor debug load failed: no saved battle named " + debugBattleNameToLoad);
            return;
        }
        LoadBattle(debugBattleNameToLoad);
        Debug.Log("BattleMapEditor debug loaded battle: " + battleID);
    }
    [ContextMenu("Debug Refresh Saved Battle Keys")]
    public void DebugRefreshSavedBattleKeys()
    {
        if (savedBattles == null)
        {
            Debug.LogWarning("BattleMapEditor debug refresh failed: missing savedBattles reference.");
            return;
        }
        if (battleSelectList != null)
        {
            battleSelectList.SetSelectables(savedBattles.GetAllKeys());
        }
        Debug.Log("BattleMapEditor debug refreshed saved battle keys.");
    }
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
        searching,
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
        battleSelectList.SetSelectables(savedBattles.GetAllKeys());
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
    public void ShowEnemySearch()
    {
        bNRS = NameRaterState.searching;
        if (nRObject != null)
        {
            nRObject.SetActive(true);
        }
        if (nameRater != null)
        {
            nameRater.ResetNewName();
        }
    }
    public void NameRaterConfirm()
    {
        if (nameRater == null){return;}
        switch (bNRS)
        {
            case NameRaterState.searching:
                FilterActorSelect(nameRater);
                break;
        }
        if (nRObject != null)
        {
            nRObject.SetActive(false);
        }
        nameRater.ResetNewName();
    }
    public void CancelNameRater()
    {
        if (nRObject != null)
        {
            nRObject.SetActive(false);
        }
        if (nameRater != null)
        {
            nameRater.ResetNewName();
        }
    }
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
        if (filter != null)
        {
            filter.ResetNewName();
        }
        if (nameRater != null)
        {
            nameRater.ResetNewName();
        }
        actorSelect.SetData(actorStats.GetAllKeys(), actorStats.GetAllKeys(), actorStats.GetAllValues());
    }
    public void FilterActorSelect()
    {
        FilterActorSelect(filter);
    }
    protected void FilterActorSelect(NameRater activeFilter)
    {
        if (activeFilter == null){return;}
        actorSelect.ResetSelected();
        List<string> filters = new List<string>();
        filters.Add(activeFilter.ReturnNameWithFirstCharUpperCase());
        filters.Add(activeFilter.ConfirmName().ToLower());
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
