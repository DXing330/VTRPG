using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AttackManagerTester : MonoBehaviour
{
    public AttackManager attackManager;
    public BattleManager battleManager;
    public PassiveOrganizer passiveOrganizer;
    // Map Stuff 
    public BattleMap map;
    public string dummyTime;
    public string dummyWeather;
    public int attackerLocation;
    public int attackerDirection;
    public string attackerBuilding;
    public string attackerTile;
    public int attackerElevation;
    public string attackerTEffect;
    public List<string> attackerBorders;
    public List<string> testAttackerPassives;
    public List<string> testAttackerPassiveLevels;
    public List<string> testAttackerBuffs;
    public List<string> testAttackerAuras;
    public int defenderLocation;
    public int defenderDirection;
    public string defenderBuilding;
    public string defenderTile;
    public int defenderElevation;
    public string defenderTEffect;
    public List<string> defenderBorders;
    public List<string> testDefenderPassives;
    public List<string> testDefenderPassiveLevels;
    public List<string> testDefenderBuffs;
    public List<string> testDefenderAuras;
    public int guardLocation;
    public int guardDirection;
    public string guardTile;
    public int guardElevation;
    public string guardTEffect;
    public List<string> guardBorders;
    public List<string> testGuardPassives;
    public List<string> testGuardPassiveLevels;
    // Actors.
    public TacticActor dummyAttacker;
    public string attackerStats;
    public TacticActor dummyDefender;
    public string defenderStats;
    public bool guard;
    public int guardDuration;
    public int guardRange;
    public TacticActor dummyGuard;
    public string guardStats;
    public bool resetActorLayerScaleBeforeTest = true;
    public Vector3 actorLayerBaseScale = new Vector3(0.6f, 0.6f, 1f);

    public void InitializeMap()
    {
        ResetTestMapActorLayerScale();
        map.ForceStart();
        map.combatLog.ForceStart();
        map.combatLog.AddNewLog();
        // Set up the actors.
        dummyAttacker.SetInitialStatsFromString(attackerStats);
        dummyAttacker.InitializeStats();
        // Need attribute/elemental/racial passives.
        for (int i = 0; i < testAttackerPassives.Count; i++)
        {
            dummyAttacker.AddPassiveSkill(testAttackerPassives[i], testAttackerPassiveLevels[i]);
        }
        for (int i = 0; i < testAttackerBuffs.Count; i++)
        {
            dummyAttacker.AddBuff(testAttackerBuffs[i], dummyAttacker.defaultBuffDuration);
        }
        for (int i = 0; i < testAttackerAuras.Count; i++)
        {
            map.AddAura(dummyAttacker, attackerLocation, testAttackerAuras[i], 1);
        }
        battleManager.actorMaker.AddElementPassives(dummyAttacker);
        battleManager.actorMaker.AddAttributePassives(dummyAttacker);
        battleManager.actorMaker.AddSpeciesPassives(dummyAttacker);
        passiveOrganizer.OrganizeActorPassives(dummyAttacker);
        dummyAttacker.SetActions(dummyAttacker.GetBaseActions());
        dummyAttacker.SetCurrentHealth(dummyAttacker.GetBaseHealth());
        dummyAttacker.SetLocation(attackerLocation);
        dummyAttacker.SetDirection(attackerDirection);
        dummyDefender.SetInitialStatsFromString(defenderStats);
        dummyDefender.InitializeStats();
        for (int i = 0; i < testDefenderPassiveLevels.Count; i++)
        {
            dummyDefender.AddPassiveSkill(testDefenderPassives[i], testDefenderPassiveLevels[i]);
        }
        for (int i = 0; i < testDefenderBuffs.Count; i++)
        {
            dummyDefender.AddBuff(testDefenderBuffs[i], dummyDefender.defaultBuffDuration);
        }
        for (int i = 0; i < testDefenderAuras.Count; i++)
        {
            map.AddAura(dummyDefender, defenderLocation, testDefenderAuras[i], 1);
        }
        battleManager.actorMaker.AddElementPassives(dummyDefender);
        battleManager.actorMaker.AddAttributePassives(dummyDefender);
        battleManager.actorMaker.AddSpeciesPassives(dummyDefender);
        passiveOrganizer.OrganizeActorPassives(dummyDefender);
        dummyDefender.SetActions(dummyDefender.GetBaseActions());
        dummyDefender.SetCurrentHealth(dummyDefender.GetBaseHealth());
        dummyDefender.SetLocation(defenderLocation);
        dummyDefender.SetDirection(defenderDirection);
        dummyDefender.ResetTarget();
        // Set up the attack conditions.
        map.SetTime(dummyTime);
        map.SetWeather(dummyWeather);
        map.AddBuilding(attackerBuilding, attackerLocation);
        map.ChangeTile(attackerLocation, "Tile", attackerTile, true);
        map.ChangeTile(attackerLocation, "Elevation", attackerElevation.ToString());
        map.ChangeTile(attackerLocation, "TerrainEffect", attackerTEffect, true);
        map.ChangeTile(attackerLocation, "Borders", String.Join("|", attackerBorders), true);
        map.AddBuilding(defenderBuilding, defenderLocation);
        map.ChangeTile(defenderLocation, "Tile", defenderTile, true);
        map.ChangeTile(defenderLocation, "Elevation", defenderElevation.ToString());
        map.ChangeTile(defenderLocation, "TerrainEffect", defenderTEffect, true);
        map.ChangeTile(defenderLocation, "Borders", String.Join("|", defenderBorders), true);
        dummyGuard.SetInitialStatsFromString(guardStats);
        dummyGuard.InitializeStats();
        for (int i = 0; i < testGuardPassiveLevels.Count; i++)
        {
            dummyAttacker.AddPassiveSkill(testGuardPassives[i], testGuardPassiveLevels[i]);
        }
        battleManager.actorMaker.AddElementPassives(dummyGuard);
        battleManager.actorMaker.AddAttributePassives(dummyGuard);
        battleManager.actorMaker.AddSpeciesPassives(dummyGuard);
        passiveOrganizer.OrganizeActorPassives(dummyGuard);
        if (guard)
        {
            dummyGuard.GainGuard(guardDuration, guardRange);
        }
        dummyGuard.SetLocation(guardLocation);
        dummyGuard.SetDirection(guardDirection);
        dummyGuard.ResetTarget();
        map.ChangeTile(guardLocation, "Tile", guardTile, true);
        map.ChangeTile(guardLocation, "Elevation", guardElevation.ToString());
        map.ChangeTile(guardLocation, "TerrainEffect", guardTEffect, true);
        map.ChangeTile(guardLocation, "Borders", String.Join("|", guardBorders), true);
        dummyAttacker.SetTeam(0);
        dummyDefender.SetTeam(1);
        dummyGuard.SetTeam(1);
        map.AddActorToBattle(dummyAttacker);
        map.AddActorToBattle(dummyDefender);
        map.AddActorToBattle(dummyGuard);
        // Apply Start Battle Passives
        battleManager.effectManager.StartBattle(dummyAttacker);
        battleManager.effectManager.StartBattle(dummyDefender);
        battleManager.effectManager.StartBattle(dummyGuard);
    }

    void ResetTestMapActorLayerScale()
    {
        if (!resetActorLayerScaleBeforeTest || map == null || map.mapTiles == null) { return; }
        int layer = map.actorLayer;
        FieldInfo initializedField = typeof(MapTile).GetField("layerAppearanceInitialized", BindingFlags.Instance | BindingFlags.NonPublic);
        for (int i = 0; i < map.mapTiles.Count; i++)
        {
            MapTile tile = map.mapTiles[i];
            if (tile == null || tile.layers == null || layer < 0 || layer >= tile.layers.Count || tile.layers[layer] == null) { continue; }
            tile.layers[layer].rectTransform.localScale = actorLayerBaseScale;
            if (initializedField != null)
            {
                initializedField.SetValue(tile, false);
            }
        }
    }

    [ContextMenu("Test Attack")]
    public void TestAttack()
    {
        InitializeMap();
        // Set up the guard if you want.
        // Show all the passives that are taking effect.
        attackManager.ActorAttacksActorWithAttackSpeed(dummyAttacker, dummyDefender, map);
        map.combatLog.DebugLatestDetailsLog();
    }

    [ContextMenu("Test Attack WO Reseting")]
    public void TestAttackWOReset()
    {
        attackManager.ActorAttacksActorWithAttackSpeed(dummyAttacker, dummyDefender, map);
        map.combatLog.DebugLatestDetailsLog();
    }

    // Active Testing.
    public ActiveManager activeManager;
    public string activeName;
    public int activeTargetTile;
    [ContextMenu("Test Active")]
    public void TestActive()
    {
        InitializeMap();
        activeManager.SetSkillFromName(activeName, dummyAttacker);
        activeManager.GetTargetableTiles(dummyAttacker.GetLocation(), battleManager.moveManager.actorPathfinder);
        activeManager.GetTargetedTiles(activeTargetTile, battleManager.moveManager.actorPathfinder);
        battleManager.ActivateSkill(activeName, dummyAttacker);
        map.combatLog.DebugLatestDetailsLog();
    }
    // TODO
    [ContextMenu("Test Basic Attack Skill")]
    public void TestAA()
    {
        InitializeMap();
        activeManager.SetSkillUser(dummyAttacker);
        activeManager.GetTargetableTiles(dummyAttacker.GetLocation(), battleManager.moveManager.actorPathfinder);
        activeManager.GetTargetedTiles(activeTargetTile, battleManager.moveManager.actorPathfinder);
        battleManager.ActivateSkill(activeName, dummyAttacker);
    }
    public string spellData;
    [ContextMenu("Test Spell")]
    public void TestSpell()
    {
        InitializeMap();
        activeManager.SetSkillUser(dummyAttacker);
        activeManager.SetSpell(spellData);
        activeManager.GetTargetableTiles(dummyAttacker.GetLocation(), battleManager.moveManager.actorPathfinder, true);
        activeManager.GetTargetedTiles(activeTargetTile, battleManager.moveManager.actorPathfinder, true);
        battleManager.ActivateSpell(dummyAttacker);
        map.UpdateMap();
    }
    // Test Start/End Turn Passives
    [ContextMenu("Test Start Turn")]
    public void TestStartTurn()
    {
        InitializeMap();
        dummyAttacker.NewTurn();
        battleManager.effectManager.StartTurn(dummyAttacker, map);
        dummyAttacker.EndTurn();
        battleManager.effectManager.EndTurn(dummyAttacker, map);
        map.ApplyEndTerrainEffect(dummyAttacker);
        map.UpdateMap();
    }
    [ContextMenu("Test End Turn")]
    public void TestEndTurn()
    {
        InitializeMap();
        dummyAttacker.EndTurn();
        battleManager.effectManager.EndTurn(dummyAttacker, map);
        map.ApplyEndTerrainEffect(dummyAttacker);
        dummyAttacker.NewTurn();
        battleManager.effectManager.StartTurn(dummyAttacker, map);
        map.UpdateMap();
    }
}
