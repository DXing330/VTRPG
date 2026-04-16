using System.Collections.Generic;
using UnityEngine;

public class CombatUpgradeTester : MonoBehaviour
{
    public AttackManagerTester attackTester;
    public BattleManager battleManager;
    public ActiveManager activeManager;
    protected List<string> debugLines = new List<string>();

    [Header("Scenario Overrides")]
    public bool guardIsAlly = false;
    public bool killDefender = false;
    public bool killGuard = false;
    public int forcedAttackerEnergy = -1;
    public int forcedAttackerActions = -1;
    public int triggeredDepthLimitOverride = -1;
    public List<string> preloadNextSkillMods = new List<string>();

    [Header("Single Skill")]
    public string activeName;
    public int activeTargetTile;

    [Header("Second Skill")]
    public string secondActiveName;
    public int secondActiveTargetTile;

    [Header("Preview")]
    public string previewActiveName;

    [ContextMenu("Test Single Active")]
    public void TestSingleActive()
    {
        StartDebugReport("Test Single Active");
        InitializeScenario();
        CastActive(activeName, activeTargetTile, "single");
        DebugLatestCombatLog();
        PrintDebugReport();
    }

    [ContextMenu("Test Active Sequence")]
    public void TestActiveSequence()
    {
        StartDebugReport("Test Active Sequence");
        InitializeScenario();
        CastActive(activeName, activeTargetTile, "first");
        CastActive(secondActiveName, secondActiveTargetTile, "second");
        DebugLatestCombatLog();
        PrintDebugReport();
    }

    [ContextMenu("Test Preview Without Cast")]
    public void TestPreviewWithoutCast()
    {
        StartDebugReport("Test Preview Without Cast");
        InitializeScenario();
        LogState("Before preview");
        activeManager.SetSkillFromName(previewActiveName, attackTester.dummyAttacker);
        LogLoadedActiveDetails("Loaded preview");
        activeManager.GetTargetableTiles(attackTester.dummyAttacker.GetLocation(), battleManager.moveManager.actorPathfinder);
        LogState("After preview");
        PrintDebugReport();
    }

    [ContextMenu("Test Failed Active Cost")]
    public void TestFailedActiveCost()
    {
        StartDebugReport("Test Failed Active Cost");
        InitializeScenario();
        attackTester.dummyAttacker.currentEnergy = 0;
        attackTester.dummyAttacker.SetActions(0);
        CastActive(activeName, activeTargetTile, "failed-cost");
        DebugLatestCombatLog();
        PrintDebugReport();
    }

    [ContextMenu("Test Passive Grants NextSkillMod")]
    public void TestPassiveGrantsNextSkillMod()
    {
        StartDebugReport("Test Passive Grants NextSkillMod");
        InitializeScenario();
        activeManager.active.AffectActor(attackTester.dummyAttacker, "NextSkillMod", "Power", 1);
        AddDebugLine("Simulated passive effect: NextSkillMod Power");
        LogState("After passive effect");
        CastActive(activeName, activeTargetTile, "after-passive");
        DebugLatestCombatLog();
        PrintDebugReport();
    }

    [ContextMenu("Test Aura Grants NextSkillMod")]
    public void TestAuraGrantsNextSkillMod()
    {
        StartDebugReport("Test Aura Grants NextSkillMod");
        InitializeScenario();
        activeManager.active.AffectActor(attackTester.dummyAttacker, "NextSkillMod", "Power", 1);
        AddDebugLine("Simulated aura effect: NextSkillMod Power");
        LogState("After aura effect");
        CastActive(activeName, activeTargetTile, "after-aura");
        DebugLatestCombatLog();
        PrintDebugReport();
    }

    protected void InitializeScenario()
    {
        ResolveReferences();
        attackTester.InitializeMap();
        if (guardIsAlly)
        {
            attackTester.dummyGuard.SetTeam(attackTester.dummyAttacker.GetTeam());
        }
        if (killDefender)
        {
            attackTester.dummyDefender.SetCurrentHealth(0);
        }
        if (killGuard)
        {
            attackTester.dummyGuard.SetCurrentHealth(0);
        }
        if (forcedAttackerEnergy >= 0)
        {
            attackTester.dummyAttacker.currentEnergy = forcedAttackerEnergy;
        }
        if (forcedAttackerActions >= 0)
        {
            attackTester.dummyAttacker.SetActions(forcedAttackerActions);
        }
        if (triggeredDepthLimitOverride >= 0)
        {
            activeManager.triggeredSkillDepthLimit = triggeredDepthLimitOverride;
            AddDebugLine("Applied triggeredDepthLimitOverride=" + triggeredDepthLimitOverride);
        }
        if (activeManager.triggeredSkillResolver != null)
        {
            activeManager.triggeredSkillResolver.ClearDebugMessages();
        }
        for (int i = 0; i < preloadNextSkillMods.Count; i++)
        {
            attackTester.dummyAttacker.AddNextSkillMod(preloadNextSkillMods[i]);
        }
        LogState("Initialized");
    }

