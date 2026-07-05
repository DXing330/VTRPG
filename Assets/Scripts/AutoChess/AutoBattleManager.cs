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
    public int CastleTile()
    {
        return map.mapUtility.ReturnTileNumberFromRowCol(map.mapSize / 2, 0, map.mapSize);
    }
    public AutoChessActorMaker actorMaker;
    public AutoChessFactionEffectManager factionEffectManager;
    public EffectManager effectManager;
    public AttackManager attackManager;
    public ActiveManager activeManager;
    public MoveCostManager moveManager;
    public ActorAI actorAI;
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
    public int currentRound = 1;
    public void StartBattle()
    {
        aegirRevives = 0;
        kjeragWindCD = -1;
        currentRound = 1;
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
        map.InitializeCombatLog();
        map.InitializeDamageTracker();
        // Make The Castle.
        map.AddBuilding("Castle", CastleTile());
        map.SetBuildingHealthAndDefense(CastleTile(), 300, 5);
        // Update The MoveCostManager From The Map.
        moveManager.UpdateInfoFromBattleMap(map);
        // Spawn The First Wave Of Enemies.
        enemyPool = enemyData.GetNextRoundEnemies();
        SpawnPhase();
        // Start The First Round.
        StartRound();
    }
    // Only If No Enemies Alive + EnemyPool Empty OR Castle Destroyed
    public int EndBattle()
    {
        // Loss
        if (!map.BuildingExists("Castle") || currentRound > 999){return 1;}
        bool enemiesAlive = false;
        for (int i = 0; i < map.battlingActors.Count; i++)
        {
            if (map.battlingActors[i].GetTeam() > 0)
            {
                enemiesAlive = true;
            }
        }
        // Win
        if (enemyPool.Count <= 0 && !enemiesAlive){return 0;}
        // Continue
        return -1;
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
        StartCoroutine(BattleLoop(roundActors));
    }
    public float turnDelay;
    public IEnumerator BattleLoop(List<TacticActor> roundActors)
    {
        for (int i = 0; i < roundActors.Count; i++)
        {
            ActorStartTurn(roundActors[i]);
            map.UpdateMap();
            yield return new WaitForSeconds(turnDelay);
        }
        EndRound();
    }
    public int currentTurnIndex = 0;
    public void ActorStartTurn(TacticActor actor)
    {
        map.AddNewCombatLog();
        map.UpdateCombatLog(actor.GetPersonalName() + "'s Turn");
        if (actor == null){return;}
        if (actor.GetHealth() <= 0)
        {
            DeathActives(actor);
            return;
        }
        actor.StartTurn();
        actor.UpdateEnergy(1);
        effectManager.StartTurn(actor, map);
        // In AutoBattle Mode Skills Are Managed Through Cooldowns.
        // Split Between Player Turn And Enemy Turn.
        if (actor.GetTeam() == 0)
        {
            PlayerTurn(actor);
        }
        else
        {
            EnemyTurn(actor);
        }
    }
    public void EndTurn(TacticActor actor, int setDirection = -1)
    {
        if (actor.GetHealth() <= 0)
        {
            DeathActives(actor);
            return;
        }
        if (setDirection >= 0)
        {
            actor.SetDirection(setDirection);
        }
        // Apply EndTurn Effects.
        actor.EndTurn();
        effectManager.EndTurn(actor, map);
        map.UpdateMap();
    }
    public void EndRound()
    {
        // Spawn Next Wave.
        SpawnPhase();
        if (EndBattle() >= 0)
        {
            // TODO
            map.combatLog.DebugAllLogs();
            return;
        }
        currentRound++;
        StartRound();
    }
    public List<int> enemyPath;
    public void EnemyTurn(TacticActor actor)
    {
        // If Stunned Then Stop.
        if (actor.StatusExists("Stun"))
        {
            EndTurn(actor);
            return;
        }
        // Move/Attack/Skill.
        List<int> attackable = map.mapUtility.GetTilesInCircleShape(actor.GetLocation(), actor.GetAttackRange(), map.mapSize);
        // Priority 1: Attack Castle
        int castleTile = CastleTile();
        if (attackable.Contains(castleTile))
        {
            map.UpdateCombatLog(actor.GetPersonalName() + " attacks the castle");
            map.DamageTileBuilding(castleTile, actor, actor.GetAttack());
            EndTurn(actor);
            return;
        }
        // Priority 2: Path Toward Castle
        enemyPath = actorAI.FindPathToTile(actor, map, moveManager, castleTile);
        // Don't Move If Frozen.
        if (map.GetActorOnTile(enemyPath[enemyPath.Count - 1]) == null && !actor.StatusExists("Frozen"))
        {
            map.MoveActorToTile(actor, enemyPath[enemyPath.Count - 1]);
            EndTurn(actor);
            return;
        }
        // Priority 3: Attack Player Units
        List<TacticActor> enemies = map.ReturnEnemiesInTiles(actor, attackable);
        if (enemies.Count > 0)
        {
            TacticActor targetedEnemy = enemies[enemyData.autoChessEnemyRNG.SeedRange(0, enemies.Count)];
            // TODO Check For Skill Usage.
            ActorAttacksActor(actor, targetedEnemy);
        }
        // Priority 4: Path Toward Player Units
        enemyPath = actorAI.FindPathToTarget(actor, map, moveManager);
        if (map.GetActorOnTile(enemyPath[enemyPath.Count - 1]) == null && !actor.StatusExists("Frozen"))
        {
            map.MoveActorToTile(actor, enemyPath[enemyPath.Count - 1]);
            EndTurn(actor);
            return;
        }
        EndTurn(actor);
    }
    public List<int> GetPlayerActorAttackRangeTiles(TacticActor actor)
    {
        int range = actor.GetAttackRange();
        string rangeType = actor.GetAutoChessAttackRangeShape();
        int location = actor.GetLocation();
        int direction = actor.GetDirection();
        int selectedTile = map.mapUtility.PointInDirection(location, direction, map.mapSize);
        List<int> rangeTiles = map.mapUtility.GetAutoActorAttackTilesByShapeSpan(selectedTile, rangeType, range, map.mapSize, location);
        return rangeTiles;
    }
    public void PlayerTurn(TacticActor actor)
    {
        // Make Sure The Direction Never Changes.
        int startDirection = actor.GetDirection();
        // Attack/Skill.
        bool skillReady = (actor.GetEnergy() >= actor.GetBaseEnergy());
        int targetTile = -1;
        if (skillReady)
        {
            activeManager.SetSkillFromName(actor.GetAutoSkill(), actor);
            // Use skill immediately if self is target location.
            targetTile = actorAI.ChooseSkillTargetLocation(actor, map, moveManager);
            if (targetTile == actor.GetLocation())
            {
                activeManager.GetTargetedTiles(actor.GetLocation());
                ActivateSkill(actor.GetAutoSkill(), actor);
                EndTurn(actor, startDirection);
                return;
            }
        }
        List<int> attackable = GetPlayerActorAttackRangeTiles(actor);
        // Healer Vs Normal.
        if (actor.AKHealer())
        {
            attackable.Add(actor.GetLocation());
            // Find Lowest Health Ally In Range.
            List<TacticActor> allies = map.ReturnAlliesInTiles(actor, attackable);
            int targetIndex = -1;
            int targetHealth = 999999;
            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i].GetHealth() < targetHealth)
                {
                    targetHealth = allies[i].GetHealth();
                    targetIndex = i;
                }
            }
            if (targetIndex >= 0)
            {
                if (skillReady)
                {
                    activeManager.GetTargetedTiles(allies[targetIndex].GetLocation());
                    ActivateSkill(actor.GetAutoSkill(), actor);
                    EndTurn(actor, startDirection);
                    return;
                }
                allies[targetIndex].Heal(actor.GetAttack());
            }
        }
        else
        {
            // Find The Closest Enemy In Range
            List<TacticActor> enemies = map.ReturnEnemiesInTiles(actor, attackable);
            int enemyTargetIndex = -1;
            int targetDistance = 999999;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (map.DistanceBetweenActors(enemies[i], actor) < targetDistance)
                {
                    targetDistance = map.DistanceBetweenActors(enemies[i], actor);
                    enemyTargetIndex = i;
                }
            }
            if (enemyTargetIndex >= 0)
            {
                if (skillReady)
                {
                    activeManager.GetTargetedTiles(enemies[enemyTargetIndex].GetLocation());
                    ActivateSkill(actor.GetAutoSkill(), actor);
                    EndTurn(actor, startDirection);
                    return;
                }
                ActorAttacksActor(actor, enemies[enemyTargetIndex]);
            }
        }
        EndTurn(actor, startDirection);
    }
    public void DeathActives(TacticActor actor)
    {
        if (actor.GetHealth() > 0){return;}
        if (actor.GetTeam() == 0)
        {
            // TODO Get When Defeated Traits.
        }
        List<string> deathActives = new List<string>(actor.GetDeathActives());
        // Only Trigger Death Actives Once For Enemies.
        actor.DisableDeathActives();
        for (int i = 0; i < deathActives.Count; i++)
        {
            if (deathActives[i].Length <= 0) { continue; }
            activeManager.SetSkillFromName(deathActives[i], actor);
            activeManager.GetTargetedTiles(actor.GetLocation());
            ActivateSkill(deathActives[i], actor);
        }
    }
    public void ActivateSkill(string skillName, TacticActor actor = null)
    {
        if (actor == null){return;}
        // Don't Bother With Costs In AutoMode.
        map.UpdateCombatLog(actor.GetPersonalName() + " uses " + skillName);
        activeManager.ActivateSkill(false);
        actor.SetCurrentEnergy(0);
        map.UpdateMap();
    }
    protected void ActorAttacksActor(TacticActor attacker, TacticActor defender)
    {
        map.UpdateCombatLog(attacker.GetPersonalName() + " attacks " + defender.GetPersonalName());
        // Show Attack Speed Rolls In The combatLog?
        attackManager.ActorAttacksActorWithAttackSpeed(attacker, defender, map, attacker.GetBasicAttackMultiplier(), attacker.GetBasicAttackDamageType());
    }
}
