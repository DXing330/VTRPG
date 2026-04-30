using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleMatchupSuiteBuilder
{
    public static BattleTestSuite BuildEnemyMatchupSuite(BattleTestSuite sourceSuite, int sourceTeam, bool orderedPairs, bool includeMirrorMatches)
    {
        return BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, 0);
    }

    public static BattleTestSuite BuildEnemyMatchupSuite(BattleTestSuite sourceSuite, int sourceTeam, bool orderedPairs, bool includeMirrorMatches, int runCountOverride)
    {
        if (sourceSuite == null)
        {
            throw new ArgumentNullException("sourceSuite");
        }

        List<BattleTestScenario> enabledScenarios = sourceSuite.EnabledScenarios();
        List<MatchupSourceEntry> sources = BuildSources(enabledScenarios, sourceTeam);
        if (sources.Count < 2)
        {
            throw new InvalidOperationException("Need at least two enabled scenarios with populated source teams to build an enemy matchup suite.");
        }

        BattleTestSuite generatedSuite = ScriptableObject.CreateInstance<BattleTestSuite>();
        generatedSuite.hideFlags = HideFlags.HideAndDontSave;
        generatedSuite.suiteName = sourceSuite.SuiteName() + " Enemy Matrix";
        generatedSuite.reportRoot = sourceSuite.reportRoot;
        generatedSuite.stopOnFailure = false;
        generatedSuite.scenarios = BuildMatchupScenarios(sources, orderedPairs, includeMirrorMatches, runCountOverride);
        return generatedSuite;
    }

    public static void DestroyGeneratedSuite(BattleTestSuite generatedSuite)
    {
        if (generatedSuite == null)
        {
            return;
        }

        if (generatedSuite.scenarios != null)
        {
            for (int i = 0; i < generatedSuite.scenarios.Count; i++)
            {
                if (generatedSuite.scenarios[i] != null)
                {
                    ScriptableObject.DestroyImmediate(generatedSuite.scenarios[i]);
                }
            }
        }

        ScriptableObject.DestroyImmediate(generatedSuite);
    }

    static List<MatchupSourceEntry> BuildSources(List<BattleTestScenario> scenarios, int sourceTeam)
    {
        List<MatchupSourceEntry> sources = new List<MatchupSourceEntry>();
        for (int i = 0; i < scenarios.Count; i++)
        {
            BattleTestScenario scenario = scenarios[i];
            if (scenario == null)
            {
                continue;
            }

            List<BattleTestActorSpec> sourceParty = sourceTeam == 0 ? scenario.partyOne : scenario.partyTwo;
            if (sourceParty == null || sourceParty.Count == 0)
            {
                Debug.LogWarning("Skipping matchup source scenario with empty team " + sourceTeam + ": " + scenario.ScenarioName());
                continue;
            }

            MatchupSourceEntry entry = new MatchupSourceEntry();
            entry.sourceScenario = scenario;
            entry.sourceTeam = sourceTeam;
            entry.actors = CloneActors(sourceParty);
            entry.battleModifiers = CloneStrings(sourceTeam == 0 ? scenario.partyOneBattleModifiers : scenario.partyTwoBattleModifiers);
            entry.label = scenario.MatrixLabel(sourceTeam);
            entry.group = scenario.MatrixGroup(sourceTeam);
            sources.Add(entry);
        }
        return sources;
    }

    static List<BattleTestScenario> BuildMatchupScenarios(List<MatchupSourceEntry> sources, bool orderedPairs, bool includeMirrorMatches, int runCountOverride)
    {
        List<BattleTestScenario> scenarios = new List<BattleTestScenario>();
        int generatedIndex = 0;
        for (int leftIndex = 0; leftIndex < sources.Count; leftIndex++)
        {
            int rightStart = orderedPairs ? 0 : leftIndex;
            for (int rightIndex = rightStart; rightIndex < sources.Count; rightIndex++)
            {
                if (!includeMirrorMatches && leftIndex == rightIndex)
                {
                    continue;
                }
                if (!orderedPairs && rightIndex < leftIndex)
                {
                    continue;
                }

                BattleTestScenario scenario = CreateScenario(sources[leftIndex], sources[rightIndex], generatedIndex, runCountOverride);
                scenarios.Add(scenario);
                generatedIndex++;
            }
        }

        if (scenarios.Count == 0)
        {
            throw new InvalidOperationException("No matchup scenarios were generated.");
        }
        return scenarios;
    }

    static BattleTestScenario CreateScenario(MatchupSourceEntry left, MatchupSourceEntry right, int generatedIndex, int runCountOverride)
    {
        BattleTestScenario scenario = ScriptableObject.CreateInstance<BattleTestScenario>();
        scenario.hideFlags = HideFlags.HideAndDontSave;
        scenario.name = "GeneratedEnemyMatchup_" + generatedIndex;
        scenario.scenarioName = "Enemy Matrix - " + left.label + " vs " + right.label + " [" + left.sourceScenario.ScenarioName() + "]";
        scenario.enabled = true;
        scenario.runCount = runCountOverride > 0 ? runCountOverride : left.sourceScenario.RunCount();
        scenario.maxRounds = Mathf.Max(left.sourceScenario.maxRounds, right.sourceScenario.maxRounds);
        scenario.maxTurns = Mathf.Max(left.sourceScenario.maxTurns, right.sourceScenario.maxTurns);
        scenario.baseSeed = 900000 + (generatedIndex * 100);
        scenario.randomizeSeed = false;
        scenario.autoBattle = left.sourceScenario.autoBattle;
        scenario.controlAI = left.sourceScenario.controlAI;

        scenario.partyOne = CloneActors(left.actors);
        scenario.partyTwo = CloneActors(right.actors);
        scenario.allowedTerrains = CloneStrings(left.sourceScenario.allowedTerrains);
        scenario.allowedWeather = CloneStrings(left.sourceScenario.allowedWeather);
        scenario.allowedTimes = CloneStrings(left.sourceScenario.allowedTimes);
        scenario.startingFormations = CloneStrings(left.sourceScenario.startingFormations);
        scenario.partyOneBattleModifiers = CloneStrings(left.battleModifiers);
        scenario.partyTwoBattleModifiers = CloneStrings(right.battleModifiers);

        scenario.includeInMatchupMatrix = true;
        scenario.partyOneMatrixLabel = left.label;
        scenario.partyTwoMatrixLabel = right.label;
        scenario.partyOneMatrixGroup = left.group;
        scenario.partyTwoMatrixGroup = right.group;

        scenario.baselineEnabled = false;
        scenario.actorBaselines = new List<BattleTestActorBaseline>();
        return scenario;
    }

    static List<BattleTestActorSpec> CloneActors(List<BattleTestActorSpec> actors)
    {
        List<BattleTestActorSpec> clones = new List<BattleTestActorSpec>();
        if (actors == null)
        {
            return clones;
        }

        for (int i = 0; i < actors.Count; i++)
        {
            BattleTestActorSpec actor = actors[i];
            if (actor == null)
            {
                clones.Add(null);
                continue;
            }

            BattleTestActorSpec clone = new BattleTestActorSpec();
            clone.spriteName = actor.spriteName;
            clone.personalName = actor.personalName;
            clone.statsOverride = actor.statsOverride;
            clone.equipment = actor.equipment;
            clone.id = actor.id;
            clones.Add(clone);
        }
        return clones;
    }

    static List<string> CloneStrings(List<string> values)
    {
        if (values == null)
        {
            return new List<string>();
        }
        return new List<string>(values);
    }

    class MatchupSourceEntry
    {
        public BattleTestScenario sourceScenario;
        public int sourceTeam;
        public List<BattleTestActorSpec> actors = new List<BattleTestActorSpec>();
        public List<string> battleModifiers = new List<string>();
        public string label = "";
        public string group = "";
    }
}
