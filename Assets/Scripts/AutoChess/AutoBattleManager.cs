using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Controls Battle Turn Setup/Cleanup/Lifecycle
public class AutoBattleManager : MonoBehaviour
{
    public GeneralUtility utility;
    public AutoChessDataManager data;
    public AutoChessFactionDataManager factionData;
    public AutoChessEnemyDataManager enemyData;
    public BattleMap map;
    public AutoChessActorMaker actorMaker;
    public AutoChessFactionEffectManager factionEffectManager;
    public EffectManager effectManager;
    public AttackManager attackManager;
    public ActiveManager activeManager;
    public MoveCostManager moveManager;
    public List<AutoActor> allAutoActors = new();
    public List<int> GetSpawnTiles()
    {
        List<int> spawnTiles = new List<int>();
        for (int i = 0; i < map.mapSize; i++)
        {
            spawnTiles.Add(map.mapUtility.ReturnTileNumberFromRowCol(i, map.mapSize - 1, map.mapSize));
        }
        return spawnTiles;
    }
    public List<string> enemyPool;
    public int aegirRevives = 0;
    public int kjeragWindCD = -1;
    public void StartBattle()
    {
        aegirRevives = 0;
        kjeragWindCD = -1;
        actorMaker.ResetID();
        // Reset The Map.
        map.ForceStart();
        // Load The Tiles/Terrain From The DataManager.
        map.SetMapInfo(data.GetMapTiles());
        map.SetTerrainEffectTiles(data.GetMapTerrain());
        // Make Actors For Each Player Unit.
        List<string> fieldActors = data.GetFieldActorData();
        for (int i = 0; i < fieldActors.Count; i++)
        {
            AutoActor newActor = actorMaker.CreateActor(fieldActors[i]);
            // Track The Actors.
            allAutoActors.Add(newActor);
        }
        // Apply Equipment Effects.
        for (int i = 0; i < allAutoActors.Count; i++)
        {
            actorMaker.ApplyAutoChessEquipmentEffects(allAutoActors[i], allAutoActors);
        }
        // Apply Faction Effects.
        factionEffectManager.ApplyFactionEffects(allAutoActors, factionData.GetActiveFactions(), factionData.GetActiveFactionCount(), factionData.GetActiveFactionStacks());
        // Track special faction effects here: Aegir/Yan/Kjerag
        if (factionData.GetCountOfFaction("Kjerag") > 5)
        {
            kjeragWindCD = (Mathf.Max(1, 6 / Mathf.Max(1, (int.Parse(factionData.GetStacksOfFaction("Kjerag")) / 100))));
        }
        if (factionData.GetCountOfFaction("Yan") > 5)
        {
            // TODO: Summon The Mega Dragon
        }
        // TODO Do The Aegir Thing
        if (factionData.FactionActive("Aegir"))
        {
            if (factionData.GetCountOfFaction("Aegir") >= 5)
            {
                aegirRevives = 3;
            }
        }
        // Add The Actors To The Map.
        for (int i = 0; i < allAutoActors.Count; i++)
        {
            // Apply Start Battle Effects.
            effectManager.StartBattle(allAutoActors[i], map);
            map.AddActorToBattle(allAutoActors[i]);
        }
        // Make The Castle.
        int castleTile = map.mapUtility.ReturnTileNumberFromRowCol(map.mapSize / 2, 0, map.mapSize);
        map.AddBuilding("Castle", castleTile);
        // Update The MoveCostManager From The Map.
        moveManager.UpdateInfoFromBattleMap(map);
        // Spawn The First Wave Of Enemies.
        enemyPool = enemyData.GetNextRoundEnemies();
        SpawnPhase();
        // Start The First Round.
        StartRound();
    }
    // Only If No Enemies Alive + EnemyPool Empty OR Castle Destroyed
    public bool EndBattle()
    {
        return false;
    }
    public void SpawnPhase()
    {
        if (enemyPool.Count <= 0){return;}
        // for each spawn zone, spawn a random enemy from the pool
        List<int> spawnZones = GetSpawnTiles();
        spawnZones = utility.ShuffleIntList(spawnZones);
        for (int i = 0; i < spawnZones.Count; i++)
        {
            if (enemyPool.Count <= 0){break;}
            if (map.GetActorOnTile(spawnZones[i]) == null)
            {
                int randomIndex = enemyData.autoChessEnemyRNG.SeedRange(0, enemyPool.Count);
                string spawnedEnemyName = enemyPool[randomIndex];
                enemyPool.RemoveAt(randomIndex);
                SpawnEnemy(spawnedEnemyName, spawnZones[i]);
            }
        }
        map.UpdateMap();
    }
    public void SpawnEnemy(string enemyName, int enemyLocation)
    {
        AutoActor newActor = actorMaker.CreateEnemyActor(enemyName, enemyLocation);
        allAutoActors.Add(newActor);
        effectManager.StartBattle(newActor, map);
        map.AddActorToBattle(newActor);
    }
    public List<TacticActor> roundActors;
    public void StartRound()
    {
        // Clean The Map/List Of Dead Actors.
        map.RemoveActorsFromBattle();
        // TODO Do The Kjerag Passive Thing.
        // Only Iterate Through The List Length At The Start, Thus Ignoring Newly Summoned Actors.
        roundActors = new List<TacticActor>(map.battlingActors);
        // Make The List Of Round Actors From The Map Battling Actors.
        for (int i = 0; i < roundActors.Count; i++)
        {
            ActorStartTurn(roundActors[i]);
        }
        EndRound();
    }
    public int currentTurnIndex = 0;
    // TODO COROUTINES???
    public void ActorStartTurn(TacticActor actor) // Calls Ally/Enemy + EndTurn
    {
        if (actor.GetHealth() <= 0)
        {
            return;
        }
        // Split Between Player Turn And Enemy Turn.
        map.UpdateMap();
    }
    public void EndTurn()
    {
        // Apply EndTurn Effects.
        map.UpdateMap();
    }
    public void EndRound()
    {
        // Spawn Next Wave.
    }
    public void EnemyTurn()
    {
        // Move/Attack/Skill.
    }
    public void PlayerTurn()
    {
        // Attack/Skill.
    }
    public void DeathActives(TacticActor actor)
    {
        if (actor.GetHealth() >= 0){return;}
        List<string> deathActives = new List<string>(actor.GetDeathActives());
        for (int i = 0; i < deathActives.Count; i++)
        {
            if (deathActives[i].Length <= 0) { continue; }
            activeManager.SetSkillFromName(deathActives[i], actor);
            activeManager.GetTargetedTiles(actor.GetLocation(), moveManager.actorPathfinder);
            ActivateSkill(deathActives[i], actor);
        }
    }
    public void ActivateSkill(string skillName, TacticActor actor = null)
    {
        if (actor == null){return;}
        activeManager.ActivateSkill(this);
        map.UpdateMap();
    }
}