    protected void ResolveReferences()
    {
        if (attackTester == null)
        {
            attackTester = GetComponent<AttackManagerTester>();
        }
        if (battleManager == null && attackTester != null)
        {
            battleManager = attackTester.battleManager;
        }
        if (activeManager == null && attackTester != null)
        {
            activeManager = attackTester.activeManager;
        }
    }

    protected void StartDebugReport(string title)
    {
        debugLines.Clear();
        debugLines.Add("=== " + title + " ===");
    }

    protected void AddDebugLine(string message)
    {
        debugLines.Add(message);
    }

    protected void PrintDebugReport()
    {
        AddTriggeredSkillDebugLines();
        Debug.Log(string.Join("\n", debugLines.ToArray()));
    }

    protected void AddTriggeredSkillDebugLines()
    {
        if (activeManager == null || activeManager.triggeredSkillResolver == null){return;}
        List<string> triggeredLines = activeManager.triggeredSkillResolver.GetDebugMessages();
        for (int i = 0; i < triggeredLines.Count; i++)
        {
            debugLines.Add(triggeredLines[i]);
        }
    }

    protected void CastActive(string skillName, int targetTile, string label)
    {
        if (string.IsNullOrEmpty(skillName))
        {
            AddDebugLine("Skipped empty skill for " + label + ".");
            return;
        }
        LogState("Before " + label);
        activeManager.SetSkillFromName(skillName, attackTester.dummyAttacker);
        LogLoadedActiveDetails("Loaded " + label);
        activeManager.GetTargetableTiles(attackTester.dummyAttacker.GetLocation(), battleManager.moveManager.actorPathfinder);
        activeManager.GetTargetedTiles(targetTile, battleManager.moveManager.actorPathfinder);
        LogTargeting(label, targetTile);
        battleManager.ActivateSkill(skillName, attackTester.dummyAttacker);
        LogState("After " + label);
    }

    protected void LogState(string label)
    {
        TacticActor actor = attackTester.dummyAttacker;
        string loadedSkill = activeManager.active == null ? "" : activeManager.active.GetSkillName();
        AddDebugLine(label
            + " | LoadedSkill=" + loadedSkill
            + " | Energy=" + actor.GetEnergy()
            + " | Actions=" + actor.GetActions()
            + " | NextSkillMods=" + string.Join(",", actor.GetNextSkillMods().ToArray()));
    }

    protected void LogTargeting(string label, int selectedTile)
    {
        AddDebugLine(label
            + " | SelectedTile=" + selectedTile
            + " | TargetableTiles=" + string.Join(",", activeManager.ReturnTargetableTiles().ToArray())
            + " | TargetedTiles=" + string.Join(",", activeManager.ReturnTargetedTiles().ToArray()));
    }

    protected void LogLoadedActiveDetails(string label)
    {
        ActiveSkill loadedActive = activeManager.active;
        AddDebugLine(label
            + " | Skill=" + loadedActive.GetSkillName()
            + " | Effect=" + loadedActive.GetEffect()
            + " | Specifics=" + loadedActive.GetSpecifics()
            + " | Power=" + loadedActive.GetPowerString()
            + " | ScalingField=" + loadedActive.GetScalingSpecifics()
            + " | EnergyCost=" + loadedActive.GetEnergyCost(attackTester.dummyAttacker, attackTester.map)
            + " | ActionCost=" + loadedActive.GetActionCost(attackTester.dummyAttacker, attackTester.map)
            + " | Range=" + loadedActive.GetRangeString(attackTester.dummyAttacker, attackTester.map)
            + " | Span=" + loadedActive.GetSpan(attackTester.dummyAttacker, attackTester.map));
    }

    protected void DebugLatestCombatLog()
    {
        if (attackTester.map == null || attackTester.map.combatLog == null)
        {
            return;
        }
        CombatLog combatLog = attackTester.map.combatLog;
        if (combatLog.detailedLogs == null || combatLog.detailedLogs.Count <= 0)
        {
            AddDebugLine("No detailed combat log entries were created.");
            return;
        }
        AddDebugLine("Latest detailed combat log:\n" + combatLog.detailedLogs[combatLog.detailedLogs.Count - 1]);
    }
}
