using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StSStateManager : MonoBehaviour
{
    // New Game/Load.
    void Start()
    {
        if (gameState.StartingNewGame())
        {
            NewGame();
        }
        else
        {
            Load();
        }
    }
    // Scene Names
    public string rewardSceneName;
    public string mapSceneName;
    public string restSceneName = "StSRest";
    // SUBMANAGERS
    public SceneMover sceneMover;
    public PartyDataManager stsParty;
    public RNGUtility masterRNG;
    public StSState gameState;
    public StSMap map;
    public StSMapSaveData mapState;
    public StSEnemyTracker enemyTracker;
    public StSRewardSaveData rewardTracker;
    public StSShopSaveData shopTracker;
    public BattleState battleState; // Needed To Save Enemies To A Battle.
    public CharacterList enemyList;
    // STATE DATA
    public List<SavedData> stsSavedState;
    public List<RNGUtility> stsRNG;
    public void NewGame()
    {
        // First RNG, Then State, Then Enemy Tracker, Then Map
        masterRNG.NewGame();
        gameState.SetUpNewGame();
        enemyTracker.NewGame();
        map.NewGame();
    }
    public void Save()
    {
        if (map != null)
        {
            map.Save();
        }
        for (int i = 0; i < stsSavedState.Count; i++)
        {
            stsSavedState[i].Save();
        }
        for (int i = 0; i < stsRNG.Count; i++)
        {
            stsRNG[i].Save();
        }
        stsParty.Save();
    }
    public void Load()
    {
        for (int i = 0; i < stsSavedState.Count; i++)
        {
            stsSavedState[i].Load();
        }
        for (int i = 0; i < stsRNG.Count; i++)
        {
            stsRNG[i].Load();
        }
        stsParty.Load();
        if (map != null)
        {
            map.Load();
        }
    }
    // STATE FUNCTIONS.
    protected bool TryUseSavedBattle(string battleName)
    {
        if (battleState.savedBattles == null || !battleState.savedBattles.BattleExists(battleName))
        {
            battleState.ClearCustomBattleName();
            return false;
        }
        List<string> savedMapInfo;
        List<string> savedTerrainEffects;
        List<int> savedElevations;
        List<string> savedBorders;
        List<string> savedBuildings;
        List<string> savedEnemies;
        List<int> savedEnemyLocations;
        string savedWeather;
        string savedTime;
        if (!battleState.savedBattles.TryLoadBattleData(battleName, out savedMapInfo, out savedTerrainEffects, out savedElevations, out savedBorders, out savedBuildings, out savedEnemies, out savedEnemyLocations, out savedWeather, out savedTime))
        {
            battleState.ClearCustomBattleName();
            return false;
        }
        battleState.SetCustomBattleName(battleName);
        battleState.SetWeather(savedWeather);
        battleState.SetTime("");
        enemyList.ResetLists();
        enemyList.AddCharacters(savedEnemies);
        return true;
    }
    public void MoveToTile(string tileType)
    {
        string newScene = "";
        switch (tileType)
        {
            // Generate Enemies/Battle.
            case "Enemy":
            gameState.UpdateState("Battle");
            string basicEnemy = enemyTracker.GetEnemyName();
            enemyList.ResetLists();
            if (!TryUseSavedBattle(basicEnemy))
            {
                string enemyData = enemyTracker.GetEnemyData(basicEnemy);
                string[] dataBlocks = enemyData.Split("-");
                battleState.ForceTerrainType(dataBlocks[0]);
                battleState.SetWeather(dataBlocks[1]);
                battleState.SetTime(dataBlocks[2]);
                enemyList.AddCharacters(dataBlocks[3].Split("|").ToList());
            }
            Save();
            sceneMover.MoveToBattle();
            break;
            case "Event":
            // Generate Event.
            break;
            // Generate Enemies/Battle.
            case "Elite":
            gameState.UpdateState("Battle");
            string eliteEnemy = enemyTracker.GetEliteName();
            enemyList.ResetLists();
            if (!TryUseSavedBattle(eliteEnemy))
            {
                string eliteData = enemyTracker.GetEliteData(eliteEnemy);
                string[] eliteBlocks = eliteData.Split("-");
                battleState.ForceTerrainType(eliteBlocks[0]);
                battleState.SetWeather(eliteBlocks[1]);
                battleState.SetTime(eliteBlocks[2]);
                enemyList.AddCharacters(eliteBlocks[3].Split("|").ToList());
            }
            Save();
            sceneMover.MoveToBattle();
            break;
            case "Shop":
            // Generate Shop.
            break;
            case "Rest":
            gameState.UpdateState("Rest");
            newScene = restSceneName;
            break;
            case "Treasure":
            // Generate Treasure.
            break;
            case "Boss":
            break;
        }
        if (newScene != "")
        {
            MoveScenes(newScene);
        }
    }
    public void WinBattle()
    {
        // Get the Battle Type From the State.
        string battleType = mapState.GetLatestTile();
        // Boss Battles Override Other Types.
        // TODO Double Bosses Means You Need To Move To The Next Boss Battle Instead.
        if (gameState.GetState() == "Boss")
        {
            battleType = "Boss";
        }
        rewardTracker.GenerateBattleRewards(battleType);
        // Some event battles also generate additional rewards.
        MoveScenes(rewardSceneName);
    }
    public void CompleteFloor(int floor)
    {
    }
    public void ReturnToMap()
    {
        MoveScenes(mapSceneName);
    }
    public void MoveScenes(string newScene)
    {
        Save();
        sceneMover.LoadScene(newScene);
    }
}
