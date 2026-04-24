using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StSRogueLikeDebugHelper : MonoBehaviour
{
    public enum DebugBattleType
    {
        Enemy,
        Elite,
        Boss
    }

    public StSStateManager manager;

    [Header("Scene Debug")]
    public string debugSceneName;

    [Header("Tile Debug")]
    public string debugTileType = "Elite";

    [Header("Battle Debug")]
    public DebugBattleType debugBattleType = DebugBattleType.Elite;
    public string debugBattleName;

    protected bool TryGetCoreReferences(out SceneMover sceneMover, out BattleState battleState, out CharacterList enemyList, out StSEnemyTracker enemyTracker, out StSState gameState)
    {
        sceneMover = null;
        battleState = null;
        enemyList = null;
        enemyTracker = null;
        gameState = null;

        if (manager == null)
        {
            Debug.LogWarning("StSRogueLikeDebugHelper failed: missing StSStateManager reference.");
            return false;
        }

        sceneMover = manager.sceneMover;
        battleState = manager.battleState;
        enemyList = manager.enemyList;
        enemyTracker = manager.enemyTracker;
        gameState = manager.gameState;

        if (sceneMover == null || battleState == null || enemyList == null || enemyTracker == null || gameState == null)
        {
            Debug.LogWarning("StSRogueLikeDebugHelper failed: manager is missing required references.");
            return false;
        }
        return true;
    }

    protected bool TryUseSavedBattle(string battleName, BattleState battleState, CharacterList enemyList)
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

    protected bool TryGetNamedBattleData(DebugBattleType battleType, string battleName, StSEnemyTracker enemyTracker, int floor, out string battleData)
    {
        battleData = "";
        switch (battleType)
        {
            case DebugBattleType.Enemy:
                if (floor <= 0 || floor > enemyTracker.floorEnemies.Count){return false;}
                if (!enemyTracker.floorEnemies[floor - 1].KeyExists(battleName)){return false;}
                battleData = enemyTracker.floorEnemies[floor - 1].ReturnValue(battleName);
                return battleData.Length > 0;
            case DebugBattleType.Elite:
                if (floor <= 0 || floor > enemyTracker.floorElites.Count){return false;}
                if (!enemyTracker.floorElites[floor - 1].KeyExists(battleName)){return false;}
                battleData = enemyTracker.floorElites[floor - 1].ReturnValue(battleName);
                return battleData.Length > 0;
            case DebugBattleType.Boss:
                if (floor <= 0 || floor > enemyTracker.floorBosses.Count){return false;}
                if (!enemyTracker.floorBosses[floor - 1].KeyExists(battleName)){return false;}
                battleData = enemyTracker.floorBosses[floor - 1].ReturnValue(battleName);
                return battleData.Length > 0;
            default:
                return false;
        }
    }

    [ContextMenu("Debug Move To Scene")]
    public void DebugMoveToScene()
    {
        if (string.IsNullOrWhiteSpace(debugSceneName))
        {
            Debug.LogWarning("StSRogueLikeDebugHelper scene move failed: debugSceneName is empty.");
            return;
        }

        SceneMover sceneMover;
        BattleState battleState;
        CharacterList enemyList;
        StSEnemyTracker enemyTracker;
        StSState gameState;
        if (!TryGetCoreReferences(out sceneMover, out battleState, out enemyList, out enemyTracker, out gameState))
        {
            return;
        }

        sceneMover.DebugMoveToScene(debugSceneName);
    }

    [ContextMenu("Debug Move To Tile Type")]
    public void DebugMoveToTileType()
    {
        if (manager == null)
        {
            Debug.LogWarning("StSRogueLikeDebugHelper tile move failed: missing StSStateManager reference.");
            return;
        }
        if (string.IsNullOrWhiteSpace(debugTileType))
        {
            Debug.LogWarning("StSRogueLikeDebugHelper tile move failed: debugTileType is empty.");
            return;
        }

        manager.MoveToTile(debugTileType);
    }

    [ContextMenu("Debug Enter Battle By Name")]
    public void DebugEnterBattleByName()
    {
        if (string.IsNullOrWhiteSpace(debugBattleName))
        {
            Debug.LogWarning("StSRogueLikeDebugHelper battle entry failed: debugBattleName is empty.");
            return;
        }

        SceneMover sceneMover;
        BattleState battleState;
        CharacterList enemyList;
        StSEnemyTracker enemyTracker;
        StSState gameState;
        if (!TryGetCoreReferences(out sceneMover, out battleState, out enemyList, out enemyTracker, out gameState))
        {
            return;
        }

        gameState.UpdateState(debugBattleType == DebugBattleType.Boss ? "Boss" : "Battle");
        enemyList.ResetLists();

        if (!TryUseSavedBattle(debugBattleName, battleState, enemyList))
        {
            string battleData;
            int floor = gameState.GetFloor();
            if (!TryGetNamedBattleData(debugBattleType, debugBattleName, enemyTracker, floor, out battleData))
            {
                Debug.LogWarning("StSRogueLikeDebugHelper battle entry failed: no " + debugBattleType + " named " + debugBattleName + " for floor " + floor);
                return;
            }

            string[] dataBlocks = battleData.Split("-");
            if (dataBlocks.Length < 4)
            {
                Debug.LogWarning("StSRogueLikeDebugHelper battle entry failed: invalid battle data for " + debugBattleName);
                return;
            }

            battleState.ClearCustomBattleName();
            battleState.ForceTerrainType(dataBlocks[0]);
            battleState.SetWeather(dataBlocks[1]);
            battleState.SetTime(dataBlocks[2]);
            enemyList.AddCharacters(dataBlocks[3].Split("|").ToList());
        }

        manager.Save();
        sceneMover.MoveToBattle();
        Debug.Log("StSRogueLikeDebugHelper entering " + debugBattleType + " battle: " + debugBattleName);
    }
}
