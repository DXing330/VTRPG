using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveManager : MonoBehaviour
{
    // Basically everything in the battle needs to know the map state at all times.
    public BattleMap map;
    public GeneralUtility utility;
    public MagicSpell magicSpell;
    public TriggeredSkillResolver triggeredSkillResolver;
    public int triggeredSkillDepthLimit = 10000;
    public int triggeredSkillStackDepthLimit = 128;
    protected int triggeredSkillDepth = 0;
    protected int triggeredSkillEnergyBeforeCast = 0;
    protected int triggeredSkillActionsBeforeCast = 0;
    public void SetSpell(string spellInfo)
    {
        magicSpell.LoadSkillFromString(spellInfo);
    }
    public void ActivateSpell(BattleManager battle)
    {
        skillUser.UseMana(magicSpell.ReturnManaCost(skillUser));
        skillUser.SpendAction(magicSpell.GetActionCost());
        List<TacticActor> targets = battle.map.GetActorsOnTiles(targetedTiles);
        List<string> effects = magicSpell.GetAllEffects();
        for (int i = 0; i < effects.Count; i++)
        {
            ApplyActiveEffects(battle, targets, effects[i], magicSpell.GetSpecificsAt(i), magicSpell.GetPowerAt(i), magicSpell.GetSelectedTile(), true);
        }
    }
    public ActiveSkill active;
    public PassiveSkill passive;
    public TacticActor skillUser;
    public void SetSkillUser(TacticActor user){skillUser = user;}
    public StatDatabase activeData;
    // 0 = off, 1 = on
    public int state;
    public List<int> targetableTiles;
    public List<int> targetedTiles;

    public bool SkillExists(string skillName)
    {
        if (skillName.Length <= 0){return false;}
        return activeData.KeyExists(skillName);
    }

    public void SetSkillFromName(string skillName, TacticActor newSkillUser)
    {
        skillUser = newSkillUser;
        string sData = activeData.ReturnValue(skillName);
        if (sData == "")
        {
            sData = skillName;
        }
        active.LoadSkillFromString(sData, skillUser);
    }

    protected void ResetTargetableTiles()
    {
        targetableTiles.Clear();
        targetedTiles.Clear();
    }

    public List<int> GetTargetableTiles(int start, MapPathfinder pathfinder, bool spell = false)
    {
        string shape = active.GetRangeShape();
        if (spell){ shape = magicSpell.GetRangeShape(); }
        targetableTiles = new List<int>(GetTiles(start, shape, pathfinder, true, spell));
        if (targetableTiles.Count <= 0) { targetableTiles.Add(start); }
        return targetableTiles;
    }

    public List<int> ReturnTargetableTiles(){return targetableTiles;}

    public void ResetTargetedTiles(){targetedTiles.Clear();}

    public void CheckIfSingleTargetableTile()
    {
        if (targetableTiles.Count == 1)
        {
            targetedTiles = new List<int>(targetableTiles);
        }
    }

    public List<int> GetTargetedTiles(int start, MapPathfinder pathfinder, bool spellCast = false)
    {
        active.SetSelectedTile(start);
        string shape = active.GetShape();
        if (spellCast)
        {
            magicSpell.SetSelectedTile(start);
            shape = magicSpell.GetShape();
        }
        targetedTiles = new List<int>(GetTiles(start, shape, pathfinder, false, spellCast));
        if (!spellCast)
        {
            if (active.GetShape() == "Circle" || active.GetShape() == "None")
            {
                targetedTiles.Add(start);
            }
        }
        else
        {
            if (magicSpell.GetShape() == "Circle" || magicSpell.GetShape() == "None")
            {
                targetedTiles.Add(start);
            }
        }
        targetedTiles = targetedTiles.Distinct().ToList();
        return targetedTiles;
    }

    public List<int> ReturnTargetedTiles(){return targetedTiles;}

    public bool ExistTargetedTiles(){return targetedTiles.Count > 0;}

    protected List<int> GetTiles(int startTile, string shape, MapPathfinder pathfinder, bool targetable = true, bool spellCast = false)
    {
        int range = active.GetRange(skillUser, map);
        if (spellCast){ range = magicSpell.GetRange(skillUser, map); }
        if (!targetable)
        {
            range = active.GetSpan(skillUser, map);
            if (spellCast)
            {
                range = magicSpell.GetSpan(skillUser, map);
            }
        }
        int direction = pathfinder.DirectionBetweenLocations(skillUser.GetLocation(), startTile);
        return pathfinder.mapUtility.GetTilesByShapeSpan(startTile, shape, range, pathfinder.mapSize, skillUser.GetLocation());
    }

    protected void ApplyActiveEffects(BattleManager battle, List<TacticActor> targets, string effect, string specifics, int power, int selectedTile = -1, bool spellCast = false)
    {
        int targetTile = -1;
        string powerString = power.ToString();
        // There are some effects that naturally target a specific group of actors.
        if (effect.Contains("AllSpritesEquals"))
        {
            string[] allSpriteDetails = effect.Split("Equals");
            string specificSprite = allSpriteDetails[1];
            targets = battle.map.AllActorsBySprite(specificSprite);
            active.AffectActors(targets, specifics, powerString, 1);
            return;
        }
        if (effect.Contains("AllSpeciesEquals"))
        {
            string[] allSpeciesDetails = effect.Split("Equals");
            string specificSpecies = allSpeciesDetails[1];
            targets = battle.map.AllActorsBySpecies(specificSpecies);
            active.AffectActors(targets, specifics, powerString, 1);
            return;
        }
        switch (effect)
        {
            case "TriggerSkill":
                ResolveTriggeredSkill(battle, specifics);
                return;
            case "Weather":
                battle.map.SetWeather(specifics);
                return;
            case "Escape":
                if (!battle.map.ActorCanEscape(skillUser)){return;}
                battle.map.ActorEscapesBattle(skillUser);
                return;
            case "Time":
                battle.map.SetTime(specifics);
                return;
            case "Tile":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    battle.map.ChangeTerrain(targetedTiles[i], specifics);
                }
                return;
            case "Border":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    battle.map.ChangeBorder(targetedTiles[i], skillUser.GetDirection(), specifics);
                }
                return;
            case "AllBorders":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    battle.map.ChangeAllBorders(targetedTiles[i], specifics);
                }
                return;
            case "Attack+Tile":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    battle.map.ChangeTerrain(targetedTiles[i], specifics);
                }
                for (int i = 0; i < targets.Count; i++)
                {
                    battle.attackManager.ActorAttacksActor(skillUser, targets[i], battle.map, power);
                }
                return;
            case "BreakSummonLink":
                for (int i = 0; i < targets.Count; i++)
                {
                    targets[i].ResetSummonedBy();
                }
                return;
            case "Summon":
                // Check if selected tile is free.
                if (battle.map.GetActorOnTile(selectedTile) == null)
                {
                    // Create a new actor on that location on the same team.
                    battle.SpawnAndAddActor(selectedTile, specifics, skillUser.GetTeam(), skillUser);
                }
                return;
            case "RespawnSummon":
                // Try to respawn at the initial location.
                selectedTile = skillUser.GetInitialLocation();
                if (battle.map.GetActorOnTile(selectedTile) == null)
                {
                    // Create a new actor on that location on the same team.
                    battle.SpawnAndAddActor(selectedTile, specifics, skillUser.GetTeam(), skillUser);
                }
                return;
            case "TributeSummon":
                if (targetedTiles.Count <= 0){return;}
                TacticActor tributeActor = battle.map.GetActorOnTile(targetedTiles[0]);
                if (tributeActor == null || tributeActor.GetTeam() != skillUser.GetTeam()){return;}
                // Create a new actor on that location on the same team.
                battle.SpawnAndAddActor(targetedTiles[0], specifics, skillUser.GetTeam(), skillUser);
                // Kill the targeted ally as tribute.
                tributeActor.MarkSacrificed();
                tributeActor.SetCurrentHealth(0);
                tributeActor.ResetActions();
                return;
            case "MassSummon":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    if (battle.map.GetActorOnTile(targetedTiles[i]) == null)
                    {
                        battle.SpawnAndAddActor(targetedTiles[i], specifics, skillUser.GetTeam(), skillUser);
                    }
                }
                return;
            case "RandomSummon":
                // Check if selected tile is free.
                if (battle.map.GetActorOnTile(selectedTile) == null)
                {
                    // Create a new actor on that location on the same team.
                    // Pick a random actor from the specifics list.
                    string[] randomSummon = specifics.Split(",");
                    battle.SpawnAndAddActor(selectedTile, randomSummon[UnityEngine.Random.Range(0, randomSummon.Length)], skillUser.GetTeam(), skillUser);
                }
                return;
            case "MassRandomSummon":
                string[] randomPool = specifics.Split(",");
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    if (battle.map.GetActorOnTile(targetedTiles[i]) == null)
                    {
                        battle.SpawnAndAddActor(targetedTiles[i], randomPool[UnityEngine.Random.Range(0, randomPool.Length)], skillUser.GetTeam(), skillUser);
                    }
                }
                return;
            case "Revive":
                battle.map.ReviveDefeatedActorsBySprite(specifics);
                return;
            case "Summon Enemy":
                // Check if selected tile is free.
                if (battle.map.GetActorOnTile(selectedTile) == null)
                {
                    // Create a new actor on that location on the opposite team.
                    battle.SpawnAndAddActor(selectedTile, specifics, (skillUser.GetTeam()+1) % 2);
                }
                return;
            case "Teleport":
                // Check if selected tile is free.
                if (battle.map.GetActorOnTile(targetedTiles[0]) == null)
                {
                    skillUser.SetLocation(targetedTiles[0]);
                    battle.map.UpdateActors();
                }
                return;
            case "TeleportTarget":
                if (battle.map.GetActorOnTile(targetedTiles[0]) == null && skillUser.GetTarget() != null)
                {
                    skillUser.GetTarget().SetLocation(targetedTiles[0]);
                    battle.map.UpdateActors();
                }
                return;
            case "Move+Tile":
                // Check if selected tile is free.
                if (battle.map.GetActorOnTile(targetedTiles[0]) == null)
                {
                    battle.map.ChangeTerrain(skillUser.GetLocation(), specifics);
                    skillUser.SetLocation(targetedTiles[0]);
                    battle.map.ChangeTerrain(skillUser.GetLocation(), specifics);
                    battle.map.UpdateMap();
                }
                return;
            // The teleport behind you skill.
            case "Teleport+Attack":
                targetTile = targetedTiles[0];
                TacticActor targetActor = battle.map.GetActorOnTile(targetTile);
                if (targetActor == null) { return; }
                if (battle.moveManager.TeleportToTarget(skillUser, targetActor, specifics, battle.map))
                {
                    battle.attackManager.ActorAttacksActor(skillUser, targetActor, battle.map, power);
                }
                return;
            case "Attack+Grapple":
                if (targets.Count <= 0) { return; }
                // Grapple the first target if there are multiple.
                skillUser.GrappleActor(targets[0]);
                for (int i = 0; i < targets.Count; i++)
                {
                    for (int j = 0; j < int.Parse(specifics); j++)
                    {
                        battle.attackManager.ActorAttacksActor(skillUser, targets[i], battle.map, power);
                    }
                }
                return;
            case "Grapple":
                if (targets.Count <= 0) { return; }
                // Grapple the first target if there are multiple.
                skillUser.GrappleActor(targets[0]);
                return;
            case "ThrowGrappled":
                if (!skillUser.Grappling()){return;}
                targetTile = targetedTiles[0];
                // Check if there is anyone there.
                if (battle.map.GetActorOnTile(targetTile) != null)
                {
                    // If so then damage both thrown and thrown into.
                    battle.moveManager.DisplaceDamage(skillUser.GetGrappledActor(), Mathf.Max(skillUser.GetWeight(), 1), battle.map, targetTile, true, battle.map.GetActorOnTile(targetTile));
                    // Bounce the thrown onto the nearest empty tile.
                    skillUser.GetGrappledActor().SetLocation(battle.map.GetClosestEmptyTile(battle.map.GetActorOnTile(targetTile)));
                }
                // Else move the thrown into the tile.
                else
                {
                    skillUser.GetGrappledActor().SetLocation(targetTile);
                }
                battle.map.UpdateMap();
                skillUser.ReleaseGrapple();
                return;
            case "Ingest":
                if (skillUser.Grappling())
                {
                    skillUser.GetGrappledActor().TakeDamage(skillUser.GetBaseHealth());
                }
                return;
            case "SwapRelease":
                if (skillUser.Grappling())
                {
                    int prevLocation = skillUser.GetLocation();
                    skillUser.SetLocation(skillUser.GetGrappledActor().GetLocation());
                    skillUser.GetGrappledActor().SetLocation(prevLocation);
                    skillUser.ReleaseGrapple();
                    battle.map.UpdateActors();
                }
                return;
            case "Attack":
                if (targets.Count <= 0) { return; }
                for (int i = 0; i < targets.Count; i++)
                {
                    for (int j = 0; j < int.Parse(specifics); j++)
                    {
                        battle.attackManager.ActorAttacksActor(skillUser, targets[i], battle.map, power);
                    }
                }
                return;
            case "Attack+Drain":
                if (targets.Count <= 0) { return; }
                for (int i = 0; i < targets.Count; i++)
                {
                    battle.attackManager.ActorAttacksActor(skillUser, targets[i], battle.map, power);
                    skillUser.UpdateHealth(Mathf.Max(1, skillUser.GetAttack() - targets[i].GetDefense()), false);
                }
                return;
            case "Attack+Status":
                if (targets.Count <= 0) { return; }
                for (int i = 0; i < targets.Count; i++)
                {
                    battle.attackManager.ActorAttacksActor(skillUser, targets[i], battle.map);
                    active.AffectActor(targets[i], "Status", specifics, power);
                }
                return;
            case "Attack+MentalState":
                if (targets.Count <= 0) { return; }
                for (int i = 0; i < targets.Count; i++)
                {
                    battle.attackManager.ActorAttacksActor(skillUser, targets[i], battle.map);
                    if (specifics == "Charmed" || specifics == "Taunted")
                    {
                        targets[i].SetTarget(skillUser);
                    }
                    active.AffectActor(targets[i], "MentalState", specifics, power);
                }
                return;
            case "Attack+Displace":
                if (targets.Count <= 0) { return; }
                for (int i = 0; i < targets.Count; i++)
                {
                    battle.attackManager.ActorAttacksActor(skillUser, targets[i], battle.map);
                }
                battle.moveManager.DisplaceSkill(skillUser, targetedTiles, specifics, power, battle.map);
                return;
            case "Attack+Move":
                if (targets.Count <= 0) { return; }
                for (int i = 0; i < targets.Count; i++)
                {
                    battle.attackManager.ActorAttacksActor(skillUser, targets[i], battle.map);
                }
                battle.moveManager.MoveSkill(skillUser, specifics, power, battle.map);
                return;
            case "Move":
                // Change your direction to face the targeted tile.
                skillUser.SetDirection(map.DirectionBetweenActorAndLocation(skillUser, targetedTiles[0]));
                battle.moveManager.MoveSkill(skillUser, specifics, power, battle.map);
                return;
            case "Move+Attack":
                // Move to the tile selected.
                int prevTile = skillUser.GetLocation();
                targetTile = targetedTiles[0];
                if (battle.map.GetActorOnTile(targetTile) == null)
                {
                    skillUser.SetLocation(targetTile);
                    // Update the direction to the moving direction.
                    skillUser.SetDirection(battle.moveManager.DirectionBetweenLocations(prevTile, targetTile));
                    battle.map.UpdateActors();
                }
                else { return; }
                // Check if an actor is on the specified tile(s).
                int attackTargetTile = battle.moveManager.PointInDirection(skillUser.GetLocation(), skillUser.GetDirection());
                if (battle.map.GetActorOnTile(attackTargetTile) != null)
                {
                    battle.attackManager.ActorAttacksActor(skillUser, battle.map.GetActorOnTile(attackTargetTile), battle.map);
                }
                return;
            case "MoveThrough+Attack":
                targetTile = targetedTiles[0];
                if (battle.map.GetActorOnTile(targetTile) == null)
                {
                    return;
                }
                battle.moveManager.MoveThroughSkill(skillUser, targetTile, battle.map);
                battle.attackManager.ActorAttacksActor(skillUser, battle.map.GetActorOnTile(targetTile), battle.map, power);
                return;
            case "Charge+Attack":
                int startChargeTile = skillUser.GetLocation();
                targetTile = targetedTiles[0];
                // Try to move in straight line to the target.
                List<int> chargePath = battle.moveManager.actorPathfinder.StraightPathToTile(startChargeTile, targetTile);
                if (chargePath.Count <= 0){return;}
                for (int i = 0; i < chargePath.Count; i++)
                {
                    if (battle.map.GetActorOnTile(chargePath[i]) != null)
                    {
                        break;
                    }
                    battle.moveManager.MoveActorToTile(skillUser, chargePath[i], battle.map);
                }
                skillUser.SetDirection(battle.moveManager.DirectionBetweenLocations(startChargeTile, targetTile));
                battle.map.UpdateActors();
                // Check if an actor is on the specified tile(s).
                int chargeInto = battle.moveManager.PointInDirection(skillUser.GetLocation(), skillUser.GetDirection());
                if (battle.map.GetActorOnTile(chargeInto) != null)
                {
                    battle.attackManager.ActorAttacksActor(skillUser, battle.map.GetActorOnTile(chargeInto), battle.map, power);
                }
                return;
            case "Displace":
                battle.moveManager.DisplaceSkill(skillUser, targetedTiles, specifics, power, battle.map);
                return;
            case "TerrainEffect":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    battle.map.ChangeTEffect(targetedTiles[i], specifics);
                }
                return;
            case "DelayedTileEffect":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    battle.map.AddAura(skillUser, targetedTiles[i], specifics, power);
                }
                return;
            case "Attack+TerrainEffect":
                for (int i = 0; i < targets.Count; i++)
                {
                    battle.attackManager.ActorAttacksActor(skillUser, targets[i], battle.map, power);
                }
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    battle.map.ChangeTEffect(targetedTiles[i], specifics);
                }
                return;
            case "Trap":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    battle.interactableMaker.PlaceTrap(battle.map, specifics, targetedTiles[i], skillUser);
                }
                return;
            case "Swap":
                if (targetedTiles.Count <= 0) { return; }
                switch (specifics)
                {
                    case "Location":
                        if (targets.Count <= 0) { break; }
                        battle.map.SwitchActorLocations(targets[0], skillUser);
                        break;
                    case "TerrainEffect":
                        battle.map.SwitchTerrainEffect(targetedTiles[0], skillUser.GetLocation());
                        break;
                    case "Tile":
                        battle.map.SwitchTile(targetedTiles[0], skillUser.GetLocation());
                        break;
                }
                return;
            case "True Attack":
                for (int i = 0; i < targets.Count; i++)
                {
                    battle.attackManager.TrueDamageAttack(skillUser, targets[i], battle.map, power, specifics);
                }
                return;
            // Should go through the attack manager for stab/mastery bonuses.
            case "ElementalAttack":
                if (targets.Count <= 0) { return; }
                for (int i = 0; i < targets.Count; i++)
                {
                    // Do an attack with stab and stuff.
                    battle.attackManager.ActorAttacksActor(skillUser, targets[i], battle.map, power, specifics);
                    // Also apply the elemental damage effects.
                    active.AffectActor(targets[i], specifics + "Damage", skillUser.GetMagicPower().ToString(), 1, battle.map.combatLog);
                    // Also affect the map.
                    battle.map.ElementalAttackOnTile(specifics, targets[i].GetLocation());
                }
                return;
            // Directly does the elemental damage, doesn't need to go through the attack manager.
            case "ElementalDamage":
                for (int i = 0; i < targets.Count; i++)
                {
                    active.AffectActor(targets[i], specifics + "Damage", (power + skillUser.GetMagicPower()).ToString(), 1, battle.map.combatLog);
                    battle.map.ElementalAttackOnTile(specifics, targets[i].GetLocation());
                }
                return;
            case "Flat Attack":
                for (int i = 0; i < targets.Count; i++)
                {
                    battle.attackManager.FlatDamageAttack(skillUser, targets[i], battle.map, int.Parse(specifics));
                }
                return;
            // Remove a random active skill.
            case "Attack+Amnesia":
                for (int i = 0; i < targets.Count; i++)
                {
                    battle.attackManager.ActorAttacksActor(skillUser, targets[i], battle.map, power);
                    for (int j = 0; j < int.Parse(specifics); j++)
                    {
                        targets[i].RemoveRandomActiveSkill();
                    }
                }
                return;
            case "AllAllies":
                // Get all allies from the map.
                targets = battle.map.AllAllies(skillUser);
                active.AffectActors(targets, specifics, powerString, 1);
                return;
            case "AllEnemies":
                targets = battle.map.AllEnemies(skillUser);
                active.AffectActors(targets, specifics, powerString, 1);
                return;
            case "Command":
                for (int i = 0; i < targets.Count; i++)
                {
                    // Only allies will obey your commands.
                    if (targets[i].GetTeam() != skillUser.GetTeam()){continue;}
                    switch (specifics)
                    {
                        default:
                        break;
                        // This is currently only for self targetting support skills.
                        // We can handle attack skill commands later.
                        case "Skill":
                        // Try to make all allies use a certain type of skill.
                        string commandSkill = targets[i].ReturnSkillContainingName(powerString);
                        if (activeData.KeyExists(commandSkill))
                        {
                            string[] commandSkillDetails = activeData.ReturnValue(commandSkill).Split(active.activeSkillDelimiter);
                            active.AffectActor(targets[i], commandSkillDetails[7], commandSkillDetails[8]);
                        }
                        break;
                        case "Attack":
                        if (battle.map.FacingActor(targets[i]))
                        {
                            battle.attackManager.ActorAttacksActor(targets[i], battle.map.ReturnClosestFacingActor(targets[i]), battle.map);
                        }
                        break;
                        case "Forward":
                        // Try to move forward.
                        if (battle.map.FacingEmptyTile(targets[i]))
                        {
                            battle.moveManager.CommandMovement(targets[i], battle.map);
                        }
                        break;
                        case "Backward":
                        // Try to move backward.
                        if (battle.map.FacingEmptyTile(targets[i], false))
                        {
                            battle.moveManager.CommandMovement(targets[i], battle.map, false);
                        }
                        break;
                    }
                }
                return;
            case "ChainLightning":
                // Keep track of the targets.
                targets = battle.map.ChainLightningTargets(targetedTiles[0]);
                active.AffectActors(targets, specifics, powerString, 1);
                return;
            case "MapChainLightning":
                // Keep track of the targets.
                targets = battle.map.ChainLightningTargets(targetedTiles[0]);
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] == null){continue;}
                    battle.map.ChangeTile(targets[i].GetLocation(), specifics, powerString);
                }
                return;
            case "Learn":
                for (int i = 0; i < targets.Count; i++)
                {
                    skillUser.AddActiveSkill(targets[i].ReturnMostRecentSkill());
                }
                return;
            case "Teach":
                for (int i = 0; i < targets.Count; i++)
                {
                    skillUser.TeachRandomActive(targets[i]);
                }
                return;
            case "Pain Split":
                int hpPool = skillUser.GetHealth();
                int poolSize = 1;
                for (int i = 0; i < targets.Count; i++)
                {
                    hpPool += targets[i].GetHealth();
                    poolSize++;
                }
                int finalHealth = Mathf.Max(1, hpPool / poolSize);
                skillUser.SetCurrentHealth(finalHealth);
                for (int i = 0; i < targets.Count; i++)
                {
                    targets[i].SetCurrentHealth(finalHealth);
                }
                return;
            case "Aura":
                battle.map.AddAura(skillUser, targetedTiles[0], specifics, power);
                return;
            case "Manaize":
                // Light/Dark is different.
                if (specifics == "Light")
                {
                    if (battle.map.GetTime() == "Day")
                    {
                        skillUser.RestoreMana(power);
                    }
                    return;
                }
                else if (specifics == "Dark")
                {
                    if (battle.map.GetTime() == "Night")
                    {
                        skillUser.RestoreMana(power);
                    }
                    return;
                }
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    // Check if the target tile is of the terrain effect.
                    if (battle.map.GetTerrainEffectOnTile(targetedTiles[i]).Contains(specifics))
                    {
                        // If so absorb the terrain effect to gain mana.
                        battle.map.RemoveTerrainEffectOnTile(targetedTiles[i]);
                        skillUser.RestoreMana(power);
                    }
                }
                return;
            case "SupportWeight":
                for (int i = 0; i < targets.Count; i++)
                {
                    // Grant them your weight and defense.
                    active.AffectActor(targets[i], "TempWeight", skillUser.GetWeight().ToString(), power);
                    active.AffectActor(targets[i], "TempDefense", skillUser.GetDefense().ToString(), power);
                }
                return;
        }
        // Covers status/mental state/amnesia/stat changes/etc.
        active.AffectActors(targets, effect, specifics, power);
    }

    // All Skill Usage Should Go Through Here
    public bool ActivateSkill(BattleManager battle, bool cost = true)
    {
        return ActivateSkillInternal(battle, cost, cost);
    }

    protected bool ActivateSkillInternal(BattleManager battle, bool spendEnergy, bool spendAction)
    {
        if (spendEnergy || spendAction)
        {
            if (!CanPaySkillCost(spendEnergy, spendAction))
            {
                return false;
            }
        }
        if (spendEnergy)
        {
            skillUser.SpendEnergy(active.GetEnergyCost(skillUser, map));
        }
        if (spendAction)
        {
            skillUser.SpendAction(active.GetActionCost(skillUser, map));
        }
        bool temp = skillUser.RemoveTempActive(active.GetSkillName());
        skillUser.UpdateRoundSkillTracker(active.GetSkillName());
        skillUser.ClearNextSkillMods();
        List<TacticActor> targets = battle.map.GetActorsOnTiles(targetedTiles);
        List<string> effects = active.GetAllEffects();
        for (int i = 0; i < effects.Count; i++)
        {
            ApplyActiveEffects(battle, targets, effects[i], active.GetSpecificsAt(i), active.GetPowerAt(i), active.GetSelectedTile());
        }
        passive.ApplyAfterSkillPassives(skillUser, targets, map, active, temp);
        // TODO Trigger After Skill Auras Here.
        battle.map.ApplyAuraEffects(skillUser, "Skill");
        return true;
    }

    protected bool CanPaySkillCost(bool spendEnergy, bool spendAction)
    {
        if (skillUser.GetSilenced()){return false;}
        if (spendEnergy && skillUser.GetEnergy() < active.GetEnergyCost(skillUser, map)){return false;}
        if (spendAction && skillUser.GetActions() < active.GetActionCost(skillUser, map)){return false;}
        return true;
    }

    public bool CheckTriggeredSkillCost(BattleMap map)
    {
        return CanPaySkillCost(true, false);
    }

    protected void ResolveTriggeredSkill(BattleManager battle, string triggerData)
    {
        if (triggeredSkillDepth >= triggeredSkillDepthLimit || triggeredSkillDepth >= triggeredSkillStackDepthLimit)
        {
            if (triggeredSkillResolver != null)
            {
                triggeredSkillResolver.AddDebugMessage("TriggeredSkill stopped"
                    + " | Depth=" + triggeredSkillDepth
                    + " | DepthLimit=" + triggeredSkillDepthLimit
                    + " | StackDepthLimit=" + triggeredSkillStackDepthLimit
                    + " | TriggerData=" + triggerData);
            }
            return;
        }
        ActiveManagerState savedState = SaveState();
        if (triggeredSkillResolver == null)
        {
            triggeredSkillResolver = GetComponent<TriggeredSkillResolver>();
            if (triggeredSkillResolver == null)
            {
                triggeredSkillResolver = gameObject.AddComponent<TriggeredSkillResolver>();
            }
        }
        TriggeredSkillResolver.TriggeredSkillCast triggeredCast;
        triggeredSkillDepth++;
        try
        {
            bool resolved = triggeredSkillResolver.TryResolve(triggerData, skillUser, this, battle, out triggeredCast);
            if (resolved)
            {
                LogTriggeredSkillLoadedDetails("Resolved");
                active.SetSelectedTile(triggeredCast.selectedTile);
                targetedTiles = new List<int>(triggeredCast.targetedTiles);
                triggeredSkillEnergyBeforeCast = skillUser.GetEnergy();
                triggeredSkillActionsBeforeCast = skillUser.GetActions();
                ActivateSkillInternal(battle, true, false);
                LogTriggeredSkillLoadedDetails("AfterCast");
            }
        }
        finally
        {
            triggeredSkillDepth--;
            RestoreState(savedState);
        }
    }

    protected void LogTriggeredSkillLoadedDetails(string label)
    {
        if (triggeredSkillResolver == null){return;}
        triggeredSkillResolver.AddDebugMessage("TriggeredSkill " + label
            + " | Skill=" + active.GetSkillName()
            + " | Effect=" + active.GetEffect()
            + " | Specifics=" + active.GetSpecifics()
            + " | Power=" + active.GetPowerString()
            + " | ScalingField=" + active.GetScalingSpecifics()
            + " | EnergyCost=" + active.GetEnergyCost(skillUser, map)
            + " | ActionCost=" + active.GetActionCost(skillUser, map)
            + " | Energy=" + skillUser.GetEnergy()
            + " | Actions=" + skillUser.GetActions());
        if (label == "AfterCast")
        {
            triggeredSkillResolver.AddDebugMessage("TriggeredSkill CostDelta"
                + " | Energy=" + triggeredSkillEnergyBeforeCast + "->" + skillUser.GetEnergy()
                + " | Actions=" + triggeredSkillActionsBeforeCast + "->" + skillUser.GetActions());
        }
    }

    protected ActiveManagerState SaveState()
    {
        ActiveManagerState state = new ActiveManagerState();
        state.skillUser = skillUser;
        state.skillInfo = new List<string>();
        state.skillInfo.Add(active.GetSkillName());
        state.skillInfo.Add(active.GetSkillType());
        state.skillInfo.Add(active.energyCost);
        state.skillInfo.Add(active.actionCost);
        state.skillInfo.Add(active.range);
        state.skillInfo.Add(active.GetRangeShape());
        state.skillInfo.Add(active.GetShape());
        state.skillInfo.Add(active.span);
        state.skillInfo.Add(active.GetEffect());
        state.skillInfo.Add(active.GetSpecifics());
        state.skillInfo.Add(active.GetPowerString());
        state.skillInfo.Add(active.healthCost);
        state.skillInfo.Add(active.GetScalingSpecifics());
        state.selectedTile = active.GetSelectedTile();
        state.targetableTiles = targetableTiles == null ? new List<int>() : new List<int>(targetableTiles);
        state.targetedTiles = targetedTiles == null ? new List<int>() : new List<int>(targetedTiles);
        return state;
    }

    protected void RestoreState(ActiveManagerState state)
    {
        skillUser = state.skillUser;
        active.skillInfoList = new List<string>(state.skillInfo);
        active.LoadSkill(active.skillInfoList);
        active.RefreshSkillInfo();
        active.SetSelectedTile(state.selectedTile);
        targetableTiles = new List<int>(state.targetableTiles);
        targetedTiles = new List<int>(state.targetedTiles);
    }

    protected class ActiveManagerState
    {
        public TacticActor skillUser;
        public List<string> skillInfo;
        public int selectedTile;
        public List<int> targetableTiles;
        public List<int> targetedTiles;
    }

    public bool CheckSkillCost(BattleMap map)
    {
        return active.Activatable(skillUser, map);
    }

    public bool CheckSpellCost(BattleMap map)
    {
        return magicSpell.Activatable(skillUser, map);
    }
}
