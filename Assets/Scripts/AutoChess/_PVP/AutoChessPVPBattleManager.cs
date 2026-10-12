using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// In charge of PVP battles: reset map -> load in actors/factions/equip/etc. -> loop turns -> end battle -> deal damage to loser
public class AutoChessPVPBattleManager : AutoBattleManager
{
    public AutoChessDataManager leftTeam;
    public List<AutoActor> leftTeamActors;
    public AutoChessDataManager rightTeam;
    public List<AutoActor> rightTeamActors;
    public RNGUtility RNG;
    public void SetTeams(AutoChessDataManager team1, AutoChessDataManager team2)
    {
        leftTeam = team1;
        rightTeam = team2;
    }
    public override void GainFactionStacks(string faction, int stackAmount, int team = 0)
    {
        if (team == 0)
        {
            leftTeam.factionData.GainFactionStacks(faction, stackAmount);
        }
        else if (team == 1)
        {
            rightTeam.factionData.GainFactionStacks(faction, stackAmount);
        }
    }
    public override void TriggerTrait(TacticActor actor, string timing, TacticActor otherActor = null)
    {
        AutoChessTrait trait = actor.autoChessTrait;
        if (trait.timing == timing)
        {
            if (!instantBattle)
            {
                map.UpdateCombatLog(actor.GetPersonalName() + "'s Trait Activates");
            }
            // Make Sure You Give Trait Stacks To The Correct Team.
            if (actor.GetTeam() == 0)
            {
                factionManager.SetDataManager(leftTeam);
            }
            else
            {
                factionManager.SetDataManager(rightTeam);
            }
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
    public int yanDragon2 = 0;
    public int yanDragonAttack2 = 0;
    public int turretActive2 = 0;
    public int aegirRevives2 = 0;
    public int generalRevives2 = 0;
    public int kjeragWindCD2 = -1;
    public void ApplyKjeragWind(TacticActor actor)
    {
        bool replaced = actor.ReplaceStatus("Cold", "Frozen", true);
        replaced = actor.ReplaceStatus("Wet", "Frozen", true) || replaced;
        if (!replaced)
        {
            actor.AddStatus("Cold", 3);
        }
    }
    public void CheckDefeatedActors(int team = 0)
    {
        for (int i = 0; i < map.battlingActors.Count; i++)
        {
            if (map.battlingActors[i].GetTeam() != team || map.battlingActors[i].GetHealth() > 0){continue;}
            if (team == 0)
            {
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
            else
            {
                if (aegirRevives2 > 0 && map.battlingActors[i].AutoChessFaction("Aegir"))
                {
                    aegirRevives2--;
                    map.ResurrectActor(map.battlingActors[i]);
                    continue;
                }
                if (generalRevives2 > 0)
                {
                    generalRevives2--;
                    map.ResurrectActor(map.battlingActors[i]);
                    continue;
                }
            }
        }
    }
    public override void DeathActives(TacticActor actor)
    {
        if (actor.GetHealth() > 0){return;}
        TriggerTrait(actor, "OnDeath");
        List<string> deathActives = new List<string>(actor.GetDeathActives());
        for (int i = 0; i < deathActives.Count; i++)
        {
            if (deathActives[i].Length <= 0) { continue; }
            activeManager.SetSkillFromName(deathActives[i], actor);
            activeManager.GetTargetedTiles(actor.GetLocation());
            ActivateSkill(deathActives[i], actor);
        }
        // Check On Revives.
        if (actor.Resurrect())
        {
            map.ResurrectActor(actor);
            ResilientCheckResurrectedAlly(actor);
            return;
        }
        if (actor.GetTeam() == 0)
        {
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
        }
        else if (actor.GetTeam() == 1)
        {
            if (aegirRevives2 > 0 && actor.AutoChessFaction("Aegir"))
            {
                aegirRevives2--;
                map.ResurrectActor(actor);
                return;
            }
            if (generalRevives2 > 0)
            {
                generalRevives2--;
                map.ResurrectActor(actor);
                return;
            }
        }
    }
    // MAP/Basic Stuff.
    public void InitializeBattleState()
    {
        if (battleUI != null)
        {
            battleUI.StartBattle();
        }
        turretActive = 0;
        yanDragon = 0;
        yanDragonAttack = 0;
        aegirRevives = 0;
        generalRevives = 0;
        kjeragWindCD = -1;
        turretActive2 = 0;
        yanDragon2 = 0;
        yanDragonAttack2 = 0;
        aegirRevives2 = 0;
        generalRevives2 = 0;
        kjeragWindCD2 = -1;
        currentRound = 1;
        actorMaker.ResetID();
        // Reset The Map.
        map.ForceStart();
        // Maybe Later If Different Teams Have Different Maps Then Randomize This.
        map.SetMapInfo(leftTeam.GetMapTiles());
        map.SetTerrainEffectTiles(leftTeam.GetMapTerrain());
        map.SetWeather(WEATHERS[RNG.SeedRange(0, WEATHERS.Count)]);
        map.SetTime(TIMES[RNG.SeedRange(0, TIMES.Count)]);
        map.InitializeCombatLog();
        map.InitializeDamageTracker();
    }
    public void InitializeActors()
    {
        leftTeamActors.Clear();
        rightTeamActors.Clear();
        List<string> fieldActors = leftTeam.GetFieldActorData();
        for (int i = 0; i < fieldActors.Count; i++)
        {
            if (fieldActors[i].Length <= 0){continue;}
            // Track The Actors.
            leftTeamActors.Add(actorMaker.CreateActorOnTeam(fieldActors[i], 0));
        }
        // Equipment can grant factions so do that first.
        for (int i = 0; i < leftTeamActors.Count; i++)
        {
            actorMaker.ApplyAutoChessEquipmentEffects(leftTeamActors[i], leftTeamActors);
        }
        factionEffectManager.ApplyFactionEffects(leftTeamActors, leftTeam.factionData);
        for (int i = 0; i < leftTeamActors.Count; i++)
        {
            actorMaker.ReorganizeActorPassives(leftTeamActors[i]);
            // Apply Start Battle Effects.
            effectManager.StartBattle(leftTeamActors[i], map);
        }
        fieldActors = rightTeam.GetFieldActorData();
        for (int i = 0; i < fieldActors.Count; i++)
        {
            if (fieldActors[i].Length <= 0){continue;}
            // Track The Actors.
            rightTeamActors.Add(actorMaker.CreateActorOnTeam(fieldActors[i], 1));
        }
        for (int i = 0; i < rightTeamActors.Count; i++)
        {
            actorMaker.ApplyAutoChessEquipmentEffects(rightTeamActors[i], rightTeamActors);
        }
        factionEffectManager.ApplyFactionEffects(rightTeamActors, rightTeam.factionData);
        for (int i = 0; i < rightTeamActors.Count; i++)
        {
            actorMaker.ReorganizeActorPassives(rightTeamActors[i]);
            // Apply Start Battle Effects.
            effectManager.StartBattle(rightTeamActors[i], map);
        }
        // ===== INTERLEAVED REGISTRATION =====
        int maxCount = Mathf.Max(leftTeamActors.Count, rightTeamActors.Count);
        for (int i = 0; i < maxCount; i++)
        {
            if (i < leftTeamActors.Count)
            {
                map.AddActorToBattle(leftTeamActors[i]);
            }
            if (i < rightTeamActors.Count)
            {
                map.AddActorToBattle(rightTeamActors[i]);
            }
        }
    }
    public void InitializeFactionEffects()
    {
        // Track special faction effects here: Aegir/Yan/Kjerag
        if (leftTeam.factionData.GetCountOfFaction("Kjerag") >= 6)
        {
            kjeragWindCD = (Mathf.Max(1, 6 / Mathf.Max(1, (int.Parse(leftTeam.factionData.GetStacksOfFaction("Kjerag")) / 100))));
        }
        if (leftTeam.factionData.GetCountOfFaction("Yan") >= 6)
        {
            yanDragon = 1;
            int totalYanAttack = 0;
            int totalYanHealth = 0;
            for (int i = 0; i < leftTeamActors.Count; i++)
            {
                if (leftTeamActors[i].AutoChessFaction("Yan"))
                {
                    totalYanHealth += leftTeamActors[i].GetBaseHealth();
                    totalYanAttack += leftTeamActors[i].GetBaseAttack();
                }
            }
            yanDragonAttack = totalYanAttack * 30 / 100;
            int dragonLocation = map.EmptyStartingTileByEdge(3);
            AutoActor yanDragonActor = actorMaker.CreateActorByName("YanDragon", 0, dragonLocation);
            yanDragonActor.UpdateBaseAttack(yanDragonAttack);
            yanDragonActor.UpdateBaseHealth(totalYanHealth * 30 / 100, false);
            actorMaker.ReorganizeActorPassives(yanDragonActor);
            map.AddActorToBattle(yanDragonActor);
        }
        if (leftTeam.factionData.FactionActive("Aegir"))
        {
            aegirManager.ApplyAegirFactionEffect(map.battlingActors, map, this);
            if (leftTeam.factionData.GetCountOfFaction("Aegir") >= 5)
            {
                aegirRevives = 2;
            }
        }
        // Track special right team faction effects here: Aegir/Yan/Kjerag
        if (rightTeam.factionData.GetCountOfFaction("Kjerag") >= 6)
        {
            kjeragWindCD2 = (Mathf.Max(1, 6 / Mathf.Max(1, (int.Parse(rightTeam.factionData.GetStacksOfFaction("Kjerag")) / 100))));
        }
        if (rightTeam.factionData.GetCountOfFaction("Yan") >= 6)
        {
            yanDragon2 = 1;
            int totalYanAttack = 0;
            int totalYanHealth = 0;
            for (int i = 0; i < rightTeamActors.Count; i++)
            {
                if (rightTeamActors[i].AutoChessFaction("Yan"))
                {
                    totalYanHealth += rightTeamActors[i].GetBaseHealth();
                    totalYanAttack += rightTeamActors[i].GetBaseAttack();
                }
            }
            yanDragonAttack2 = totalYanAttack * 30 / 100;
            int dragonLocation2 = map.EmptyStartingTileByEdge(1);
            AutoActor yanDragonActor2 = actorMaker.CreateActorByName("YanDragon", 1, dragonLocation2);
            yanDragonActor2.UpdateBaseAttack(yanDragonAttack2);
            yanDragonActor2.UpdateBaseHealth(totalYanHealth * 30 / 100, false);
            actorMaker.ReorganizeActorPassives(yanDragonActor2);
            map.AddActorToBattle(yanDragonActor2);
        }
        if (rightTeam.factionData.FactionActive("Aegir"))
        {
            aegirManager.ApplyAegirFactionEffect(map.battlingActors, map, this, 1);
            if (rightTeam.factionData.GetCountOfFaction("Aegir") >= 5)
            {
                aegirRevives2 = 2;
            }
        }
    }
    public override void StartBattle()
    {
        InitializeBattleState();
        InitializeActors();
        InitializeFactionEffects();
        StartRound();
    }
    public override void RespawnPhase()
    {
        // Update Any Dead Allies That Are Ready And Have Open Tiles.
        List<TacticActor> defeatedActors = map.GetDefeatedActors();
        for (int i = 0; i < defeatedActors.Count; i++)
        {
            if (defeatedActors[i].ReadyToRespawn())
            {
                // Check If The Tile Is Open.
                if (map.GetActorOnTile(defeatedActors[i].GetLocation()) != null){continue;}
                // Revive Them.
                TacticActor respawnedActor = defeatedActors[i];
                respawnedActor.ResetRespawnTimer();
                map.ResurrectActor(respawnedActor);
                ResilientCheckResurrectedAlly(respawnedActor);
            }
        }
    }
    public override void StartRound()
    {
        map.NextRound();
        if (kjeragWindCD > 0 && currentRound % kjeragWindCD == 0)
        {
            List<TacticActor> team1 = map.AllTeamMembers(1);
            for (int i = 0; i < team1.Count; i++)
            {
                ApplyKjeragWind(team1[i]);
            }
        }
        if (kjeragWindCD2 > 0 && currentRound % kjeragWindCD2 == 0)
        {
            List<TacticActor> team0 = map.AllTeamMembers(0);
            for (int i = 0; i < team0.Count; i++)
            {
                ApplyKjeragWind(team0[i]);
            }
        }
        CheckDefeatedActors();
        CheckDefeatedActors(1);
        map.RemoveActorsFromBattle();
        map.SetRound(currentRound);
        roundActors = new List<TacticActor>(map.battlingActors);
        if (!instantBattle)
        {
            StartCoroutine(BattleLoop(roundActors));
        }
        else
        {
            InstantBattleLoop(roundActors);
        }
    }
    public void InstantBattleLoop(List<TacticActor> roundActors)
    {
        for (int i = 0; i < roundActors.Count; i++)
        {
            ActorStartTurn(roundActors[i]);
        }
        EndRound();
    }
    public override void ActorStartTurn(TacticActor actor)
    {
        map.AddNewCombatLog();
        if (!instantBattle)
        {
            map.UpdateCombatLog(actor.GetPersonalName() + "'s Turn");
        }
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
        // If Stunned/Sleeping Then Stop.
        if (actor.StatusExists("Stun"))
        {
            map.UpdateCombatLog(actor.GetPersonalName() + " is stunned");
            EndTurn(actor);
            return;
        }
        else if (actor.GetSleeping())
        {
            map.UpdateCombatLog(actor.GetPersonalName() + " is asleep");
            EndTurn(actor);
            return;
        }
        PVPTurn(actor, (float) actor.GetActions());
    }
    public override List<int> GetPlayerActorAttackRangeTiles(TacticActor actor)
    {
        int range = actor.GetAttackRange();
        // If You Are Not AOE Then You Get A Circle, Since Turning Direction Is A Free Action.
        string rangeType = actor.GetAutoChessAttackRangeShape(true);
        int location = actor.GetLocation();
        int direction = actor.GetDirection();
        int selectedTile = map.mapUtility.PointInDirection(location, direction, map.mapSize);
        List<int> rangeTiles = map.mapUtility.GetPVPAutoActorAttackTilesByShapeSpan(selectedTile, rangeType, range, map.mapSize, location);
        return rangeTiles;
    }
    protected bool SafeMoveAction(TacticActor actor, int tile)
    {
        if (tile < 0){return false;}
        if (map.mapInfo[tile] != "Pit" && map.GetActorOnTile(tile) == null)
        {
            map.AIMoveActorToTile(actor, tile);
            return true;
        }
        return false;
    }
    // Always Decreases Actions So Can't Infinitely Loop.
    protected void MoveAction(TacticActor actor, float remainingActions)
    {
        if (remainingActions < 1.0f)
        {
            EndTurn(actor);
            return;
        }
        if (actor.StatusExists("Frozen"))
        {
            map.UpdateCombatLog(actor.GetPersonalName() + " is frozen");
            EndTurn(actor);
            return;
        }
        List<int> path = actorAI.FastFindPathToTarget(actor, map, moveManager);
        if (path.Count > 0)
        {
            int tile = path[path.Count - 1];
            int direction = map.mapUtility.DirectionBetweenLocations(actor.GetLocation(), tile, map.mapSize);
            if (!SafeMoveAction(actor, tile))
            {
                int leftDirection = (direction + 5) % 6;
                int leftTile = map.mapUtility.PointInDirection(actor.GetLocation(), leftDirection, map.mapSize);
                if (!SafeMoveAction(actor, leftTile))
                {
                    int rightDirection = (direction + 1) % 6;
                    int rightTile = map.mapUtility.PointInDirection(actor.GetLocation(), rightDirection, map.mapSize);
                    SafeMoveAction(actor, rightTile);
                }
            }
        }
        PVPTurn(actor, remainingActions - 1.0f);
    }
    // Kinda Similar To EnemyTurn
    public void PVPTurn(TacticActor actor, float remainingActions = 4.0f)
    {
        if (remainingActions <= 0f)
        {
            EndTurn(actor);
            return;
        }
        // Max 20 Attacks/Round Usually?
        float attackCost = Mathf.Max(0.2f, actor.GetSubAttackActionCostAfterAttackSpeed());
        List<int> attackable = GetPlayerActorAttackRangeTiles(actor);
        int targetTile = PrepareSkill(actor);
        // Skill Used Up Whole Turn Immediately.
        bool skillReady = (targetTile >= 0);
        if (targetTile == SKILLUSEDALREADY)
        {
            EndTurn(actor);
            return;
        }
        // Split Healer Vs AOE Vs Normal
        if (actor.AKHealer())
        {
            List<TacticActor> hurtAllies = map.ReturnHurtAlliesInTiles(actor, attackable);
            // Do Healer Stuff.
            if (actor.AKAOE() && hurtAllies.Count > 0)
            {
                for (int i = 0; i < hurtAllies.Count; i++)
                {
                    hurtAllies[i].Heal(actor.GetAttack());
                }
            }
            else if (hurtAllies.Count <= 0)
            {
                MoveAction(actor, remainingActions);
                return;
            }
            else if (!actor.AKAOE() && hurtAllies.Count > 0)
            {
                TacticActor healTarget = map.FindActorByStat(hurtAllies, "Health", false);
                healTarget.Heal(actor.GetAttack());
            }
            PVPTurn(actor, remainingActions - attackCost);
            return;
        }
        List<TacticActor> enemies = map.ReturnEnemiesInTiles(actor, attackable);
        // AOE Attack
        if (actor.AKAOE())
        {
            // Attack
            if (enemies.Count > 0)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    ActorAttacksActor(actor, enemies[i]);
                }
            }
            // Move
            else
            {
                MoveAction(actor, remainingActions);
                return;
            }
            PVPTurn(actor, remainingActions - attackCost);
            return;
        }
        // Normal.
        if (enemies.Count > 0)
        {
            if (skillReady)
            {
                // Ends The Turn.
                UsePreparedSkill(actor, targetTile);
                return;
            }
            TacticActor target = map.GetClosestActor(actor, enemies);
            // Update Targets Based On Attack Shape + Span.
            List<TacticActor> targets = map.GetActorsOnTiles(map.mapUtility.GetTilesByShapeSpan(target.GetLocation(), actor.GetBasicAttackShape(), actor.GetBasicAttackSpan(), map.mapSize, actor.GetLocation()));
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].GetTeam() == actor.GetTeam()){continue;}
                ActorAttacksActor(actor, targets[i]);
            }
        }
        // Move
        else
        {
            MoveAction(actor, remainingActions);
            return;
        }
        PVPTurn(actor, remainingActions - attackCost);
    }
    public override int EndBattle()
    {
        int winningTeam = -1;
        // 2 Means Tie.
        if (currentRound > 30){return 2;}
        List<TacticActor> actors = map.battlingActors;
        List<int> teams = new List<int>();
        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i].GetHealth() <= 0){continue;}
            if (teams.IndexOf(actors[i].GetTeam()) < 0)
            {
                teams.Add(actors[i].GetTeam());
            }
        }
        if (teams.Count == 1)
        {
            return teams[0];
        }
        if (teams.Count == 0)
        {
            return 2;
        }
        return winningTeam;
    }
    public StatDatabase unitRarity;
    public override void EndRound()
    {
        int endBattleResult = EndBattle();
        if (endBattleResult >= 0)
        {
            int unitDamage = 0;
            for (int i = 0; i < map.battlingActors.Count; i++)
            {
                if (map.battlingActors[i].GetHealth() <= 0 || map.battlingActors[i].GetTeam() != endBattleResult){continue;}
                unitDamage += int.Parse(unitRarity.ReturnValue(map.battlingActors[i].GetSpriteName()));
                unitDamage += map.battlingActors[i].GetAutoChessLevel() / 2;
            }
            // 2 = TIE
            if (endBattleResult == 2)
            {
                if (!instantBattle)
                {
                    map.UpdateCombatLog("TIE");
                }
                // Both Lose
                leftTeam.LoseHealth((leftTeam.GetRound() + rightTeam.GetLevel()) / 2);
                leftTeam.NewRound(1);
                rightTeam.LoseHealth((rightTeam.GetRound() + leftTeam.GetLevel()) / 2);
                rightTeam.NewRound(1);
            }
            // 0 = Left Wins
            else if (endBattleResult == 0)
            {
                if (!instantBattle)
                {
                    map.UpdateCombatLog("Left Team Wins");
                }
                leftTeam.NewRound(0);
                rightTeam.LoseHealth((rightTeam.GetRound() + leftTeam.GetLevel()) / 2 + unitDamage);
                rightTeam.NewRound(1);
            }
            // 1 = Right Wins
            else if (endBattleResult == 1)
            {
                if (!instantBattle)
                {
                    map.UpdateCombatLog("Right Team Wins");
                }
                leftTeam.LoseHealth((leftTeam.GetRound() + rightTeam.GetLevel()) / 2 + unitDamage);
                leftTeam.NewRound(1);
                rightTeam.NewRound(0);
            }
            // Move To Next Round + Save.
            leftTeam.Save();
            rightTeam.Save();
            if (battleUI != null && !instantBattle)
            {
                battleUI.EndBattle();
            }
            return;
        }
        currentRound++;
        RespawnPhase();
        StartRound();
    }
    // DON'T USE ATTACKSPEED HERE, FOR THIS GAME MODE ATKSPD AFFECTS ATK ACTION COST INSTEAD.
    protected override void ActorAttacksActor(TacticActor attacker, TacticActor defender)
    {
        TriggerTrait(attacker, "OnAttack");
        if (!instantBattle)
        {
            map.UpdateCombatLog(attacker.GetPersonalName() + " attacks " + defender.GetPersonalName());
        }
        attackManager.ActorAttacksActor(attacker, defender, map, attacker.GetBasicAttackMultiplier(), attacker.GetBasicAttackDamageType());
    }
    public override void ActivateSkill(string skillName, TacticActor actor = null)
    {
        if (actor == null){return;}
        // Check Traits For Both You And The Actor Behind You.
        List<TacticActor> behindActors = map.GetActorsBehindActor(actor);
        for (int i = 0; i < behindActors.Count; i++)
        {
            if (behindActors[i] != null && behindActors[i].GetTeam() == actor.GetTeam())
            {
                TriggerTrait(behindActors[i], "OnForwardSkill", actor);
            }
        }
        TriggerTrait(actor, "OnSkill");
        // Don't Bother With Costs In AutoMode.
        if (!instantBattle)
        {
            map.UpdateCombatLog(actor.GetPersonalName() + " uses " + skillName);
        }
        actor.SetCurrentEnergy(0);
        activeManager.ActivateSkill(false);
        map.UpdateMap();
    }
}
