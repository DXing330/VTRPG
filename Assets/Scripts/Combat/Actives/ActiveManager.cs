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
    public AttackManager attackManager;
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
        List<TacticActor> targets = map.GetActorsOnTiles(targetedTiles);
        List<string> effects = magicSpell.GetAllEffects();
        for (int i = 0; i < effects.Count; i++)
        {
            ApplyActiveEffects(targets, effects[i], magicSpell.GetSpecificsAt(i), magicSpell.GetPowerAt(i), magicSpell.GetSelectedTile(), true);
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
    public List<int> GetTargetableTiles(int start, bool spell = false)
    {
        string shape = active.GetRangeShape();
        if (spell){ shape = magicSpell.GetRangeShape(); }
        targetableTiles = new List<int>(GetTiles(start, shape, true, spell));
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
    public void DebugTargetedTiles()
    {
        string debugTargetedTiles = "";
        for (int i = 0; i < targetedTiles.Count; i++)
        {
            debugTargetedTiles += targetedTiles[i] + " ";
        }
        Debug.Log(debugTargetedTiles);
    }
    public List<int> GetTargetedTiles(int start, bool spellCast = false)
    {
        active.SetSelectedTile(start);
        string shape = active.GetShape();
        if (spellCast)
        {
            magicSpell.SetSelectedTile(start);
            shape = magicSpell.GetShape();
        }
        targetedTiles = new List<int>(GetTiles(start, shape, false, spellCast));
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
    protected List<int> GetTiles(int startTile, string shape, bool targetable = true, bool spellCast = false)
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
        return map.mapUtility.GetTilesByShapeSpan(startTile, shape, range, map.mapSize, skillUser.GetLocation());
    }
    protected void ApplyActiveEffects(List<TacticActor> targets, string effect, string specifics, int power, int selectedTile = -1, bool spellCast = false)
    {
        int targetTile = -1;
        string powerString = power.ToString();
        // There are some effects that naturally target a specific group of actors.
        if (effect.Contains("AllSpritesEquals"))
        {
            string[] allSpriteDetails = effect.Split("Equals");
            string specificSprite = allSpriteDetails[1];
            targets = map.AllActorsBySprite(specificSprite);
            active.AffectActors(targets, specifics, powerString, 1);
            return;
        }
        if (effect.Contains("AllSpeciesEquals"))
        {
            string[] allSpeciesDetails = effect.Split("Equals");
            string specificSpecies = allSpeciesDetails[1];
            targets = map.AllActorsBySpecies(specificSpecies);
            active.AffectActors(targets, specifics, powerString, 1);
            return;
        }
        switch (effect)
        {
            case "TriggerSkill":
                AutoSkillByName(skillUser, specifics);
                return;
            case "Weather":
                map.SetWeather(specifics);
                return;
            case "Escape":
                if (!map.ActorCanEscape(skillUser)){return;}
                map.ActorEscapesBattle(skillUser);
                return;
            case "Time":
                map.SetTime(specifics);
                return;
            case "Tile":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    map.ChangeTerrain(targetedTiles[i], specifics);
                }
                return;
            case "Border":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    map.ChangeBorder(targetedTiles[i], skillUser.GetDirection(), specifics);
                }
                return;
            case "AllBorders":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    map.ChangeAllBorders(targetedTiles[i], specifics);
                }
                return;
            case "Attack+Tile":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    map.ChangeTerrain(targetedTiles[i], specifics);
                }
                for (int i = 0; i < targets.Count; i++)
                {
                    attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targets[i], map, power, skillUser.GetBasicAttackDamageType());
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
                if (map.GetActorOnTile(selectedTile) == null)
                {
                    // Create a new actor on that location on the same team.
                    map.SpawnAndAddActor(selectedTile, specifics, skillUser.GetTeam(), skillUser);
                }
                return;
            case "RespawnSummon":
                // Try to respawn at the initial location.
                selectedTile = skillUser.GetInitialLocation();
                if (map.GetActorOnTile(selectedTile) == null)
                {
                    // Create a new actor on that location on the same team.
                    map.SpawnAndAddActor(selectedTile, specifics, skillUser.GetTeam(), skillUser);
                }
                return;
            case "TributeSummon":
                if (targetedTiles.Count <= 0){return;}
                TacticActor tributeActor = map.GetActorOnTile(targetedTiles[0]);
                if (tributeActor == null || tributeActor.GetTeam() != skillUser.GetTeam()){return;}
                // Create a new actor on that location on the same team.
                map.SpawnAndAddActor(targetedTiles[0], specifics, skillUser.GetTeam(), skillUser);
                // Kill the targeted ally as tribute.
                tributeActor.MarkSacrificed();
                tributeActor.SetCurrentHealth(0);
                tributeActor.ResetActions();
                return;
            case "MassSummon":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    if (map.GetActorOnTile(targetedTiles[i]) == null)
                    {
                        map.SpawnAndAddActor(targetedTiles[i], specifics, skillUser.GetTeam(), skillUser);
                    }
                }
                return;
            case "RandomSummon":
                // Check if selected tile is free.
                if (map.GetActorOnTile(selectedTile) == null)
                {
                    // Create a new actor on that location on the same team.
                    // Pick a random actor from the specifics list.
                    string[] randomSummon = specifics.Split(",");
                    map.SpawnAndAddActor(selectedTile, randomSummon[UnityEngine.Random.Range(0, randomSummon.Length)], skillUser.GetTeam(), skillUser);
                }
                return;
            case "MassRandomSummon":
                string[] randomPool = specifics.Split(",");
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    if (map.GetActorOnTile(targetedTiles[i]) == null)
                    {
                        map.SpawnAndAddActor(targetedTiles[i], randomPool[UnityEngine.Random.Range(0, randomPool.Length)], skillUser.GetTeam(), skillUser);
                    }
                }
                return;
            // Summon A Quantity Of Units Based On The Attack Divided By Power.
            case "AKSinkingSandSummon":
                // Guarantee Summon 1 On Tile.
                map.SpawnAndAddActor(targetedTiles[0], specifics, skillUser.GetTeam(), skillUser);
                int additionalSummons = skillUser.GetBaseAttack() / power;
                List<int> adjacentTiles = map.mapUtility.AdjacentTiles(targetedTiles[0], map.mapSize);
                int summonCount = 0;
                for (int i = 0; i < adjacentTiles.Count; i++)
                {
                    if (map.GetActorOnTile(adjacentTiles[i]) == null)
                    {
                        summonCount++;
                        map.SpawnAndAddActor(adjacentTiles[i], specifics, skillUser.GetTeam(), skillUser);
                    }
                    if (summonCount >= additionalSummons){return;}
                }
                return;
            // Use Half Base Health To Summon A Clone On The Facing Tile.
            case "Substitute":
                // If Less Than Half Health Then Do Nothing.
                if (skillUser.GetHealth() <= skillUser.GetBaseHealth() / 2){return;}
                // Get The Tile In Direction.
                selectedTile = map.mapUtility.PointInDirection(skillUser.GetLocation(), skillUser.GetDirection(), map.mapSize);
                if (map.GetActorOnTile(selectedTile) != null){return;}
                skillUser.UpdateHealth(skillUser.GetBaseHealth() / 2);
                map.SpawnAndAddActor(selectedTile, "Dummy Substitute", skillUser.GetTeam(), skillUser);
                return;
            case "Revive":
                if (specifics == "Random")
                {
                    map.ReviveRandomAlly(skillUser);
                    return;
                }
                map.ReviveDefeatedActorsBySprite(specifics);
                return;
            case "AbsorbDeadEnemy":
                TacticActor deadToAbsorb = map.ReturnFirstDeadEnemy(skillUser);
                if (deadToAbsorb == null){return;}
                int amountToAbsorb = 0;
                // Determine the relevant base stat, usually attack.
                // Gain half of the stat.
                switch (specifics)
                {
                    case "BaseAttack":
                    amountToAbsorb = deadToAbsorb.GetBaseAttack();
                    active.AffectActor(skillUser, "BaseAttack", (amountToAbsorb / 2).ToString(), 1);
                    break;
                }
                map.RemoveDefeatedActor(deadToAbsorb);
                return;
            case "SummonEnemy":
                // Check if selected tile is free.
                if (map.GetActorOnTile(selectedTile) == null)
                {
                    // Create a new actor on that location on the opposite team.
                    map.SpawnAndAddActor(selectedTile, specifics, (skillUser.GetTeam()+1) % 2);
                }
                return;
            case "Teleport":
                // Check if selected tile is free.
                if (map.GetActorOnTile(targetedTiles[0]) == null)
                {
                    skillUser.SetLocation(targetedTiles[0]);
                    map.UpdateActors();
                }
                return;
            case "TeleportTarget":
                if (map.GetActorOnTile(targetedTiles[0]) == null && skillUser.GetTarget() != null)
                {
                    skillUser.GetTarget().SetLocation(targetedTiles[0]);
                    map.UpdateActors();
                }
                return;
            // This Acts Like A Teleport
            case "Move+Tile":
                // Check if selected tile is free.
                if (map.GetActorOnTile(targetedTiles[0]) == null)
                {
                    map.ChangeTerrain(skillUser.GetLocation(), specifics);
                    map.MoveActorToTile(skillUser, targetedTiles[0]);
                    map.ChangeTerrain(skillUser.GetLocation(), specifics);
                    map.UpdateMap();
                }
                return;
            // The teleport behind you skill.
            case "Teleport+Attack":
                targetTile = targetedTiles[0];
                TacticActor targetActor = map.GetActorOnTile(targetTile);
                if (targetActor == null) { return; }
                if (map.TeleportToTarget(skillUser, targetActor, specifics))
                {
                    attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targetActor, map, power, skillUser.GetBasicAttackDamageType());
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
                        attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targets[i], map, power, skillUser.GetBasicAttackDamageType());
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
                if (map.GetActorOnTile(targetTile) != null)
                {
                    // If so then damage both thrown and thrown into.
                    map.DisplaceDamage(skillUser.GetGrappledActor(), Mathf.Max(skillUser.GetWeight(), 1), targetTile, true, map.GetActorOnTile(targetTile));
                    // Bounce the thrown onto the nearest empty tile.
                    map.MoveActorToTile(skillUser.GetGrappledActor(), map.GetClosestEmptyTile(map.GetActorOnTile(targetTile)));
                }
                // Else move the thrown into the tile.
                else
                {
                    map.MoveActorToTile(skillUser.GetGrappledActor(), targetTile);
                }
                map.UpdateMap();
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
                    int grapplerLocation = skillUser.GetLocation();
                    int grappledLocation = skillUser.GetGrappledActor().GetLocation();
                    map.MoveActorToTile(skillUser, grappledLocation);
                    map.MoveActorToTile(skillUser.GetGrappledActor(), grapplerLocation);
                    skillUser.ReleaseGrapple();
                    map.UpdateActors();
                }
                return;
            case "Attack":
                if (targets.Count <= 0) { return; }
                for (int i = 0; i < targets.Count; i++)
                {
                    for (int j = 0; j < int.Parse(specifics); j++)
                    {
                        attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targets[i], map, power, skillUser.GetBasicAttackDamageType());
                    }
                }
                return;
            case "AttackEnemies":
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i].GetTeam() == skillUser.GetTeam()){continue;}
                    for (int j = 0; j < int.Parse(specifics); j++)
                    {
                        attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targets[i], map, power, skillUser.GetBasicAttackDamageType());
                    }
                }
                return;
            case "AttackEnemies+Status":
                if (targets.Count <= 0) { return; }
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i].GetTeam() == skillUser.GetTeam()){continue;}
                    attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targets[i], map);
                    active.AffectActor(targets[i], "Status", specifics, power);
                }
                return;
            case "AttackAllEnemies":
                targets = map.AllEnemies(skillUser);
                if (targets.Count <= 0) { return; }
                for (int i = 0; i < targets.Count; i++)
                {
                    for (int j = 0; j < int.Parse(specifics); j++)
                    {
                        attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targets[i], map, power, skillUser.GetBasicAttackDamageType());
                    }
                }
                return;
            case "Attack+Drain":
                if (targets.Count <= 0) { return; }
                for (int i = 0; i < targets.Count; i++)
                {
                    for (int j = 0; j < int.Parse(specifics); j++)
                    {
                        attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targets[i], map, power, skillUser.GetBasicAttackDamageType());
                        skillUser.UpdateHealth(Mathf.Max(1, skillUser.GetAttack() - targets[i].GetDefense()), false);
                    }
                }
                return;
            case "Attack+Status":
                if (targets.Count <= 0) { return; }
                for (int i = 0; i < targets.Count; i++)
                {
                    attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targets[i], map);
                    active.AffectActor(targets[i], "Status", specifics, power);
                }
                return;
            case "Attack+MentalState":
                if (targets.Count <= 0) { return; }
                for (int i = 0; i < targets.Count; i++)
                {
                    attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targets[i], map);
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
                    attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targets[i], map);
                }
                map.DisplaceSkill(skillUser, targetedTiles, specifics, power);
                return;
            case "Attack+Move":
                if (targets.Count <= 0) { return; }
                targetTile = targetedTiles[0];
                for (int i = 0; i < targets.Count; i++)
                {
                    attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targets[i], map);
                }
                map.MoveSkill(skillUser, targetTile, specifics, power);
                return;
            case "Move":
                targetTile = targetedTiles[0];
                map.MoveSkill(skillUser, targetTile, specifics, power);
                return;
            case "Move+Attack":
                // Move to the tile selected.
                int prevTile = skillUser.GetLocation();
                targetTile = targetedTiles[0];
                if (map.GetActorOnTile(targetTile) == null)
                {
                    map.MoveActorToTile(skillUser, targetTile);
                    map.UpdateActors();
                }
                else { return; }
                // Check if an actor is on the specified tile(s).
                int attackTargetTile = map.mapUtility.PointInDirection(skillUser.GetLocation(), skillUser.GetDirection(), map.mapSize);
                if (map.GetActorOnTile(attackTargetTile) != null)
                {
                    attackManager.ActorAttacksActorWithAttackSpeed(skillUser, map.GetActorOnTile(attackTargetTile), map);
                }
                return;
            case "MoveThrough+Attack":
                targetTile = targetedTiles[0];
                if (map.GetActorOnTile(targetTile) == null)
                {
                    return;
                }
                map.MoveThroughSkill(skillUser, targetTile);
                attackManager.ActorAttacksActorWithAttackSpeed(skillUser, map.GetActorOnTile(targetTile), map, power, skillUser.GetBasicAttackDamageType());
                return;
            case "Charge+Attack":
                int startChargeTile = skillUser.GetLocation();
                targetTile = targetedTiles[0];
                // Try to move in straight line to the target.
                List<int> chargePath = map.mapUtility.StraightPathToTile(startChargeTile, targetTile, map.mapSize);
                if (chargePath.Count <= 0){return;}
                for (int i = 0; i < chargePath.Count; i++)
                {
                    if (map.GetActorOnTile(chargePath[i]) != null)
                    {
                        break;
                    }
                    map.MoveActorToTile(skillUser, chargePath[i]);
                }
                skillUser.SetDirection(map.mapUtility.DirectionBetweenLocations(startChargeTile, targetTile, map.mapSize));
                map.UpdateActors();
                // Check if an actor is on the specified tile(s).
                int chargeInto = map.mapUtility.PointInDirection(skillUser.GetLocation(), skillUser.GetDirection(), map.mapSize);
                if (map.GetActorOnTile(chargeInto) != null)
                {
                    attackManager.ActorAttacksActorWithAttackSpeed(skillUser, map.GetActorOnTile(chargeInto), map, power, skillUser.GetBasicAttackDamageType());
                }
                return;
            case "Displace":
                map.DisplaceSkill(skillUser, targetedTiles, specifics, power);
                return;
            case "TerrainEffect":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    map.ChangeTEffect(targetedTiles[i], specifics);
                }
                return;
            case "DelayedTileEffect":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    map.AddAura(skillUser, targetedTiles[i], specifics, power);
                }
                return;
            case "Attack+TerrainEffect":
                for (int i = 0; i < targets.Count; i++)
                {
                    attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targets[i], map, power, skillUser.GetBasicAttackDamageType());
                }
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    map.ChangeTEffect(targetedTiles[i], specifics);
                }
                return;
            case "Trap":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    // TODO Move This To Map.
                    // interactableMaker.PlaceTrap(map, specifics, targetedTiles[i], skillUser);
                }
                return;
            case "Swap":
                if (targetedTiles.Count <= 0) { return; }
                switch (specifics)
                {
                    case "Location":
                        if (targets.Count <= 0) { break; }
                        map.SwitchActorLocations(targets[0], skillUser);
                        break;
                    case "TerrainEffect":
                        map.SwitchTerrainEffect(targetedTiles[0], skillUser.GetLocation());
                        break;
                    case "Tile":
                        map.SwitchTile(targetedTiles[0], skillUser.GetLocation());
                        break;
                }
                return;
            case "True Attack":
                for (int i = 0; i < targets.Count; i++)
                {
                    attackManager.TrueDamageAttack(skillUser, targets[i], map, power, specifics);
                }
                return;
            // Should go through the attack manager for stab/mastery bonuses.
            case "ElementalAttack":
                if (targets.Count <= 0) { return; }
                for (int i = 0; i < targets.Count; i++)
                {
                    // Do an attack with stab and stuff.
                    attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targets[i], map, power, specifics);
                }
                return;
            // Directly does the elemental damage, doesn't need to go through the attack manager.
            case "ElementalDamage":
                for (int i = 0; i < targets.Count; i++)
                {
                    active.ApplyElementalDamageToTarget(targets[i], specifics + "Damage", (power + skillUser.GetMagicPower()), map);
                }
                return;
            case "Flat Attack":
                for (int i = 0; i < targets.Count; i++)
                {
                    attackManager.FlatDamageAttack(skillUser, targets[i], map, int.Parse(specifics));
                }
                return;
            // Remove a random active skill.
            case "Attack+Amnesia":
                for (int i = 0; i < targets.Count; i++)
                {
                    attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targets[i], map, power);
                    for (int j = 0; j < int.Parse(specifics); j++)
                    {
                        targets[i].RemoveRandomActiveSkill();
                    }
                }
                return;
            case "AllAllies":
                // Get all allies from the map.
                targets = map.AllAllies(skillUser);
                active.AffectActors(targets, specifics, powerString, 1);
                return;
            case "TargetAllies":
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i].GetTeam() != skillUser.GetTeam()){continue;}
                    // Grant them your weight and defense.
                    active.AffectActor(targets[i], specifics, powerString, 1);
                }
                return;
            case "AllEnemies":
                targets = map.AllEnemies(skillUser);
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
                        if (map.FacingActor(targets[i]))
                        {
                            attackManager.ActorAttacksActorWithAttackSpeed(targets[i], map.ReturnClosestFacingActor(targets[i]), map);
                        }
                        break;
                        case "Forward":
                        // Try to move forward.
                        if (map.FacingEmptyTile(targets[i]))
                        {
                            map.CommandMovement(targets[i]);
                        }
                        break;
                        case "Backward":
                        // Try to move backward.
                        if (map.FacingEmptyTile(targets[i], false))
                        {
                            map.CommandMovement(targets[i], false);
                        }
                        break;
                    }
                }
                return;
            case "ChainLightningAttack":
                // Keep track of the targets.
                targets = map.ChainLightningTargets(targetedTiles[0], int.Parse(specifics), power);
                for (int i = 0; i < targets.Count; i++)
                {
                    attackManager.ActorAttacksActorWithAttackSpeed(skillUser, targets[i], map, skillUser.GetBasicAttackMultiplier(), skillUser.GetBasicAttackDamageType());
                }
                return;
            case "ChainLightning":
                // Keep track of the targets.
                targets = map.ChainLightningTargets(targetedTiles[0]);
                active.AffectActors(targets, specifics, powerString, 1);
                return;
            case "MapChainLightning":
                // Keep track of the targets.
                targets = map.ChainLightningTargets(targetedTiles[0]);
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] == null){continue;}
                    map.ChangeTile(targets[i].GetLocation(), specifics, powerString);
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
            case "PainSplit":
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
                map.AddAura(skillUser, targetedTiles[0], specifics, power);
                return;
            case "Manaize":
                // Light/Dark is different.
                if (specifics == "Light")
                {
                    if (map.GetTime() == "Day")
                    {
                        skillUser.RestoreMana(power);
                    }
                    return;
                }
                else if (specifics == "Dark")
                {
                    if (map.GetTime() == "Night")
                    {
                        skillUser.RestoreMana(power);
                    }
                    return;
                }
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    // Check if the target tile is of the terrain effect.
                    if (map.GetTerrainEffectOnTile(targetedTiles[i]).Contains(specifics))
                    {
                        // If so absorb the terrain effect to gain mana.
                        map.RemoveTerrainEffectOnTile(targetedTiles[i]);
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
            // Generally for stealing base stats.
            case "Steal":
                int stealPower = power;
                string stealTarget = specifics;
                for (int i = 0; i < targets.Count; i++)
                {
                    active.AffectActor(targets[i], stealTarget, (-stealPower).ToString(), 1);
                    active.AffectActor(skillUser, stealTarget, (stealPower).ToString(), 1);
                }
                return;
            // HP Cost For Actions.
            case "Bloodletting":
                active.AffectActor(skillUser, "Health%", specifics, 1);
                active.AffectActor(skillUser, "Actions", power.ToString(), 1);
                return;
            case "OpForGun":
                // TODO Run To Highest Defense Enemy + Attack, Push + Attack Any Enemies Inbetween Aside.
                // For Now Just Fire A Laser At The Highest Defense Enemy.
                // Get Highest Defense Enemy.
                TacticActor opForGunTarget = map.FindEnemyByStat(skillUser, "Defense");
                // Draw A Line.
                List<int> opForGunTargetTiles = map.mapUtility.ShortestLineBetweenPoints(skillUser.GetLocation(), opForGunTarget.GetLocation(), map.mapSize);
                opForGunTargetTiles.Add(opForGunTarget.GetLocation());
                List<TacticActor> opForGunTargets = map.GetActorsOnTiles(opForGunTargetTiles);
                // Target The Enemy And Anyone On That Line.
                for (int i = 0; i < opForGunTargets.Count; i++)
                {
                    if (opForGunTargets[i].GetTeam() == skillUser.GetTeam()){continue;}
                    attackManager.ActorAttacksActorWithAttackSpeed(skillUser, opForGunTargets[i], map);
                }
                return;
            case "OpForArmor":
                // Get The Highest Attack Enemy -> Attack + Stun Them + Adjacent Enemies.
                TacticActor opForArmorTarget = map.FindEnemyByStat(skillUser, "Attack");
                List<TacticActor> opForArmorTargetAdj = map.GetAdjacentAllies(opForArmorTarget);
                attackManager.ActorAttacksActorWithAttackSpeed(skillUser, opForArmorTarget, map);
                active.AffectActor(opForArmorTarget, "Status", "Stun", power);
                for (int i = 0; i < opForArmorTargetAdj.Count; i++)
                {
                    attackManager.ActorAttacksActorWithAttackSpeed(skillUser, opForArmorTargetAdj[i], map);
                    active.AffectActor(opForArmorTargetAdj[i], "Status", "Stun", power);
                }
                return;
        }
        // Covers status/mental state/amnesia/stat changes/etc.
        active.AffectActors(targets, effect, specifics, power);
    }
    // All Skill Usage Should Go Through Here
    public bool ActivateSkill(bool cost = true)
    {
        return ActivateSkillInternal(cost, cost);
    }
    protected bool ActivateSkillInternal(bool spendEnergy, bool spendAction)
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
        List<TacticActor> targets = map.GetActorsOnTiles(targetedTiles);
        List<string> effects = active.GetAllEffects();
        for (int i = 0; i < effects.Count; i++)
        {
            ApplyActiveEffects(targets, effects[i], active.GetSpecificsAt(i), active.GetPowerAt(i), active.GetSelectedTile());
        }
        passive.ApplyAfterSkillPassives(skillUser, targets, map, active, temp);
        map.ApplyAuraEffects(skillUser, "Skill");
        return true;
    }
    protected bool CanPaySkillCost(bool spendEnergy, bool spendAction)
    {
        if (skillUser.GetSilenced()){return false;}
        if (spendEnergy && skillUser.GetEnergy() < active.GetEnergyCost(skillUser, map)){return false;}
        if (spendAction && skillUser.GetActions() < active.GetActionCost(skillUser, map)){return false;}
        return true;
    }
    public bool CheckTriggeredSkillCost()
    {
        return CanPaySkillCost(true, false);
    }
    // TODO AUTO CASTING SKILLS/SPELLS
    public ActorAI actorAI;
    public void AutoSkillByName(TacticActor skillUser, string skillName)
    {
        if (skillName == "" || skillUser == null){return;}
        SetSkillFromName(skillName, skillUser);
        int targetedTile = actorAI.ChooseSkillTargetLocation(skillUser, map);
        if (targetedTile < 0){return;}
        GetTargetedTiles(targetedTile);
        ActivateSkill(false);
    }
    public void AutoSkill(TacticActor skillUser, string effect, string specifics)
    {
        // Determine what skill to use.
        string skillName = "";
        switch (effect)
        {
            default:
            return;
            case "LastUsed":
            skillName = skillUser.ReturnMostRecentSkill();
            break;
        }
        Debug.Log("AUTOSKILL NAME: " + skillName);
        if (skillName == ""){return;}
        // Load The Active By Name?
        SetSkillFromName(skillName, skillUser);
        // Determine what targets and if targets are valid.
        // The Actor AI uses the same scriptable object so the skill should already be loaded and ready to check.
        int targetedTile = actorAI.ChooseSkillTargetLocation(skillUser, map);
        Debug.Log("AUTOSKILL TARGET: " + targetedTile);
        if (targetedTile < 0){return;}
        GetTargetedTiles(targetedTile);
        // Use The Skill For Free.
        ActivateSkill(false);
    }
    public bool CheckSkillCost(BattleMap map)
    {
        return active.Activatable(skillUser, map);
    }
    public bool CheckSpellCost(BattleMap map)
    {
        return magicSpell.Activatable(skillUser, map);
    }
    /*protected void ResolveTriggeredSkill(string triggerData)
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
            bool resolved = triggeredSkillResolver.TryResolve(triggerData, skillUser, this, out triggeredCast);
            if (resolved)
            {
                LogTriggeredSkillLoadedDetails("Resolved");
                active.SetSelectedTile(triggeredCast.selectedTile);
                targetedTiles = new List<int>(triggeredCast.targetedTiles);
                triggeredSkillEnergyBeforeCast = skillUser.GetEnergy();
                triggeredSkillActionsBeforeCast = skillUser.GetActions();
                ActivateSkillInternal(true, false);
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
    }*/
}
