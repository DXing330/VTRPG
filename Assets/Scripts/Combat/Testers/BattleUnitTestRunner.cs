using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class BattleUnitTestRunner : MonoBehaviour
{
    // This runner is intentionally not a Unity Test Framework test suite.
    // It is a TestBattle scene tool: use the component context menu, let it rebuild the small fixture for each test, then read the console/report.
    //
    // Each test follows the same pattern:
    // 1. ConfigureBasicFixture() resets TestBattle to a known small setup.
    // 2. The test calls one battle/map function.
    // 3. Assert* helpers compare expected vs. actual values.
    // 4. RunCase records PASS/FAIL/SKIP and keeps the suite moving.
    public AttackManagerTester attackTester;
    public BattleMapTester battleMapTester;
    public MapStateTester mapStateTester;
    public PassiveEffectTester passiveEffectTester;
    public ActiveTester activeTester;
    public MiscTester miscTester;
    public string reportPath = "Assets/output/battle-tests/battle-unit-test-report.txt";
    public bool logPassedTests = true;
    private readonly List<TestResult> results = new List<TestResult>();
    private BattleMap map;
    private BattleManager battleManager;
    private ActiveManager activeManager;
    private MoveCostManager moveManager;
    private AttackManager attackManager;
    [ContextMenu("Run All Battle Unit Tests")]
    public void RunAllBattleUnitTests()
    {
        RunTests("All Battle Unit Tests", delegate
        {
            AddActorStateTests();
            AddTurnLifecycleTests();
            AddStatusBuffTests();
            AddCombatTests();
            AddGuardInterceptTests();
            AddMapMovementTests();
            AddMapStateInteractionTests();
            AddSkillPassiveTests();
            AddMapPassiveTests();
            AddAiTests();
        });
    }

    [ContextMenu("Run Combat Unit Tests")]
    public void RunCombatUnitTests()
    {
        RunTests("Combat Unit Tests", delegate
        {
            AddCombatTests();
            AddGuardInterceptTests();
        });
    }

    [ContextMenu("Run Turn Lifecycle Unit Tests")]
    public void RunTurnLifecycleUnitTests()
    {
        RunTests("Turn Lifecycle Unit Tests", AddTurnLifecycleTests);
    }

    [ContextMenu("Run Status/Buff Unit Tests")]
    public void RunStatusBuffUnitTests()
    {
        RunTests("Status/Buff Unit Tests", AddStatusBuffTests);
    }

    [ContextMenu("Run Map/Movement Unit Tests")]
    public void RunMapMovementUnitTests()
    {
        RunTests("Map/Movement Unit Tests", delegate
        {
            AddMapMovementTests();
            AddMapStateInteractionTests();
        });
    }

    [ContextMenu("Run Skill/Passive Unit Tests")]
    public void RunSkillPassiveUnitTests()
    {
        RunTests("Skill/Passive Unit Tests", delegate
        {
            AddSkillPassiveTests();
            AddMapPassiveTests();
        });
    }

    [ContextMenu("Run Map Passive Unit Tests")]
    public void RunMapPassiveUnitTests()
    {
        RunTests("Map Passive Unit Tests", AddMapPassiveTests);
    }

    [ContextMenu("Clear Battle Unit Test Output")]
    public void ClearBattleUnitTestOutput()
    {
        results.Clear();
        string fullPath = FullReportPath();
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        Debug.Log("Battle unit test output cleared: " + fullPath);
    }

    private void RunTests(string suiteName, Action addTests)
    {
        // Resolve scene references once, then let each RunCase reset its own fixture. This keeps tests independent: one failed test should not poison the next test's map/actor state.
        results.Clear();
        ResolveReferences();
        addTests();
        WriteReport(suiteName);
    }

    private void AddActorStateTests()
    {
        RunCase("stat string initialization gives expected actor stats", "Actor state", "Inspect TacticActor.SetInitialStatsFromString and ActorStats.InitializeStats.", delegate
        {
            ConfigureBasicFixture(attackerHealth: 50, attackerAttack: 12, attackerRange: 2, attackerDefense: 3, attackerMoveSpeed: 4, attackerMoveType: "Walking");
            AssertEqual(50, attackTester.dummyAttacker.GetBaseHealth(), "base HP");
            AssertEqual(50, attackTester.dummyAttacker.GetHealth(), "current HP");
            AssertEqual(12, attackTester.dummyAttacker.GetAttack(), "attack");
            AssertEqual(2, attackTester.dummyAttacker.GetAttackRange(), "range");
            AssertEqual(3, attackTester.dummyAttacker.GetDefense(), "defense");
            AssertEqual(4, attackTester.dummyAttacker.GetMoveSpeed(), "move speed");
            AssertEqual("Walking", attackTester.dummyAttacker.GetMoveType(), "move type");
        });

        RunCase("new turn restores actions", "Actor state", "Inspect TacticActor.NewTurn and action reset logic.", delegate
        {
            ConfigureBasicFixture();
            attackTester.dummyAttacker.SetActions(0);
            attackTester.dummyAttacker.NewTurn();
            AssertEqual(attackTester.dummyAttacker.GetBaseActions(), attackTester.dummyAttacker.GetActions(), "actions after NewTurn");
        });

        RunCase("healing clamps to base health", "Actor state", "Inspect ActorStats.Heal and ActorStats.SetCurrentHealth.", delegate
        {
            ConfigureBasicFixture(attackerHealth: 40);
            attackTester.dummyAttacker.SetCurrentHealth(5);
            attackTester.dummyAttacker.Heal(999);
            AssertEqual(40, attackTester.dummyAttacker.GetHealth(), "health after large heal");
        });

        RunCase("permanent status applications stack", "Actor state", "Inspect ActorStats.AddStatus for permanent-duration status handling.", delegate
        {
            ConfigureBasicFixture();
            UnityEngine.Random.InitState(2100);
            ApplyEffectToActor(attackTester.dummyAttacker, "Status", "Poison");
            ApplyEffectToActor(attackTester.dummyAttacker, "Status", "Poison");
            AssertEqual(2, CountStatus(attackTester.dummyAttacker, "Poison"), "Poison status count");
        });
    }

    private void AddCombatTests()
    {
        RunCase("guaranteed hit damages defender", "Combat", "Inspect AttackManager.ActorAttacksActor hit and damage flow.", delegate
        {
            ConfigureBasicFixture(attackerAttack: 35, attackerHitChance: 999, attackerCritChance: 0, defenderDefense: 0, defenderDodge: 0);
            int before = attackTester.dummyDefender.GetHealth();
            UnityEngine.Random.InitState(3100);
            attackManager.ActorAttacksActor(attackTester.dummyAttacker, attackTester.dummyDefender, map);
            AssertLess(attackTester.dummyDefender.GetHealth(), before, "defender HP after guaranteed hit");
        });

        RunCase("guaranteed miss does not damage defender", "Combat", "Inspect AttackManager.ActorAttacksActor hit-roll branch.", delegate
        {
            ConfigureBasicFixture(attackerAttack: 35, attackerHitChance: 0, attackerCritChance: 0, defenderDefense: 0, defenderDodge: 999);
            int before = attackTester.dummyDefender.GetHealth();
            UnityEngine.Random.InitState(3200);
            attackManager.ActorAttacksActor(attackTester.dummyAttacker, attackTester.dummyDefender, map);
            AssertEqual(before, attackTester.dummyDefender.GetHealth(), "defender HP after guaranteed miss");
        });

        RunCase("defense reduces damage", "Combat", "Inspect AttackManager damage calculation and ActorStats.TakeDamage defense application.", delegate
        {
            ConfigureBasicFixture(attackerAttack: 45, attackerHitChance: 999, attackerCritChance: 0, defenderHealth: 100, defenderDefense: 0, defenderDodge: 0);
            int beforeNoDefense = attackTester.dummyDefender.GetHealth();
            UnityEngine.Random.InitState(3300);
            attackManager.ActorAttacksActor(attackTester.dummyAttacker, attackTester.dummyDefender, map);
            int noDefenseDamage = beforeNoDefense - attackTester.dummyDefender.GetHealth();

            ConfigureBasicFixture(attackerAttack: 45, attackerHitChance: 999, attackerCritChance: 0, defenderHealth: 100, defenderDefense: 20, defenderDodge: 0);
            int beforeDefense = attackTester.dummyDefender.GetHealth();
            UnityEngine.Random.InitState(3300);
            attackManager.ActorAttacksActor(attackTester.dummyAttacker, attackTester.dummyDefender, map);
            int defenseDamage = beforeDefense - attackTester.dummyDefender.GetHealth();

            AssertLess(defenseDamage, noDefenseDamage, "damage with defense compared to no defense");
        });

        RunCase("building on defender tile takes damage before defender defense", "Combat", "Inspect AttackManager.ActorAttacksActor and BattleMap.DamageActorBuilding.", delegate
        {
            ConfigureBasicFixture(attackerAttack: 300, attackerHitChance: 999, attackerCritChance: 0, defenderDefense: 80, defenderDodge: 0);
            int defenderTile = attackTester.dummyDefender.GetLocation();
            map.AddBuilding("Tower", defenderTile);
            int beforeBuildingHealth = map.GetBuildingHealthOnLocation(defenderTile);
            UnityEngine.Random.InitState(3400);
            attackManager.ActorAttacksActor(attackTester.dummyAttacker, attackTester.dummyDefender, map);
            bool buildingStillExists = map.GetBuildingIndexFromLocation(defenderTile) >= 0;
            bool damagedOrRemoved = !buildingStillExists || map.GetBuildingHealthOnLocation(defenderTile) < beforeBuildingHealth;
            AssertTrue(damagedOrRemoved, "building health after attack", "removed or less than " + beforeBuildingHealth, buildingStillExists ? map.GetBuildingHealthOnLocation(defenderTile).ToString() : "removed");
        });
    }

    private void AddTurnLifecycleTests()
    {
        // These follow the same order the battle manager uses:
        // NewTurn/StartTurn at the beginning, then TacticActor.EndTurn,
        // EffectManager.EndTurn, and map end terrain effects at the end.
        RunCase("start turn manager applies weather start effects", "Turn Lifecycle", "Inspect EffectManager.StartTurn and BattleMap.ApplyWeatherStartEffect.", delegate
        {
            ConfigureBasicFixture();
            map.SetWeather("Cutting Wind");
            battleManager.effectManager.StartTurn(attackTester.dummyAttacker, map);
            AssertEqual(1, CountStatus(attackTester.dummyAttacker, "Bleed"), "Bleed status count after StartTurn weather effect");
        });

        RunCase("start turn manager applies tile start effects", "Turn Lifecycle", "Inspect EffectManager.StartTurn and BattleMap.ApplyTileStartEffect.", delegate
        {
            ConfigureBasicFixture();
            map.ChangeTerrain(attackTester.dummyAttacker.GetLocation(), "Snow", true);
            battleManager.effectManager.StartTurn(attackTester.dummyAttacker, map);
            AssertEqual(1, CountStatus(attackTester.dummyAttacker, "Cold"), "Cold status count after StartTurn tile effect");
        });

        RunCase("end turn flow applies terrain-effect end effects", "Turn Lifecycle", "Inspect TacticActor.EndTurn, EffectManager.EndTurn, and BattleMap.ApplyEndTerrainEffect ordering.", delegate
        {
            ConfigureBasicFixture();
            map.ChangeTEffect(attackTester.dummyAttacker.GetLocation(), "Water", true);
            ApplyFullEndTurnFlow(attackTester.dummyAttacker);
            AssertEqual(1, CountStatus(attackTester.dummyAttacker, "Wet"), "Wet status count after full EndTurn flow");
        });

        RunCase("actor end turn clears temporary movement and bonus actions", "Turn Lifecycle", "Inspect TacticActor.EndTurnResetStats.", delegate
        {
            ConfigureBasicFixture();
            attackTester.dummyAttacker.GainTempMovement(5);
            attackTester.dummyAttacker.GainBonusActions(2);
            attackTester.dummyAttacker.EndTurn();
            AssertEqual(0, attackTester.dummyAttacker.GetMovement(), "movement after EndTurn");
            AssertEqual(attackTester.dummyAttacker.GetBaseActions(), attackTester.dummyAttacker.GetActions(), "actions after EndTurn clears bonus actions");
        });

        RunCase("actor end turn clears temporary attack and defense", "Turn Lifecycle", "Inspect ActorStats.EndTurnResetStats temp stat reset.", delegate
        {
            ConfigureBasicFixture(attackerAttack: 20, attackerDefense: 4);
            attackTester.dummyAttacker.UpdateTempAttack(10);
            attackTester.dummyAttacker.UpdateTempDefense(6);
            AssertEqual(30, attackTester.dummyAttacker.GetAttack(), "attack before EndTurn temp reset");
            AssertEqual(10, attackTester.dummyAttacker.GetDefense(), "defense before EndTurn temp reset");
            attackTester.dummyAttacker.EndTurn();
            AssertEqual(20, attackTester.dummyAttacker.GetAttack(), "attack after EndTurn temp reset");
            AssertEqual(4, attackTester.dummyAttacker.GetDefense(), "defense after EndTurn temp reset");
        });
    }

    private void AddStatusBuffTests()
    {
        // Status/buff timing is split between EffectManager and TacticActor:
        // EffectManager applies start/end effects and decrements durations;
        // TacticActor.EndTurn removes statuses/buffs whose duration is zero.
        RunCase("permanent status effects stack by count", "Status/Buff", "Inspect SkillEffect.AffectActor Status handling for Burn/Poison/Bleed.", delegate
        {
            ConfigureBasicFixture();
            UnityEngine.Random.InitState(4100);
            ApplyEffectToActor(attackTester.dummyAttacker, "Status", "Burn");
            ApplyEffectToActor(attackTester.dummyAttacker, "Status", "Burn");
            AssertEqual(2, CountStatus(attackTester.dummyAttacker, "Burn"), "Burn stack count after two SkillEffect applications");
            AssertEqual(-1, attackTester.dummyAttacker.ReturnStatusDuration("Burn"), "Burn permanent duration");
        });
        RunCase("timed status effects merge duration", "Status/Buff", "Inspect SkillEffect.AffectActor Status handling for timed statuses.", delegate
        {
            ConfigureBasicFixture();
            UnityEngine.Random.InitState(4105);
            ApplyEffectToActor(attackTester.dummyAttacker, "Status", "Paralyze", 4);
            ApplyEffectToActor(attackTester.dummyAttacker, "Status", "Paralyze", 4);
            AssertEqual(8, attackTester.dummyAttacker.ReturnStatusDuration("Paralyze"), "Paralyze duration after two SkillEffect applications");
            AssertEqual(1, CountStatus(attackTester.dummyAttacker, "Paralyze"), "Paralyze stack count after timed duration merge");
        });
        RunCase("buff applications merge duration", "Status/Buff", "Inspect ActorStats.AddBuff duration merge behavior.", delegate
        {
            ConfigureBasicFixture();
            ApplyEffectToActor(attackTester.dummyAttacker, "Buff", "Action Buff", 4);
            ApplyEffectToActor(attackTester.dummyAttacker, "Buff", "Action Buff", 5);
            AssertEqual(9, ReturnBuffDuration(attackTester.dummyAttacker, "Action Buff"), "Action Buff duration after merge");
            AssertEqual(1, CountBuff(attackTester.dummyAttacker, "Action Buff"), "Action Buff stack count after merge");
        });
        RunCase("start status applies effect and decrements duration", "Status/Buff", "Inspect EffectManager.StartTurn and Condition.ApplyStartEndEffects.", delegate
        {
            ConfigureBasicFixture();
            UnityEngine.Random.InitState(4110);
            ApplyEffectToActor(attackTester.dummyAttacker, "Status", "Paralyze", 4);
            int beforeActions = attackTester.dummyAttacker.GetActions();
            battleManager.effectManager.StartTurn(attackTester.dummyAttacker, map);
            AssertLess(attackTester.dummyAttacker.GetActions(), beforeActions, "actions after Paralyze start effect");
            AssertEqual(3, attackTester.dummyAttacker.ReturnStatusDuration("Paralyze"), "Paralyze duration after StartTurn");
        });
        RunCase("end status applies effect and decrements duration", "Status/Buff", "Inspect EffectManager.EndTurn and Condition.ApplyStartEndEffects.", delegate
        {
            ConfigureBasicFixture();
            UnityEngine.Random.InitState(4120);
            ApplyEffectToActor(attackTester.dummyAttacker, "Status", "Hungry", 4);
            int beforeEnergy = attackTester.dummyAttacker.GetEnergy();
            battleManager.effectManager.EndTurn(attackTester.dummyAttacker, map);
            AssertLess(attackTester.dummyAttacker.GetEnergy(), beforeEnergy, "energy after Hungry end effect");
            AssertEqual(3, attackTester.dummyAttacker.ReturnStatusDuration("Hungry"), "Hungry duration after EndTurn effects");
        });
        RunCase("start buff applies effect and decrements duration", "Status/Buff", "Inspect EffectManager.StartTurn and Condition.ApplyBuffEffects.", delegate
        {
            ConfigureBasicFixture();
            ApplyEffectToActor(attackTester.dummyAttacker, "Buff", "Action Buff", 4);
            int beforeActions = attackTester.dummyAttacker.GetActions();
            battleManager.effectManager.StartTurn(attackTester.dummyAttacker, map);
            AssertGreater(attackTester.dummyAttacker.GetActions(), beforeActions, "actions after Action Buff start effect");
            AssertEqual(3, ReturnBuffDuration(attackTester.dummyAttacker, "Action Buff"), "Action Buff duration after StartTurn");
        });
        RunCase("other timing status duration decrements on start turn", "Status/Buff", "Inspect Condition.AdjustOtherTimingDurations.", delegate
        {
            ConfigureBasicFixture();
            UnityEngine.Random.InitState(4130);
            ApplyEffectToActor(attackTester.dummyAttacker, "Status", "Vulnerable", 4);
            battleManager.effectManager.StartTurn(attackTester.dummyAttacker, map);
            AssertEqual(3, attackTester.dummyAttacker.ReturnStatusDuration("Vulnerable"), "Vulnerable duration after StartTurn other-timing decrement");
        });
        RunCase("zero-duration status is removed by actor end turn cleanup", "Status/Buff", "Inspect TacticActor.EndTurn and ActorStats.CheckStatusDuration.", delegate
        {
            ConfigureBasicFixture();
            UnityEngine.Random.InitState(4140);
            ApplyEffectToActor(attackTester.dummyAttacker, "Status", "Paralyze", 4);
            for (int i = 0; i < 4; i++)
                battleManager.effectManager.StartTurn(attackTester.dummyAttacker, map);
            AssertEqual(0, attackTester.dummyAttacker.ReturnStatusDuration("Paralyze"), "Paralyze duration after StartTurn decrement");
            attackTester.dummyAttacker.EndTurn();
            AssertTrue(!attackTester.dummyAttacker.StatusExists("Paralyze"), "Paralyze after actor EndTurn cleanup", "removed", "present");
        });
        RunCase("zero-duration buff is removed by actor end turn cleanup", "Status/Buff", "Inspect TacticActor.EndTurn and ActorStats.CheckBuffDuration.", delegate
        {
            ConfigureBasicFixture();
            ApplyEffectToActor(attackTester.dummyAttacker, "Buff", "Action Buff", 4);
            for (int i = 0; i < 4; i++)
                battleManager.effectManager.StartTurn(attackTester.dummyAttacker, map);
            AssertEqual(0, ReturnBuffDuration(attackTester.dummyAttacker, "Action Buff"), "Action Buff duration after StartTurn decrement");
            attackTester.dummyAttacker.EndTurn();
            AssertTrue(!attackTester.dummyAttacker.BuffExists("Action Buff"), "Action Buff after actor EndTurn cleanup", "removed", "present");
        });
    }

    private void AddGuardInterceptTests()
    {
        // Guard/intercept tests use the real attack path. The only setup is giving the test guard a guard duration/range, then checking whether AttackManager redirects damage away from the original defender.
        RunCase("guard in range intercepts attack and protects defender", "Guard/Intercept", "Inspect BattleMap.GetGuardingAlly and AttackManager.ActorAttacksActor guard branch.", delegate
        {
            ConfigureBasicFixture(attackerAttack: 35, attackerHitChance: 999, attackerCritChance: 0, defenderDefense: 0, defenderDodge: 0);
            PlaceGuardInInterceptPosition(2);
            attackTester.dummyGuard.GainGuard(2, 2);
            int defenderBefore = attackTester.dummyDefender.GetHealth();
            int guardBefore = attackTester.dummyGuard.GetHealth();
            string beforeAttackContext = GuardInterceptDebugContext();
            UnityEngine.Random.InitState(3600);
            attackManager.ActorAttacksActor(attackTester.dummyAttacker, attackTester.dummyDefender, map);
            string afterAttackContext = beforeAttackContext + "\nAfter attack: defender HP " + defenderBefore + " -> " + attackTester.dummyDefender.GetHealth() + ", guard HP " + guardBefore + " -> " + attackTester.dummyGuard.GetHealth();
            AssertEqual(defenderBefore, attackTester.dummyDefender.GetHealth(), "defender HP when guarded", afterAttackContext);
            AssertLess(attackTester.dummyGuard.GetHealth(), guardBefore, "guard HP after intercepting", afterAttackContext);
        });
        RunCase("non-guarding ally does not intercept attack", "Guard/Intercept", "Inspect TacticActor.Guarding and BattleMap.GetGuardingAlly.", delegate
        {
            ConfigureBasicFixture(attackerAttack: 35, attackerHitChance: 999, attackerCritChance: 0, defenderDefense: 0, defenderDodge: 0);
            int defenderBefore = attackTester.dummyDefender.GetHealth();
            int guardBefore = attackTester.dummyGuard.GetHealth();
            UnityEngine.Random.InitState(3610);
            attackManager.ActorAttacksActor(attackTester.dummyAttacker, attackTester.dummyDefender, map);
            AssertLess(attackTester.dummyDefender.GetHealth(), defenderBefore, "defender HP without active guard");
            AssertEqual(guardBefore, attackTester.dummyGuard.GetHealth(), "guard HP without active guard");
        });
        RunCase("guard outside range does not intercept attack", "Guard/Intercept", "Inspect BattleMap.AdjustForMeleeGuardRange and guard-range distance checks.", delegate
        {
            ConfigureBasicFixture(attackerAttack: 35, attackerHitChance: 999, attackerCritChance: 0, defenderDefense: 0, defenderDodge: 0);
            attackTester.dummyGuard.SetLocation(24);
            attackTester.dummyGuard.GainGuard(2, 1);
            int defenderBefore = attackTester.dummyDefender.GetHealth();
            int guardBefore = attackTester.dummyGuard.GetHealth();
            UnityEngine.Random.InitState(3620);
            attackManager.ActorAttacksActor(attackTester.dummyAttacker, attackTester.dummyDefender, map);
            AssertLess(attackTester.dummyDefender.GetHealth(), defenderBefore, "defender HP with out-of-range guard");
            AssertEqual(guardBefore, attackTester.dummyGuard.GetHealth(), "guard HP with out-of-range guard");
        });

        RunCase("dead guard does not intercept attack", "Guard/Intercept", "Inspect BattleMap.GetGuardingAlly health check.", delegate
        {
            ConfigureBasicFixture(attackerAttack: 35, attackerHitChance: 999, attackerCritChance: 0, defenderDefense: 0, defenderDodge: 0);
            attackTester.dummyGuard.GainGuard(2, 2);
            attackTester.dummyGuard.SetCurrentHealth(0);
            int defenderBefore = attackTester.dummyDefender.GetHealth();
            UnityEngine.Random.InitState(3630);
            attackManager.ActorAttacksActor(attackTester.dummyAttacker, attackTester.dummyDefender, map);
            AssertLess(attackTester.dummyDefender.GetHealth(), defenderBefore, "defender HP with dead guard");
            AssertEqual(0, attackTester.dummyGuard.GetHealth(), "dead guard HP after attack");
        });

        RunCase("guard duration check clears expired guard state", "Guard/Intercept", "Inspect TacticActor.GainGuard and CheckGuard.", delegate
        {
            ConfigureBasicFixture();
            attackTester.dummyGuard.GainGuard(1, 2);
            attackTester.dummyGuard.CheckGuard();
            AssertTrue(!attackTester.dummyGuard.Guarding(), "guard state after duration check", "not guarding", attackTester.dummyGuard.Guarding().ToString());
            AssertEqual(0, attackTester.dummyGuard.GetGuardRange(), "guard range after expiration");
        });
    }

    private void AddMapMovementTests()
    {
        // These protect map math and movement plumbing. They are deliberately small checks, not full tactical movement scenarios.
        RunCase("adjacency for known center tile is stable", "Map/Movement", "Inspect MapUtility.AdjacentTiles flat-top coordinate mapping.", delegate
        {
            ConfigureBasicFixture();
            List<int> expected = new List<int> { 7, 13, 17, 16, 11, 6 };
            List<int> actual = map.mapUtility.AdjacentTiles(12, 5);
            AssertListEqual(expected, actual, "adjacent tiles for 12 on size 5");
        });
        RunCase("distance is symmetric for adjacent known tiles", "Map/Movement", "Inspect MapUtility.DistanceBetweenTiles.", delegate
        {
            ConfigureBasicFixture();
            int forward = map.mapUtility.DistanceBetweenTiles(12, 17, 5);
            int backward = map.mapUtility.DistanceBetweenTiles(17, 12, 5);
            AssertEqual(1, forward, "distance 12 to 17");
            AssertEqual(forward, backward, "distance symmetry");
        });
        RunCase("terrain change applies expected database interaction", "Map/Movement", "Inspect BattleMap.ChangeTerrain and BattleMapTester tile-tile change database setup.", delegate
        {
            ConfigureBasicFixture();
            if (battleMapTester == null || battleMapTester.tileTileChanges == null || battleMapTester.tileTileChanges.keys == null || battleMapTester.tileTileChanges.keys.Count == 0)
            {
                RecordSkip("terrain change applies expected database interaction", "Map/Movement", "BattleMapTester.tileTileChanges is not available in this scene.", "BattleMapTester");
                return;
            }
            List<string> failures = new List<string>();
            for (int i = 0; i < battleMapTester.tileTileChanges.keys.Count; i++)
            {
                string key = battleMapTester.tileTileChanges.keys[i];
                string[] parts = key.Split('-');
                if (parts.Length < 2)
                {
                    failures.Add(key + " expected valid A-B key, actual malformed key");
                    continue;
                }

                map.ChangeTerrain(0, parts[0], true);
                map.ChangeTerrain(0, parts[1]);
                string interactionResult = battleMapTester.tileTileChanges.ReturnValue(key);
                string expected = interactionResult == "" ? parts[1] : interactionResult;
                string actual = map.mapInfo[0];
                if (actual != expected)
                    failures.Add(key + " expected " + expected + ", actual " + actual);
            }

            AssertTrue(failures.Count == 0, "terrain interaction DB", "all " + battleMapTester.tileTileChanges.keys.Count + " interactions pass", FailureListToString(failures));
        });
        RunCase("terrain-effect change applies expected database interaction", "Map/Movement", "Inspect BattleMap.ChangeTEffect and BattleMapTester effect-effect change database setup.", delegate
        {
            ConfigureBasicFixture();
            if (battleMapTester == null || battleMapTester.effectEffectChanges == null || battleMapTester.effectEffectChanges.keys == null || battleMapTester.effectEffectChanges.keys.Count == 0)
            {
                RecordSkip("terrain-effect change applies expected database interaction", "Map/Movement", "BattleMapTester.effectEffectChanges is not available in this scene.", "BattleMapTester");
                return;
            }
            List<string> failures = new List<string>();
            for (int i = 0; i < battleMapTester.effectEffectChanges.keys.Count; i++)
            {
                string key = battleMapTester.effectEffectChanges.keys[i];
                string[] parts = key.Split('-');
                if (parts.Length < 2)
                {
                    failures.Add(key + " expected valid A-B key, actual malformed key");
                    continue;
                }

                ClearTerrainEffects();
                map.ChangeTEffect(0, parts[0], true);
                map.ChangeTEffect(0, parts[1]);
                string interactionResult = battleMapTester.effectEffectChanges.ReturnValue(key);
                string expected = interactionResult == "" ? parts[1] : interactionResult;
                if (interactionResult.Contains("ChainReplace"))
                {
                    string[] chainReplace = interactionResult.Split(new string[] { ">>" }, StringSplitOptions.None);
                    expected = chainReplace.Length >= 2 ? chainReplace[1] : parts[1];
                }

                string actual = map.terrainEffectTiles[0];
                if (actual != expected)
                    failures.Add(key + " expected " + expected + ", actual " + actual);
            }

            AssertTrue(failures.Count == 0, "terrain-effect interaction DB", "all " + battleMapTester.effectEffectChanges.keys.Count + " interactions pass", FailureListToString(failures));
        });
        RunCase("reachable tiles include a known adjacent open tile", "Map/Movement", "Inspect MoveCostManager.GetAllReachableTiles and BattleMap actor occupancy.", delegate
        {
            ConfigureBasicFixture(attackerMoveSpeed: 2);
            moveManager.UpdateInfoFromBattleMap(map);
            List<int> reachable = moveManager.GetAllReachableTiles(attackTester.dummyAttacker, map.battlingActors);
            AssertTrue(reachable.Contains(11), "reachable tiles", "contains 11", ListToString(reachable));
        });
        RunCase("path cost is stable for a short controlled path", "Map/Movement", "Inspect MoveCostManager.GetAllMoveCosts and GetPrecomputedPath.", delegate
        {
            ConfigureBasicFixture(attackerMoveSpeed: 2);
            moveManager.UpdateInfoFromBattleMap(map);
            moveManager.GetAllMoveCosts(attackTester.dummyAttacker, map.battlingActors);
            List<int> path = moveManager.GetPrecomputedPath(attackTester.dummyAttacker.GetLocation(), 7);
            AssertTrue(path != null && path.Count > 0, "path from 12 to 7", "non-empty", path == null ? "null" : ListToString(path));
            AssertTrue(moveManager.GetMoveCost() >= 1, "move cost from 12 to 7", ">= 1", moveManager.GetMoveCost().ToString());
        });
    }

    private void AddMapStateInteractionTests()
    {
        // These protect map-state mutation rules. They do not run a battle;
        // each row creates one tile/effect/weather combination and checks that the configured interaction database produces the expected map change.
        RunCase("terrain-effect weather interactions apply all configured DB rows", "Map State", "Inspect BattleMap.NextRound terrainWeatherInteractions handling.", delegate
        {
            ConfigureBasicFixture();
            if (map.terrainWeatherInteractions == null || map.terrainWeatherInteractions.keys == null || map.terrainWeatherInteractions.keys.Count == 0)
            {
                RecordSkip("terrain-effect weather interactions apply all configured DB rows", "Map State", "BattleMap.terrainWeatherInteractions is not available.", "BattleMap");
                return;
            }
            List<string> failures = new List<string>();
            for (int i = 0; i < map.terrainWeatherInteractions.keys.Count; i++)
            {
                string key = map.terrainWeatherInteractions.keys[i];
                string[] parts = key.Split('-');
                if (parts.Length < 2)
                {
                    failures.Add(key + " expected valid Effect-Weather key, actual malformed key");
                    continue;
                }
                ResetMapInteractionTile(12, "Plains", parts[0], parts[1]);
                string result = map.terrainWeatherInteractions.ReturnValue(key);
                ApplyTerrainEffectInteractionResult(12, result);
                ValidateTerrainEffectInteractionResult(key, result, parts[0], failures);
            }
            AssertTrue(failures.Count == 0, "terrain-effect/weather DB", "all " + map.terrainWeatherInteractions.keys.Count + " interactions pass", FailureListToString(failures));
        });
        RunCase("terrain-effect tile interactions apply all configured DB rows", "Map State", "Inspect BattleMap.NextRound terrainTileInteractions handling.", delegate
        {
            ConfigureBasicFixture();
            if (map.terrainTileInteractions == null || map.terrainTileInteractions.keys == null || map.terrainTileInteractions.keys.Count == 0)
            {
                RecordSkip("terrain-effect tile interactions apply all configured DB rows", "Map State", "BattleMap.terrainTileInteractions is not available.", "BattleMap");
                return;
            }
            List<string> failures = new List<string>();
            for (int i = 0; i < map.terrainTileInteractions.keys.Count; i++)
            {
                string key = map.terrainTileInteractions.keys[i];
                string[] parts = key.Split('-');
                if (parts.Length < 2)
                {
                    failures.Add(key + " expected valid Tile-Effect key, actual malformed key");
                    continue;
                }
                ResetMapInteractionTile(12, parts[0], parts[1], "");
                string result = map.terrainTileInteractions.ReturnValue(key);
                ApplyTerrainEffectInteractionResult(12, result);
                ValidateTerrainEffectInteractionResult(key, result, parts[1], failures);
            }
            AssertTrue(failures.Count == 0, "terrain-effect/tile DB", "all " + map.terrainTileInteractions.keys.Count + " interactions pass", FailureListToString(failures));
        });
        RunCase("tile weather interactions apply all configured DB rows", "Map State", "Inspect BattleMap.NextRound tileWeatherInteractions handling.", delegate
        {
            ConfigureBasicFixture();
            if (map.tileWeatherInteractions == null || map.tileWeatherInteractions.keys == null || map.tileWeatherInteractions.keys.Count == 0)
            {
                RecordSkip("tile weather interactions apply all configured DB rows", "Map State", "BattleMap.tileWeatherInteractions is not available.", "BattleMap");
                return;
            }
            List<string> failures = new List<string>();
            for (int i = 0; i < map.tileWeatherInteractions.keys.Count; i++)
            {
                string key = map.tileWeatherInteractions.keys[i];
                string[] parts = key.Split('-');
                if (parts.Length < 2)
                {
                    failures.Add(key + " expected valid Tile-Weather key, actual malformed key");
                    continue;
                }

                ResetMapInteractionTile(12, parts[0], "", parts[1]);
                string result = map.tileWeatherInteractions.ReturnValue(key);
                ApplyTileWeatherInteractionResult(12, key, result, failures);
            }
            AssertTrue(failures.Count == 0, "tile/weather DB", "all " + map.tileWeatherInteractions.keys.Count + " interactions pass", FailureListToString(failures));
        });
    }

    private void AddSkillPassiveTests()
    {
        // These cover the active-skill/passive timing surface at a smoke-test level. More specific passive behavior lives in AddMapPassiveTests().
        RunCase("configured active cannot fire without enough actions", "Skills/Passives", "Inspect ActiveManager.CanPaySkillCost action-cost check.", delegate
        {
            ConfigureBasicFixture(attackerAttack: 35, attackerHitChance: 999, attackerCritChance: 0, defenderDefense: 0, defenderDodge: 0);
            if (!PrepareConfiguredActiveOnDefender("configured active cannot fire without enough actions"))
                return;

            int beforeHealth = attackTester.dummyDefender.GetHealth();
            int beforeEnergy = attackTester.dummyAttacker.GetEnergy();
            attackTester.dummyAttacker.SetActions(0);
            bool activated = activeManager.ActivateSkill(battleManager);
            AssertTrue(!activated, "active activation without actions", "false", activated.ToString());
            AssertEqual(beforeHealth, attackTester.dummyDefender.GetHealth(), "defender HP after failed action-cost activation");
            AssertEqual(beforeEnergy, attackTester.dummyAttacker.GetEnergy(), "attacker energy after failed action-cost activation");
            AssertEqual(0, attackTester.dummyAttacker.GetActions(), "attacker actions after failed action-cost activation");
        });

        RunCase("configured active cannot fire without enough energy", "Skills/Passives", "Inspect ActiveManager.CanPaySkillCost energy-cost check.", delegate
        {
            ConfigureBasicFixture(attackerAttack: 35, attackerHitChance: 999, attackerCritChance: 0, defenderDefense: 0, defenderDodge: 0);
            if (!PrepareConfiguredActiveOnDefender("configured active cannot fire without enough energy"))
                return;

            int beforeHealth = attackTester.dummyDefender.GetHealth();
            int beforeActions = attackTester.dummyAttacker.GetActions();
            attackTester.dummyAttacker.LoseEnergy(999);
            bool activated = activeManager.ActivateSkill(battleManager);
            AssertTrue(!activated, "active activation without energy", "false", activated.ToString());
            AssertEqual(beforeHealth, attackTester.dummyDefender.GetHealth(), "defender HP after failed energy-cost activation");
            AssertEqual(beforeActions, attackTester.dummyAttacker.GetActions(), "attacker actions after failed energy-cost activation");
            AssertEqual(0, attackTester.dummyAttacker.GetEnergy(), "attacker energy after failed energy-cost activation");
        });

        RunCase("configured active targetable tiles exclude a computed invalid tile", "Skills/Passives", "Inspect ActiveManager.GetTargetableTiles range filtering.", delegate
        {
            ConfigureBasicFixture(attackerAttack: 35, attackerRange: 1);
            string skillName = attackTester.activeName;
            if (!activeManager.SkillExists(skillName))
            {
                RecordSkip("configured active targetable tiles exclude a computed invalid tile", "Skills/Passives", "Configured active not found: " + skillName, "ActiveManager");
                return;
            }

            activeManager.SetSkillFromName(skillName, attackTester.dummyAttacker);
            moveManager.UpdateInfoFromBattleMap(map);
            List<int> targetable = activeManager.GetTargetableTiles(attackTester.dummyAttacker.GetLocation(), moveManager.actorPathfinder);
            int invalidTile = FindTileOutsideList(targetable);
            AssertTrue(!targetable.Contains(invalidTile), "targetable tiles for computed invalid tile " + invalidTile, "does not contain " + invalidTile, ListToString(targetable));
        });

        RunCase("configured active targets exactly one actor in controlled fixture", "Skills/Passives", "Inspect ActiveManager.GetTargetedTiles and BattleMap.GetActorsOnTiles.", delegate
        {
            ConfigureBasicFixture();
            if (!PrepareConfiguredActiveOnDefender("configured active targets exactly one actor in controlled fixture"))
                return;

            List<int> targetedTiles = activeManager.ReturnTargetedTiles();
            List<TacticActor> targets = map.GetActorsOnTiles(targetedTiles);
            AssertEqual(1, targets.Count, "target actor count for configured active");
            AssertEqual(attackTester.dummyDefender, targets[0], "target actor for configured active");
        });

        RunCase("configured active exposes targetable tiles", "Skills/Passives", "Inspect ActiveManager.SetSkillFromName and targetable tile generation.", delegate
        {
            ConfigureBasicFixture();
            string skillName = attackTester.activeName;
            if (!activeManager.SkillExists(skillName))
            {
                RecordSkip("configured active exposes targetable tiles", "Skills/Passives", "Configured active not found: " + skillName, "ActiveManager");
                return;
            }

            activeManager.SetSkillFromName(skillName, attackTester.dummyAttacker);
            moveManager.UpdateInfoFromBattleMap(map);
            List<int> targetable = activeManager.GetTargetableTiles(attackTester.dummyAttacker.GetLocation(), moveManager.actorPathfinder);
            AssertTrue(targetable.Count > 0, "targetable tile count for " + skillName, "> 0", targetable.Count.ToString());
        });

        RunCase("configured active spends actions or applies damage once", "Skills/Passives", "Inspect BattleManager.ActivateSkill and ActiveManager.ActivateSkill.", delegate
        {
            ConfigureBasicFixture(attackerAttack: 35, attackerHitChance: 999, attackerCritChance: 0, defenderDefense: 0, defenderDodge: 0);
            string skillName = attackTester.activeName;
            if (!activeManager.SkillExists(skillName))
            {
                RecordSkip("configured active spends actions or applies damage once", "Skills/Passives", "Configured active not found: " + skillName, "ActiveManager");
                return;
            }

            activeManager.SetSkillFromName(skillName, attackTester.dummyAttacker);
            moveManager.UpdateInfoFromBattleMap(map);
            activeManager.GetTargetedTiles(attackTester.dummyDefender.GetLocation(), moveManager.actorPathfinder);
            int beforeHealth = attackTester.dummyDefender.GetHealth();
            int beforeActions = attackTester.dummyAttacker.GetActions();
            UnityEngine.Random.InitState(5100);
            battleManager.ActivateSkill(skillName, attackTester.dummyAttacker);
            bool changed = attackTester.dummyDefender.GetHealth() < beforeHealth || attackTester.dummyAttacker.GetActions() < beforeActions;
            AssertTrue(changed, "active result for " + skillName, "damage or action spend", "HP " + beforeHealth + " -> " + attackTester.dummyDefender.GetHealth() + ", actions " + beforeActions + " -> " + attackTester.dummyAttacker.GetActions());
        });

        RunCase("end turn preserves temporary status duration until current Unity decrement timing", "Skills/Passives", "Inspect TacticActor.EndTurn and ActorStats.CheckStatusDuration.", delegate
        {
            ConfigureBasicFixture();
            UnityEngine.Random.InitState(5200);
            ApplyEffectToActor(attackTester.dummyAttacker, "Status", "Paralyze", 4);
            attackTester.dummyAttacker.EndTurn();
            AssertEqual(4, attackTester.dummyAttacker.ReturnStatusDuration("Paralyze"), "Paralyze duration after one actor-only EndTurn");
        });
    }

    private void AddMapPassiveTests()
    {
        // Map passives come from the map state itself: current weather, tile, terrain effect, or building. These tests check both DB shape and a few concrete effects that should stay fast and deterministic.
        RunCase("map passive databases expose expected timing slots", "Map Passives", "Inspect TerrainPassivesList data for weather, terrain, terrain effects, and buildings.", delegate
        {
            ConfigureBasicFixture();
            List<string> failures = new List<string>();
            ValidateMapPassiveTimingSlots("weather", map.weatherPassives, failures);
            ValidateMapPassiveTimingSlots("terrain", map.terrainPassives, failures);
            ValidateMapPassiveTimingSlots("terrain effect", map.terrainEffectData, failures);
            ValidateMapPassiveTimingSlots("building", GetBuildingPassiveData(), failures);
            AssertTrue(failures.Count == 0, "map passive DB timing slots", "all rows have attack/defend/move/start/end slots", FailureListToString(failures));
        });

        RunCase("weather start passive applies status from current weather", "Map Passives", "Inspect BattleMap.ApplyWeatherStartEffect and WeatherPassives Cutting Wind row.", delegate
        {
            ConfigureBasicFixture();
            map.SetWeather("Cutting Wind");
            map.ApplyWeatherStartEffect(attackTester.dummyAttacker);
            AssertEqual(1, CountStatus(attackTester.dummyAttacker, "Bleed"), "Bleed status count after Cutting Wind start");
        });

        RunCase("tile start passive applies status from current terrain", "Map Passives", "Inspect BattleMap.ApplyTileStartEffect and BasicTerrainPassives Snow row.", delegate
        {
            ConfigureBasicFixture();
            map.ChangeTerrain(attackTester.dummyAttacker.GetLocation(), "Snow", true);
            map.ApplyTileStartEffect(attackTester.dummyAttacker);
            AssertEqual(1, CountStatus(attackTester.dummyAttacker, "Cold"), "Cold status count after Snow start");
        });

        RunCase("terrain-effect moving passive applies damage from current terrain effect", "Map Passives", "Inspect BattleMap.ApplyMovingTileEffect and SpecialTerrainPassives Fire row.", delegate
        {
            ConfigureBasicFixture();
            map.ChangeTEffect(attackTester.dummyAttacker.GetLocation(), "Fire", true);
            int before = attackTester.dummyAttacker.GetHealth();
            map.ApplyMovingTileEffect(attackTester.dummyAttacker, attackTester.dummyAttacker.GetLocation(), moveManager);
            AssertLess(attackTester.dummyAttacker.GetHealth(), before, "health after Fire terrain-effect moving passive");
        });

        RunCase("terrain-effect end passive applies status from current terrain effect", "Map Passives", "Inspect BattleMap.ApplyEndTerrainEffect and SpecialTerrainPassives Water row.", delegate
        {
            ConfigureBasicFixture();
            map.ChangeTEffect(attackTester.dummyAttacker.GetLocation(), "Water", true);
            map.ApplyEndTerrainEffect(attackTester.dummyAttacker);
            AssertEqual(1, CountStatus(attackTester.dummyAttacker, "Wet"), "Wet status count after Water end");
        });

        RunCase("building start passive applies from current building state", "Map Passives", "Inspect BattleMap.ApplyBuildingStartEffect and BuildingPassives Castle row.", delegate
        {
            ConfigureBasicFixture();
            TerrainPassivesList buildingPassives = GetBuildingPassiveData();
            if (buildingPassives == null)
            {
                RecordSkip("building start passive applies from current building state", "Map Passives", "Building passive data is not assigned.", "BattleMap or AttackManager");
                return;
            }

            map.AddBuilding("Castle", attackTester.dummyAttacker.GetLocation());
            attackTester.dummyAttacker.LoseEnergy(1);
            int before = attackTester.dummyAttacker.GetEnergy();
            ApplyBuildingStartEffectWithFallback(attackTester.dummyAttacker, buildingPassives);
            AssertEqual(before + 1, attackTester.dummyAttacker.GetEnergy(), "energy after Castle start passive");
        });

        RunCase("building end passive applies from current building state", "Map Passives", "Inspect BattleMap.ApplyBuildingEndEffect and BuildingPassives Castle row.", delegate
        {
            ConfigureBasicFixture();
            TerrainPassivesList buildingPassives = GetBuildingPassiveData();
            if (buildingPassives == null)
            {
                RecordSkip("building end passive applies from current building state", "Map Passives", "Building passive data is not assigned.", "BattleMap or AttackManager");
                return;
            }

            map.AddBuilding("Castle", attackTester.dummyAttacker.GetLocation());
            int before = attackTester.dummyAttacker.GetWeight();
            ApplyBuildingEndEffectWithFallback(attackTester.dummyAttacker, buildingPassives);
            AssertEqual(before + 3, attackTester.dummyAttacker.GetWeight(), "weight after Castle end passive");
        });
    }

    private void AddAiTests()
    {
        RunCase("closest hostile target is selected from controlled fixture", "AI", "Inspect ActorAI.GetClosestEnemy and MoveCostManager path costs.", delegate
        {
            ConfigureBasicFixture();
            ActorAI actorAI = battleManager.actorAI;
            if (actorAI == null)
            {
                RecordSkip("closest hostile target is selected from controlled fixture", "AI", "BattleManager.actorAI is not assigned.", "BattleManager");
                return;
            }

            moveManager.UpdateInfoFromBattleMap(map);
            moveManager.GetAllMoveCosts(attackTester.dummyAttacker, map.battlingActors);
            TacticActor closest = actorAI.GetClosestEnemy(map.battlingActors, attackTester.dummyAttacker, moveManager);
            AssertEqual(attackTester.dummyDefender, closest, "closest hostile target");
        });

        RunCase("retained target stays selected while alive and hostile", "AI", "Inspect ActorAI.FindPathToTarget target-retention branch.", delegate
        {
            ConfigureBasicFixture();
            ActorAI actorAI = battleManager.actorAI;
            if (actorAI == null)
            {
                RecordSkip("retained target stays selected while alive and hostile", "AI", "BattleManager.actorAI is not assigned.", "BattleManager");
                return;
            }

            attackTester.dummyAttacker.SetTarget(attackTester.dummyDefender);
            moveManager.UpdateInfoFromBattleMap(map);
            actorAI.FindPathToTarget(attackTester.dummyAttacker, map, moveManager);
            AssertEqual(attackTester.dummyDefender, attackTester.dummyAttacker.GetTarget(), "retained target");
        });
    }

    private void ConfigureBasicFixture(
        int attackerHealth = 60,
        int attackerAttack = 20,
        int attackerRange = 1,
        int attackerDefense = 0,
        int attackerMoveSpeed = 3,
        string attackerMoveType = "Walking",
        int attackerHitChance = 100,
        int attackerCritChance = 0,
        int defenderHealth = 60,
        int defenderDefense = 0,
        int defenderDodge = 0)
    {
        EnsureReferences();
        UnityEngine.Random.InitState(1000);

        attackTester.dummyTime = "";
        attackTester.dummyWeather = "";
        attackTester.attackerLocation = 12;
        attackTester.defenderLocation = 13;
        attackTester.guardLocation = 18;
        attackTester.guard = false;

        attackTester.attackerTile = "Plains";
        attackTester.defenderTile = "Plains";
        attackTester.guardTile = "Plains";
        attackTester.attackerElevation = 0;
        attackTester.defenderElevation = 0;
        attackTester.guardElevation = 0;
        attackTester.attackerTEffect = "";
        attackTester.defenderTEffect = "";
        attackTester.guardTEffect = "";
        attackTester.attackerBuilding = "";
        attackTester.defenderBuilding = "";
        attackTester.attackerBorders = EmptyBorders();
        attackTester.defenderBorders = EmptyBorders();
        attackTester.guardBorders = EmptyBorders();

        attackTester.testAttackerPassives = new List<string>();
        attackTester.testAttackerPassiveLevels = new List<string>();
        attackTester.testDefenderPassives = new List<string>();
        attackTester.testDefenderPassiveLevels = new List<string>();
        attackTester.testGuardPassives = new List<string>();
        attackTester.testGuardPassiveLevels = new List<string>();
        attackTester.testAttackerBuffs = new List<string>();
        attackTester.testDefenderBuffs = new List<string>();
        attackTester.testAttackerAuras = new List<string>();
        attackTester.testDefenderAuras = new List<string>();

        attackTester.attackerStats = BuildStats(attackTester.dummyAttacker, "Unit Test Attacker", attackerHealth, attackerAttack, attackerRange, attackerDefense, attackerMoveSpeed, attackerMoveType, attackerHitChance, 0, attackerCritChance, "Triple Stab");
        attackTester.defenderStats = BuildStats(attackTester.dummyDefender, "Unit Test Defender", defenderHealth, 10, 1, defenderDefense, 3, "Walking", 100, defenderDodge, 0, "");
        attackTester.guardStats = BuildStats(attackTester.dummyGuard, "Unit Test Guard", 60, 10, 1, 0, 3, "Walking", 100, 0, 0, "");

        attackTester.InitializeMap();
        attackTester.dummyAttacker.SetPersonalName("Unit Test Attacker");
        attackTester.dummyDefender.SetPersonalName("Unit Test Defender");
        attackTester.dummyGuard.SetPersonalName("Unit Test Guard");
        UnityEngine.Random.InitState(1000);
    }

    private string BuildStats(TacticActor template, string sprite, int health, int attack, int range, int defense, int moveSpeed, string moveType, int hitChance, int dodge, int critChance, string actives)
    {
        List<string> values = new List<string>();
        for (int i = 0; i < template.statNames.Count; i++)
            values.Add("");

        SetStat(values, template, "Sprite", sprite);
        SetStat(values, template, "Health", health.ToString());
        SetStat(values, template, "Attack", attack.ToString());
        SetStat(values, template, "Range", range.ToString());
        SetStat(values, template, "Defense", defense.ToString());
        SetStat(values, template, "MoveSpeed", moveSpeed.ToString());
        SetStat(values, template, "MoveType", moveType);
        SetStat(values, template, "Weight", "1");
        SetStat(values, template, "Initiative", "0");
        SetStat(values, template, "Luck", "0");
        SetStat(values, template, "CritChance", critChance.ToString());
        SetStat(values, template, "CritPower", "200");
        SetStat(values, template, "HitChance", hitChance.ToString());
        SetStat(values, template, "Dodge", dodge.ToString());
        SetStat(values, template, "AttackSpeed", "0");
        SetStat(values, template, "Energy", "10");
        SetStat(values, template, "Actives", actives);
        SetStat(values, template, "MagicPower", "0");
        SetStat(values, template, "MagicResist", "0");
        SetStat(values, template, "ManaEfficiency", "0");
        SetStat(values, template, "MaxMana", "0");
        SetStat(values, template, "CurrentMana", "0");
        SetStat(values, template, "CurrentHealth", health.ToString());
        return string.Join(template.delimiter, values.ToArray());
    }

    private void SetStat(List<string> values, TacticActor template, string name, string value)
    {
        int index = template.statNames.IndexOf(name);
        if (index >= 0)
            values[index] = value;
    }

    private List<string> EmptyBorders()
    {
        return new List<string> { "", "", "", "", "", "" };
    }

    private void ClearTerrainEffects()
    {
        for (int i = 0; i < map.terrainEffectTiles.Count; i++)
            map.terrainEffectTiles[i] = "";
    }

    private void ResetMapInteractionTile(int tile, string terrain, string terrainEffect, string weather)
    {
        ClearTerrainEffects();
        map.ChangeTerrain(tile, terrain, true);
        map.ChangeTEffect(tile, terrainEffect, true);
        map.SetWeather(weather);
        UnityEngine.Random.InitState(7000);
    }

    private void ApplyTerrainEffectInteractionResult(int tile, string result)
    {
        switch (result)
        {
            case "":
                break;
            case "Remove":
                map.ChangeTEffect(tile, "");
                break;
            case "Expand":
                map.SpreadTerrainEffect(tile);
                break;
            case "Spread":
                map.RandomlySpreadTerrainEffect(tile);
                break;
        }
    }

    private void ValidateTerrainEffectInteractionResult(string key, string result, string originalEffect, List<string> failures)
    {
        switch (result)
        {
            case "":
                if (map.terrainEffectTiles[12] != originalEffect)
                    failures.Add(key + " expected " + originalEffect + " to remain, actual " + map.terrainEffectTiles[12]);
                break;
            case "Remove":
                if (map.terrainEffectTiles[12] != "")
                    failures.Add(key + " expected removed effect, actual " + map.terrainEffectTiles[12]);
                break;
            case "Expand":
            case "Spread":
                if (map.terrainEffectTiles[12] != originalEffect || CountTerrainEffect(originalEffect) < 2)
                    failures.Add(key + " expected " + result + " to keep center and add at least one " + originalEffect + ", actual center " + map.terrainEffectTiles[12] + ", count " + CountTerrainEffect(originalEffect));
                break;
            default:
                failures.Add(key + " expected result '', Remove, Expand, or Spread, actual " + result);
                break;
        }
    }

    private void ApplyTileWeatherInteractionResult(int tile, string key, string result, List<string> failures)
    {
        if (result == "")
            return;

        string[] action = result.Split('-');
        if (action.Length < 2)
        {
            failures.Add(key + " expected valid Tile-X or Feature-X result, actual " + result);
            return;
        }

        switch (action[0])
        {
            case "Tile":
                string startingTerrain = map.mapInfo[tile];
                map.ChangeTerrain(tile, action[1]);
                string expectedTerrain = ExpectedTerrainAfterChange(startingTerrain, action[1]);
                if (map.mapInfo[tile] != expectedTerrain)
                    failures.Add(key + " expected tile " + expectedTerrain + ", actual " + map.mapInfo[tile]);
                break;
            case "Feature":
                map.ChangeTEffect(tile, action[1]);
                if (map.terrainEffectTiles[tile] != action[1])
                    failures.Add(key + " expected terrain effect " + action[1] + ", actual " + map.terrainEffectTiles[tile]);
                break;
            default:
                failures.Add(key + " expected Tile or Feature result, actual " + result);
                break;
        }
    }

    private string ExpectedTerrainAfterChange(string startingTerrain, string changedTerrain)
    {
        if (battleMapTester == null || battleMapTester.tileTileChanges == null)
            return changedTerrain;
        string interactionResult = battleMapTester.tileTileChanges.ReturnValue(startingTerrain + "-" + changedTerrain);
        return interactionResult == "" ? changedTerrain : interactionResult;
    }

    private int CountTerrainEffect(string terrainEffect)
    {
        int count = 0;
        for (int i = 0; i < map.terrainEffectTiles.Count; i++)
        {
            if (map.terrainEffectTiles[i] == terrainEffect)
                count++;
        }
        return count;
    }

    private void ApplyEffectToActor(TacticActor actor, string effect, string specifics, int level = 1)
    {
        battleManager.effectManager.passive.AffectActor(actor, effect, specifics, level, map.combatLog);
    }

    private bool PrepareConfiguredActiveOnDefender(string testName)
    {
        string skillName = attackTester.activeName;
        if (!activeManager.SkillExists(skillName))
        {
            RecordSkip(testName, "Skills/Passives", "Configured active not found: " + skillName, "ActiveManager");
            return false;
        }

        activeManager.SetSkillFromName(skillName, attackTester.dummyAttacker);
        moveManager.UpdateInfoFromBattleMap(map);
        activeManager.GetTargetableTiles(attackTester.dummyAttacker.GetLocation(), moveManager.actorPathfinder);
        activeManager.GetTargetedTiles(attackTester.dummyDefender.GetLocation(), moveManager.actorPathfinder);
        return true;
    }

    private int FindTileOutsideList(List<int> excludedTiles)
    {
        for (int i = map.mapInfo.Count - 1; i >= 0; i--)
        {
            if (!excludedTiles.Contains(i))
                return i;
        }
        throw new InvalidOperationException("Could not find a tile outside the provided targetable list.");
    }

    private void ApplyFullEndTurnFlow(TacticActor actor)
    {
        actor.EndTurn();
        battleManager.effectManager.EndTurn(actor, map);
        map.ApplyEndTerrainEffect(actor);
    }

    private string GuardInterceptDebugContext()
    {
        TacticActor selectedGuard = map.GetGuardingAlly(attackTester.dummyDefender, attackTester.dummyAttacker);
        return "Before attack guard context:"
            + "\n  attacker loc/team/HP: " + attackTester.dummyAttacker.GetLocation() + "/" + attackTester.dummyAttacker.GetTeam() + "/" + attackTester.dummyAttacker.GetHealth()
            + "\n  defender loc/team/HP: " + attackTester.dummyDefender.GetLocation() + "/" + attackTester.dummyDefender.GetTeam() + "/" + attackTester.dummyDefender.GetHealth()
            + "\n  guard loc/team/HP: " + attackTester.dummyGuard.GetLocation() + "/" + attackTester.dummyGuard.GetTeam() + "/" + attackTester.dummyGuard.GetHealth()
            + "\n  guard active/range: " + attackTester.dummyGuard.Guarding() + "/" + attackTester.dummyGuard.GetGuardRange()
            + "\n  distance attacker-defender: " + map.DistanceBetweenActors(attackTester.dummyAttacker, attackTester.dummyDefender)
            + "\n  distance defender-guard: " + map.DistanceBetweenActors(attackTester.dummyDefender, attackTester.dummyGuard)
            + "\n  distance attacker-guard: " + map.DistanceBetweenActors(attackTester.dummyAttacker, attackTester.dummyGuard)
            + "\n  selected guard: " + (selectedGuard == null ? "none" : selectedGuard.GetPersonalName());
    }

    private void PlaceGuardInInterceptPosition(int guardRange)
    {
        List<int> adjacentToDefender = map.mapUtility.AdjacentTiles(attackTester.dummyDefender.GetLocation(), map.mapSize);
        for (int i = 0; i < adjacentToDefender.Count; i++)
        {
            int tile = adjacentToDefender[i];
            if (tile == attackTester.dummyAttacker.GetLocation() || tile == attackTester.dummyDefender.GetLocation())
                continue;
            int distanceToAttacker = map.mapUtility.DistanceBetweenTiles(tile, attackTester.dummyAttacker.GetLocation(), map.mapSize);
            if (distanceToAttacker <= guardRange)
            {
                attackTester.dummyGuard.SetLocation(tile);
                return;
            }
        }
        throw new InvalidOperationException("Could not find an adjacent guard tile within guard range " + guardRange + " for the TestBattle guard fixture.");
    }

    private TerrainPassivesList GetBuildingPassiveData()
    {
        if (map != null && map.buildingEffectData != null)
            return map.buildingEffectData;
        if (attackManager != null && attackManager.buildingPassives != null)
            return attackManager.buildingPassives;
        return null;
    }

    private void ValidateMapPassiveTimingSlots(string label, TerrainPassivesList passiveData, List<string> failures)
    {
        if (passiveData == null)
        {
            failures.Add(label + " passive data is not assigned");
            return;
        }
        if (passiveData.delimiterTwo == "")
        {
            failures.Add(label + " passive data has no delimiterTwo");
            return;
        }

        for (int i = 0; i < passiveData.keys.Count; i++)
        {
            string key = passiveData.keys[i];
            string[] slots = passiveData.ReturnValue(key).Split(new string[] { passiveData.delimiterTwo }, StringSplitOptions.None);
            if (slots.Length < 5)
                failures.Add(label + " " + key + " expected 5 timing slots, actual " + slots.Length);
        }
    }

    private void ApplyBuildingStartEffectWithFallback(TacticActor actor, TerrainPassivesList buildingPassives)
    {
        TerrainPassivesList original = map.buildingEffectData;
        map.buildingEffectData = buildingPassives;
        try
        {
            map.ApplyBuildingStartEffect(actor);
        }
        finally
        {
            map.buildingEffectData = original;
        }
    }

    private void ApplyBuildingEndEffectWithFallback(TacticActor actor, TerrainPassivesList buildingPassives)
    {
        TerrainPassivesList original = map.buildingEffectData;
        map.buildingEffectData = buildingPassives;
        try
        {
            map.ApplyBuildingEndEffect(actor);
        }
        finally
        {
            map.buildingEffectData = original;
        }
    }

    private int CountStatus(TacticActor actor, string statusName)
    {
        int count = 0;
        List<string> statuses = actor.GetStatuses();
        for (int i = 0; i < statuses.Count; i++)
        {
            if (statuses[i] == statusName)
                count++;
        }
        return count;
    }

    private int CountBuff(TacticActor actor, string buffName)
    {
        int count = 0;
        List<string> buffs = actor.GetBuffs();
        for (int i = 0; i < buffs.Count; i++)
        {
            if (buffs[i] == buffName)
                count++;
        }
        return count;
    }

    private int ReturnBuffDuration(TacticActor actor, string buffName)
    {
        int index = actor.GetBuffs().IndexOf(buffName);
        if (index < 0)
            return 0;
        return actor.GetBuffDurations()[index];
    }

    private void ResolveReferences()
    {
        if (attackTester == null)
            attackTester = FindLoadedObject<AttackManagerTester>();
        if (battleMapTester == null)
            battleMapTester = FindLoadedObject<BattleMapTester>();
        if (mapStateTester == null)
            mapStateTester = FindLoadedObject<MapStateTester>();
        if (passiveEffectTester == null)
            passiveEffectTester = FindLoadedObject<PassiveEffectTester>();
        if (activeTester == null)
            activeTester = FindLoadedObject<ActiveTester>();
        if (miscTester == null)
            miscTester = FindLoadedObject<MiscTester>();

        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (attackTester == null)
            throw new InvalidOperationException("BattleUnitTestRunner requires an AttackManagerTester reference in TestBattle.");

        map = attackTester.map;
        battleManager = attackTester.battleManager;
        activeManager = attackTester.activeManager;
        moveManager = battleManager == null ? null : battleManager.moveManager;
        attackManager = attackTester.attackManager;

        if (map == null)
            throw new InvalidOperationException("AttackManagerTester.map is not assigned.");
        if (battleManager == null)
            throw new InvalidOperationException("AttackManagerTester.battleManager is not assigned.");
        if (activeManager == null)
            throw new InvalidOperationException("AttackManagerTester.activeManager is not assigned.");
        if (moveManager == null)
            throw new InvalidOperationException("BattleMap.moveManager is not assigned.");
        if (attackManager == null)
            throw new InvalidOperationException("AttackManagerTester.attackManager is not assigned.");
    }

    private T FindLoadedObject<T>() where T : UnityEngine.Object
    {
        T[] objects = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < objects.Length; i++)
        {
            Component component = objects[i] as Component;
            if (component == null || (component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded))
                return objects[i];
        }
        return null;
    }

    private void RunCase(string testName, string subsystem, string likelyNextFile, Action test)
    {
        try
        {
            test();
            bool alreadyRecorded = false;
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].name == testName && results[i].subsystem == subsystem)
                    alreadyRecorded = true;
            }
            if (!alreadyRecorded)
                RecordPass(testName, subsystem, likelyNextFile);
        }
        catch (AssertionException assertion)
        {
            TestResult result = new TestResult(testName, subsystem, TestStatus.Fail, assertion.expected, assertion.actual, likelyNextFile, assertion.Message);
            results.Add(result);
            Debug.LogError(FormatFailureForConsole(result));
        }
        catch (Exception exception)
        {
            TestResult result = new TestResult(testName, subsystem, TestStatus.Fail, "no exception", exception.GetType().Name, likelyNextFile, exception.Message);
            results.Add(result);
            Debug.LogError(FormatFailureForConsole(result));
        }
    }

    private void AssertEqual<T>(T expected, T actual, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new AssertionException(label, ValueToString(expected), ValueToString(actual));
    }

    private void AssertEqual<T>(T expected, T actual, string label, string context)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new AssertionException(label + "\n" + context, ValueToString(expected), ValueToString(actual));
    }

    private void AssertLess(int actual, int upperBound, string label)
    {
        if (actual >= upperBound)
            throw new AssertionException(label, "< " + upperBound, actual.ToString());
    }

    private void AssertGreater(int actual, int lowerBound, string label)
    {
        if (actual <= lowerBound)
            throw new AssertionException(label, "> " + lowerBound, actual.ToString());
    }

    private void AssertLess(int actual, int upperBound, string label, string context)
    {
        if (actual >= upperBound)
            throw new AssertionException(label + "\n" + context, "< " + upperBound, actual.ToString());
    }

    private void AssertTrue(bool condition, string label, string expected, string actual)
    {
        if (!condition)
            throw new AssertionException(label, expected, actual);
    }

    private void AssertListEqual(List<int> expected, List<int> actual, string label)
    {
        if (expected.Count != actual.Count)
            throw new AssertionException(label, ListToString(expected), ListToString(actual));
        for (int i = 0; i < expected.Count; i++)
        {
            if (expected[i] != actual[i])
                throw new AssertionException(label, ListToString(expected), ListToString(actual));
        }
    }

    private void RecordPass(string name, string subsystem, string likelyNextFile)
    {
        results.Add(new TestResult(name, subsystem, TestStatus.Pass, "", "", likelyNextFile, ""));
        if (logPassedTests)
            Debug.Log("[BattleUnitTestRunner] PASS - " + subsystem + " - " + name);
    }

    private void RecordSkip(string name, string subsystem, string reason, string likelyNextFile)
    {
        results.Add(new TestResult(name, subsystem, TestStatus.Skip, "available fixture data", reason, likelyNextFile, reason));
        Debug.LogWarning("[BattleUnitTestRunner] SKIP - " + subsystem + " - " + name + ": " + reason);
    }

    private void WriteReport(string suiteName)
    {
        int passed = results.Count(result => result.status == TestStatus.Pass);
        int failed = results.Count(result => result.status == TestStatus.Fail);
        int skipped = results.Count(result => result.status == TestStatus.Skip);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Battle Unit Test Report");
        builder.AppendLine("Suite: " + suiteName);
        builder.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        builder.AppendLine("");

        for (int i = 0; i < results.Count; i++)
        {
            TestResult result = results[i];
            builder.AppendLine(result.status.ToString().ToUpperInvariant() + " | " + result.subsystem + " | " + result.name);
            if (result.status != TestStatus.Pass)
            {
                builder.AppendLine("  Expected: " + result.expected);
                builder.AppendLine("  Actual: " + result.actual);
                builder.AppendLine("  Likely next file to inspect: " + result.likelyNextFile);
                if (result.details != "")
                    builder.AppendLine("  Details: " + result.details);
            }
        }

        builder.AppendLine("");
        builder.AppendLine("Total passed: " + passed);
        builder.AppendLine("Total failed: " + failed);
        builder.AppendLine("Total skipped: " + skipped);

        string fullPath = FullReportPath();
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllText(fullPath, builder.ToString());

        if (failed > 0)
            Debug.LogError("[BattleUnitTestRunner] " + suiteName + " complete. Passed: " + passed + ", Failed: " + failed + ", Skipped: " + skipped + ". Report: " + fullPath);
        else
            Debug.Log("[BattleUnitTestRunner] " + suiteName + " complete. Passed: " + passed + ", Failed: " + failed + ", Skipped: " + skipped + ". Report: " + fullPath);
    }

    private string FullReportPath()
    {
        if (Path.IsPathRooted(reportPath))
            return reportPath;

        string root = Application.dataPath;
        if (reportPath.StartsWith("Assets/") || reportPath.StartsWith("Assets\\"))
            return Path.Combine(root.Substring(0, root.Length - "Assets".Length), reportPath);

        return Path.Combine(root, reportPath);
    }

    private string ValueToString(object value)
    {
        return value == null ? "null" : value.ToString();
    }

    private string ListToString(List<int> values)
    {
        return "[" + string.Join(", ", values.Select(value => value.ToString()).ToArray()) + "]";
    }

    private string FailureListToString(List<string> failures)
    {
        return failures.Count == 0 ? "none" : string.Join("; ", failures.ToArray());
    }

    private string FormatFailureForConsole(TestResult result)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("[BattleUnitTestRunner] FAIL");
        builder.AppendLine("Subsystem: " + result.subsystem);
        builder.AppendLine("Test: " + result.name);
        builder.AppendLine("Expected: " + result.expected);
        builder.AppendLine("Actual: " + result.actual);
        builder.AppendLine("Likely next file to inspect: " + result.likelyNextFile);
        if (result.details != "")
            builder.AppendLine("Details: " + result.details);
        return builder.ToString();
    }

    private enum TestStatus
    {
        Pass,
        Fail,
        Skip
    }

    private class TestResult
    {
        public readonly string name;
        public readonly string subsystem;
        public readonly TestStatus status;
        public readonly string expected;
        public readonly string actual;
        public readonly string likelyNextFile;
        public readonly string details;

        public TestResult(string name, string subsystem, TestStatus status, string expected, string actual, string likelyNextFile, string details)
        {
            this.name = name;
            this.subsystem = subsystem;
            this.status = status;
            this.expected = expected;
            this.actual = actual;
            this.likelyNextFile = likelyNextFile;
            this.details = details;
        }
    }

    private class AssertionException : Exception
    {
        public readonly string expected;
        public readonly string actual;

        public AssertionException(string label, string expected, string actual) : base(label)
        {
            this.expected = expected;
            this.actual = actual;
        }
    }
}
