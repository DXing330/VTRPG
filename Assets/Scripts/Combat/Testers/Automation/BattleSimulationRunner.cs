using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BattleSimulationRunner
{
    static List<string> capturedErrors = new List<string>();

    public static BattleSimulationRunResult RunScenarioInLoadedScene(BattleTestScenario scenario, int runIndex)
    {
        Debug.Log(scenario);
        BattleSimulationRunResult result = CreateResult(scenario, runIndex);
        capturedErrors = new List<string>();
        Application.logMessageReceived += CaptureUnityErrors;
        BattleSimulator simulator = null;
        string stage = "Finding BattleSimulator";
        try
        {
            AddDebugStep(result, stage);
            simulator = FindLoadedSimulator();
            CaptureScenarioDebug(result, scenario);
            CaptureSimulatorDebug(result, simulator);
            stage = "Validating scenario";
            AddDebugStep(result, stage);
            ValidateScenario(simulator, scenario);
            CaptureActorDebug(result, simulator, scenario);
            CaptureBossAiDebug(result, simulator, scenario, "Before ApplyScenario");
            stage = "Applying scenario";
            AddDebugStep(result, stage);
            ApplyScenario(simulator, scenario, runIndex, result.seed);
            CapturePartyDebug(result, simulator, "After ApplyScenario");
            CaptureSimulatorDebug(result, simulator);
            stage = "Starting BattleSimulator.StartBattle";
            AddDebugStep(result, stage);
            simulator.StartBattle();
            if (simulator.battleManager == null)
            {
                throw new InvalidOperationException("BattleSimulator has no BattleManager reference.");
            }
            CaptureSimulatorDebug(result, simulator);
            CaptureBossAiDebug(result, simulator, scenario, "After StartBattle");
            stage = "Starting BattleManager.ForceStart";
            AddDebugStep(result, stage);
            simulator.battleManager.ForceStart();
            CaptureSimulatorDebug(result, simulator);
            CapturePartyDebug(result, simulator, "After ForceStart");
            stage = "Collecting result";
            AddDebugStep(result, stage);
            CollectResult(simulator, scenario, result);
        }
        catch (Exception exception)
        {
            Debug.Log("Battle simulation run failed during " + stage + ": " + exception.GetType().Name + ": " + exception.Message);
            Debug.Log(scenario);
            Debug.Log(simulator);
            CaptureScenarioDebug(result, scenario);
            CaptureActorDebug(result, simulator, scenario);
            CaptureSimulatorDebug(result, simulator);
            CapturePartyDebug(result, simulator, "Exception at " + stage);
            CaptureBossAiDebug(result, simulator, scenario, "Exception at " + stage);
            CaptureCombatLogForFailure(result, simulator);
            CaptureCombatLogTail(result, simulator, 20);
            CaptureCrashInference(result, simulator, scenario);
            CaptureRuntimeBossState(result, simulator, "Exception at " + stage);
            MarkFailed(result, exception.Message);
            result.failureStage = stage;
            result.exceptionType = exception.GetType().FullName;
            result.exceptionMessage = exception.Message;
            result.exceptionStackTrace = exception.StackTrace;
        }
        finally
        {
            Application.logMessageReceived -= CaptureUnityErrors;
        }
        if (capturedErrors.Count > 0)
        {
            result.errors.AddRange(capturedErrors);
            MarkFailed(result, "Unity logged errors or exceptions during the run.");
        }

        return result;
    }

    static void AddDebugStep(BattleSimulationRunResult result, string step)
    {
        if (result == null || string.IsNullOrEmpty(step))
        {
            return;
        }
        result.debugSteps.Add(DateTime.Now.ToString("HH:mm:ss.fff") + " - " + step);
    }

    static BattleSimulationRunResult CreateResult(BattleTestScenario scenario, int runIndex)
    {
        BattleSimulationRunResult result = new BattleSimulationRunResult();
        result.scenarioName = scenario == null ? "Missing Scenario" : scenario.ScenarioName();
        result.runIndex = runIndex;
        result.seed = scenario == null ? 0 : scenario.SeedForRun(runIndex);
        return result;
    }

    static BattleSimulator FindLoadedSimulator()
    {
        BattleSimulator[] simulators = Resources.FindObjectsOfTypeAll<BattleSimulator>();
        for (int i = 0; i < simulators.Length; i++)
        {
            Scene scene = simulators[i].gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
            {
                return simulators[i];
            }
        }
        throw new InvalidOperationException("No loaded BattleSimulator was found.");
    }

    static void CaptureScenarioDebug(BattleSimulationRunResult result, BattleTestScenario scenario)
    {
        if (result == null)
        {
            return;
        }
        result.scenarioDebug.Clear();
        if (scenario == null)
        {
            result.scenarioDebug.Add("Scenario: null");
            return;
        }

        result.scenarioDebug.Add("Scenario asset: " + scenario.name);
        result.scenarioDebug.Add("Scenario name: " + scenario.ScenarioName());
        result.scenarioDebug.Add("Run count: " + scenario.RunCount());
        result.scenarioDebug.Add("Max rounds: " + scenario.maxRounds);
        result.scenarioDebug.Add("Max turns/logs: " + scenario.maxTurns);
        result.scenarioDebug.Add("Base seed: " + scenario.baseSeed);
        result.scenarioDebug.Add("Run seed: " + result.seed);
        result.scenarioDebug.Add("Randomize seed: " + scenario.randomizeSeed);
        result.scenarioDebug.Add("Auto battle: " + scenario.autoBattle);
        result.scenarioDebug.Add("Control AI: " + scenario.controlAI);
        result.scenarioDebug.Add("Party one actors: " + DescribeActorSpecs(scenario.partyOne));
        result.scenarioDebug.Add("Party two actors: " + DescribeActorSpecs(scenario.partyTwo));
        result.scenarioDebug.Add("Terrains: " + JoinList(scenario.allowedTerrains));
        result.scenarioDebug.Add("Weather: " + JoinList(scenario.allowedWeather));
        result.scenarioDebug.Add("Times: " + JoinList(scenario.allowedTimes));
        result.scenarioDebug.Add("Starting formations: " + JoinList(scenario.startingFormations));
        result.scenarioDebug.Add("Party one modifiers: " + JoinList(scenario.partyOneBattleModifiers));
        result.scenarioDebug.Add("Party two modifiers: " + JoinList(scenario.partyTwoBattleModifiers));
    }

    static void CaptureSimulatorDebug(BattleSimulationRunResult result, BattleSimulator simulator)
    {
        if (result == null)
        {
            return;
        }
        if (simulator == null)
        {
            result.simulatorDebug.Add("BattleSimulator: null");
            return;
        }

        result.simulatorDebug.Add("Snapshot: " + DateTime.Now.ToString("HH:mm:ss.fff"));
        result.simulatorDebug.Add("Simulator object: " + simulator.name + " active=" + simulator.gameObject.activeInHierarchy + " scene=" + simulator.gameObject.scene.path);
        result.simulatorDebug.Add("simulatorState: " + (simulator.simulatorState == null ? "null" : simulator.simulatorState.name));
        result.simulatorDebug.Add("actorStats: " + (simulator.actorStats == null ? "null" : simulator.actorStats.name));
        if (simulator.actorStats != null)
        {
            result.simulatorDebug.Add("actorStats keys=" + SafeListCount(simulator.actorStats.keys) + " values=" + SafeListCount(simulator.actorStats.values));
        }
        result.simulatorDebug.Add("partyOneList: " + DescribeCharacterList(simulator.partyOneList));
        result.simulatorDebug.Add("partyTwoList: " + DescribeCharacterList(simulator.partyTwoList));
        if (simulator.simulatorState != null)
        {
            result.simulatorDebug.Add("selectedTerrain=" + simulator.simulatorState.selectedTerrain
                + " selectedWeather=" + simulator.simulatorState.selectedWeather
                + " selectedTime=" + simulator.simulatorState.selectedTime
                + " selectedFormation=" + simulator.simulatorState.selectedStartingFormation);
            result.simulatorDebug.Add("selected terrains=" + JoinList(simulator.simulatorState.selectedTerrainTypes));
            result.simulatorDebug.Add("selected weather=" + JoinList(simulator.simulatorState.selectedWeathers));
            result.simulatorDebug.Add("selected times=" + JoinList(simulator.simulatorState.selectedTimes));
            result.simulatorDebug.Add("selected formations=" + JoinList(simulator.simulatorState.selectedStartingFormations));
            result.simulatorDebug.Add("selected P1 modifiers=" + JoinList(simulator.simulatorState.selectedP1BattleMods));
            result.simulatorDebug.Add("selected P2 modifiers=" + JoinList(simulator.simulatorState.selectedP2BattleMods));
            result.simulatorDebug.Add("multiBattle=" + simulator.simulatorState.multiBattle
                + " current=" + simulator.simulatorState.multiBattleCurrent
                + " count=" + simulator.simulatorState.multiBattleCount
                + " autoBattle=" + simulator.simulatorState.autoBattle
                + " controlAI=" + simulator.simulatorState.controlAI
                + " winningTeam=" + simulator.simulatorState.winningTeam);
        }
        result.simulatorDebug.Add("battleManager: " + (simulator.battleManager == null ? "null" : simulator.battleManager.name + " active=" + simulator.battleManager.gameObject.activeInHierarchy));
        if (simulator.battleManager != null)
        {
            result.simulatorDebug.Add("battleManager round=" + simulator.battleManager.GetRoundNumber()
                + " turnIndex=" + simulator.battleManager.GetTurnIndex());
            result.simulatorDebug.Add("battleStatsTracker: " + (simulator.battleManager.battleStatsTracker == null ? "null" : "present"));
            result.simulatorDebug.Add("combatLog: " + (simulator.battleManager.combatLog == null ? "null" : "entries=" + simulator.battleManager.combatLog.allLogs.Count));
        }
    }

    static void CaptureActorDebug(BattleSimulationRunResult result, BattleSimulator simulator, BattleTestScenario scenario)
    {
        if (result == null)
        {
            return;
        }
        result.actorDebug.Clear();
        if (simulator == null || scenario == null || simulator.actorStats == null)
        {
            return;
        }

        AddActorDebug(result, "Party One", scenario.partyOne, simulator.actorStats);
        AddActorDebug(result, "Party Two", scenario.partyTwo, simulator.actorStats);
    }

    static void AddActorDebug(BattleSimulationRunResult result, string partyName, List<BattleTestActorSpec> actors, StatDatabase actorStats)
    {
        if (actors == null)
        {
            return;
        }
        for (int i = 0; i < actors.Count; i++)
        {
            BattleTestActorSpec spec = actors[i];
            BattleSimulationActorDebugInfo debugInfo = new BattleSimulationActorDebugInfo();
            debugInfo.partyName = partyName;
            debugInfo.index = i;
            if (spec == null)
            {
                debugInfo.displayName = "null actor spec";
                result.actorDebug.Add(debugInfo);
                continue;
            }

            debugInfo.displayName = spec.DisplayName(i);
            debugInfo.spriteName = spec.spriteName;
            debugInfo.id = spec.ActorId(i);
            debugInfo.equipment = spec.equipment;
            debugInfo.equipmentLength = string.IsNullOrEmpty(spec.equipment) ? 0 : spec.equipment.Length;
            debugInfo.actorStatsKeyExists = actorStats.KeyExists(spec.spriteName);
            debugInfo.usesStatsOverride = !string.IsNullOrEmpty(spec.statsOverride);

            string stats = debugInfo.usesStatsOverride ? spec.statsOverride : actorStats.ReturnValue(spec.spriteName);
            debugInfo.statsLength = string.IsNullOrEmpty(stats) ? 0 : stats.Length;
            debugInfo.statsPreview = Truncate(stats, 300);
            string[] fields = string.IsNullOrEmpty(stats) ? new string[0] : stats.Split('!');
            debugInfo.statsFieldCount = fields.Length;
            for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
            {
                debugInfo.indexedStats.Add(fieldIndex + ": " + Truncate(fields[fieldIndex], 160));
            }
            result.actorDebug.Add(debugInfo);
        }
    }

    static void CapturePartyDebug(BattleSimulationRunResult result, BattleSimulator simulator, string label)
    {
        if (result == null || simulator == null)
        {
            return;
        }
        result.partyDebug.Add(label);
        AppendCharacterListDebug(result.partyDebug, "Party One List", simulator.partyOneList);
        AppendCharacterListDebug(result.partyDebug, "Party Two List", simulator.partyTwoList);
    }

    static void AppendCharacterListDebug(List<string> debugLines, string label, CharacterList list)
    {
        if (list == null)
        {
            debugLines.Add(label + ": null");
            return;
        }
        int count = list.characters == null ? 0 : list.characters.Count;
        debugLines.Add(label + ": characters=" + count
            + " names=" + SafeListCount(list.characterNames)
            + " stats=" + SafeListCount(list.stats)
            + " ids=" + SafeListCount(list.characterIDs)
            + " equipment=" + SafeListCount(list.equipment)
            + " modifiers=" + SafeListCount(list.battleModifiers));
        for (int i = 0; i < count; i++)
        {
            string name = ValueAt(list.characterNames, i);
            string sprite = ValueAt(list.characters, i);
            string stats = ValueAt(list.stats, i);
            string id = ValueAt(list.characterIDs, i);
            string equipment = ValueAt(list.equipment, i);
            string[] fields = string.IsNullOrEmpty(stats) ? new string[0] : stats.Split('!');
            debugLines.Add(label + "[" + i + "]: name=" + name
                + " sprite=" + sprite
                + " id=" + id
                + " statsLength=" + (string.IsNullOrEmpty(stats) ? 0 : stats.Length)
                + " statsFields=" + fields.Length
                + " equipmentLength=" + (string.IsNullOrEmpty(equipment) ? 0 : equipment.Length));
        }
    }

    static void CaptureCombatLogTail(BattleSimulationRunResult result, BattleSimulator simulator, int maxEntries)
    {
        if (result == null || simulator == null || simulator.battleManager == null || simulator.battleManager.combatLog == null)
        {
            return;
        }
        List<string> logs = simulator.battleManager.combatLog.allLogs;
        int start = Mathf.Max(0, logs.Count - maxEntries);
        result.combatLogTail.Clear();
        for (int i = start; i < logs.Count; i++)
        {
            result.combatLogTail.Add(i + ": " + logs[i]);
        }
    }

    static void CaptureCombatLogForFailure(BattleSimulationRunResult result, BattleSimulator simulator)
    {
        if (result == null || simulator == null || simulator.battleManager == null || simulator.battleManager.combatLog == null)
        {
            return;
        }

        result.logEntries = simulator.battleManager.combatLog.allLogs.Count;
        result.rounds = simulator.battleManager.GetRoundNumber();
        result.turnIndex = simulator.battleManager.GetTurnIndex();
        result.combatLogs = new List<string>(simulator.battleManager.combatLog.allLogs);
        result.combatLogEntries = BuildCombatLogEntries(simulator.battleManager.combatLog);
        if (simulator.simulatorState != null)
        {
            result.terrain = simulator.simulatorState.selectedTerrain;
            result.weather = simulator.simulatorState.selectedWeather;
            result.time = simulator.simulatorState.selectedTime;
            result.startingFormation = simulator.simulatorState.selectedStartingFormation;
        }
    }

    static void CaptureBossAiDebug(BattleSimulationRunResult result, BattleSimulator simulator, BattleTestScenario scenario, string label)
    {
        try
        {
            CaptureBossAiDebugUnsafe(result, simulator, scenario, label);
        }
        catch (Exception exception)
        {
            if (result != null)
            {
                result.bossAiDebug.Add("Boss AI debug capture failed during `" + label + "`: " + exception.GetType().Name + ": " + exception.Message);
            }
        }
    }

    static void CaptureBossAiDebugUnsafe(BattleSimulationRunResult result, BattleSimulator simulator, BattleTestScenario scenario, string label)
    {
        if (result == null)
        {
            return;
        }

        int startIndex = result.bossAiDebug.Count;
        result.bossAiDebug.Add("Snapshot: " + DateTime.Now.ToString("HH:mm:ss.fff") + " - " + label);
        if (simulator == null)
        {
            result.bossAiDebug.Add("BattleSimulator: null");
            return;
        }
        if (simulator.battleManager == null)
        {
            result.bossAiDebug.Add("BattleManager: null");
            return;
        }
        ActorAI actorAI = simulator.battleManager.actorAI;
        if (actorAI == null)
        {
            result.bossAiDebug.Add("ActorAI: null");
            return;
        }

        result.bossAiDebug.Add("ActorAI: " + actorAI.name);
        AppendDatabaseDebug(result.bossAiDebug, "actorSkillRotation", actorAI.actorSkillRotation);
        AppendDatabaseDebug(result.bossAiDebug, "spriteToBossRotation", actorAI.spriteToBossRotation);
        AppendDatabaseDebug(result.bossAiDebug, "bossSkillRotation", actorAI.bossSkillRotation);
        AppendDatabaseDebug(result.bossAiDebug, "activeData", actorAI.activeData);

        if (scenario == null)
        {
            result.bossAiDebug.Add("Scenario: null");
            return;
        }

        CaptureBossActorSpecs(result, simulator, scenario.partyOne, "Party One", actorAI);
        CaptureBossActorSpecs(result, simulator, scenario.partyTwo, "Party Two", actorAI);
        if (!BossDebugHasActorData(result.bossAiDebug, startIndex))
        {
            result.bossAiDebug.RemoveRange(startIndex, result.bossAiDebug.Count - startIndex);
        }
    }

    static bool BossDebugHasActorData(List<string> lines, int startIndex)
    {
        for (int i = startIndex; i < lines.Count; i++)
        {
            if (lines[i].Contains("block[") || lines[i].Contains("WARNING"))
            {
                return true;
            }
        }
        return false;
    }

    static void CaptureBossActorSpecs(BattleSimulationRunResult result, BattleSimulator simulator, List<BattleTestActorSpec> actors, string partyName, ActorAI actorAI)
    {
        if (actors == null)
        {
            result.bossAiDebug.Add(partyName + " boss-AI specs: null");
            return;
        }

        for (int i = 0; i < actors.Count; i++)
        {
            BattleTestActorSpec actor = actors[i];
            if (actor == null || string.IsNullOrEmpty(actor.spriteName))
            {
                result.bossAiDebug.Add(partyName + "[" + i + "]: no actor spec or sprite.");
                continue;
            }
            if (!ShouldCaptureBossActor(actorAI, actor.spriteName))
            {
                continue;
            }
            CaptureBossActorDebug(result, simulator, actorAI, actor.spriteName, partyName + "[" + i + "] " + actor.DisplayName(i), 0);
        }
    }

    static bool ShouldCaptureBossActor(ActorAI actorAI, string spriteName)
    {
        if (actorAI == null || string.IsNullOrEmpty(spriteName))
        {
            return false;
        }
        return actorAI.actorSkillRotation != null && actorAI.actorSkillRotation.ReturnValue(spriteName) == "Boss";
    }

    static void CaptureBossActorDebug(BattleSimulationRunResult result, BattleSimulator simulator, ActorAI actorAI, string spriteName, string label, int depth)
    {
        if (depth > 2)
        {
            result.bossAiDebug.Add(label + ": form-chain debug stopped at depth " + depth + ".");
            return;
        }

        string indent = new string(' ', depth * 2);
        result.bossAiDebug.Add(indent + label + ": sprite=`" + spriteName + "`");
        string actorStats = simulator.actorStats == null ? "" : simulator.actorStats.ReturnValue(spriteName);
        result.bossAiDebug.Add(indent + "actorStats key=" + (simulator.actorStats != null && simulator.actorStats.KeyExists(spriteName))
            + " fields=" + FieldCount(actorStats, '!')
            + " hp=" + ActorStatField(actorStats, 4)
            + " energy=" + ActorStatField(actorStats, 12)
            + " actives=`" + ActorStatField(actorStats, 21) + "`");

        if (actorAI.spriteToBossRotation == null)
        {
            result.bossAiDebug.Add(indent + "spriteToBossRotation: null");
            return;
        }

        bool spriteMappingExists = actorAI.spriteToBossRotation.KeyExists(spriteName);
        string rotationKey = actorAI.spriteToBossRotation.ReturnValue(spriteName);
        result.bossAiDebug.Add(indent + "spriteToBoss key exists=" + spriteMappingExists + " rotationKey=`" + rotationKey + "`");
        if (string.IsNullOrEmpty(rotationKey))
        {
            result.bossAiDebug.Add(indent + "WARNING: sprite maps to an empty boss rotation key. ActorAI will parse an empty rotation block.");
            return;
        }

        if (actorAI.bossSkillRotation == null)
        {
            result.bossAiDebug.Add(indent + "bossSkillRotation: null");
            return;
        }

        bool rotationExists = actorAI.bossSkillRotation.KeyExists(rotationKey);
        string rotation = actorAI.bossSkillRotation.ReturnValue(rotationKey);
        result.bossAiDebug.Add(indent + "boss rotation key exists=" + rotationExists + " valueLength=" + (string.IsNullOrEmpty(rotation) ? 0 : rotation.Length));
        if (string.IsNullOrEmpty(rotation))
        {
            result.bossAiDebug.Add(indent + "WARNING: boss rotation value is empty. ReturnBossActions will produce a malformed block.");
            return;
        }

        List<string> changeForms = AppendRotationBlockDebug(result.bossAiDebug, indent, rotation, actorAI, simulator.actorStats);
        for (int i = 0; i < changeForms.Count; i++)
        {
            CaptureBossActorDebug(result, simulator, actorAI, changeForms[i], label + " -> Change Form " + changeForms[i], depth + 1);
        }
    }

    static List<string> AppendRotationBlockDebug(List<string> lines, string indent, string rotation, ActorAI actorAI, StatDatabase actorStats)
    {
        List<string> changeForms = new List<string>();
        string[] blocks = rotation.Split('#');
        lines.Add(indent + "rotation blocks=" + blocks.Length);
        for (int i = 0; i < blocks.Length; i++)
        {
            string[] fields = blocks[i].Split('|');
            lines.Add(indent + "block[" + i + "] fields=" + fields.Length + " raw=`" + Truncate(blocks[i], 220) + "`");
            if (fields.Length < 4)
            {
                lines.Add(indent + "WARNING: block[" + i + "] has fewer than 4 pipe fields. ActorAI can throw before choosing an action.");
                continue;
            }

            string condition = fields[0];
            string specifics = fields[1];
            string actionType = fields[2];
            string actionSpecifics = fields[3];
            int conditionCount = string.IsNullOrEmpty(condition) ? 0 : condition.Split(',').Length;
            int specificsCount = string.IsNullOrEmpty(specifics) ? 0 : specifics.Split(',').Length;
            lines.Add(indent + "  condition=`" + condition + "` specifics=`" + specifics + "` action=`" + actionType + "` target=`" + actionSpecifics + "`");
            if (conditionCount != specificsCount)
            {
                lines.Add(indent + "  WARNING: condition/specific counts differ (" + conditionCount + " vs " + specificsCount + "). AIConditionChecker can index past specifics.");
            }
            if (ActionUsesSkill(actionType))
            {
                AppendSkillReferenceDebug(lines, indent + "  ", actorAI.activeData, actionSpecifics);
            }
            if (actionType == "Change Form")
            {
                bool targetStats = actorStats != null && actorStats.KeyExists(actionSpecifics);
                bool targetMapping = actorAI.spriteToBossRotation != null && actorAI.spriteToBossRotation.KeyExists(actionSpecifics);
                string targetRotation = actorAI.spriteToBossRotation == null ? "" : actorAI.spriteToBossRotation.ReturnValue(actionSpecifics);
                bool targetRotationExists = actorAI.bossSkillRotation != null && actorAI.bossSkillRotation.KeyExists(targetRotation);
                lines.Add(indent + "  changeForm targetStats=" + targetStats + " targetSpriteToBoss=" + targetMapping + " targetRotation=`" + targetRotation + "` targetRotationExists=" + targetRotationExists);
                if (!string.IsNullOrEmpty(actionSpecifics) && !changeForms.Contains(actionSpecifics))
                {
                    changeForms.Add(actionSpecifics);
                }
            }
        }
        return changeForms;
    }

    static void AppendSkillReferenceDebug(List<string> lines, string indent, StatDatabase activeData, string actionSpecifics)
    {
        if (activeData == null)
        {
            lines.Add(indent + "activeData=null");
            return;
        }
        string[] skills = string.IsNullOrEmpty(actionSpecifics) ? new string[0] : actionSpecifics.Split(',');
        for (int i = 0; i < skills.Length; i++)
        {
            string skill = skills[i].Trim();
            if (string.IsNullOrEmpty(skill) || skill == "None")
            {
                continue;
            }
            bool exists = activeData.KeyExists(skill);
            string value = activeData.ReturnValue(skill);
            lines.Add(indent + "skill `" + skill + "` exists=" + exists + " fields=" + FieldCount(value, '_') + " value=`" + Truncate(value, 160) + "`");
        }
    }

    static bool ActionUsesSkill(string actionType)
    {
        return actionType == "Skill"
            || actionType == "Random Skill"
            || actionType == "One Time Skill"
            || actionType == "Chain Skill"
            || actionType == "One Time Chain Skill";
    }

    static void AppendDatabaseDebug(List<string> lines, string label, StatDatabase database)
    {
        if (database == null)
        {
            lines.Add(label + ": null");
            return;
        }

        lines.Add(label + ": " + database.name
            + " inputKeysAndValues=" + database.inputKeysAndValues
            + " keyValueDelimiter=`" + database.keyValueDelimiter + "`"
            + " keyDelimiter=`" + database.keyDelimiter + "`"
            + " valueDelimiter=`" + database.valueDelimiter + "`"
            + " keys=" + SafeListCount(database.keys)
            + " values=" + SafeListCount(database.values));
    }

    static void CaptureCrashInference(BattleSimulationRunResult result, BattleSimulator simulator, BattleTestScenario scenario)
    {
        try
        {
            CaptureCrashInferenceUnsafe(result, simulator, scenario);
        }
        catch (Exception exception)
        {
            if (result != null)
            {
                result.crashInference.Add("Crash inference failed: " + exception.GetType().Name + ": " + exception.Message);
            }
        }
    }

    static void CaptureRuntimeBossState(BattleSimulationRunResult result, BattleSimulator simulator, string label)
    {
        try
        {
            CaptureRuntimeBossStateUnsafe(result, simulator, label);
        }
        catch (Exception exception)
        {
            if (result != null)
            {
                result.crashInference.Add("Runtime boss-state capture failed: " + exception.GetType().Name + ": " + exception.Message);
            }
        }
    }

    static void CaptureRuntimeBossStateUnsafe(BattleSimulationRunResult result, BattleSimulator simulator, string label)
    {
        if (result == null)
        {
            return;
        }
        result.crashInference.Add("Runtime snapshot: " + label);
        if (simulator == null)
        {
            result.crashInference.Add("  BattleSimulator: null");
            return;
        }
        BattleManager battleManager = simulator.battleManager;
        if (battleManager == null)
        {
            result.crashInference.Add("  BattleManager: null");
            return;
        }

        result.crashInference.Add("  round=" + battleManager.GetRoundNumber() + " turnIndex=" + battleManager.GetTurnIndex());
        TacticActor actor = battleManager.GetTurnActor();
        if (actor == null)
        {
            result.crashInference.Add("  turnActor: null");
            return;
        }

        AppendRuntimeActorState(result.crashInference, battleManager, actor, "turnActor");
        AppendRuntimeBossRotationState(result.crashInference, battleManager, actor);
        AppendRuntimeBattleActors(result.crashInference, battleManager, actor);
    }

    static void AppendRuntimeActorState(List<string> lines, BattleManager battleManager, TacticActor actor, string label)
    {
        lines.Add("  " + label + ": name=`" + SafeActorName(actor)
            + "` sprite=`" + SafeActorSprite(actor)
            + "` team=" + actor.GetTeam()
            + " hp=" + actor.GetHealth() + "/" + actor.GetBaseHealth()
            + " damaged=" + (actor.GetBaseHealth() - actor.GetHealth())
            + " energy=" + actor.GetEnergy() + "/" + actor.GetBaseEnergy()
            + " actions=" + actor.GetActions()
            + " movement=" + actor.GetMovement()
            + " location=" + actor.GetLocation()
            + " counter=" + actor.GetCounter());
        lines.Add("  " + label + " stats: attack=" + actor.GetAttack()
            + " defense=" + actor.GetDefense()
            + " range=" + actor.GetAttackRange()
            + " speed=" + actor.GetSpeed()
            + " moveType=`" + actor.GetMoveType() + "`");
        lines.Add("  " + label + " species=`" + actor.GetSpecies()
            + "` elements=`" + JoinList(actor.GetElements())
            + "` attributes=`" + JoinList(actor.GetAttributes()) + "`");
        lines.Add("  " + label + " activeSkills=`" + JoinList(actor.GetActiveSkills()) + "`");
        lines.Add("  " + label + " tempActives=`" + JoinList(actor.GetTempActives()) + "`");
        lines.Add("  " + label + " passives=`" + JoinList(actor.GetPassiveSkillsAndLevels()) + "`");
        lines.Add("  " + label + " tempPassives=`" + DescribeTempPassives(actor) + "`");
        lines.Add("  " + label + " hurtBy=`" + DescribeHurtBy(actor) + "`");
        lines.Add("  " + label + " target=`" + DescribeTarget(actor) + "`");

        BattleMap map = battleManager.map;
        if (map != null)
        {
            lines.Add("  " + label + " mapContext: adjacentEnemies=" + SafeActorListCount(() => map.GetAdjacentEnemies(actor))
                + " adjacentAllies=" + SafeActorListCount(() => map.GetAdjacentAllies(actor))
                + " attackableEnemies=" + SafeActorListCount(() => map.GetAttackableEnemies(actor))
                + " allEnemies=" + SafeActorListCount(() => map.AllEnemies(actor))
                + " allAllies=" + SafeActorListCount(() => map.AllAllies(actor)));
        }
    }

    static void AppendRuntimeBossRotationState(List<string> lines, BattleManager battleManager, TacticActor actor)
    {
        ActorAI actorAI = battleManager.actorAI;
        if (actorAI == null)
        {
            lines.Add("  ActorAI: null");
            return;
        }

        string spriteName = SafeActorSprite(actor);
        string aiRotationType = SafeDatabaseValue(actorAI.actorSkillRotation, spriteName);
        bool isBoss = aiRotationType == "Boss";
        string bossRotationKey = SafeDatabaseValue(actorAI.spriteToBossRotation, spriteName);
        string bossRotation = SafeDatabaseValue(actorAI.bossSkillRotation, bossRotationKey);
        lines.Add("  runtime AI: actorSkillRotation=`" + aiRotationType
            + "` bossTurn=" + isBoss
            + " spriteToBossKeyExists=" + SafeDatabaseKeyExists(actorAI.spriteToBossRotation, spriteName)
            + " rotationKey=`" + bossRotationKey
            + "` bossRotationKeyExists=" + SafeDatabaseKeyExists(actorAI.bossSkillRotation, bossRotationKey)
            + " rotationLength=" + (string.IsNullOrEmpty(bossRotation) ? 0 : bossRotation.Length));

        if (string.IsNullOrEmpty(bossRotation))
        {
            lines.Add("  WARNING: live actor has no boss rotation value. ActorAI.ReturnBossActions will parse a malformed block.");
            return;
        }

        AppendRuntimeRotationBlocks(lines, battleManager, actor, bossRotation);
    }

    static void AppendRuntimeRotationBlocks(List<string> lines, BattleManager battleManager, TacticActor actor, string rotation)
    {
        string[] blocks = rotation.Split('#');
        lines.Add("  runtime boss rotation blocks=" + blocks.Length);
        int firstKnownMatch = -1;
        for (int i = 0; i < blocks.Length; i++)
        {
            string block = blocks[i];
            string[] fields = block.Split('|');
            lines.Add("    block[" + i + "] fields=" + fields.Length + " raw=`" + Truncate(block, 220) + "`");
            if (fields.Length < 4)
            {
                lines.Add("      WARNING: live block has fewer than 4 pipe fields. ActorAI can throw before choosing an action.");
                continue;
            }

            RuntimeConditionResult condition = DescribeRuntimeConditions(battleManager, actor, fields[0], fields[1]);
            lines.Add("      condition=`" + fields[0] + "` specifics=`" + fields[1] + "` action=`" + fields[2] + "` target=`" + fields[3] + "`");
            lines.Add("      evaluation: " + condition.description);
            if (condition.canEvaluate && condition.matches && firstKnownMatch < 0)
            {
                firstKnownMatch = i;
            }
        }

        if (firstKnownMatch >= 0)
        {
            string[] fields = blocks[firstKnownMatch].Split('|');
            lines.Add("  predicted first matching runtime block: block[" + firstKnownMatch + "] action=`" + fields[2] + "` target=`" + fields[3] + "`");
        }
        else
        {
            lines.Add("  predicted first matching runtime block: none from known conditions.");
        }
    }

    static RuntimeConditionResult DescribeRuntimeConditions(BattleManager battleManager, TacticActor actor, string conditions, string specifics)
    {
        RuntimeConditionResult result = new RuntimeConditionResult();
        string[] allConditions = string.IsNullOrEmpty(conditions) ? new string[0] : conditions.Split(',');
        string[] allSpecifics = string.IsNullOrEmpty(specifics) ? new string[0] : specifics.Split(',');
        if (allConditions.Length != allSpecifics.Length)
        {
            result.canEvaluate = false;
            result.matches = false;
            result.description = "WARNING: condition/specific counts differ (" + allConditions.Length + " vs " + allSpecifics.Length + "). AIConditionChecker can index past specifics.";
            return result;
        }

        result.canEvaluate = true;
        result.matches = true;
        List<string> parts = new List<string>();
        for (int i = 0; i < allConditions.Length; i++)
        {
            RuntimeConditionResult condition = DescribeRuntimeCondition(battleManager, actor, allConditions[i], allSpecifics[i]);
            parts.Add(condition.description);
            if (!condition.canEvaluate)
            {
                result.canEvaluate = false;
            }
            if (!condition.matches)
            {
                result.matches = false;
            }
        }

        result.description = string.Join("; ", parts.ToArray());
        return result;
    }

    static RuntimeConditionResult DescribeRuntimeCondition(BattleManager battleManager, TacticActor actor, string condition, string specifics)
    {
        RuntimeConditionResult result = new RuntimeConditionResult();
        result.canEvaluate = true;
        result.matches = false;
        BattleMap map = battleManager == null ? null : battleManager.map;

        switch (condition)
        {
            case "Damaged>":
                int damagedOver = actor.GetBaseHealth() - actor.GetHealth();
                int damagedOverThreshold = SafeParseInt(specifics);
                result.matches = damagedOver > damagedOverThreshold;
                result.description = "Damaged " + damagedOver + " > " + damagedOverThreshold + " => " + result.matches;
                return result;
            case "Damaged<":
                int damagedUnder = actor.GetBaseHealth() - actor.GetHealth();
                int damagedUnderThreshold = SafeParseInt(specifics);
                result.matches = damagedUnder < damagedUnderThreshold;
                result.description = "Damaged " + damagedUnder + " < " + damagedUnderThreshold + " => " + result.matches;
                return result;
            case "Sprite":
                result.matches = actor.GetSpriteName() == specifics;
                result.description = "Sprite `" + actor.GetSpriteName() + "` == `" + specifics + "` => " + result.matches;
                return result;
            case "TempPassive<>":
                result.matches = !StringListContains(actor.tempPassives, specifics);
                result.description = "Missing temp passive `" + specifics + "` => " + result.matches;
                return result;
            case "SkillExists":
                result.matches = actor.SkillExists(specifics);
                result.description = "SkillExists `" + specifics + "` => " + result.matches;
                return result;
            case "TempActiveCount>":
                result.matches = actor.GetTempActives().Count > SafeParseInt(specifics);
                result.description = "TempActiveCount " + actor.GetTempActives().Count + " > " + SafeParseInt(specifics) + " => " + result.matches;
                return result;
            case "TempActiveCount<":
                result.matches = actor.GetTempActives().Count < SafeParseInt(specifics);
                result.description = "TempActiveCount " + actor.GetTempActives().Count + " < " + SafeParseInt(specifics) + " => " + result.matches;
                return result;
            case "AdjacentEnemyCount>":
                return CompareCount("AdjacentEnemyCount", SafeActorListCount(() => map.GetAdjacentEnemies(actor)), ">", specifics);
            case "AdjacentEnemyCount<":
                return CompareCount("AdjacentEnemyCount", SafeActorListCount(() => map.GetAdjacentEnemies(actor)), "<", specifics);
            case "AdjacentEnemyCount":
                return CompareCount("AdjacentEnemyCount", SafeActorListCount(() => map.GetAdjacentEnemies(actor)), "=", specifics);
            case "AdjacentAllyCount>":
                return CompareCount("AdjacentAllyCount", SafeActorListCount(() => map.GetAdjacentAllies(actor)), ">", specifics);
            case "AdjacentAllyCount<":
                return CompareCount("AdjacentAllyCount", SafeActorListCount(() => map.GetAdjacentAllies(actor)), "<", specifics);
            case "AttackableEnemyCount>":
                return CompareCount("AttackableEnemyCount", SafeActorListCount(() => map.GetAttackableEnemies(actor)), ">", specifics);
            case "AttackableEnemyCount<":
                return CompareCount("AttackableEnemyCount", SafeActorListCount(() => map.GetAttackableEnemies(actor)), "<", specifics);
            case "MaxEnergy":
                result.matches = actor.GetEnergy() >= actor.GetBaseEnergy();
                result.description = "Energy " + actor.GetEnergy() + " >= base " + actor.GetBaseEnergy() + " => " + result.matches;
                return result;
            case "Health":
                if (specifics == "<Half")
                {
                    result.matches = actor.GetHealth() * 2 <= actor.GetBaseHealth();
                    result.description = "Health " + actor.GetHealth() + " <= half of " + actor.GetBaseHealth() + " => " + result.matches;
                    return result;
                }
                if (specifics == ">Half")
                {
                    result.matches = actor.GetHealth() * 2 >= actor.GetBaseHealth();
                    result.description = "Health " + actor.GetHealth() + " >= half of " + actor.GetBaseHealth() + " => " + result.matches;
                    return result;
                }
                break;
            case "Energy<":
                result.matches = actor.GetEnergy() < SafeParseInt(specifics);
                result.description = "Energy " + actor.GetEnergy() + " < " + SafeParseInt(specifics) + " => " + result.matches;
                return result;
            case "Counter":
                result.matches = actor.GetCounter() == SafeParseInt(specifics);
                result.description = "Counter " + actor.GetCounter() + " == " + SafeParseInt(specifics) + " => " + result.matches;
                return result;
            case "Counter<":
                result.matches = actor.GetCounter() < SafeParseInt(specifics);
                result.description = "Counter " + actor.GetCounter() + " < " + SafeParseInt(specifics) + " => " + result.matches;
                return result;
            case "Counter>":
                result.matches = actor.GetCounter() > SafeParseInt(specifics);
                result.description = "Counter " + actor.GetCounter() + " > " + SafeParseInt(specifics) + " => " + result.matches;
                return result;
            case "EnemyCount>":
                return CompareCount("EnemyCount", SafeActorListCount(() => map.AllEnemies(actor)), ">", specifics);
            case "EnemyCount<":
                return CompareCount("EnemyCount", SafeActorListCount(() => map.AllEnemies(actor)), "<", specifics);
            case "AllyCount>":
                return CompareCount("AllyCount", SafeActorListCount(() => map.AllAllies(actor)), ">", specifics);
            case "AllyCount<":
                return CompareCount("AllyCount", SafeActorListCount(() => map.AllAllies(actor)), "<", specifics);
            case "Grappling":
                result.matches = actor.Grappling();
                result.description = "Grappling => " + result.matches;
                return result;
            case "Grappling<>":
                result.matches = !actor.Grappling();
                result.description = "Not grappling => " + result.matches;
                return result;
            case "None":
                result.matches = true;
                result.description = "None => True";
                return result;
        }

        result.canEvaluate = false;
        result.description = condition + "|" + specifics + " not evaluated by automation diagnostics.";
        return result;
    }

    static RuntimeConditionResult CompareCount(string label, int count, string comparison, string specifics)
    {
        RuntimeConditionResult result = new RuntimeConditionResult();
        result.canEvaluate = true;
        int target = SafeParseInt(specifics);
        if (comparison == ">")
        {
            result.matches = count > target;
        }
        else if (comparison == "<")
        {
            result.matches = count < target;
        }
        else
        {
            result.matches = count == target;
        }
        result.description = label + " " + count + " " + comparison + " " + target + " => " + result.matches;
        return result;
    }

    static void AppendRuntimeBattleActors(List<string> lines, BattleManager battleManager, TacticActor turnActor)
    {
        if (battleManager.map == null || battleManager.map.battlingActors == null)
        {
            lines.Add("  battlingActors: unavailable");
            return;
        }

        lines.Add("  battlingActors=" + battleManager.map.battlingActors.Count);
        for (int i = 0; i < battleManager.map.battlingActors.Count; i++)
        {
            TacticActor actor = battleManager.map.battlingActors[i];
            if (actor == null)
            {
                lines.Add("    [" + i + "] null");
                continue;
            }
            string marker = actor == turnActor ? " currentTurn" : "";
            lines.Add("    [" + i + "]" + marker
                + " name=`" + SafeActorName(actor)
                + "` sprite=`" + SafeActorSprite(actor)
                + "` team=" + actor.GetTeam()
                + " hp=" + actor.GetHealth() + "/" + actor.GetBaseHealth()
                + " energy=" + actor.GetEnergy() + "/" + actor.GetBaseEnergy()
                + " actions=" + actor.GetActions()
                + " location=" + actor.GetLocation());
        }
    }

    static void CaptureCrashInferenceUnsafe(BattleSimulationRunResult result, BattleSimulator simulator, BattleTestScenario scenario)
    {
        if (result == null || simulator == null || simulator.battleManager == null || simulator.battleManager.combatLog == null)
        {
            return;
        }

        List<string> logs = simulator.battleManager.combatLog.allLogs;
        result.crashInference.Clear();
        result.crashInference.Add("Log entries captured before failure: " + logs.Count);
        result.crashInference.Add("Last turn actor: " + InferLastTurnActor(logs));

        Dictionary<string, int> damageTaken = InferDamageTaken(logs);
        foreach (KeyValuePair<string, int> entry in damageTaken)
        {
            result.crashInference.Add("Damage inferred: " + entry.Key + " took " + entry.Value);
        }

        if (scenario != null && simulator.actorStats != null && simulator.battleManager.actorAI != null)
        {
            AppendBossThresholdInference(result, simulator, scenario.partyOne, "Party One", damageTaken);
            AppendBossThresholdInference(result, simulator, scenario.partyTwo, "Party Two", damageTaken);
        }
    }

    static void AppendBossThresholdInference(BattleSimulationRunResult result, BattleSimulator simulator, List<BattleTestActorSpec> actors, string partyName, Dictionary<string, int> damageTaken)
    {
        if (actors == null)
        {
            return;
        }
        ActorAI actorAI = simulator.battleManager.actorAI;
        for (int i = 0; i < actors.Count; i++)
        {
            BattleTestActorSpec actor = actors[i];
            if (actor == null || string.IsNullOrEmpty(actor.spriteName))
            {
                continue;
            }
            string displayName = actor.DisplayName(i);
            int damage = 0;
            damageTaken.TryGetValue(displayName, out damage);
            string stats = string.IsNullOrEmpty(actor.statsOverride) ? simulator.actorStats.ReturnValue(actor.spriteName) : actor.statsOverride;
            int maxHealth = SafeParseInt(ActorStatField(stats, 4));
            string rotationKey = actorAI.spriteToBossRotation == null ? "" : actorAI.spriteToBossRotation.ReturnValue(actor.spriteName);
            string rotation = actorAI.bossSkillRotation == null ? "" : actorAI.bossSkillRotation.ReturnValue(rotationKey);
            if (string.IsNullOrEmpty(rotation))
            {
                continue;
            }

            result.crashInference.Add(partyName + "[" + i + "] " + displayName + " sprite=" + actor.spriteName + " hp=" + maxHealth + " inferredDamage=" + damage + " rotation=" + rotationKey);
            string[] blocks = rotation.Split('#');
            for (int blockIndex = 0; blockIndex < blocks.Length; blockIndex++)
            {
                string[] fields = blocks[blockIndex].Split('|');
                if (fields.Length < 4)
                {
                    result.crashInference.Add("  block[" + blockIndex + "] malformed fields=" + fields.Length + " raw=`" + blocks[blockIndex] + "`");
                    continue;
                }
                if (fields[0] == "Damaged>")
                {
                    int threshold = SafeParseInt(fields[1]);
                    result.crashInference.Add("  block[" + blockIndex + "] Damaged>" + threshold + " wouldMatch=" + (damage > threshold) + " action=" + fields[2] + " target=" + fields[3]);
                }
            }
        }
    }

    static Dictionary<string, int> InferDamageTaken(List<string> logs)
    {
        Dictionary<string, int> damageTaken = new Dictionary<string, int>();
        Regex damageRegex = new Regex(@"^(.*?) takes ([0-9]+) damage\.$");
        for (int i = 0; i < logs.Count; i++)
        {
            string[] events = logs[i].Split('|');
            for (int eventIndex = 0; eventIndex < events.Length; eventIndex++)
            {
                Match match = damageRegex.Match(events[eventIndex].Trim());
                if (!match.Success)
                {
                    continue;
                }
                string actorName = match.Groups[1].Value;
                int damage = SafeParseInt(match.Groups[2].Value);
                if (!damageTaken.ContainsKey(actorName))
                {
                    damageTaken.Add(actorName, 0);
                }
                damageTaken[actorName] += damage;
            }
        }
        return damageTaken;
    }

    static string InferLastTurnActor(List<string> logs)
    {
        for (int i = logs.Count - 1; i >= 0; i--)
        {
            string[] events = logs[i].Split('|');
            for (int eventIndex = events.Length - 1; eventIndex >= 0; eventIndex--)
            {
                string text = events[eventIndex].Trim();
                string marker = "'s Turn";
                int markerIndex = text.IndexOf(marker, StringComparison.Ordinal);
                if (markerIndex > 0)
                {
                    return text.Substring(0, markerIndex);
                }
            }
        }
        return "";
    }

    static string DescribeActorSpecs(List<BattleTestActorSpec> actors)
    {
        if (actors == null)
        {
            return "null";
        }
        List<string> labels = new List<string>();
        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i] == null)
            {
                labels.Add(i + "=null");
            }
            else
            {
                labels.Add(i + "=" + actors[i].spriteName + " as " + actors[i].DisplayName(i));
            }
        }
        return string.Join(", ", labels.ToArray());
    }

    static string DescribeCharacterList(CharacterList list)
    {
        if (list == null)
        {
            return "null";
        }
        return "characters=" + SafeListCount(list.characters)
            + " names=" + SafeListCount(list.characterNames)
            + " stats=" + SafeListCount(list.stats)
            + " ids=" + SafeListCount(list.characterIDs)
            + " equipment=" + SafeListCount(list.equipment)
            + " modifiers=" + SafeListCount(list.battleModifiers);
    }

    static int SafeListCount<T>(List<T> list)
    {
        return list == null ? 0 : list.Count;
    }

    static string ValueAt(List<string> list, int index)
    {
        if (list == null || index < 0 || index >= list.Count)
        {
            return "";
        }
        return list[index];
    }

    static string JoinList(List<string> values)
    {
        if (values == null)
        {
            return "null";
        }
        return string.Join(", ", values.ToArray());
    }

    static bool StringListContains(List<string> values, string target)
    {
        return values != null && values.Contains(target);
    }

    static bool SafeDatabaseKeyExists(StatDatabase database, string key)
    {
        if (database == null || string.IsNullOrEmpty(key))
        {
            return false;
        }
        try
        {
            return database.KeyExists(key);
        }
        catch
        {
            return false;
        }
    }

    static string SafeDatabaseValue(StatDatabase database, string key)
    {
        if (database == null || string.IsNullOrEmpty(key))
        {
            return "";
        }
        try
        {
            return database.ReturnValue(key);
        }
        catch (Exception exception)
        {
            return "ERROR:" + exception.GetType().Name + ":" + exception.Message;
        }
    }

    static int SafeActorListCount(Func<List<TacticActor>> listProvider)
    {
        try
        {
            List<TacticActor> actors = listProvider();
            return actors == null ? -1 : actors.Count;
        }
        catch
        {
            return -1;
        }
    }

    static string SafeActorName(TacticActor actor)
    {
        if (actor == null)
        {
            return "";
        }
        try
        {
            return actor.GetPersonalName();
        }
        catch
        {
            return "";
        }
    }

    static string SafeActorSprite(TacticActor actor)
    {
        if (actor == null)
        {
            return "";
        }
        try
        {
            return actor.GetSpriteName();
        }
        catch
        {
            return "";
        }
    }

    static string DescribeTarget(TacticActor actor)
    {
        TacticActor target = actor.GetTarget();
        if (target == null)
        {
            return "null";
        }
        return SafeActorName(target)
            + " sprite=" + SafeActorSprite(target)
            + " team=" + target.GetTeam()
            + " hp=" + target.GetHealth() + "/" + target.GetBaseHealth()
            + " location=" + target.GetLocation();
    }

    static string DescribeTempPassives(TacticActor actor)
    {
        if (actor == null || actor.tempPassives == null)
        {
            return "null";
        }
        List<string> passives = new List<string>();
        for (int i = 0; i < actor.tempPassives.Count; i++)
        {
            string duration = actor.tempPassiveDurations != null && i < actor.tempPassiveDurations.Count
                ? actor.tempPassiveDurations[i].ToString()
                : "?";
            passives.Add(actor.tempPassives[i] + ":" + duration);
        }
        return string.Join(", ", passives.ToArray());
    }

    static string DescribeHurtBy(TacticActor actor)
    {
        if (actor == null || actor.hurtByList == null)
        {
            return "null";
        }
        List<string> hurtBy = new List<string>();
        for (int i = 0; i < actor.hurtByList.Count; i++)
        {
            TacticActor source = actor.hurtByList[i];
            string count = actor.hurtCount != null && i < actor.hurtCount.Count ? actor.hurtCount[i].ToString() : "?";
            string amount = actor.hurtAmount != null && i < actor.hurtAmount.Count ? actor.hurtAmount[i].ToString() : "?";
            hurtBy.Add(SafeActorName(source) + " count=" + count + " amount=" + amount);
        }
        return string.Join(", ", hurtBy.ToArray());
    }

    static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }
        return value.Substring(0, maxLength) + "...";
    }

    static int FieldCount(string value, char delimiter)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }
        return value.Split(delimiter).Length;
    }

    static string ActorStatField(string stats, int index)
    {
        if (string.IsNullOrEmpty(stats))
        {
            return "";
        }
        string[] fields = stats.Split('!');
        if (index < 0 || index >= fields.Length)
        {
            return "";
        }
        return fields[index];
    }

    static int SafeParseInt(string value)
    {
        int parsed;
        if (!int.TryParse(value, out parsed))
        {
            return 0;
        }
        return parsed;
    }

    static void ValidateScenario(BattleSimulator simulator, BattleTestScenario scenario)
    {
        if (scenario == null)
        {
            throw new InvalidOperationException("Scenario is missing.");
        }
        if (simulator == null)
        {
            throw new InvalidOperationException("BattleSimulator is missing.");
        }
        if (simulator.simulatorState == null)
        {
            throw new InvalidOperationException("BattleSimulator has no simulatorState reference.");
        }
        if (simulator.partyOneList == null || simulator.partyTwoList == null)
        {
            throw new InvalidOperationException("BattleSimulator has missing party list references.");
        }
        if (simulator.actorStats == null)
        {
            throw new InvalidOperationException("BattleSimulator has no actorStats reference.");
        }
        simulator.actorStats.Initialize();

        ValidateParty("Party One", scenario.partyOne, simulator.actorStats);
        ValidateParty("Party Two", scenario.partyTwo, simulator.actorStats);
        ValidateChoices("terrain", scenario.allowedTerrains, simulator.simulatorState.allTerrainTypes);
        ValidateChoices("weather", scenario.allowedWeather, simulator.simulatorState.allWeathers);
        ValidateChoices("time", scenario.allowedTimes, simulator.simulatorState.allTimes);
        ValidateChoices("starting formation", scenario.startingFormations, simulator.simulatorState.allStartingFormations);
        ValidateChoices("party one battle modifier", scenario.partyOneBattleModifiers, simulator.simulatorState.allBattleModifiers);
        ValidateChoices("party two battle modifier", scenario.partyTwoBattleModifiers, simulator.simulatorState.allBattleModifiers);
    }

    static void ValidateParty(string partyName, List<BattleTestActorSpec> actors, StatDatabase actorStats)
    {
        if (actors == null || actors.Count == 0)
        {
            throw new InvalidOperationException(partyName + " has no actors.");
        }
        for (int i = 0; i < actors.Count; i++)
        {
            BattleTestActorSpec actor = actors[i];
            if (actor == null || string.IsNullOrEmpty(actor.spriteName))
            {
                throw new InvalidOperationException(partyName + " actor " + i + " has no spriteName.");
            }
            if (!actorStats.KeyExists(actor.spriteName) && string.IsNullOrEmpty(actor.statsOverride))
            {
                throw new InvalidOperationException(partyName + " actor '" + actor.spriteName + "' is missing from actorStats and has no statsOverride.");
            }
        }
    }

    static void ValidateChoices(string label, List<string> selected, List<string> allowed)
    {
        if (selected == null || selected.Count == 0)
        {
            return;
        }
        for (int i = 0; i < selected.Count; i++)
        {
            if (string.IsNullOrEmpty(selected[i]))
            {
                continue;
            }
            if (allowed == null || !allowed.Contains(selected[i]))
            {
                throw new InvalidOperationException("Invalid " + label + " '" + selected[i] + "'.");
            }
        }
    }

    static void ApplyScenario(BattleSimulator simulator, BattleTestScenario scenario, int runIndex, int seed)
    {
        UnityEngine.Random.InitState(seed);
        PopulateParty(simulator.partyOneList, scenario.partyOne, simulator.actorStats, runIndex, 0);
        PopulateParty(simulator.partyTwoList, scenario.partyTwo, simulator.actorStats, runIndex, 1);

        BattleSimulatorState state = simulator.simulatorState;
        state.selectedTerrainTypes = NewList(scenario.allowedTerrains);
        state.selectedWeathers = NewList(scenario.allowedWeather);
        state.selectedTimes = NewList(scenario.allowedTimes);
        state.selectedStartingFormations = NewList(scenario.startingFormations);
        state.selectedP1BattleMods = NewList(scenario.partyOneBattleModifiers);
        state.selectedP2BattleMods = NewList(scenario.partyTwoBattleModifiers);
        // Use the existing multibattle stats path for exactly one run so BattleStatsTracker keeps damage data.
        state.multiBattle = 1;
        state.prevMultiBattle = 0;
        state.multiBattleCurrent = 1;
        state.multiBattleCount = 1;
        state.autoBattle = scenario.autoBattle ? 1 : 0;
        state.controlAI = scenario.controlAI ? 1 : 0;
        state.winningTeam = -1;
        state.ClearCustomBattleName();

        if (simulator.battleStatsTrackerSaving != null)
        {
            simulator.battleStatsTrackerSaving.NewGame();
        }
        if (simulator.battleManager != null && simulator.battleManager.battleEndManager != null)
        {
            simulator.battleManager.battleEndManager.test = true;
        }
    }

    static void PopulateParty(CharacterList targetList, List<BattleTestActorSpec> actors, StatDatabase actorStats, int runIndex, int team)
    {
        targetList.ResetLists();
        for (int i = 0; i < actors.Count; i++)
        {
            BattleTestActorSpec actor = actors[i];
            string stats = string.IsNullOrEmpty(actor.statsOverride) ? actorStats.ReturnValue(actor.spriteName) : actor.statsOverride;
            string id = actor.ActorId((runIndex * 1000) + (team * 100) + i);
            targetList.AddMemberToParty(actor.DisplayName(i), stats, actor.spriteName, id, actor.equipment);
        }
    }

    static List<string> NewList(List<string> source)
    {
        if (source == null)
        {
            return new List<string>();
        }
        return new List<string>(source);
    }

    static void CollectResult(BattleSimulator simulator, BattleTestScenario scenario, BattleSimulationRunResult result)
    {
        BattleManager battleManager = simulator.battleManager;
        BattleStatsTracker tracker = battleManager.battleStatsTracker;

        result.rounds = battleManager.GetRoundNumber();
        result.turnIndex = battleManager.GetTurnIndex();
        result.winningTeam = tracker == null ? -1 : tracker.winningTeam;
        if (result.winningTeam < 0)
        {
            result.winningTeam = battleManager.FindWinningTeam();
        }
        result.terrain = simulator.simulatorState.selectedTerrain;
        result.weather = simulator.simulatorState.selectedWeather;
        result.time = simulator.simulatorState.selectedTime;
        result.startingFormation = simulator.simulatorState.selectedStartingFormation;
        if (battleManager.combatLog != null)
        {
            result.logEntries = battleManager.combatLog.allLogs.Count;
            result.combatLogs = new List<string>(battleManager.combatLog.allLogs);
            result.combatLogEntries = BuildCombatLogEntries(battleManager.combatLog);
        }
        if (tracker != null)
        {
            AddActorResults(result, tracker, simulator, scenario);
        }
        if (result.winningTeam < 0)
        {
            MarkFailed(result, "Runner could not determine a winning team.");
        }
        if (scenario.maxRounds > 0 && result.rounds > scenario.maxRounds)
        {
            MarkFailed(result, "Battle exceeded max rounds: " + result.rounds + " > " + scenario.maxRounds + ".");
        }
        if (scenario.maxTurns > 0 && result.logEntries > scenario.maxTurns)
        {
            MarkFailed(result, "Battle exceeded max turn/log limit: " + result.logEntries + " > " + scenario.maxTurns + ".");
        }
    }

    static List<BattleSimulationCombatLogEntry> BuildCombatLogEntries(CombatLog combatLog)
    {
        List<BattleSimulationCombatLogEntry> entries = new List<BattleSimulationCombatLogEntry>();
        for (int i = 0; i < combatLog.allLogs.Count; i++)
        {
            string[] events = combatLog.allLogs[i].Split('|');
            int round = i < combatLog.combatRoundTracker.Count ? combatLog.combatRoundTracker[i] : 0;
            int turn = i < combatLog.combatTurnTracker.Count ? combatLog.combatTurnTracker[i] : 0;
            for (int eventIndex = 0; eventIndex < events.Length; eventIndex++)
            {
                if (string.IsNullOrEmpty(events[eventIndex]))
                {
                    continue;
                }
                BattleSimulationCombatLogEntry entry = new BattleSimulationCombatLogEntry();
                entry.round = round;
                entry.turn = turn;
                entry.eventIndex = eventIndex;
                entry.text = events[eventIndex];
                ParseCombatAction(entry);
                entries.Add(entry);
            }
        }
        return entries;
    }

    static void ParseCombatAction(BattleSimulationCombatLogEntry entry)
    {
        ParseAction(entry, " uses ", "Skill");
        if (!string.IsNullOrEmpty(entry.actionType))
        {
            return;
        }
        ParseAction(entry, " casts  ", "Spell");
        if (!string.IsNullOrEmpty(entry.actionType))
        {
            return;
        }
        ParseAction(entry, " casts ", "Spell");
        if (!string.IsNullOrEmpty(entry.actionType))
        {
            return;
        }
        ParseAction(entry, " attacks ", "Attack");
    }

    static void ParseAction(BattleSimulationCombatLogEntry entry, string marker, string actionType)
    {
        int markerIndex = entry.text.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return;
        }

        entry.actorName = entry.text.Substring(0, markerIndex).Trim();
        entry.actionType = actionType;
        string skill = entry.text.Substring(markerIndex + marker.Length).Trim();
        if (skill.EndsWith("."))
        {
            skill = skill.Substring(0, skill.Length - 1);
        }
        entry.skillName = skill;
    }

    static void AddActorResults(BattleSimulationRunResult result, BattleStatsTracker tracker, BattleSimulator simulator, BattleTestScenario scenario)
    {
        List<string> names = tracker.GetActorNames();
        List<string> sprites = tracker.GetActorSprites();
        List<int> teams = tracker.GetActorTeams();
        List<int> damageDealt = tracker.GetDamageDealt();
        List<int> damageTaken = tracker.GetDamageTaken();

        for (int i = 0; i < names.Count; i++)
        {
            BattleSimulationActorResult actor = new BattleSimulationActorResult();
            actor.actorName = names[i];
            actor.spriteName = i < sprites.Count ? sprites[i] : "";
            actor.team = i < teams.Count ? teams[i] : -1;
            actor.damageDealt = i < damageDealt.Count ? damageDealt[i] : 0;
            actor.damageTaken = i < damageTaken.Count ? damageTaken[i] : 0;
            AddBaseStats(actor, simulator, scenario);
            result.actors.Add(actor);
        }
    }

    static void AddBaseStats(BattleSimulationActorResult actor, BattleSimulator simulator, BattleTestScenario scenario)
    {
        BattleTestActorSpec spec = FindActorSpec(actor, scenario);
        string stats = "";
        if (spec != null)
        {
            stats = string.IsNullOrEmpty(spec.statsOverride) ? simulator.actorStats.ReturnValue(spec.spriteName) : spec.statsOverride;
        }
        else if (!string.IsNullOrEmpty(actor.spriteName))
        {
            stats = simulator.actorStats.ReturnValue(actor.spriteName);
        }

        ActorStatSummary summary = ActorStatSummary.FromString(stats);
        actor.species = summary.species;
        actor.elements = summary.elements;
        actor.attributes = summary.attributes;
        actor.health = summary.health;
        actor.attack = summary.attack;
        actor.range = summary.range;
        actor.defense = summary.defense;
        actor.moveSpeed = summary.moveSpeed;
        actor.moveType = summary.moveType;
        actor.energy = summary.energy;
        actor.initiative = summary.initiative;
        actor.dodge = summary.dodge;
        actor.magicPower = summary.magicPower;
        actor.magicResist = summary.magicResist;
        actor.passives = summary.passives;
        actor.actives = summary.actives;
        actor.spells = summary.spells;
        actor.baseStats = summary.CompactString();
    }

    static BattleTestActorSpec FindActorSpec(BattleSimulationActorResult actor, BattleTestScenario scenario)
    {
        List<BattleTestActorSpec> actors = actor.team == 0 ? scenario.partyOne : scenario.partyTwo;
        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i] == null)
            {
                continue;
            }
            if (actor.actorName == actors[i].DisplayName(i))
            {
                return actors[i];
            }
        }
        return null;
    }

    class ActorStatSummary
    {
        public string species = "";
        public string elements = "";
        public string attributes = "";
        public int health;
        public int attack;
        public int range;
        public int defense;
        public int moveSpeed;
        public string moveType = "";
        public int energy;
        public int initiative;
        public int dodge;
        public int magicPower;
        public int magicResist;
        public string passives = "";
        public string actives = "";
        public string spells = "";

        public static ActorStatSummary FromString(string stats)
        {
            ActorStatSummary summary = new ActorStatSummary();
            if (string.IsNullOrEmpty(stats))
            {
                return summary;
            }

            string[] values = stats.Split('!');
            summary.species = Value(values, 1);
            summary.elements = Value(values, 2);
            summary.attributes = Value(values, 3);
            summary.health = IntValue(values, 4);
            summary.attack = IntValue(values, 5);
            summary.range = IntValue(values, 6);
            summary.defense = IntValue(values, 7);
            summary.moveSpeed = IntValue(values, 8);
            summary.moveType = Value(values, 9);
            summary.initiative = IntValue(values, 11);
            summary.energy = IntValue(values, 12);
            summary.dodge = IntValue(values, 17);
            summary.passives = Value(values, 18);
            summary.actives = Value(values, 21);
            summary.magicPower = IntValue(values, 24);
            summary.magicResist = IntValue(values, 25);
            summary.spells = Value(values, 28);
            return summary;
        }

        public string CompactString()
        {
            return "HP " + health
                + ", ATK " + attack
                + ", DEF " + defense
                + ", RNG " + range
                + ", SPD " + moveSpeed
                + ", EN " + energy
                + ", INIT " + initiative
                + ", DODGE " + dodge
                + ", MP " + magicPower
                + ", MR " + magicResist;
        }

        static string Value(string[] values, int index)
        {
            if (index < 0 || index >= values.Length)
            {
                return "";
            }
            return values[index];
        }

        static int IntValue(string[] values, int index)
        {
            int parsed;
            if (!int.TryParse(Value(values, index), out parsed))
            {
                return 0;
            }
            return parsed;
        }
    }

    static void MarkFailed(BattleSimulationRunResult result, string reason)
    {
        result.failed = true;
        if (string.IsNullOrEmpty(result.failureReason))
        {
            result.failureReason = reason;
        }
        else if (!result.failureReason.Contains(reason))
        {
            result.failureReason += " " + reason;
        }
    }

    static void CaptureUnityErrors(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Assert && type != LogType.Error && type != LogType.Exception)
        {
            return;
        }
        capturedErrors.Add(type + ": " + condition + "\n" + stackTrace);
    }

    class RuntimeConditionResult
    {
        public bool canEvaluate;
        public bool matches;
        public string description;
    }
}
