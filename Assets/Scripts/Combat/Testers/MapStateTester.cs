using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapStateTester : MonoBehaviour
{
    public BattleMap map;
    public BattleManager battleManager;
    public MoveCostManager moveManager;
    public TacticActor testGuardActor;
    public int guardRange = -1;
    public int testTileNumber = 0;
    public bool reachableTilesOnly = true;
    public bool includeCurrentTile = true;
    public bool logOnlyGoodTiles = true;
    public int maxLoggedTiles = 80;

    [ContextMenu("Find References")]
    public void FindReferences()
    {
        if (battleManager == null)
        {
            battleManager = FindLoadedObject<BattleManager>();
        }
        if (map == null && battleManager != null)
        {
            map = battleManager.map;
        }
        if (map == null)
        {
            map = FindLoadedObject<BattleMap>();
        }
        if (moveManager == null && battleManager != null)
        {
            moveManager = battleManager.moveManager;
        }
        if (moveManager == null)
        {
            moveManager = FindLoadedObject<MoveCostManager>();
        }

        Debug.Log("MapStateTester references: map=" + ObjectName(map)
            + " battleManager=" + ObjectName(battleManager)
            + " moveManager=" + ObjectName(moveManager));
    }

    [ContextMenu("Guard Tiles > Test Tile")]
    public void TestGuardTile()
    {
        if (!Ready()) { return; }

        List<TacticActor> actors = ActorsToTest();
        for (int i = 0; i < actors.Count; i++)
        {
            TacticActor actor = actors[i];

            TacticActor coveredAlly;
            TacticActor blockedEnemy;
            bool goodTile = TryFindCoverage(actor, testTileNumber, out coveredAlly, out blockedEnemy);
            Debug.Log(GuardTileLine(actor, testTileNumber, goodTile, coveredAlly, blockedEnemy));
        }
    }

    [ContextMenu("Guard Tiles > Scan Matching Actors")]
    public void ScanGuardTiles()
    {
        if (!Ready()) { return; }
        List<TacticActor> actors = ActorsToTest();
        for (int i = 0; i < actors.Count; i++)
        {
            ScanGuardTiles(actors[i]);
        }
    }

    [ContextMenu("Guard Tiles > Current Positions")]
    public void LogCurrentGuardPositions()
    {
        if (!Ready()) { return; }

        for (int i = 0; i < map.battlingActors.Count; i++)
        {
            TacticActor actor = map.battlingActors[i];
            if (actor == null || actor.GetHealth() <= 0) { continue; }

            TacticActor coveredAlly;
            TacticActor blockedEnemy;
            bool goodTile = TryFindCoverage(actor, actor.GetLocation(), out coveredAlly, out blockedEnemy);
            Debug.Log(GuardTileLine(actor, actor.GetLocation(), goodTile, coveredAlly, blockedEnemy));
        }
    }

    void ScanGuardTiles(TacticActor actor)
    {
        List<int> candidateTiles = CandidateTiles(actor);
        int resolvedGuardRange = ResolvedGuardRange(actor);
        int goodTiles = 0;
        int loggedTiles = 0;
        Debug.Log("MapStateTester scanning " + ActorLabel(actor) + " candidateTiles=" + candidateTiles.Count + " guardRange=" + resolvedGuardRange + " reachableOnly=" + reachableTilesOnly);
        for (int i = 0; i < candidateTiles.Count; i++)
        {
            int tile = candidateTiles[i];
            TacticActor coveredAlly;
            TacticActor blockedEnemy;
            bool goodTile = TryFindCoverage(actor, tile, resolvedGuardRange, out coveredAlly, out blockedEnemy);
            if (goodTile)
            {
                goodTiles++;
            }
            if (logOnlyGoodTiles && !goodTile)
            {
                continue;
            }
            if (loggedTiles >= maxLoggedTiles)
            {
                continue;
            }
            Debug.Log(GuardTileLine(actor, tile, goodTile, coveredAlly, blockedEnemy));
            loggedTiles++;
        }
        Debug.Log("MapStateTester " + ActorLabel(actor) + " goodGuardTiles=" + goodTiles + "/" + candidateTiles.Count);
    }

    bool TryFindCoverage(TacticActor guardActor, int tileNumber, out TacticActor coveredAlly, out TacticActor blockedEnemy)
    {
        return TryFindCoverage(guardActor, tileNumber, ResolvedGuardRange(guardActor), out coveredAlly, out blockedEnemy);
    }

    bool TryFindCoverage(TacticActor guardActor, int tileNumber, int resolvedGuardRange, out TacticActor coveredAlly, out TacticActor blockedEnemy)
    {
        coveredAlly = null;
        blockedEnemy = null;
        if (map == null || map.battleMapUtility == null || guardActor == null) { return false; }

        if (!map.battleMapUtility.TileCanGuardAnyAllyFromAnyEnemy(map, guardActor, tileNumber, resolvedGuardRange))
        {
            return false;
        }

        List<TacticActor> allies = map.AllAllies(guardActor);
        List<TacticActor> enemies = map.AllEnemies(guardActor);
        for (int i = 0; i < allies.Count; i++)
        {
            TacticActor ally = allies[i];
            if (ally == null || ally == guardActor || ally.GetHealth() <= 0) { continue; }
            for (int j = 0; j < enemies.Count; j++)
            {
                TacticActor enemy = enemies[j];
                if (enemy == null || enemy.GetHealth() <= 0 || enemy.invisible) { continue; }
                if (map.battleMapUtility.TileCanGuardAllyFromEnemy(map, guardActor, tileNumber, ally, enemy, resolvedGuardRange))
                {
                    coveredAlly = ally;
                    blockedEnemy = enemy;
                    return true;
                }
            }
        }
        return false;
    }

    int ResolvedGuardRange(TacticActor guardActor)
    {
        if (guardRange >= 0 || guardActor == null)
        {
            return guardRange;
        }
        return guardActor.GetGuardRange();
    }

    List<int> CandidateTiles(TacticActor actor)
    {
        List<int> tiles = new List<int>();
        if (reachableTilesOnly && moveManager != null)
        {
            moveManager.UpdateInfoFromBattleMap(map);
            tiles.AddRange(moveManager.GetAllReachableTiles(actor, map.battlingActors));
        }
        else
        {
            for (int i = 0; i < map.mapSize * map.mapSize; i++)
            {
                tiles.Add(i);
            }
        }

        if (includeCurrentTile && !tiles.Contains(actor.GetLocation()))
        {
            tiles.Add(actor.GetLocation());
        }
        return tiles;
    }

    bool Ready()
    {
        FindReferences();
        if (map == null)
        {
            Debug.Log("MapStateTester has no BattleMap.");
            return false;
        }
        if (map.battleMapUtility == null)
        {
            Debug.Log("MapStateTester map has no BattleMapUtility.");
            return false;
        }
        if (map.battlingActors == null || map.battlingActors.Count == 0)
        {
            Debug.Log("MapStateTester map has no battling actors. Start or set up the test battle first.");
            return false;
        }
        return true;
    }

    List<TacticActor> ActorsToTest()
    {
        List<TacticActor> actors = new List<TacticActor>();
        if (testGuardActor != null)
        {
            if (testGuardActor.GetHealth() > 0)
            {
                actors.Add(testGuardActor);
            }
            else
            {
                Debug.Log("MapStateTester test guard actor is not alive: " + ActorLabel(testGuardActor));
            }
            return actors;
        }

        Debug.Log("MapStateTester has no test guard actor assigned. Drag a guard actor into `testGuardActor`.");
        return actors;
    }

    string GuardTileLine(TacticActor actor, int tile, bool goodTile, TacticActor coveredAlly, TacticActor blockedEnemy)
    {
        string result = goodTile ? "GOOD" : "bad";
        string coverage = goodTile
            ? " protects " + ActorLabel(coveredAlly) + " from " + ActorLabel(blockedEnemy)
            : "";
        return "MapStateTester " + ActorLabel(actor) + " tile " + tile + " => " + result + coverage;
    }

    string ActorLabel(TacticActor actor)
    {
        if (actor == null) { return "null"; }
        return actor.GetPersonalName() + " [" + actor.GetSpriteName() + "]";
    }

    string ObjectName(Object unityObject)
    {
        return unityObject == null ? "null" : unityObject.name;
    }

    T FindLoadedObject<T>() where T : Object
    {
        T[] objects = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < objects.Length; i++)
        {
            Component component = objects[i] as Component;
            if (component == null) { continue; }

            Scene scene = component.gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
            {
                return objects[i];
            }
        }
        return null;
    }
}
