using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Controls Battle Turn Setup/Cleanup/Lifecycle/BattleStacks
public class AutoBattleManager : MonoBehaviour
{
    public GeneralUtility utility;
    public AutoChessBattleUIManager battleUI;
    public AutoChessDataManager data;
    public AutoChessTactician tactician;
    public AutoChessFactionManager factionManager;
    public virtual void GainFactionStacks(string faction, int stackAmount, int team = 0)
    {
        factionManager.GainFactionStacks(faction, stackAmount);
    }
    public virtual void TriggerTrait(TacticActor actor, string timing, TacticActor otherActor = null)
    {
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
    protected int castleTile;
    public void GetCastleTile()
    {
        castleTile = map.mapUtility.ReturnTileNumberFromRowCol(map.mapSize / 2, 0, map.mapSize);
    }
    public int CastleHealthLoss()
    {
        int finalCastleHealth = map.GetBuildingHealthOnLocation(castleTile);
        // Max Loss
        if (finalCastleHealth <= 0)
        {
            if (data.LastBattle())
            {
                return 999;
            }
            return 10;
        }
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
    protected List<int> spawnTiles;
    protected void GenerateSpawnTiles()
    {
        spawnTiles = new List<int>();
        for (int i = 0; i < map.mapSize; i++)
        {
            spawnTiles.Add(map.mapUtility.ReturnTileNumberFromRowCol(i, map.mapSize - 1, map.mapSize));
        }
    }
    public List<int> GetSpawnTiles()
    {
        return spawnTiles;
    }
    public List<string> enemyPool;
    // Key Constants
    protected const int SKILLUSEDALREADY = -2;
    protected List<string> WEATHERS = new List<string>{"Sunny", "Rainy", "Windy", "None"};
    protected List<string> TIMES = new List<string>{"Day", "Day", "Night"};
    // Faction Specific Stuff.
    public AutoChessAegirManager aegirManager;
    public int yanDragon = 0;
    public int yanDragonAttack = 0;
    public int turretActive = 0;
    public int aegirRevives = 0;
    public int generalRevives = 0;
    public int kjeragWindCD = -1;
    public void CheckDefeatedAllies()
    {
        for (int i = 0; i < map.battlingActors.Count; i++)
        {
            if (map.battlingActors[i].GetTeam() != 0 || map.battlingActors[i].GetHealth() > 0){continue;}
            if (aegirRevives > 0 && map.battlingActors[i].AutoChessFaction("Aegir"))
            {
                aegirRevives--;
                map.ResurrectActor(map.battlingActors[i]);
                continue;
            }
            if (generalRevives > 0)
            {
                generalRevives--;
                map.ResurrectActor(map.battlingActors[i]);
                continue;
            }
        }
    }
    public void ResilientCheckResurrectedAlly(TacticActor actor)
    {
        if (actor.PassiveExists("AKResilient"))
        {
            // Trigger The Passive Again.
            effectManager.ApplyPassiveByName(actor, map, "AKResilient");
        }
    }
    public int currentRound = 1;
    public virtual void StartBattle()
    {
        battleUI.StartBattle();
        GenerateSpawnTiles();
        GetCastleTile();
        data.AddLog("Starting Battle");
        turretActive = 0;
        yanDragon = 0;
        yanDragonAttack = 0;
        aegirRevives = 0;
        generalRevives = 0;
        kjeragWindCD = -1;
        currentRound = 1;
        actorMaker.ResetID();
        // Reset The Map.
        map.combatLog.ForceStart();
        map.ForceStart();
        // Load The Tiles/Terrain From The DataManager.
        map.SetMapInfo(data.GetMapTiles());
        map.SetTerrainEffectTiles(data.GetMapTerrain());
        // Generate A Random Weather And Time.
        map.SetWeather(WEATHERS[enemyData.autoChessEnemyRNG.SeedRange(0, WEATHERS.Count)]);
        map.SetTime(TIMES[enemyData.autoChessEnemyRNG.SeedRange(0, TIMES.Count)]);
        // Make Actors For Each Player Unit.
        List<string> fieldActors = data.GetFieldActorData();
        for (int i = 0; i < fieldActors.Count; i++)
        {
            if (fieldActors[i].Length <= 0){continue;}
            AutoActor newActor = actorMaker.CreateActor(fieldActors[i]);
            // Track The Actors.
            allAutoActors.Add(newActor);
        }
        // Apply Faction Effects.
        // Apply Equipment Effects.
        for (int i = 0; i < allAutoActors.Count; i++)
        {
            actorMaker.ApplyAutoChessEquipmentEffects(allAutoActors[i], allAutoActors);
        }
        factionEffectManager.ApplyFactionEffects(allAutoActors, factionManager.factionData);
        map.InitializeCombatLog();
        map.InitializeDamageTracker();
        // Add The Actors To The Map.
        for (int i = 0; i < allAutoActors.Count; i++)
        {
            actorMaker.ReorganizeActorPassives(allAutoActors[i]);
            // Apply Start Battle Effects.
            effectManager.StartBattle(allAutoActors[i], map);
            map.AddActorToBattle(allAutoActors[i]);
        }
        // Tactician Effects.
        if (tactician != null)
        {
            tactician.Load();
            tactician.ApplyStartBattleEffect(this, map.battlingActors);
        }
        // Track special faction effects here: Aegir/Yan/Kjerag
        if (factionManager.factionData.GetCountOfFaction("Kjerag") >= 5)
        {
            kjeragWindCD = (Mathf.Max(1, 6 / Mathf.Max(1, (int.Parse(factionManager.factionData.GetStacksOfFaction("Kjerag")) / 100))));
        }
        if (factionManager.factionData.GetCountOfFaction("Yan") >= 5)
        {
            yanDragon = 1;
            int totalYanAttack = 0;
            for (int i = 0; i < allAutoActors.Count; i++)
            {
                if (allAutoActors[i].AutoChessFaction("Yan"))
                {
                    totalYanAttack += allAutoActors[i].GetBaseAttack();
                }
            }
            yanDragonAttack = totalYanAttack * 30 / 100;
        }
        if (factionManager.factionData.FactionActive("Aegir"))
        {
            aegirManager.ApplyAegirFactionEffect(map.battlingActors, map, this);
            if (factionManager.factionData.GetCountOfFaction("Aegir") >= 5)
            {
                aegirRevives = 2;
            }
        }
        // Make The Castle.
        map.AddBuilding("Castle", castleTile);
        int castleHealth = 300;
        // If Last Round Then Castle Is All Health.
        if (data.LastBattle())
        {
            castleHealth = data.GetHealth() * 30;
        }
        map.SetBuildingHealthAndDefense(castleTile, castleHealth, 5);
        // Update The MoveCostManager From The Map.
        moveManager.UpdateInfoFromBattleMap(map);
        // Spawn The First Wave Of Enemies.
        enemyPool = enemyData.GetNextRoundEnemies();
        SpawnPhase();
        // Start The First Round.
        StartRound();
    }
    // Only If No Enemies Alive + EnemyPool Empty OR Castle Destroyed
    public virtual int EndBattle()
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
        AutoActor newActor = actorMaker.CreateEnemyAutoActor(enemyName, enemyLocation, data.GetDifficultyScaling());
        allAutoActors.Add(newActor);
        effectManager.StartBattle(newActor, map);
        map.AddActorToBattle(newActor);
    }
    public List<TacticActor> roundActors;
    public virtual void StartRound()
    {
        map.NextRound();
        // Check Revives Before Removing Actors Just In Case.
        CheckDefeatedAllies();
        // Clean The Map/List Of Dead Actors.
        map.RemoveActorsFromBattle();
        map.SetRound(currentRound);
        // TODO Do The Kjerag Passive Thing.
        // Only Iterate Through The List Length At The Start, Thus Ignoring Newly Summoned Actors.
        roundActors = new List<TacticActor>(map.battlingActors);
        // TODO Do The Turret + Yan Dragon Thing.
        if (turretActive == 1)
        {

        }
        if (yanDragon == 1)
        {

        }
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
    public virtual void ActorStartTurn(TacticActor actor)
    {
        map.AddNewCombatLog();
        map.UpdateCombatLog(actor.GetPersonalName() + "'s Turn");
        if (actor == null){return;}
        if (actor.GetHealth() <= 0)
        {
            DeathActives(actor);
            // If They Revived Then Keep Going.
            if (actor.GetHealth() <= 0){return;}
        }
        actor.StartTurn();
        actor.UpdateEnergy(1);
        effectManager.StartTurn(actor, map);
        // Show Start Stats.
        // If Stunned Then Stop.
        if (actor.StatusExists("Stun"))
        {
            map.UpdateCombatLog(actor.GetPersonalName() + " is stunned");
            EndTurn(actor);
            return;
        }
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
        actor.EndTurn();
        effectManager.EndTurn(actor, map);
        map.UpdateMap();
    }
    public virtual void EndRound()
    {
        // Spawn Next Wave.
        SpawnPhase();
        int endBattleResult = EndBattle();
        if (endBattleResult >= 0)
        {
            data.AddLog("Battle Ended");
            map.combatLog.DebugAllLogs();
            // Lose Health Based On Castle Damage.
            data.LoseHealth(CastleHealthLoss());
            // Increment Round.
            data.NewRound(endBattleResult);
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
        // Move/Attack/Skill.
        List<int> attackable = map.mapUtility.GetTilesInCircleShape(actor.GetLocation(), actor.GetAttackRange(), map.mapSize);
        // Priority 0: Activate Skill
        int targetTile = PrepareSkill(actor);
        bool skillReady = targetTile >= 0;
        // Skill Was Used.
        if (targetTile == SKILLUSEDALREADY)
        {
            EndTurn(actor);
            return;
        }
        // Priority 1: Attack Castle
        if (attackable.Contains(castleTile))
        {
            map.UpdateCombatLog(actor.GetPersonalName() + " attacks the castle");
            actor.UpdateRoundAttackTracker();
            map.DamageTileBuilding(castleTile, actor, actor.GetAttack());
            // Castle Will Do Some Reflect Damage.
            actor.UpdateHealth(map.GetBuildingDefenseOnLocation(castleTile));
            EndTurn(actor);
            return;
        }
        // Priority 2: Attack Player Units
        List<TacticActor> enemies = map.ReturnEnemiesInTiles(actor, attackable);
        // Priority 3: Path Toward Castle
        enemyPath = actorAI.FindSpecificTilePathToTile(actor, map, moveManager, castleTile);
        // Ranged Units Occasionally Move Instead Of Attacking Units If They Can Do Both.
        if (actor.GetAttackRange() > 1 && enemies.Count > 0 && enemyPath.Count > 0 && map.GetActorOnTile(enemyPath[enemyPath.Count - 1]) == null)
        {
            int attackRoll = enemyData.autoChessEnemyRNG.SeedRange(0, actor.ReturnTotalRoundAttacks());
            int moveRoll = enemyData.autoChessEnemyRNG.SeedRange(0, actor.ReturnTotalRoundMoves());
            if (attackRoll <= moveRoll)
            {
                TacticActor targetedEnemy = enemies[enemyData.autoChessEnemyRNG.SeedRange(0, enemies.Count)];
                // Use Attack Skill If Available.
                if (skillReady)
                {
                    UsePreparedSkill(actor, targetTile);
                    return;
                }
                ActorAttacksActor(actor, targetedEnemy);
            }
            else
            {
                if (actor.StatusExists("Frozen"))
                {
                    map.UpdateCombatLog(actor.GetPersonalName() + " is frozen");
                    EndTurn(actor);
                    return;
                }
                map.MoveActorToTile(actor, enemyPath[enemyPath.Count - 1]);
            }
            EndTurn(actor);
            return;
        }
        if (enemies.Count > 0)
        {
            TacticActor targetedEnemy = enemies[enemyData.autoChessEnemyRNG.SeedRange(0, enemies.Count)];
            // Use Attack Skill If Available.
            if (skillReady)
            {
                UsePreparedSkill(actor, targetTile);
                return;
            }
            ActorAttacksActor(actor, targetedEnemy);
        }
        // Don't Move If Frozen.
        if (actor.StatusExists("Frozen"))
        {
            map.UpdateCombatLog(actor.GetPersonalName() + " is frozen");
            EndTurn(actor);
            return;
        }
        if (enemyPath.Count > 0 && map.GetActorOnTile(enemyPath[enemyPath.Count - 1]) == null)
        {
            map.MoveActorToTile(actor, enemyPath[enemyPath.Count - 1]);
            EndTurn(actor);
            return;
        }
        EndTurn(actor);
    }
    public virtual List<int> GetPlayerActorAttackRangeTiles(TacticActor actor)
    {
        int range = actor.GetAttackRange();
        string rangeType = actor.GetAutoChessAttackRangeShape();
        int location = actor.GetLocation();
        int direction = actor.GetDirection();
        int selectedTile = map.mapUtility.PointInDirection(location, direction, map.mapSize);
        List<int> rangeTiles = map.mapUtility.GetAutoActorAttackTilesByShapeSpan(selectedTile, rangeType, range, map.mapSize, location);
        return rangeTiles;
    }
    public int PrepareSkill(TacticActor actor)
    {
        if (actor.GetEnergy() < actor.GetBaseEnergy() || actor.GetBaseEnergy() <= 0 || actor.GetAutoSkill() == "")
        {
            return -1;
        }
        int targetTile = -1;
        activeManager.SetSkillFromName(actor.GetAutoSkill(), actor);
        // Use skill immediately if self is target location.
        targetTile = actorAI.ChooseSkillTargetLocation(actor, map);
        if (targetTile == actor.GetLocation())
        {
            activeManager.GetTargetedTiles(actor.GetLocation());
            ActivateSkill(actor.GetAutoSkill(), actor);
            return SKILLUSEDALREADY;
        }
        // Either -2 = skill used, -1 = no skill ready, >= 0 = skill ready
        return targetTile;
    }
    // Ends The Turn
    public void UsePreparedSkill(TacticActor actor, int targetTile, int direction = -1)
    {
        activeManager.GetTargetedTiles(targetTile);
        ActivateSkill(actor.GetAutoSkill(), actor);
        if (direction < 0)
        {
            direction = actor.GetDirection();
        }
        EndTurn(actor, direction);
    }
    public void PlayerTurn(TacticActor actor)
    {
        // Make Sure The Direction Never Changes.
        int startDirection = actor.GetDirection();
        // Attack/Skill.
        int targetTile = PrepareSkill(actor);
        // Skill Was Used.
        if (targetTile == SKILLUSEDALREADY)
        {
            EndTurn(actor, startDirection);
            return;
        }
        bool skillReady = targetTile >= 0;
        List<int> attackable = GetPlayerActorAttackRangeTiles(actor);
        // Healer Vs Normal.
        if (actor.AKHealer())
        {
            attackable.Add(actor.GetLocation());
            List<TacticActor> allies = map.ReturnAlliesInTiles(actor, attackable);
            if (actor.AKAOE())
            {
                if (skillReady)
                {
                    UsePreparedSkill(actor, targetTile, startDirection);
                    return;
                }
                // Else Heal All Allies In Range.
                for (int i = 0; i < allies.Count; i++)
                {
                    allies[i].Heal(actor.GetAttack());
                }
            }
            else
            {
                // Find Lowest Health Ally In Range.
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
                        UsePreparedSkill(actor, targetTile, startDirection);
                        return;
                    }
                    allies[targetIndex].Heal(actor.GetAttack());
                }
            }
        }
        else
        {
            // Find The Closest Enemy In Range
            List<TacticActor> enemies = map.ReturnEnemiesInTiles(actor, attackable);
            if (actor.AKAOE())
            {
                if (skillReady)
                {
                    UsePreparedSkill(actor, targetTile, startDirection);
                    return;
                }
                // Else Attack All Enemies In Range.
                for (int i = 0; i < enemies.Count; i++)
                {
                    ActorAttacksActor(actor, enemies[i]);
                }
            }
            else
            {
                TacticActor target = map.GetClosestActor(actor, enemies);
                if (target == null)
                {
                    if (skillReady)
                    {
                        UsePreparedSkill(actor, targetTile, startDirection);
                        return;
                    }
                    ActorAttacksActor(actor, target);
                }
            }
        }
        EndTurn(actor, startDirection);
    }
    public virtual void DeathActives(TacticActor actor)
    {
        if (actor.GetHealth() > 0){return;}
        if (actor.GetTeam() == 0)
        {
            actor.ResetRespawnTimer();
            TriggerTrait(actor, "OnDeath");
        }
        // Check On Revives.
        if (aegirRevives > 0 && actor.AutoChessFaction("Aegir"))
        {
            aegirRevives--;
            map.ResurrectActor(actor);
            return;
        }
        if (generalRevives > 0)
        {
            generalRevives--;
            map.ResurrectActor(actor);
            return;
        }
        // Resurrect After Gaining Stacks.
        if (actor.Resurrect())
        {
            map.ResurrectActor(actor);
            ResilientCheckResurrectedAlly(actor);
            return;
        }
        // Only Enemies Have Death Actives.
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
    public virtual void ActivateSkill(string skillName, TacticActor actor = null)
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
    protected virtual void ActorAttacksActor(TacticActor attacker, TacticActor defender)
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
