using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Controls Battle Turn Setup/Cleanup/Lifecycle/BattleStacks
public class AutoBattleManager : MonoBehaviour
{
    public GeneralUtility utility;
    public AutoChessBattleUIManager battleUI;
    public AutoChessDataManager data;
    public AutoChessFactionManager factionManager;
    public void TriggerTrait(TacticActor actor, string timing, TacticActor otherActor = null)
    {
        // TODO Update The Combat Log To Show Stack Gain.
        AutoChessTrait trait = actor.autoChessTrait;
        if (trait.timing == timing)
        {
            map.UpdateCombatLog(actor.GetPersonalName() + "'s Trait Activates");
            if (otherActor == null)
            {
                factionManager.GainStacksFromTraitSwitch(trait, actor.GetAutoChessFactions());
            }
            else
            {
                factionManager.GainStacksFromTraitSwitch(trait, actor.GetAutoChessFactions(), 1, otherActor.GetAutoChessFactions());
            }
        }
    }
    public AutoChessEnemyDataManager enemyData;
    public BattleMap map;
    public int CastleTile()
    {
        return map.mapUtility.ReturnTileNumberFromRowCol(map.mapSize / 2, 0, map.mapSize);
    }
    public int CastleHealthLoss()
    {
        int finalCastleHealth = map.GetBuildingHealthOnLocation(CastleTile());
        // Max Loss
        if (finalCastleHealth <= 0){return 10;}
        // 300 -> 0
        // 299-270 -> 1
        // 0 -> 10
        int damage = 300 - finalCastleHealth;
        if (damage <= 0){return 0;}
        return 1 + (damage / 30);
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
    // Faction Specific Stuff.
    public int aegirRevives = 0;
    public int kjeragWindCD = -1;
    public void ResilientCheckResurrectedAlly(TacticActor actor)
    {
        if (actor.PassiveExists("AKResilient"))
        {
            // Trigger The Passive Again.
            effectManager.ApplyPassiveByName(actor, map, "AKResilient");
        }
    }
    public int currentRound = 1;
    public void StartBattle()
    {
        battleUI.StartBattle();
        aegirRevives = 0;
        kjeragWindCD = -1;
        currentRound = 1;
        actorMaker.ResetID();
        // Reset The Map.
        map.combatLog.ForceStart();
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
        // Apply Faction Effects.
        factionEffectManager.ApplyFactionEffects(allAutoActors, factionManager.factionData.GetActiveFactions(), factionManager.factionData.GetActiveFactionCount(), factionManager.factionData.GetActiveFactionStacks());
        // Apply Equipment Effects.
        for (int i = 0; i < allAutoActors.Count; i++)
        {
            actorMaker.ApplyAutoChessEquipmentEffects(allAutoActors[i], allAutoActors);
            actorMaker.ReorganizeActorPassives(allAutoActors[i]);
        }
        // Track special faction effects here: Aegir/Yan/Kjerag
        if (factionManager.factionData.GetCountOfFaction("Kjerag") > 5)
        {
            kjeragWindCD = (Mathf.Max(1, 6 / Mathf.Max(1, (int.Parse(factionManager.factionData.GetStacksOfFaction("Kjerag")) / 100))));
        }
        if (factionManager.factionData.GetCountOfFaction("Yan") > 5)
        {
            // TODO: Summon The Mega Dragon
        }
        // TODO Do The Aegir Thing
        if (factionManager.factionData.FactionActive("Aegir"))
        {
            if (factionManager.factionData.GetCountOfFaction("Aegir") >= 5)
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
        // Update Any Dead Allies That Are Ready And Have Open Tiles.
        List<TacticActor> defeatedActors = map.GetDefeatedActors();
        for (int i = 0; i < defeatedActors.Count; i++)
        {
            if (defeatedActors[i].GetTeam() != 0){continue;}
            Debug.Log(defeatedActors[i].currentRespawnTimer + "/" + defeatedActors[i].baseRespawnTimer);
            if (defeatedActors[i].ReadyToRespawn())
            {
                // Check If The Tile Is Open.
                if (map.GetActorOnTile(defeatedActors[i].GetLocation()) != null){continue;}
                // Revive Them.
                map.ResurrectActor(defeatedActors[i]);
                ResilientCheckResurrectedAlly(defeatedActors[i]);
            }
        }
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
            map.combatLog.DebugAllLogs();
            // Lose Health Based On Castle Damage.
            data.LoseHealth(CastleHealthLoss());
            // Gain Gold + Exp Based On Win/Lose/Etc.
            data.GainExpAfterBattle();
            data.GainGoldAfterBattle();
            // TODO Gain Gold Based On Foresight/Marvel Stacks.
            // Increment Round.
            data.NewRound();
            // SAVE
            data.Save();
            battleUI.EndBattle();
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
            actor.UpdateRoundAttackTracker();
            map.DamageTileBuilding(castleTile, actor, actor.GetAttack());
            EndTurn(actor);
            return;
        }
        // Priority 2: Attack Player Units
        List<TacticActor> enemies = map.ReturnEnemiesInTiles(actor, attackable);
        if (enemies.Count > 0)
        {
            TacticActor targetedEnemy = enemies[enemyData.autoChessEnemyRNG.SeedRange(0, enemies.Count)];
            // TODO Check For Skill Usage.
            ActorAttacksActor(actor, targetedEnemy);
        }
        // Don't Move If Frozen.
        if (actor.StatusExists("Frozen"))
        {
            EndTurn(actor);
            return;
        }
        // Priority 3: Path Toward Castle
        enemyPath = actorAI.FindPathToTile(actor, map, moveManager, castleTile);
        if (enemyPath.Count > 0 && map.GetActorOnTile(enemyPath[enemyPath.Count - 1]) == null)
        {
            map.MoveActorToTile(actor, enemyPath[enemyPath.Count - 1]);
            EndTurn(actor);
            return;
        }
        // Priority 4: Path Toward Player Units
        enemyPath = actorAI.FindPathToTarget(actor, map, moveManager);
        if (enemyPath.Count > 0 && map.GetActorOnTile(enemyPath[enemyPath.Count - 1]) == null)
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
            targetTile = actorAI.ChooseSkillTargetLocation(actor, map);
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
            actor.ResetRespawnTimer();
            TriggerTrait(actor, "OnDeath");
        }
        // Resurrect After Gaining Stacks.
        if (actor.Resurrect())
        {
            // Can't Just Start Battle, The BS Stat Buffs Will Stack.
            // effectManager.StartBattle(actor, map);
            // Need To Do The Resilient Check.
            map.ResurrectActor(actor);
            ResilientCheckResurrectedAlly(actor);
            return;
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
        // Check Traits For Both You And The Actor Behind You.
        if (actor.GetTeam() == 0)
        {
            TacticActor behindActor = map.GetActorBehindActor(actor);
            if (behindActor != null && behindActor.GetTeam() == 0)
            {
                TriggerTrait(behindActor, "OnForwardSkill", actor);
            }
            TriggerTrait(actor, "OnSkill");
            if (actor.ReturnTotalRoundSkills() == 0)
            {
                TriggerTrait(actor, "FirstSkill");
            }
        }
        // Don't Bother With Costs In AutoMode.
        map.UpdateCombatLog(actor.GetPersonalName() + " uses " + skillName);
        activeManager.ActivateSkill(false);
        actor.SetCurrentEnergy(0);
        map.UpdateMap();
    }
    protected void ActorAttacksActor(TacticActor attacker, TacticActor defender)
    {
        if (attacker.GetTeam() == 0)
        {
            TriggerTrait(attacker, "OnAttack");
            TacticActor behindActor = map.GetActorBehindActor(attacker);
            if (behindActor != null && behindActor.GetTeam() == 0)
            {
                TriggerTrait(behindActor, "OnForwardAttack", attacker);
            }
        }
        map.UpdateCombatLog(attacker.GetPersonalName() + " attacks " + defender.GetPersonalName());
        // Show Attack Speed Rolls In The combatLog?
        attackManager.ActorAttacksActorWithAttackSpeed(attacker, defender, map, attacker.GetBasicAttackMultiplier(), attacker.GetBasicAttackDamageType());
        if (attacker.GetTeam() == 0 && defender.GetHealth() <= 0)
        {
            TriggerTrait(attacker, "OnKill");
        }
    }
}
