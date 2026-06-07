using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BattleTraceCli
{
    const string SimulatorScenePath = "Assets/Scenes/DebugScenes/BattleSimulator.unity";

    public static void RunScenarioTrace()
    {
        int exitCode = 0;
        try
        {
            string scenarioPath = GetRequiredArg("-scenarioPath");
            string tracePath = GetRequiredArg("-tracePath");
            int runIndex = GetIntArg("-runIndex", 0);

            AssetDatabase.Refresh();
            BattleTestScenario scenario = AssetDatabase.LoadAssetAtPath<BattleTestScenario>(scenarioPath);
            if (scenario == null)
            {
                throw new InvalidOperationException("Could not load battle test scenario at " + scenarioPath + ".");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);
            WritePrepSnapshot(tracePath, BuildPrepSnapshot(simulator, scenario.ScenarioName(), runIndex));
            BattleSimulationRunResult result = BattleSimulationRunner.RunScenarioInLoadedScene(scenario, runIndex);

            string directory = Path.GetDirectoryName(tracePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(tracePath, JsonUtility.ToJson(result, true));
            Debug.Log("Battle trace written to: " + tracePath);
            if (result != null && result.failed)
            {
                exitCode = 1;
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            exitCode = 1;
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupTrace()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            string tracePath = GetRequiredArg("-tracePath");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);
            WritePrepSnapshot(tracePath, BuildPrepSnapshot(simulator, scenario.ScenarioName(), runIndex));
            BattleSimulationRunResult result = BattleSimulationRunner.RunScenarioInLoadedScene(scenario, runIndex);

            string directory = Path.GetDirectoryName(tracePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(tracePath, JsonUtility.ToJson(result, true));
            Debug.Log("Battle trace written to: " + tracePath);
            if (result != null && result.failed)
            {
                exitCode = 1;
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupMapTrace()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            string tracePath = GetRequiredArg("-tracePath");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);
            WritePrepSnapshot(tracePath, BuildPrepSnapshot(simulator, scenario.ScenarioName(), runIndex));

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            BattleManager manager = PrepareBattleManagerForTrace(simulator);
            UnityMapTrace trace = BuildUnityMapTrace(simulator, scenario, pairIndex, runIndex, seed);
            string directory = Path.GetDirectoryName(tracePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(tracePath, JsonUtility.ToJson(trace, true));
            Debug.Log("Battle matchup map trace written to: " + tracePath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupStartupTrace()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            string tracePath = GetRequiredArg("-tracePath");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = Resources.FindObjectsOfTypeAll<BattleSimulator>()[0];
            int seed = scenario.SeedForRun(runIndex);
            BattleSimulatorState state = simulator.simulatorState;
            BattleManager manager = simulator.battleManager;
            BattleStartManager startManager = manager == null ? null : manager.startManager;
            BattleState managerState = manager == null ? null : manager.battleState;
            BattleState startManagerState = startManager == null ? null : startManager.battleState;
            UnityEngine.Random.InitState(seed);

            state.selectedTerrainTypes = NewList(scenario.allowedTerrains);
            state.selectedWeathers = NewList(scenario.allowedWeather);
            state.selectedTimes = NewList(scenario.allowedTimes);
            state.selectedStartingFormations = NewList(scenario.startingFormations);

            StartupTrace trace = new StartupTrace();
            trace.scenarioName = scenario.ScenarioName();
            trace.seed = seed;
            trace.allowedTerrains = NewList(scenario.allowedTerrains);
            trace.allowedWeather = NewList(scenario.allowedWeather);
            trace.allowedTimes = NewList(scenario.allowedTimes);
            trace.allowedFormations = NewList(scenario.startingFormations);
            trace.simulatorStateName = state == null ? "" : state.name;
            trace.simulatorStateType = state == null ? "" : state.GetType().FullName;
            trace.managerStateName = managerState == null ? "" : managerState.name;
            trace.managerStateType = managerState == null ? "" : managerState.GetType().FullName;
            trace.startManagerStateName = startManagerState == null ? "" : startManagerState.name;
            trace.startManagerStateType = startManagerState == null ? "" : startManagerState.GetType().FullName;

            trace.terrainCall = state.GetTerrainType();
            trace.selectedTerrainAfterCall = state.selectedTerrain;
            trace.startBattleTimeCall = state.GetTime();
            trace.selectedTimeAfterStartBattle = state.selectedTime;
            trace.startBattleWeatherCall = state.GetWeather();
            trace.selectedWeatherAfterStartBattle = state.selectedWeather;
            trace.initializeMapWeatherCall = state.GetWeather();
            trace.selectedWeatherAfterInitializeMap = state.selectedWeather;
            trace.initializeMapWeatherLogCall = state.GetWeather();
            trace.selectedWeatherAfterWeatherLog = state.selectedWeather;
            trace.initializeMapTimeCall = state.GetTime();
            trace.selectedTimeAfterInitializeMap = state.selectedTime;
            trace.initializeMapTimeLogCall = state.GetTime();
            trace.selectedTimeAfterTimeLog = state.selectedTime;
            state.GetStartingFormation();
            trace.startingFormationCall = state.selectedStartingFormation;
            if (startManagerState != null)
            {
                UnityEngine.Random.InitState(seed);
                trace.startManagerWeatherCall = startManagerState.GetWeather();
                trace.startManagerWeatherLogCall = startManagerState.GetWeather();
                trace.startManagerTimeCall = startManagerState.GetTime();
                trace.startManagerTimeLogCall = startManagerState.GetTime();
                trace.startManagerAllySpawnPattern = startManagerState.GetAllySpawnPattern();
                trace.startManagerEnemySpawnPattern = startManagerState.GetEnemySpawnPattern();
            }

            string directory = Path.GetDirectoryName(tracePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(tracePath, JsonUtility.ToJson(trace, true));
            Debug.Log("Battle startup trace written to: " + tracePath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupMoveProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            string outPath = GetRequiredArg("-outPath");
            string actorName = GetRequiredArg("-actorName");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            simulator.StartBattle();
            if (simulator.battleManager == null)
            {
                throw new InvalidOperationException("BattleSimulator has no BattleManager reference.");
            }
            simulator.battleManager.ForceStart();
            AdvanceNpcTurns(simulator.battleManager, turnsToAdvance);

            UnityMoveProbe probe = BuildMoveProbe(simulator.battleManager, actorName, pairIndex, scenario, runIndex, seed);
            probe.turnsAdvanced = turnsToAdvance;
            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle move probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupNpcTurnProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            string outPath = GetRequiredArg("-outPath");
            string actorName = GetRequiredArg("-actorName");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            BattleManager manager = PrepareBattleManagerForTrace(simulator);
            AdvanceNpcTurns(manager, turnsToAdvance);

            UnityNpcTurnProbe probe = BuildNpcTurnProbe(manager, actorName, scenario, pairIndex, runIndex, seed, turnsToAdvance);
            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle NPC turn probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupAnchoredNpcTurnProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        string outPath = "";
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            outPath = GetRequiredArg("-outPath");
            string actorName = GetRequiredArg("-actorName");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int targetRound = GetIntArg("-targetRound", 1);
            int targetTurnIndex = GetIntArg("-targetTurnIndex", 0);
            int maxTurnsToAdvance = GetIntArg("-maxTurnsToAdvance", 200);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }

            BattleManager manager = PrepareBattleManagerForTrace(simulator);
            List<UnityAdvanceSnapshot> advanceSnapshots = new List<UnityAdvanceSnapshot>();
            int turnsAdvanced = AdvanceNpcTurnsUntil(manager, actorName, targetRound, targetTurnIndex, maxTurnsToAdvance, advanceSnapshots);
            UnityNpcTurnProbe probe = BuildNpcTurnProbe(manager, actorName, scenario, pairIndex, runIndex, seed, turnsAdvanced);
            probe.advanceSnapshots = advanceSnapshots;

            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle anchored NPC turn probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            TryWriteProbeError(outPath, exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupActualTurnProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        string outPath = "";
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            outPath = GetRequiredArg("-outPath");
            string actorName = GetRequiredArg("-actorName");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            BattleManager manager = PrepareBattleManagerForTrace(simulator);
            AdvanceNpcTurns(manager, turnsToAdvance);

            UnityActualTurnProbe probe = BuildActualTurnProbe(manager, actorName, scenario, pairIndex, runIndex, seed, turnsToAdvance);
            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle actual-turn probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            TryWriteProbeError(outPath, exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupAnchoredActualTurnProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        string outPath = "";
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            outPath = GetRequiredArg("-outPath");
            string actorName = GetRequiredArg("-actorName");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int targetRound = GetIntArg("-targetRound", -1);
            int targetTurnIndex = GetIntArg("-targetTurnIndex", -1);
            int maxTurnsToAdvance = GetIntArg("-maxTurnsToAdvance", 400);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }
            if (targetRound < 0 || targetTurnIndex < 0)
            {
                throw new InvalidOperationException("Missing required anchor arguments -targetRound and -targetTurnIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            BattleManager manager = PrepareBattleManagerForTrace(simulator);
            List<UnityAdvanceSnapshot> advanceSnapshots = new List<UnityAdvanceSnapshot>();
            int turnsAdvanced = AdvanceNpcTurnsUntil(manager, actorName, targetRound, targetTurnIndex, maxTurnsToAdvance, advanceSnapshots);

            UnityActualTurnProbe probe = BuildActualTurnProbe(manager, actorName, scenario, pairIndex, runIndex, seed, turnsAdvanced);
            probe.advanceSnapshots = advanceSnapshots;
            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle anchored actual-turn probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            TryWriteProbeError(outPath, exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupIsolatedActualTurnProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        string outPath = "";
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            outPath = GetRequiredArg("-outPath");
            string actorName = GetRequiredArg("-actorName");
            string suppressActorName = GetRequiredArg("-suppressActorName");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            simulator.StartBattle();
            if (simulator.battleManager == null)
            {
                throw new InvalidOperationException("BattleSimulator has no BattleManager reference.");
            }
            simulator.battleManager.ForceStart();
            AdvanceNpcTurns(simulator.battleManager, turnsToAdvance);

            TacticActor suppressActor = FindActorByName(simulator.battleManager, suppressActorName);
            bool originalSummoned = suppressActor.summoned;
            suppressActor.summoned = false;
            UnityActualTurnProbe probe = BuildActualTurnProbe(simulator.battleManager, actorName, scenario, pairIndex, runIndex, seed, turnsToAdvance);
            suppressActor.summoned = originalSummoned;

            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle isolated actual-turn probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            TryWriteProbeError(outPath, exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupAttackSkillProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            string outPath = GetRequiredArg("-outPath");
            string actorName = GetRequiredArg("-actorName");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            simulator.StartBattle();
            if (simulator.battleManager == null)
            {
                throw new InvalidOperationException("BattleSimulator has no BattleManager reference.");
            }
            simulator.battleManager.ForceStart();
            AdvanceNpcTurns(simulator.battleManager, turnsToAdvance);

            UnityAttackSkillProbe probe = BuildAttackSkillProbe(simulator.battleManager, actorName, scenario, pairIndex, runIndex, seed, turnsToAdvance);
            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle attack-skill probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupSpellAttemptProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        string outPath = "";
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            outPath = GetRequiredArg("-outPath");
            string actorName = GetRequiredArg("-actorName");
            string spellName = GetRequiredArg("-spellName");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            BattleManager manager = PrepareBattleManagerForTrace(simulator);
            AdvanceNpcTurns(manager, turnsToAdvance);

            UnitySpellAttemptProbe probe = BuildSpellAttemptProbe(manager, actorName, spellName, scenario, pairIndex, runIndex, seed, turnsToAdvance);
            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle spell-attempt probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            TryWriteProbeError(outPath, exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupSkillAttemptProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        string outPath = "";
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            outPath = GetRequiredArg("-outPath");
            string actorName = GetRequiredArg("-actorName");
            string skillName = GetRequiredArg("-skillName");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            BattleManager manager = PrepareBattleManagerForTrace(simulator);
            AdvanceNpcTurns(manager, turnsToAdvance);

            UnitySkillAttemptProbe probe = BuildSkillAttemptProbe(manager, actorName, skillName, scenario, pairIndex, runIndex, seed, turnsToAdvance);
            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle skill-attempt probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            TryWriteProbeError(outPath, exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupTurnDecisionProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        string outPath = "";
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            outPath = GetRequiredArg("-outPath");
            string actorName = GetRequiredArg("-actorName");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            BattleManager manager = PrepareBattleManagerForTrace(simulator);
            AdvanceNpcTurns(manager, turnsToAdvance);

            UnityTurnDecisionProbe probe = BuildTurnDecisionProbe(manager, actorName, scenario, pairIndex, runIndex, seed, turnsToAdvance);
            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle turn-decision probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            TryWriteProbeError(outPath, exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupFollowupNpcTurnProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            string tracePath = GetRequiredArg("-outPath");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);
            string actorName = GetRequiredArg("-actorName");
            string followActorName = GetRequiredArg("-followActorName");

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);
            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            simulator.StartBattle();
            simulator.battleManager.ForceStart();

            AdvanceNpcTurns(simulator.battleManager, turnsToAdvance);

            TacticActor followActor = FindActorByName(simulator.battleManager, followActorName);
            bool originalSummoned = followActor.summoned;
            followActor.summoned = false;
            BuildActualTurnProbe(simulator.battleManager, actorName, scenario, pairIndex, runIndex, seed, turnsToAdvance);
            followActor.summoned = originalSummoned;

            UnityNpcTurnProbe probe = BuildNpcTurnProbe(simulator.battleManager, followActorName, scenario, pairIndex, runIndex, seed, turnsToAdvance + 1);

            string directory = Path.GetDirectoryName(tracePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(tracePath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle followup NPC probe written to: " + tracePath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            exitCode = 1;
        }
        finally
        {
            if (generatedSuite != null)
            {
                UnityEngine.Object.DestroyImmediate(generatedSuite);
            }
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
        }
    }

    public static void RunEnemyMatchupChainedActualTurnProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            string tracePath = GetRequiredArg("-outPath");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);
            string actorName = GetRequiredArg("-actorName");

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);
            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            simulator.StartBattle();
            simulator.battleManager.ForceStart();

            AdvanceNpcTurns(simulator.battleManager, turnsToAdvance);
            UnityActualTurnProbe probe = BuildChainedActualTurnProbe(simulator.battleManager, actorName, scenario, pairIndex, runIndex, seed, turnsToAdvance);

            string directory = Path.GetDirectoryName(tracePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(tracePath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle chained actual-turn probe written to: " + tracePath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupActorStateDump()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            string outPath = GetRequiredArg("-outPath");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);
            string actorName = GetRequiredArg("-actorName");

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Pair index " + pairIndex + " is out of range for generated suite.");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            int seed = scenario.SeedForRun(runIndex);

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            simulator.StartBattle();
            simulator.battleManager.ForceStart();
            AdvanceNpcTurns(simulator.battleManager, turnsToAdvance);

            BattleManager manager = simulator.battleManager;
            TacticActor actor = FindActorByName(manager, actorName);
            manager.moveManager.GetAllMoveCosts(actor, manager.map.battlingActors);
            UnityActorStateDump dump = BuildActorStateDump(manager, actor, scenario, pairIndex, runIndex, seed, turnsToAdvance);
            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(outPath, JsonUtility.ToJson(dump, true));
            Debug.Log("Battle actor state dump written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupAttackStepProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        string outPath = "";
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            outPath = GetRequiredArg("-outPath");
            string actorName = GetRequiredArg("-actorName");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            simulator.StartBattle();
            if (simulator.battleManager == null)
            {
                throw new InvalidOperationException("BattleSimulator has no BattleManager reference.");
            }
            simulator.battleManager.ForceStart();
            AdvanceNpcTurns(simulator.battleManager, turnsToAdvance);

            UnityAttackStepProbe probe = BuildAttackStepProbe(simulator.battleManager, actorName, scenario, pairIndex, runIndex, seed, turnsToAdvance);
            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle attack-step probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            TryWriteProbeError(outPath, exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupTurnTrace()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        string outPath = "";
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            outPath = GetRequiredArg("-outPath");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int maxTurns = GetIntArg("-maxTurns", 12);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            BattleManager manager = PrepareBattleManagerForTrace(simulator);
            UnityTurnTrace trace = BuildTurnTrace(manager, scenario, pairIndex, runIndex, seed, maxTurns);
            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(trace, true));
            Debug.Log("Battle turn trace written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            TryWriteProbeError(outPath, exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void DumpBuffStatusValue()
    {
        int exitCode = 0;
        try
        {
            string statusId = GetRequiredArg("-statusId");
            string outPath = GetRequiredArg("-outPath");

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);
            if (simulator.battleManager == null || simulator.battleManager.attackManager == null || simulator.battleManager.attackManager.buffStatusData == null)
            {
                throw new InvalidOperationException("BattleSimulator attackManager buffStatusData is missing.");
            }

            string raw = simulator.battleManager.attackManager.buffStatusData.ReturnValue(statusId);
            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(new UnityBuffStatusValueDump
            {
                statusId = statusId,
                raw = raw
            }, true));
            Debug.Log("Battle buff-status dump written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            exitCode = 1;
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupSkillTargetProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            string outPath = GetRequiredArg("-outPath");
            string actorName = GetRequiredArg("-actorName");
            string skillName = GetRequiredArg("-skillName");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            simulator.StartBattle();
            if (simulator.battleManager == null)
            {
                throw new InvalidOperationException("BattleSimulator has no BattleManager reference.");
            }
            simulator.battleManager.ForceStart();

            AdvanceNpcTurns(simulator.battleManager, turnsToAdvance);
            UnitySkillTargetProbe probe = BuildSkillTargetProbe(simulator.battleManager, actorName, skillName, pairIndex, scenario, runIndex, seed, turnsToAdvance);
            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle skill target probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupPostSkillEnemyChoiceProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        string outPath = "";
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            outPath = GetRequiredArg("-outPath");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);
            string actorName = GetRequiredArg("-actorName");
            string skillName = GetRequiredArg("-skillName");
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, 1, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            simulator.StartBattle();
            if (simulator.battleManager == null)
            {
                throw new InvalidOperationException("BattleSimulator has no BattleManager reference.");
            }
            simulator.battleManager.ForceStart();

            AdvanceNpcTurns(simulator.battleManager, turnsToAdvance);
            UnityPostSkillEnemyChoiceProbe probe = BuildPostSkillEnemyChoiceProbe(simulator.battleManager, actorName, skillName, pairIndex, scenario, runIndex, seed, turnsToAdvance);
            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle post-skill enemy choice probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            TryWriteProbeError(outPath, exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunEnemyMatchupBossFallbackProbe()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        string outPath = "";
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            outPath = GetRequiredArg("-outPath");
            int pairIndex = GetIntArg("-pairIndex", -1);
            int runIndex = GetIntArg("-runIndex", 0);
            int turnsToAdvance = GetIntArg("-turnsToAdvance", 0);
            string actorName = GetRequiredArg("-actorName");
            string skillName = GetRequiredArg("-skillName");
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);

            if (pairIndex < 0)
            {
                throw new InvalidOperationException("Missing required argument -pairIndex.");
            }

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, 1, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null || pairIndex >= generatedSuite.scenarios.Count)
            {
                throw new InvalidOperationException("Generated matchup suite does not contain pair index " + pairIndex + ".");
            }

            BattleTestScenario scenario = generatedSuite.scenarios[pairIndex];
            if (scenario == null)
            {
                throw new InvalidOperationException("Generated matchup scenario at pair index " + pairIndex + " is null.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = FindTraceSimulator();
            EnsureTracePartyData(simulator);

            int seed = scenario.SeedForRun(runIndex);
            ApplyScenarioForTrace(simulator, scenario, runIndex, seed);
            if (simulator.simulatorState != null)
            {
                simulator.simulatorState.autoBattle = 0;
                simulator.simulatorState.controlAI = 1;
            }
            simulator.StartBattle();
            if (simulator.battleManager == null)
            {
                throw new InvalidOperationException("BattleSimulator has no BattleManager reference.");
            }
            simulator.battleManager.ForceStart();

            AdvanceNpcTurns(simulator.battleManager, turnsToAdvance);
            UnityBossFallbackProbe probe = BuildBossFallbackProbe(simulator.battleManager, actorName, skillName, pairIndex, scenario, runIndex, seed, turnsToAdvance);
            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Battle boss fallback probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            TryWriteProbeError(outPath, exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void ExportEnemyMatchupStartupSettings()
    {
        int exitCode = 0;
        BattleTestSuite generatedSuite = null;
        try
        {
            string suitePath = GetRequiredArg("-suitePath");
            string outPath = GetRequiredArg("-outPath");
            int sourceTeam = GetIntArg("-sourceTeam", 1);
            bool orderedPairs = GetBoolArg("-orderedPairs", true);
            bool includeMirrorMatches = GetBoolArg("-includeMirrorMatches", false);
            int runCountOverride = GetIntArg("-runCountOverride", 1);

            AssetDatabase.Refresh();
            BattleTestSuite sourceSuite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
            if (sourceSuite == null)
            {
                throw new InvalidOperationException("Could not load battle test suite at " + suitePath + ".");
            }

            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            if (generatedSuite == null || generatedSuite.scenarios == null)
            {
                throw new InvalidOperationException("Failed to build generated matchup suite.");
            }

            EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
            BattleSimulator simulator = Resources.FindObjectsOfTypeAll<BattleSimulator>()[0];
            List<EnemyMatchupStartupSetting> settings = new List<EnemyMatchupStartupSetting>();
            for (int i = 0; i < generatedSuite.scenarios.Count; i++)
            {
                BattleTestScenario scenario = generatedSuite.scenarios[i];
                if (scenario == null)
                {
                    continue;
                }
                settings.Add(CaptureEnemyMatchupStartupSetting(simulator, scenario, i, 0));
            }

            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonHelper.ToJson(settings.ToArray(), true));
            Debug.Log("Enemy matchup startup settings written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            exitCode = 1;
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunRandomSequenceProbe()
    {
        int exitCode = 0;
        try
        {
            string outPath = GetRequiredArg("-outPath");
            int seed = GetIntArg("-seed", 0);
            string rangesArg = GetArg("-intRanges");
            List<Vector2Int> ranges = string.IsNullOrEmpty(rangesArg) ? new List<Vector2Int>() : ParseIntRangesCsv(rangesArg);
            List<int> maxes = ranges.Count > 0 ? new List<int>() : ParseIntCsv(GetRequiredArg("-intMaxes"));
            if (maxes.Count == 0 && ranges.Count == 0)
            {
                throw new InvalidOperationException("No integer max values were provided.");
            }

            UnityEngine.Random.InitState(seed);
            UnityRandomSequenceProbe probe = new UnityRandomSequenceProbe();
            probe.seed = seed;
            probe.maxExclusive = new List<int>(maxes);
            probe.initialStateJson = JsonUtility.ToJson(UnityEngine.Random.state);
            for (int i = 0; i < maxes.Count; i++)
            {
                probe.stateBeforeEachDraw.Add(JsonUtility.ToJson(UnityEngine.Random.state));
                probe.values.Add(UnityEngine.Random.Range(0, maxes[i]));
                probe.stateAfterEachDraw.Add(JsonUtility.ToJson(UnityEngine.Random.state));
            }

            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Unity random sequence probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            exitCode = 1;
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    public static void RunRandomStateProbe()
    {
        int exitCode = 0;
        try
        {
            string outPath = GetRequiredArg("-outPath");
            int s0 = GetIntArg("-s0", 0);
            int s1 = GetIntArg("-s1", 0);
            int s2 = GetIntArg("-s2", 0);
            int s3 = GetIntArg("-s3", 0);
            string rangesArg = GetArg("-intRanges");
            List<Vector2Int> ranges = string.IsNullOrEmpty(rangesArg) ? new List<Vector2Int>() : ParseIntRangesCsv(rangesArg);
            List<int> maxes = ranges.Count > 0 ? new List<int>() : ParseIntCsv(GetRequiredArg("-intMaxes"));
            if (maxes.Count == 0 && ranges.Count == 0)
            {
                throw new InvalidOperationException("No integer max values were provided.");
            }

            UnityEngine.Random.State probeState = default;
            OverwriteRandomState(ref probeState, s0, s1, s2, s3);
            string overwrittenStateJson = JsonUtility.ToJson(probeState);
            UnityEngine.Random.state = probeState;

            UnityRandomSequenceProbe probe = new UnityRandomSequenceProbe();
            probe.seed = 0;
            probe.maxExclusive = new List<int>(maxes);
            for (int i = 0; i < ranges.Count; i++)
            {
                probe.minInclusive.Add(ranges[i].x);
                probe.maxExclusive.Add(ranges[i].y);
            }
            probe.requestedStateJson = "{\"s0\":" + s0 + ",\"s1\":" + s1 + ",\"s2\":" + s2 + ",\"s3\":" + s3 + "}";
            probe.overwrittenStateJson = overwrittenStateJson;
            probe.reflectionFields = DescribeRandomStateFields();
            probe.initialStateJson = JsonUtility.ToJson(UnityEngine.Random.state);
            int drawCount = ranges.Count > 0 ? ranges.Count : maxes.Count;
            for (int i = 0; i < drawCount; i++)
            {
                probe.stateBeforeEachDraw.Add(JsonUtility.ToJson(UnityEngine.Random.state));
                if (ranges.Count > 0)
                {
                    probe.values.Add(UnityEngine.Random.Range(ranges[i].x, ranges[i].y));
                }
                else
                {
                    probe.values.Add(UnityEngine.Random.Range(0, maxes[i]));
                }
                probe.stateAfterEachDraw.Add(JsonUtility.ToJson(UnityEngine.Random.state));
            }

            string directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outPath, JsonUtility.ToJson(probe, true));
            Debug.Log("Unity random state probe written to: " + outPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            exitCode = 1;
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    static string GetRequiredArg(string argName)
    {
        string value = GetArg(argName);
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException("Missing required argument " + argName + ".");
        }

        return value;
    }

    static string GetArg(string argName)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], argName, StringComparison.OrdinalIgnoreCase))
            {
                List<string> values = new List<string>();
                for (int j = i + 1; j < args.Length; j++)
                {
                    int ignoredNegativeInt;
                    if (args[j].StartsWith("-") && !int.TryParse(args[j], out ignoredNegativeInt))
                    {
                        break;
                    }
                    values.Add(args[j]);
                }

                return string.Join(" ", values);
            }
        }

        return "";
    }

    static int GetIntArg(string argName, int fallback)
    {
        string value = GetArg(argName);
        int parsedValue;
        if (int.TryParse(value, out parsedValue))
        {
            return parsedValue;
        }

        return fallback;
    }

    static List<int> ParseIntCsv(string csv)
    {
        List<int> values = new List<int>();
        string[] split = csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < split.Length; i++)
        {
            int value;
            if (!int.TryParse(split[i].Trim(), out value))
            {
                throw new InvalidOperationException("Could not parse integer value '" + split[i] + "' in CSV list.");
            }
            values.Add(value);
        }
        return values;
    }

    static List<Vector2Int> ParseIntRangesCsv(string csv)
    {
        List<Vector2Int> values = new List<Vector2Int>();
        string[] split = csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < split.Length; i++)
        {
            string[] range = split[i].Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (range.Length != 2)
            {
                throw new InvalidOperationException("Could not parse integer range '" + split[i] + "'. Use min:max.");
            }
            int min;
            int max;
            if (!int.TryParse(range[0].Trim(), out min) || !int.TryParse(range[1].Trim(), out max))
            {
                throw new InvalidOperationException("Could not parse integer range '" + split[i] + "'. Use min:max.");
            }
            values.Add(new Vector2Int(min, max));
        }
        return values;
    }

    static void OverwriteRandomState(ref UnityEngine.Random.State state, int s0, int s1, int s2, int s3)
    {
        SetRandomStateField(ref state, "s0", s0);
        SetRandomStateField(ref state, "s1", s1);
        SetRandomStateField(ref state, "s2", s2);
        SetRandomStateField(ref state, "s3", s3);
    }

    static void SetRandomStateField(ref UnityEngine.Random.State state, string fieldName, int value)
    {
        FieldInfo field = typeof(UnityEngine.Random.State).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
        {
            throw new InvalidOperationException("Could not find UnityEngine.Random.State field `" + fieldName + "`.");
        }

        if (field.FieldType == typeof(int))
        {
            field.SetValueDirect(__makeref(state), value);
            return;
        }

        if (field.FieldType == typeof(uint))
        {
            field.SetValueDirect(__makeref(state), unchecked((uint)value));
            return;
        }

        throw new InvalidOperationException("Unsupported UnityEngine.Random.State field type for `" + fieldName + "`: " + field.FieldType.FullName);
    }

    static List<string> DescribeRandomStateFields()
    {
        FieldInfo[] fields = typeof(UnityEngine.Random.State).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        List<string> descriptions = new List<string>();
        for (int i = 0; i < fields.Length; i++)
        {
            descriptions.Add(fields[i].Name + ":" + fields[i].FieldType.FullName);
        }
        return descriptions;
    }

    static bool GetBoolArg(string argName, bool fallback)
    {
        string value = GetArg(argName);
        bool parsedValue;
        if (bool.TryParse(value, out parsedValue))
        {
            return parsedValue;
        }

        return fallback;
    }

    static List<string> NewList(List<string> source)
    {
        if (source == null)
        {
            return new List<string>();
        }
        return new List<string>(source);
    }

    static EnemyMatchupStartupSetting CaptureEnemyMatchupStartupSetting(BattleSimulator simulator, BattleTestScenario scenario, int pairIndex, int runIndex)
    {
        int seed = scenario.SeedForRun(runIndex);
        BattleSimulatorState state = simulator.simulatorState;
        BattleManager manager = simulator.battleManager;
        BattleStartManager startManager = manager == null ? null : manager.startManager;
        BattleState startManagerState = startManager == null ? null : startManager.battleState;

        state.selectedTerrainTypes = NewList(scenario.allowedTerrains);
        state.selectedWeathers = NewList(scenario.allowedWeather);
        state.selectedTimes = NewList(scenario.allowedTimes);
        state.selectedStartingFormations = NewList(scenario.startingFormations);

        UnityEngine.Random.InitState(seed);
        string terrain = state.GetTerrainType();
        state.GetTime();
        state.GetWeather();

        string weather = "";
        string time = "";
        string allySpawnPattern = "";
        string enemySpawnPattern = "";
        if (startManagerState != null)
        {
            weather = startManagerState.GetWeather();
            startManagerState.GetWeather();
            time = startManagerState.GetTime();
            startManagerState.GetTime();
            allySpawnPattern = startManagerState.GetAllySpawnPattern();
            enemySpawnPattern = startManagerState.GetEnemySpawnPattern();
        }

        string formation = scenario.startingFormations == null || scenario.startingFormations.Count == 0 ? "" : scenario.startingFormations[0];
        return new EnemyMatchupStartupSetting
        {
            PairIndex = pairIndex,
            ScenarioName = scenario.ScenarioName(),
            Seed = seed,
            Terrain = terrain,
            Weather = weather,
            Time = time,
            Formation = formation,
            AllySpawnPattern = allySpawnPattern,
            EnemySpawnPattern = enemySpawnPattern
        };
    }

    static BattleSimulator FindTraceSimulator()
    {
        BattleSimulator[] simulators = Resources.FindObjectsOfTypeAll<BattleSimulator>();
        if (simulators == null || simulators.Length <= 0)
        {
            throw new InvalidOperationException("Could not find BattleSimulator in loaded simulator scene.");
        }
        return simulators[0];
    }

    static BattleManager PrepareBattleManagerForTrace(BattleSimulator simulator)
    {
        if (simulator == null)
        {
            throw new InvalidOperationException("BattleSimulator is required for trace preparation.");
        }

        simulator.StartBattle();
        BattleManager manager = simulator.battleManager;
        if (manager == null)
        {
            throw new InvalidOperationException("BattleSimulator has no BattleManager reference.");
        }

        bool needsForceStart = manager.GetTurnActor() == null
            && (manager.map == null || manager.map.battlingActors == null || manager.map.battlingActors.Count <= 0);
        if (needsForceStart)
        {
            manager.ForceStart();
        }

        if (simulator.simulatorState != null)
        {
            manager.SetAutoBattle(simulator.simulatorState.autoBattle > 0);
            manager.SetControlAI(simulator.simulatorState.controlAI > 0);
        }

        return manager;
    }

    static void EnsureTracePartyData(BattleSimulator simulator)
    {
        BattleManager manager = simulator == null ? null : simulator.battleManager;
        if (manager == null)
        {
            return;
        }

        if (manager.map != null && manager.map.battleManager == null)
        {
            manager.map.battleManager = manager;
        }

        if (manager.partyData == null)
        {
            PartyDataManager[] partyManagers = Resources.FindObjectsOfTypeAll<PartyDataManager>();
            if (partyManagers != null && partyManagers.Length > 0)
            {
                manager.partyData = partyManagers[0];
            }
        }

        PartyDataManager partyData = manager.partyData;
        if (partyData == null || partyData.inventory != null)
        {
            return;
        }

        Inventory inventory = ScriptableObject.CreateInstance<Inventory>();
        inventory.ClearItems();
        inventory.SetItemLimit(inventory.minimumItemLimit);
        partyData.inventory = inventory;
    }

    static void ApplyScenarioForTrace(BattleSimulator simulator, BattleTestScenario scenario, int runIndex, int seed)
    {
        MethodInfo applyScenario = typeof(BattleSimulationRunner).GetMethod("ApplyScenario", BindingFlags.NonPublic | BindingFlags.Static);
        if (applyScenario == null)
        {
            throw new InvalidOperationException("BattleSimulationRunner.ApplyScenario could not be found.");
        }
        applyScenario.Invoke(null, new object[] { simulator, scenario, runIndex, seed });
    }

    static UnityMapTrace BuildUnityMapTrace(BattleSimulator simulator, BattleTestScenario scenario, int pairIndex, int runIndex, int seed)
    {
        BattleManager manager = simulator == null ? null : simulator.battleManager;
        BattleMap map = manager == null ? null : manager.map;
        if (map == null)
        {
            throw new InvalidOperationException("BattleManager map is not available after startup.");
        }

        UnityMapTrace trace = new UnityMapTrace();
        trace.scenarioName = scenario == null ? "" : scenario.ScenarioName();
        trace.pairIndex = pairIndex;
        trace.runIndex = runIndex;
        trace.seed = seed;
        trace.terrain = simulator.simulatorState == null ? "" : simulator.simulatorState.selectedTerrain;
        UnityEngine.Random.State randomStateBeforeTraceMetadata = UnityEngine.Random.state;
        trace.weather = map.GetWeather();
        trace.time = map.GetTime();
        trace.startingFormation = simulator.simulatorState == null ? "" : simulator.simulatorState.selectedStartingFormation;
        UnityEngine.Random.state = randomStateBeforeTraceMetadata;
        trace.battleStartRandomStateJson = JsonUtility.ToJson(UnityEngine.Random.state);
        UnityEngine.Random.State savedState = UnityEngine.Random.state;
        UnityEngine.Random.Range(0, 1);
        trace.firstTurnRandomStateJson = JsonUtility.ToJson(UnityEngine.Random.state);
        UnityEngine.Random.state = savedState;
        trace.mapSize = map.mapSize;
        trace.mapInfo = map.mapInfo == null ? new List<string>() : new List<string>(map.mapInfo);
        trace.terrainEffectTiles = map.terrainEffectTiles == null ? new List<string>() : new List<string>(map.terrainEffectTiles);
        trace.elevations = map.mapElevations == null ? new List<int>() : new List<int>(map.mapElevations);
        trace.borderDetails = map.borderDetails == null ? new List<string>() : new List<string>(map.borderDetails);
        trace.buildings = BuildBuildingEntries(map);
        trace.actors = BuildActorEntries(manager);
        return trace;
    }

    static UnityMoveProbe BuildMoveProbe(BattleManager manager, string actorName, int pairIndex, BattleTestScenario scenario, int runIndex, int seed)
    {
        if (manager == null || manager.map == null || manager.moveManager == null || manager.actorAI == null)
        {
            throw new InvalidOperationException("BattleManager move probe dependencies are missing.");
        }

        TacticActor actor = null;
        for (int i = 0; i < manager.map.battlingActors.Count; i++)
        {
            if (manager.map.battlingActors[i].GetPersonalName() == actorName)
            {
                actor = manager.map.battlingActors[i];
                break;
            }
        }
        if (actor == null)
        {
            throw new InvalidOperationException("Could not find actor named " + actorName + " in battlingActors.");
        }

        manager.moveManager.GetAllMoveCosts(actor, manager.map.battlingActors);
        string randomStateBeforeTargetSelection = JsonUtility.ToJson(UnityEngine.Random.state);
        TacticActor target = manager.actorAI.GetClosestEnemy(manager.map.battlingActors, actor, manager.moveManager);
        actor.SetTarget(target);
        manager.moveManager.GetAllMoveCosts(actor, manager.map.battlingActors);

        int actorLocation = actor.GetLocation();
        int targetLocation = target == null ? -1 : target.GetLocation();
        int approachTile = -1;
        if (target != null)
        {
            if (actor.GetAttackRange() <= 1)
            {
                approachTile = manager.map.ReturnClosestTileWithinElevationDifference(actorLocation, targetLocation, actor.GetWeaponReach(), manager.moveManager.pathCosts);
            }
            else
            {
                approachTile = manager.map.ReturnClosestTileWithLineOfSight(actorLocation, targetLocation, actor.GetAttackRange(), manager.moveManager.pathCosts);
            }
        }

        List<int> fullPath = target == null ? new List<int>() : manager.moveManager.GetPrecomputedPath(actorLocation, approachTile, true);
        int fullPathCost = manager.moveManager.moveCost;
        List<int> trimmedPath = manager.actorAI.FindPathToTarget(actor, manager.map, manager.moveManager);
        List<int> reachableTiles = manager.moveManager.GetAllReachableTiles(actor, manager.map.battlingActors);

        UnityMoveProbe probe = new UnityMoveProbe();
        probe.scenarioName = scenario == null ? "" : scenario.ScenarioName();
        probe.pairIndex = pairIndex;
        probe.runIndex = runIndex;
        probe.seed = seed;
        probe.randomStateBeforeTargetSelection = randomStateBeforeTargetSelection;
        probe.actorName = actor.GetPersonalName();
        probe.actorLocation = actorLocation;
        probe.actorMoveType = actor.GetMoveType();
        probe.actorMoveRange = actor.GetMoveRange();
        probe.actorAttackRange = actor.GetAttackRange();
        probe.actorWeaponReach = actor.GetWeaponReach();
        probe.targetName = target == null ? "" : target.GetPersonalName();
        probe.targetLocation = targetLocation;
        probe.approachTile = approachTile;
        probe.pathCostToApproach = approachTile >= 0 && approachTile < manager.moveManager.pathCosts.Count ? manager.moveManager.pathCosts[approachTile] : -1;
        probe.fullPathCost = fullPathCost;
        probe.currentMoveCosts = new List<int>(manager.moveManager.currentMoveCosts);
        probe.pathCosts = new List<int>(manager.moveManager.pathCosts);
        probe.fullPath = new List<int>(fullPath);
        probe.trimmedPath = new List<int>(trimmedPath);
        probe.reachableTiles = new List<int>(reachableTiles);
        probe.fullPathTiles = BuildMoveProbeTiles(manager.map, manager.moveManager, actorLocation, fullPath);
        probe.trimmedPathTiles = BuildMoveProbeTiles(manager.map, manager.moveManager, actorLocation, trimmedPath);
        probe.attackableTilesFromStart = manager.map.GetAttackableTiles(actor);
        if (target != null)
        {
            probe.targetAttackableTiles = manager.map.GetAttackableTiles(target);
        }
        probe.enemyChoices = BuildEnemyChoiceProbes(manager, actor);
        return probe;
    }

    static UnityActorStateDump BuildActorStateDump(BattleManager manager, TacticActor actor, BattleTestScenario scenario, int pairIndex, int runIndex, int seed, int turnsAdvanced)
    {
        UnityActorStateDump dump = new UnityActorStateDump();
        dump.scenarioName = scenario == null ? "" : scenario.ScenarioName();
        dump.pairIndex = pairIndex;
        dump.runIndex = runIndex;
        dump.seed = seed;
        dump.turnsAdvanced = turnsAdvanced;
        dump.round = manager.GetRoundNumber();
        dump.turnIndex = manager.GetTurnIndex();
        dump.actorName = actor.GetPersonalName();
        dump.location = actor.GetLocation();
        dump.actions = actor.GetActions();
        dump.movement = actor.GetMovement();
        dump.moveType = actor.GetMoveType();
        dump.targetName = PeekTargetName(actor);
        dump.randomState = JsonUtility.ToJson(UnityEngine.Random.state);
        dump.passiveSkillsRaw = string.Join("||", actor.GetPassiveSkills());
        dump.passiveLevelsRaw = string.Join("||", actor.GetPassiveLevels());
        dump.movingPassivesRaw = string.Join("||", actor.GetMovingPassives());
        dump.currentMoveCosts = new List<int>(manager.moveManager.currentMoveCosts);
        dump.pathCosts = new List<int>(manager.moveManager.pathCosts);
        dump.attackableTiles = manager.moveManager.GetTilesInAttackRange(actor, manager.map);
        dump.reachableTiles = manager.moveManager.GetAllReachableTiles(actor, manager.map.battlingActors);
        return dump;
    }

    static TacticActor FindActorByName(BattleManager manager, string actorName)
    {
        if (manager == null || manager.map == null)
        {
            throw new InvalidOperationException("BattleManager actor lookup dependencies are missing.");
        }
        for (int i = 0; i < manager.map.battlingActors.Count; i++)
        {
            if (manager.map.battlingActors[i].GetPersonalName() == actorName)
            {
                return manager.map.battlingActors[i];
            }
        }
        throw new InvalidOperationException("Could not find actor named " + actorName + " in battlingActors.");
    }

    static void TryWriteProbeError(string outPath, Exception exception)
    {
        if (string.IsNullOrEmpty(outPath) || exception == null)
        {
            return;
        }

        try
        {
            string errorPath = outPath + ".error.txt";
            string directory = Path.GetDirectoryName(errorPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(errorPath, exception.ToString());
        }
        catch
        {
        }
    }

    static List<UnityEnemyChoiceProbe> BuildEnemyChoiceProbes(BattleManager manager, TacticActor actor)
    {
        List<UnityEnemyChoiceProbe> choices = new List<UnityEnemyChoiceProbe>();
        if (manager == null || manager.map == null || manager.moveManager == null || actor == null)
        {
            return choices;
        }

        for (int i = 0; i < manager.map.battlingActors.Count; i++)
        {
            TacticActor enemy = manager.map.battlingActors[i];
            if (enemy == null || enemy.GetTeam() == actor.GetTeam() || enemy.invisible)
            {
                continue;
            }

            manager.moveManager.GetAllMoveCosts(actor, manager.map.battlingActors);
            List<int> enemyPath = manager.moveManager.GetPrecomputedPath(actor.GetLocation(), enemy.GetLocation(), true);
            UnityEnemyChoiceProbe choice = new UnityEnemyChoiceProbe();
            choice.personalName = enemy.GetPersonalName();
            choice.location = enemy.GetLocation();
            choice.pathCost = enemyPath.Count <= 0 ? -1 : manager.moveManager.moveCost;
            choice.path = new List<int>(enemyPath);
            choices.Add(choice);
        }

        return choices;
    }

    static List<UnityMoveProbeTile> BuildMoveProbeTiles(BattleMap map, MoveCostManager moveManager, int startTile, List<int> path)
    {
        List<UnityMoveProbeTile> tiles = new List<UnityMoveProbeTile>();
        if (map == null || path == null)
        {
            return tiles;
        }

        int previous = startTile;
        for (int i = path.Count - 1; i >= 0; i--)
        {
            int tile = path[i];
            UnityMoveProbeTile entry = new UnityMoveProbeTile();
            entry.tile = tile;
            entry.terrain = tile >= 0 && tile < map.mapInfo.Count ? map.mapInfo[tile] : "";
            entry.terrainEffect = tile >= 0 && tile < map.terrainEffectTiles.Count ? map.terrainEffectTiles[tile] : "";
            entry.elevation = tile >= 0 && tile < map.mapElevations.Count ? map.mapElevations[tile] : 0;
            entry.stepCost = moveManager == null ? 0 : moveManager.MoveCostOfTile(tile);
            entry.directionFromPrevious = map.mapUtility == null ? -1 : map.mapUtility.DirectionBetweenLocations(previous, tile, map.mapSize);
            previous = tile;
            tiles.Add(entry);
        }
        return tiles;
    }

    static UnityTurnTrace BuildTurnTrace(BattleManager manager, BattleTestScenario scenario, int pairIndex, int runIndex, int seed, int maxTurns)
    {
        if (manager == null || manager.map == null || manager.combatLog == null)
        {
            throw new InvalidOperationException("BattleManager turn trace dependencies are missing.");
        }

        UnityTurnTrace trace = new UnityTurnTrace();
        trace.scenarioName = scenario == null ? "" : scenario.ScenarioName();
        trace.pairIndex = pairIndex;
        trace.runIndex = runIndex;
        trace.seed = seed;
        trace.terrain = manager.map.mapInfo != null && manager.map.mapInfo.Count > 0 ? manager.map.mapInfo[0] : "";
        UnityEngine.Random.State randomStateBeforeTraceMetadata = UnityEngine.Random.state;
        trace.weather = manager.map.GetWeather();
        trace.time = manager.map.GetTime();
        trace.startingFormation = manager.battleState == null ? "" : manager.battleState.GetAllySpawnPattern();
        UnityEngine.Random.state = randomStateBeforeTraceMetadata;
        trace.mapSize = manager.map.mapSize;
        trace.initialActors = BuildTurnActors(manager);

        MethodInfo npcTurn = typeof(BattleManager).GetMethod("NPCTurn", BindingFlags.Instance | BindingFlags.NonPublic);
        if (npcTurn == null)
        {
            throw new InvalidOperationException("BattleManager.NPCTurn could not be found.");
        }

        int previousLogCount = manager.combatLog.allLogs == null ? 0 : manager.combatLog.allLogs.Count;
        for (int step = 0; step < maxTurns; step++)
        {
            TacticActor turnActor = manager.GetTurnActor();
            if (turnActor == null)
            {
                break;
            }

            UnityTurnFrame frame = new UnityTurnFrame();
            frame.step = step;
            frame.round = manager.GetRoundNumber();
            frame.turnIndex = manager.GetTurnIndex();
            frame.turnActor = turnActor.GetPersonalName();
            frame.turnActorTeam = turnActor.GetTeam();
            frame.turnActorLocation = turnActor.GetLocation();
            frame.turnActorActions = turnActor.GetActions();
            frame.turnActorTarget = PeekTargetName(turnActor);
            frame.randomStateBeforeTurn = JsonUtility.ToJson(UnityEngine.Random.state);
            frame.beforeActors = BuildTurnActors(manager);

            npcTurn.Invoke(manager, null);

            frame.randomStateAfterTurn = JsonUtility.ToJson(UnityEngine.Random.state);
            int currentLogCount = manager.combatLog.allLogs == null ? 0 : manager.combatLog.allLogs.Count;
            frame.combatLogDelta = SliceCombatLogs(manager.combatLog, previousLogCount, currentLogCount);
            previousLogCount = currentLogCount;
            frame.afterActors = BuildTurnActors(manager);
            frame.nextRound = manager.GetRoundNumber();
            frame.nextTurnIndex = manager.GetTurnIndex();
            frame.nextTurnActor = manager.GetTurnActor() == null ? "" : manager.GetTurnActor().GetPersonalName();
            trace.turns.Add(frame);

            if (manager.FindWinningTeam() >= 0)
            {
                break;
            }
        }

        trace.finalWinningTeam = manager.FindWinningTeam();
        trace.finalRound = manager.GetRoundNumber();
        trace.finalTurnIndex = manager.GetTurnIndex();
        trace.finalActors = BuildTurnActors(manager);
        return trace;
    }

    static void AdvanceNpcTurns(BattleManager manager, int turnsToAdvance)
    {
        if (manager == null || turnsToAdvance <= 0)
        {
            return;
        }

        MethodInfo npcTurn = typeof(BattleManager).GetMethod("NPCTurn", BindingFlags.Instance | BindingFlags.NonPublic);
        if (npcTurn == null)
        {
            throw new InvalidOperationException("BattleManager.NPCTurn could not be found.");
        }

        for (int i = 0; i < turnsToAdvance; i++)
        {
            if (manager.GetTurnActor() == null || manager.FindWinningTeam() >= 0)
            {
                break;
            }
            npcTurn.Invoke(manager, null);
        }
    }

    static int AdvanceNpcTurnsUntil(BattleManager manager, string actorName, int targetRound, int targetTurnIndex, int maxTurnsToAdvance, List<UnityAdvanceSnapshot> snapshots = null)
    {
        if (manager == null)
        {
            throw new InvalidOperationException("BattleManager is required to advance turns.");
        }

        MethodInfo npcTurn = typeof(BattleManager).GetMethod("NPCTurn", BindingFlags.Instance | BindingFlags.NonPublic);
        if (npcTurn == null)
        {
            throw new InvalidOperationException("BattleManager.NPCTurn could not be found.");
        }

        int advanced = 0;
        while (advanced <= maxTurnsToAdvance)
        {
            TacticActor currentActor = manager.GetTurnActor();
            if (currentActor == null || manager.FindWinningTeam() >= 0)
            {
                break;
            }
            if (manager.GetRoundNumber() == targetRound && manager.GetTurnIndex() == targetTurnIndex && currentActor.GetPersonalName() == actorName)
            {
                return advanced;
            }

            UnityAdvanceSnapshot snapshot = null;
            if (snapshots != null)
            {
                snapshot = new UnityAdvanceSnapshot();
                snapshot.step = advanced;
                snapshot.roundBefore = manager.GetRoundNumber();
                snapshot.turnIndexBefore = manager.GetTurnIndex();
                snapshot.actorBefore = currentActor.GetPersonalName();
                snapshot.actorLocationBefore = currentActor.GetLocation();
                snapshot.randomStateBefore = JsonUtility.ToJson(UnityEngine.Random.state);
                snapshot.actorsBefore = BuildCompactActorStates(manager);
            }

            npcTurn.Invoke(manager, null);

            if (snapshot != null)
            {
                TacticActor nextActor = manager.GetTurnActor();
                snapshot.roundAfter = manager.GetRoundNumber();
                snapshot.turnIndexAfter = manager.GetTurnIndex();
                snapshot.actorAfter = nextActor == null ? "" : nextActor.GetPersonalName();
                snapshot.actorLocationAfter = nextActor == null ? -1 : nextActor.GetLocation();
                snapshot.randomStateAfter = JsonUtility.ToJson(UnityEngine.Random.state);
                snapshot.actorsAfter = BuildCompactActorStates(manager);
                snapshots.Add(snapshot);
            }
            advanced++;
        }

        TacticActor finalActor = manager.GetTurnActor();
        string finalActorName = finalActor == null ? "" : finalActor.GetPersonalName();
        throw new InvalidOperationException("Could not reach anchored turn for `" + actorName + "` at round " + targetRound + " turn " + targetTurnIndex + ". Stopped at round " + manager.GetRoundNumber() + " turn " + manager.GetTurnIndex() + " actor `" + finalActorName + "` after " + advanced + " turn advances.");
    }

    static List<string> BuildCompactActorStates(BattleManager manager)
    {
        List<string> states = new List<string>();
        if (manager == null || manager.map == null || manager.map.battlingActors == null)
        {
            return states;
        }

        for (int i = 0; i < manager.map.battlingActors.Count; i++)
        {
            TacticActor actor = manager.map.battlingActors[i];
            if (actor == null)
            {
                continue;
            }
            states.Add(actor.GetPersonalName() + "@T" + actor.GetTeam() + ":" + actor.GetLocation() + ":hp" + actor.GetHealth() + ":a" + actor.GetActions() + ":target=" + PeekTargetName(actor));
        }
        return states;
    }

    static UnitySkillTargetProbe BuildSkillTargetProbe(BattleManager manager, string actorName, string skillName, int pairIndex, BattleTestScenario scenario, int runIndex, int seed, int turnsAdvanced)
    {
        if (manager == null || manager.map == null || manager.moveManager == null || manager.actorAI == null || manager.activeManager == null)
        {
            throw new InvalidOperationException("BattleManager skill target probe dependencies are missing.");
        }

        TacticActor actor = null;
        for (int i = 0; i < manager.map.battlingActors.Count; i++)
        {
            if (manager.map.battlingActors[i].GetPersonalName() == actorName)
            {
                actor = manager.map.battlingActors[i];
                break;
            }
        }
        if (actor == null)
        {
            throw new InvalidOperationException("Could not find actor named " + actorName + " in battlingActors.");
        }

        string stateBeforeTargetSelection = JsonUtility.ToJson(UnityEngine.Random.state);
        manager.moveManager.GetAllMoveCosts(actor, manager.map.battlingActors);
        TacticActor chosenTarget = manager.actorAI.GetClosestEnemy(manager.map.battlingActors, actor, manager.moveManager);
        actor.SetTarget(chosenTarget);

        manager.activeManager.SetSkillFromName(skillName, actor);
        manager.actorAI.active.LoadSkillFromString(manager.activeManager.activeData.ReturnValue(skillName), actor);
        List<int> targetableTiles = manager.moveManager.actorPathfinder.FindTilesInRange(actor.GetLocation(), manager.actorAI.active.GetRange(actor));
        List<int> emptyTargetableTiles = manager.map.ReturnEmptyTiles(new List<int>(targetableTiles));
        string stateBeforeTileChoice = JsonUtility.ToJson(UnityEngine.Random.state);
        int chosenTile = manager.actorAI.ChooseSkillTargetLocation(actor, manager.map, manager.moveManager);
        string stateAfterTileChoice = JsonUtility.ToJson(UnityEngine.Random.state);

        UnitySkillTargetProbe probe = new UnitySkillTargetProbe();
        probe.scenarioName = scenario == null ? "" : scenario.ScenarioName();
        probe.pairIndex = pairIndex;
        probe.runIndex = runIndex;
        probe.seed = seed;
        probe.turnsAdvanced = turnsAdvanced;
        probe.round = manager.GetRoundNumber();
        probe.turnIndex = manager.GetTurnIndex();
        probe.actorName = actor.GetPersonalName();
        probe.actorLocation = actor.GetLocation();
        probe.skillName = skillName;
        probe.currentTarget = PeekTargetName(actor);
        probe.targetableTiles = new List<int>(targetableTiles);
        probe.emptyTargetableTiles = new List<int>(emptyTargetableTiles);
        probe.chosenTile = chosenTile;
        probe.randomStateBeforeTargetSelection = stateBeforeTargetSelection;
        probe.randomStateBeforeTileChoice = stateBeforeTileChoice;
        probe.randomStateAfterTileChoice = stateAfterTileChoice;
        probe.actors = BuildTurnActors(manager);
        return probe;
    }

    static UnityPostSkillEnemyChoiceProbe BuildPostSkillEnemyChoiceProbe(BattleManager manager, string actorName, string skillName, int pairIndex, BattleTestScenario scenario, int runIndex, int seed, int turnsAdvanced)
    {
        if (manager == null || manager.map == null || manager.moveManager == null || manager.actorAI == null || manager.activeManager == null)
        {
            throw new InvalidOperationException("BattleManager post-skill enemy choice probe dependencies are missing.");
        }

        TacticActor actor = FindActorByName(manager, actorName);
        string stateBeforeSkillTargetSelection = JsonUtility.ToJson(UnityEngine.Random.state);
        manager.moveManager.GetAllMoveCosts(actor, manager.map.battlingActors);
        TacticActor chosenTarget = manager.actorAI.GetClosestEnemy(manager.map.battlingActors, actor, manager.moveManager);
        actor.SetTarget(chosenTarget);

        manager.activeManager.SetSkillFromName(skillName, actor);
        manager.actorAI.active.LoadSkillFromString(manager.activeManager.activeData.ReturnValue(skillName), actor);
        int chosenTile = manager.actorAI.ChooseSkillTargetLocation(actor, manager.map, manager.moveManager);
        string stateAfterSkillTargetSelection = JsonUtility.ToJson(UnityEngine.Random.state);

        manager.moveManager.GetAllMoveCosts(actor, manager.map.battlingActors);
        TacticActor postSkillTarget = manager.actorAI.GetClosestEnemy(manager.map.battlingActors, actor, manager.moveManager);

        UnityPostSkillEnemyChoiceProbe probe = new UnityPostSkillEnemyChoiceProbe();
        probe.scenarioName = scenario == null ? "" : scenario.ScenarioName();
        probe.pairIndex = pairIndex;
        probe.runIndex = runIndex;
        probe.seed = seed;
        probe.turnsAdvanced = turnsAdvanced;
        probe.actorName = actor.GetPersonalName();
        probe.skillName = skillName;
        probe.chosenTile = chosenTile;
        probe.stateBeforeSkillTargetSelection = stateBeforeSkillTargetSelection;
        probe.stateAfterSkillTargetSelection = stateAfterSkillTargetSelection;
        probe.targetName = postSkillTarget == null ? "" : postSkillTarget.GetPersonalName();
        probe.enemyChoices = BuildEnemyChoiceProbes(manager, actor);
        probe.actors = BuildTurnActors(manager);
        return probe;
    }

    static UnityBossFallbackProbe BuildBossFallbackProbe(BattleManager manager, string actorName, string skillName, int pairIndex, BattleTestScenario scenario, int runIndex, int seed, int turnsAdvanced)
    {
        if (manager == null || manager.map == null)
        {
            throw new InvalidOperationException("BattleManager boss fallback probe dependencies are missing.");
        }

        TacticActor actor = FindActorByName(manager, actorName);
        MethodInfo tryNpcSkillOnce = typeof(BattleManager).GetMethod("TryNPCSkillOnce", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo basicNpcAction = typeof(BattleManager).GetMethod("BasicNPCAction", BindingFlags.Instance | BindingFlags.NonPublic);
        if (tryNpcSkillOnce == null || basicNpcAction == null)
        {
            throw new InvalidOperationException("Could not find BattleManager boss fallback methods.");
        }

        UnityBossFallbackProbe probe = new UnityBossFallbackProbe();
        probe.scenarioName = scenario == null ? "" : scenario.ScenarioName();
        probe.pairIndex = pairIndex;
        probe.runIndex = runIndex;
        probe.seed = seed;
        probe.turnsAdvanced = turnsAdvanced;
        probe.actorName = actor.GetPersonalName();
        probe.initialRandomState = JsonUtility.ToJson(UnityEngine.Random.state);
        probe.initialTarget = PeekTargetName(actor);
        probe.initialActors = BuildTurnActors(manager);

        bool skillResult = (bool)tryNpcSkillOnce.Invoke(manager, new object[] { skillName });
        probe.skillResult = skillResult;
        probe.afterSkillRandomState = JsonUtility.ToJson(UnityEngine.Random.state);
        probe.afterSkillTarget = PeekTargetName(actor);
        probe.afterSkillActors = BuildTurnActors(manager);

        basicNpcAction.Invoke(manager, null);
        probe.afterBasicRandomState = JsonUtility.ToJson(UnityEngine.Random.state);
        probe.afterBasicTarget = PeekTargetName(actor);
        probe.afterBasicActors = BuildTurnActors(manager);
        return probe;
    }

    static UnityNpcTurnProbe BuildNpcTurnProbe(BattleManager manager, string actorName, BattleTestScenario scenario, int pairIndex, int runIndex, int seed, int turnsAdvanced)
    {
        if (manager == null || manager.map == null || manager.moveManager == null || manager.actorAI == null)
        {
            throw new InvalidOperationException("BattleManager NPC turn probe dependencies are missing.");
        }

        TacticActor actor = null;
        for (int i = 0; i < manager.map.battlingActors.Count; i++)
        {
            if (manager.map.battlingActors[i].GetPersonalName() == actorName)
            {
                actor = manager.map.battlingActors[i];
                break;
            }
        }
        if (actor == null)
        {
            throw new InvalidOperationException("Could not find actor named " + actorName + " in battlingActors.");
        }

        UnityNpcTurnProbe probe = new UnityNpcTurnProbe();
        probe.scenarioName = scenario == null ? "" : scenario.ScenarioName();
        probe.pairIndex = pairIndex;
        probe.runIndex = runIndex;
        probe.seed = seed;
        probe.turnsAdvanced = turnsAdvanced;
        probe.actorName = actor.GetPersonalName();
        probe.round = manager.GetRoundNumber();
        probe.turnIndex = manager.GetTurnIndex();
        probe.initialRandomState = JsonUtility.ToJson(UnityEngine.Random.state);
        probe.initialActors = BuildTurnActors(manager);
        probe.states.Add(new UnityNpcTurnProbeState("initial_passive_snapshot", JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName())
        {
            passiveId = "skills=" + string.Join("||", actor.GetPassiveSkills()) + " :: levels=" + string.Join("||", actor.GetPassiveLevels()) + " :: moving=" + string.Join("||", actor.GetMovingPassives())
        });

        manager.moveManager.GetAllMoveCosts(actor, manager.map.battlingActors);
        probe.states.Add(new UnityNpcTurnProbeState("after_get_all_move_costs", JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()));

        TacticActor chosenTarget = actor.GetTarget();
        if (chosenTarget == null || chosenTarget.GetHealth() <= 0 || chosenTarget.invisible || chosenTarget.GetTeam() == actor.GetTeam())
        {
            chosenTarget = manager.actorAI.GetClosestEnemy(manager.map.battlingActors, actor, manager.moveManager);
            actor.SetTarget(chosenTarget);
        }
        probe.states.Add(new UnityNpcTurnProbeState("after_choose_target", JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()));

        bool inAttackRange = manager.actorAI.EnemyInAttackRange(actor, actor.GetTarget(), manager.map);
        probe.states.Add(new UnityNpcTurnProbeState("after_check_attack_range", JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { inAttackRange = inAttackRange });

        bool inAttackableRange = manager.actorAI.EnemyInAttackableRange(actor, actor.GetTarget(), manager.map, manager.moveManager);
        probe.states.Add(new UnityNpcTurnProbeState("after_check_attackable_range", JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { inAttackableRange = inAttackableRange });

        List<int> path = manager.actorAI.FindPathToTarget(actor, manager.map, manager.moveManager);
        probe.path = new List<int>(path);
        UnityNpcTurnProbeState pathState = new UnityNpcTurnProbeState("after_find_path_to_target", JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName());
        if (path.Count > 0)
        {
            pathState.stepTile = path[path.Count - 1];
            pathState.passiveId = "moveCost=" + manager.moveManager.MoveCostOfTile(path[path.Count - 1]);
        }
        probe.states.Add(pathState);

        for (int i = path.Count - 1; i >= 0; i--)
        {
            int prevLoc = actor.GetLocation();
            actor.SetDirection(manager.moveManager.DirectionBetweenLocations(prevLoc, path[i]));
            probe.states.Add(new UnityNpcTurnProbeState("before_move_step_" + (path.Count - i), JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { stepTile = path[i] });
            actor.SetLocation(path[i]);
            probe.states.Add(new UnityNpcTurnProbeState("after_set_location_" + (path.Count - i), JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { stepTile = path[i] });
            manager.map.combatLog.UpdateNewestLog(actor.GetPersonalName() + " moves to " + manager.map.mapUtility.GetRowColumnCoordinateString(path[i], manager.map.mapSize));
            probe.states.Add(new UnityNpcTurnProbeState("after_moving_log_" + (path.Count - i), JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { stepTile = path[i] });
            actor.UpdateRoundMoveTracker();
            probe.states.Add(new UnityNpcTurnProbeState("after_update_round_move_tracker_" + (path.Count - i), JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { stepTile = path[i] });
            manager.map.combatLog.AddDetailedLogs("Move Cost: " + manager.moveManager.MoveCostOfTile(path[i]));
            probe.states.Add(new UnityNpcTurnProbeState("after_move_cost_log_" + (path.Count - i), JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { stepTile = path[i] });

            InvokeBattleMapProtected(manager.map, "ApplyTileMovingEffect", actor, path[i]);
            probe.states.Add(new UnityNpcTurnProbeState("after_apply_tile_moving_effect_" + (path.Count - i), JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { stepTile = path[i] });
            InvokeBattleMapProtected(manager.map, "ApplyTerrainMovingEffect", actor, path[i]);
            probe.states.Add(new UnityNpcTurnProbeState("after_apply_terrain_moving_effect_" + (path.Count - i), JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { stepTile = path[i] });
            InvokeBattleMapProtected(manager.map, "ApplyBuildingMovingEffect", actor, path[i]);
            probe.states.Add(new UnityNpcTurnProbeState("after_apply_building_moving_effect_" + (path.Count - i), JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { stepTile = path[i] });
            InvokeBattleMapProtected(manager.map, "ApplyInteractableEffect", actor, path[i]);
            probe.states.Add(new UnityNpcTurnProbeState("after_apply_interactable_effect_" + (path.Count - i), JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { stepTile = path[i] });
            manager.map.ApplyAuraEffects(actor);
            probe.states.Add(new UnityNpcTurnProbeState("after_apply_aura_effects_" + (path.Count - i), JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { stepTile = path[i] });
            List<string> movingPassives = actor.GetMovingPassives();
            for (int j = 0; j < movingPassives.Count; j++)
            {
                List<string> passiveInfo = movingPassives[j].Split("|").ToList();
                if (passiveInfo.Count < 6) { continue; }
                bool passed = manager.moveManager.passiveSkill.CheckMovingCondition(actor, passiveInfo[1], passiveInfo[2], path[i], manager.map);
                probe.states.Add(new UnityNpcTurnProbeState("after_check_moving_passive_" + (path.Count - i) + "_" + j, JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { stepTile = path[i], passiveId = passiveInfo[0], passivePassed = passed });
                if (!passed) { continue; }
                switch (passiveInfo[3])
                {
                    case "Self":
                        manager.moveManager.passiveSkill.AffectActor(actor, passiveInfo[4], passiveInfo[5]);
                        break;
                    case "Map":
                        manager.moveManager.passiveSkill.AffectMap(actor, passiveInfo[4], passiveInfo[5], manager.map);
                        break;
                }
                probe.states.Add(new UnityNpcTurnProbeState("after_apply_moving_passive_" + (path.Count - i) + "_" + j, JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { stepTile = path[i], passiveId = passiveInfo[0], passivePassed = passed });
            }
            probe.states.Add(new UnityNpcTurnProbeState("after_move_actor_to_tile_" + (path.Count - i), JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { stepTile = path[i] });
            actor.PayMoveCost(manager.moveManager.MoveCostOfTile(path[i]));
            probe.states.Add(new UnityNpcTurnProbeState("after_pay_move_cost_" + (path.Count - i), JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()) { stepTile = path[i] });
            if (actor.GetMovement() < 0)
            {
                break;
            }
        }

        manager.map.UpdateActors();
        probe.states.Add(new UnityNpcTurnProbeState("after_update_actors", JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()));
        manager.ResetState();
        probe.states.Add(new UnityNpcTurnProbeState("after_reset_state", JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName()));
        InvokeBattleManagerProtected(manager, "EndTurn");
        probe.states.Add(new UnityNpcTurnProbeState("after_end_turn", JsonUtility.ToJson(UnityEngine.Random.state), actor.GetLocation(), manager.GetTurnActor() == null ? "" : manager.GetTurnActor().GetPersonalName()));

        probe.finalActors = BuildTurnActors(manager);
        probe.finalRandomState = JsonUtility.ToJson(UnityEngine.Random.state);
        return probe;
    }

    static UnityActualTurnProbe BuildActualTurnProbe(BattleManager manager, string actorName, BattleTestScenario scenario, int pairIndex, int runIndex, int seed, int turnsAdvanced)
    {
        if (manager == null || manager.map == null || manager.combatLog == null)
        {
            throw new InvalidOperationException("BattleManager actual turn probe dependencies are missing.");
        }

        TacticActor currentActor = manager.GetTurnActor();
        if (currentActor == null)
        {
            throw new InvalidOperationException("BattleManager has no current turn actor.");
        }
        if (currentActor.GetPersonalName() != actorName)
        {
            throw new InvalidOperationException("Expected turn actor `" + actorName + "` but current turn actor is `" + currentActor.GetPersonalName() + "`.");
        }

        UnityActualTurnProbe probe = new UnityActualTurnProbe();
        probe.scenarioName = scenario == null ? "" : scenario.ScenarioName();
        probe.pairIndex = pairIndex;
        probe.runIndex = runIndex;
        probe.seed = seed;
        probe.turnsAdvanced = turnsAdvanced;
        probe.round = manager.GetRoundNumber();
        probe.turnIndex = manager.GetTurnIndex();
        probe.turnActor = currentActor.GetPersonalName();
        probe.turnActorTeam = currentActor.GetTeam();
        probe.turnActorLocation = currentActor.GetLocation();
        probe.turnActorTarget = PeekTargetName(currentActor);
        probe.randomStateBeforeTurn = JsonUtility.ToJson(UnityEngine.Random.state);
        probe.beforeActors = BuildTurnActors(manager);

        int previousLogCount = manager.combatLog.allLogs == null ? 0 : manager.combatLog.allLogs.Count;
        bool previousAutoBattle = manager.autoBattle;
        bool previousControlAI = manager.controlAI;
        manager.SetAutoBattle(false);
        manager.SetControlAI(true);
        try
        {
            InvokeBattleManagerProtected(manager, "NPCTurn");
        }
        finally
        {
            manager.SetAutoBattle(previousAutoBattle);
            manager.SetControlAI(previousControlAI);
        }

        probe.randomStateAfterTurn = JsonUtility.ToJson(UnityEngine.Random.state);
        int currentLogCount = manager.combatLog.allLogs == null ? 0 : manager.combatLog.allLogs.Count;
        probe.combatLogDelta = SliceCombatLogs(manager.combatLog, previousLogCount, currentLogCount);
        probe.afterActors = BuildTurnActors(manager);
        probe.nextRound = manager.GetRoundNumber();
        probe.nextTurnIndex = manager.GetTurnIndex();
        probe.nextTurnActor = manager.GetTurnActor() == null ? "" : manager.GetTurnActor().GetPersonalName();
        return probe;
    }

    static UnityAttackSkillProbe BuildAttackSkillProbe(BattleManager manager, string actorName, BattleTestScenario scenario, int pairIndex, int runIndex, int seed, int turnsAdvanced)
    {
        if (manager == null || manager.map == null || manager.moveManager == null || manager.actorAI == null)
        {
            throw new InvalidOperationException("BattleManager attack-skill probe dependencies are missing.");
        }

        TacticActor actor = manager.GetTurnActor();
        if (actor == null)
        {
            throw new InvalidOperationException("BattleManager has no current turn actor.");
        }
        if (actor.GetPersonalName() != actorName)
        {
            throw new InvalidOperationException("Expected turn actor `" + actorName + "` but current turn actor is `" + actor.GetPersonalName() + "`.");
        }

        if (actor.GetTarget() == null || actor.GetTarget().GetHealth() <= 0 || actor.GetTarget().invisible || actor.GetTarget().GetTeam() == actor.GetTeam())
        {
            manager.moveManager.GetAllMoveCosts(actor, manager.map.battlingActors);
            actor.SetTarget(manager.actorAI.GetClosestEnemy(manager.map.battlingActors, actor, manager.moveManager));
        }

        string rawAttackSkill = manager.actorAI.actorAttackSkills.ReturnValue(actor.GetSpriteName());
        TacticActor target = actor.GetTarget();
        bool skillWouldHealTarget = manager.actorAI.SkillWouldHealTarget(actor, target, rawAttackSkill);
        string returnedAttackSkill = manager.actorAI.ReturnAIAttackSkill(actor);

        UnityAttackSkillProbe probe = new UnityAttackSkillProbe();
        probe.scenarioName = scenario == null ? "" : scenario.ScenarioName();
        probe.pairIndex = pairIndex;
        probe.runIndex = runIndex;
        probe.seed = seed;
        probe.turnsAdvanced = turnsAdvanced;
        probe.round = manager.GetRoundNumber();
        probe.turnIndex = manager.GetTurnIndex();
        probe.actorName = actor.GetPersonalName();
        probe.targetName = target == null ? "" : target.GetPersonalName();
        probe.targetWaterResistance = target == null ? 0 : target.ReturnDamageResistanceOfType("Water");
        probe.rawAttackSkill = rawAttackSkill;
        probe.skillWouldHealTarget = skillWouldHealTarget;
        probe.returnedAttackSkill = returnedAttackSkill;
        probe.randomState = JsonUtility.ToJson(UnityEngine.Random.state);
        probe.actors = BuildTurnActors(manager);
        return probe;
    }

    static UnitySpellAttemptProbe BuildSpellAttemptProbe(BattleManager manager, string actorName, string spellName, BattleTestScenario scenario, int pairIndex, int runIndex, int seed, int turnsAdvanced)
    {
        if (manager == null || manager.map == null || manager.moveManager == null || manager.actorAI == null || manager.activeManager == null)
        {
            throw new InvalidOperationException("BattleManager spell-attempt probe dependencies are missing.");
        }

        TacticActor actor = manager.GetTurnActor();
        if (actor == null)
        {
            throw new InvalidOperationException("BattleManager has no current turn actor.");
        }
        if (actor.GetPersonalName() != actorName)
        {
            throw new InvalidOperationException("Expected turn actor `" + actorName + "` but current turn actor is `" + actor.GetPersonalName() + "`.");
        }

        manager.activeManager.SetSkillUser(actor);
        manager.activeManager.SetSpell(spellName);

        UnitySpellAttemptProbe probe = new UnitySpellAttemptProbe();
        probe.scenarioName = scenario == null ? "" : scenario.ScenarioName();
        probe.pairIndex = pairIndex;
        probe.runIndex = runIndex;
        probe.seed = seed;
        probe.turnsAdvanced = turnsAdvanced;
        probe.round = manager.GetRoundNumber();
        probe.turnIndex = manager.GetTurnIndex();
        probe.actorName = actor.GetPersonalName();
        probe.spellName = spellName;
        probe.initialRandomState = JsonUtility.ToJson(UnityEngine.Random.state);
        probe.beforeActors = BuildTurnActors(manager);
        probe.spellType = manager.activeManager.magicSpell == null ? "" : manager.activeManager.magicSpell.GetSkillType();
        probe.spellEffect = manager.activeManager.magicSpell == null ? "" : manager.activeManager.magicSpell.GetEffect();
        probe.spellRange = manager.activeManager.magicSpell == null ? -1 : manager.activeManager.magicSpell.GetRange(actor, manager.map);

        probe.chosenTile = manager.actorAI.ChooseSpellTargetLocation(actor, manager.map, manager.moveManager, manager.activeManager.magicSpell);
        probe.randomStateAfterTargetChoice = JsonUtility.ToJson(UnityEngine.Random.state);
        probe.costOk = manager.activeManager.CheckSpellCost(manager.map);
        if (probe.chosenTile >= 0)
        {
            manager.activeManager.GetTargetedTiles(probe.chosenTile, manager.moveManager.actorPathfinder, true);
            probe.targetedTiles = new List<int>(manager.activeManager.targetedTiles);
            probe.validTargets = manager.actorAI.ValidSkillTargets(actor, manager.map, manager.activeManager, true);
        }
        else
        {
            probe.validTargets = false;
        }
        probe.afterActors = BuildTurnActors(manager);
        return probe;
    }

    static UnitySkillAttemptProbe BuildSkillAttemptProbe(BattleManager manager, string actorName, string skillName, BattleTestScenario scenario, int pairIndex, int runIndex, int seed, int turnsAdvanced)
    {
        if (manager == null || manager.map == null || manager.moveManager == null || manager.actorAI == null || manager.activeManager == null)
        {
            throw new InvalidOperationException("BattleManager skill-attempt probe dependencies are missing.");
        }

        TacticActor actor = manager.GetTurnActor();
        if (actor == null)
        {
            throw new InvalidOperationException("BattleManager has no current turn actor.");
        }
        if (actor.GetPersonalName() != actorName)
        {
            throw new InvalidOperationException("Expected turn actor `" + actorName + "` but current turn actor is `" + actor.GetPersonalName() + "`.");
        }

        manager.activeManager.SetSkillFromName(skillName, actor);

        UnitySkillAttemptProbe probe = new UnitySkillAttemptProbe();
        probe.scenarioName = scenario == null ? "" : scenario.ScenarioName();
        probe.pairIndex = pairIndex;
        probe.runIndex = runIndex;
        probe.seed = seed;
        probe.turnsAdvanced = turnsAdvanced;
        probe.round = manager.GetRoundNumber();
        probe.turnIndex = manager.GetTurnIndex();
        probe.actorName = actor.GetPersonalName();
        probe.skillName = skillName;
        probe.initialRandomState = JsonUtility.ToJson(UnityEngine.Random.state);
        probe.beforeActors = BuildTurnActors(manager);
        probe.skillType = manager.activeManager.active == null ? "" : manager.activeManager.active.GetSkillType();
        probe.skillEffect = manager.activeManager.active == null ? "" : manager.activeManager.active.GetEffect();
        probe.skillRange = manager.activeManager.active == null ? -1 : manager.activeManager.active.GetRange(actor, manager.map);
        probe.skillActionCost = manager.activeManager.active == null ? -1 : manager.activeManager.active.GetActionCost(actor, manager.map);
        probe.skillEnergyCost = manager.activeManager.active == null ? -1 : manager.activeManager.active.GetEnergyCost(actor, manager.map);

        probe.chosenTile = manager.actorAI.ChooseSkillTargetLocation(actor, manager.map, manager.moveManager);
        probe.randomStateAfterTargetChoice = JsonUtility.ToJson(UnityEngine.Random.state);
        probe.costOk = manager.activeManager.CheckSkillCost(manager.map);
        if (probe.chosenTile >= 0)
        {
            manager.activeManager.GetTargetedTiles(probe.chosenTile, manager.moveManager.actorPathfinder);
            probe.targetedTiles = new List<int>(manager.activeManager.targetedTiles);
            probe.validTargets = manager.actorAI.ValidSkillTargets(actor, manager.map, manager.activeManager);
        }
        else
        {
            probe.validTargets = false;
        }
        probe.afterActors = BuildTurnActors(manager);
        return probe;
    }

    static UnityTurnDecisionProbe BuildTurnDecisionProbe(BattleManager manager, string actorName, BattleTestScenario scenario, int pairIndex, int runIndex, int seed, int turnsAdvanced)
    {
        if (manager == null || manager.map == null || manager.moveManager == null || manager.actorAI == null)
        {
            throw new InvalidOperationException("BattleManager turn-decision probe dependencies are missing.");
        }

        TacticActor actor = manager.GetTurnActor();
        if (actor == null)
        {
            throw new InvalidOperationException("BattleManager has no current turn actor.");
        }
        if (actor.GetPersonalName() != actorName)
        {
            throw new InvalidOperationException("Expected turn actor `" + actorName + "` but current turn actor is `" + actor.GetPersonalName() + "`.");
        }

        UnityTurnDecisionProbe probe = new UnityTurnDecisionProbe();
        probe.scenarioName = scenario == null ? "" : scenario.ScenarioName();
        probe.pairIndex = pairIndex;
        probe.runIndex = runIndex;
        probe.seed = seed;
        probe.turnsAdvanced = turnsAdvanced;
        probe.round = manager.GetRoundNumber();
        probe.turnIndex = manager.GetTurnIndex();
        probe.actorName = actor.GetPersonalName();
        probe.spriteName = actor.GetSpriteName();
        probe.team = actor.GetTeam();
        probe.location = actor.GetLocation();
        probe.actions = actor.GetActions();
        probe.initialRandomState = JsonUtility.ToJson(UnityEngine.Random.state);
        probe.beforeActors = BuildTurnActors(manager);
        probe.activeSkills = actor.GetActiveSkills();
        probe.spells = actor.GetSpells();
        probe.bossTurn = manager.actorAI.BossTurn(actor);
        probe.actorSkillRotation = manager.actorAI.actorSkillRotation.ReturnValue(actor.GetSpriteName());
        probe.actorAttackSkill = manager.actorAI.actorAttackSkills.ReturnValue(actor.GetSpriteName());

        List<string> bossActions = manager.actorAI.ReturnBossActions(actor, manager.map);
        probe.bossAction = bossActions.Count > 0 ? bossActions[0] : "";
        probe.bossSpecifics = bossActions.Count > 1 ? bossActions[1] : "";
        probe.randomStateAfterBossDecision = JsonUtility.ToJson(UnityEngine.Random.state);

        if (!probe.bossTurn)
        {
            manager.moveManager.GetAllMoveCosts(actor, manager.map.battlingActors);
            probe.normalTurnReturnedTrue = manager.actorAI.NormalTurn(actor, manager.GetRoundNumber(), manager.map, manager.moveManager);
            probe.activeSkillNameAfterNormalTurn = manager.actorAI.ReturnAIActiveSkill();
            probe.randomStateAfterNormalDecision = JsonUtility.ToJson(UnityEngine.Random.state);
        }
        else
        {
            probe.randomStateAfterNormalDecision = probe.randomStateAfterBossDecision;
        }
        probe.afterActors = BuildTurnActors(manager);
        return probe;
    }

    static UnityActualTurnProbe BuildChainedActualTurnProbe(BattleManager manager, string actorName, BattleTestScenario scenario, int pairIndex, int runIndex, int seed, int turnsAdvanced)
    {
        if (manager == null || manager.map == null || manager.combatLog == null)
        {
            throw new InvalidOperationException("BattleManager chained actual turn probe dependencies are missing.");
        }

        TacticActor currentActor = manager.GetTurnActor();
        if (currentActor == null)
        {
            throw new InvalidOperationException("BattleManager has no current turn actor.");
        }

        UnityActualTurnProbe probe = new UnityActualTurnProbe();
        probe.scenarioName = scenario == null ? "" : scenario.ScenarioName();
        probe.pairIndex = pairIndex;
        probe.runIndex = runIndex;
        probe.seed = seed;
        probe.turnsAdvanced = turnsAdvanced;
        probe.round = manager.GetRoundNumber();
        probe.turnIndex = manager.GetTurnIndex();
        probe.turnActor = currentActor.GetPersonalName();
        probe.turnActorTeam = currentActor.GetTeam();
        probe.turnActorLocation = currentActor.GetLocation();
        probe.turnActorTarget = PeekTargetName(currentActor);
        probe.randomStateBeforeTurn = JsonUtility.ToJson(UnityEngine.Random.state);
        probe.beforeActors = BuildTurnActors(manager);

        int previousLogCount = manager.combatLog.allLogs == null ? 0 : manager.combatLog.allLogs.Count;
        bool previousAutoBattle = manager.autoBattle;
        bool previousControlAI = manager.controlAI;
        manager.SetAutoBattle(false);
        manager.SetControlAI(true);
        try
        {
            InvokeBattleManagerProtected(manager, "NPCTurn");
        }
        finally
        {
            manager.SetAutoBattle(previousAutoBattle);
            manager.SetControlAI(previousControlAI);
        }
        probe.randomStateAfterTurn = JsonUtility.ToJson(UnityEngine.Random.state);
        int currentLogCount = manager.combatLog.allLogs == null ? 0 : manager.combatLog.allLogs.Count;
        probe.combatLogDelta = SliceCombatLogs(manager.combatLog, previousLogCount, currentLogCount);
        probe.afterActors = BuildTurnActors(manager);
        probe.nextRound = manager.GetRoundNumber();
        probe.nextTurnIndex = manager.GetTurnIndex();
        probe.nextTurnActor = manager.GetTurnActor() == null ? "" : manager.GetTurnActor().GetPersonalName();
        ApplyAttackManagerSnapshot(manager.attackManager, probe);

        if (!probe.combatLogDelta.Any(x => x.Contains(actorName + "'s Turn") || x.Contains(actorName + " turn", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Chained NPC turn did not include actor `" + actorName + "`.");
        }

        return probe;
    }

    static UnityAttackStepProbe BuildAttackStepProbe(BattleManager manager, string actorName, BattleTestScenario scenario, int pairIndex, int runIndex, int seed, int turnsAdvanced)
    {
        if (manager == null || manager.map == null || manager.combatLog == null)
        {
            throw new InvalidOperationException("BattleManager attack-step probe dependencies are missing.");
        }

        TacticActor actor = manager.GetTurnActor();
        if (actor == null)
        {
            throw new InvalidOperationException("BattleManager has no current turn actor.");
        }
        if (actor.GetPersonalName() != actorName)
        {
            throw new InvalidOperationException("Expected turn actor `" + actorName + "` but current turn actor is `" + actor.GetPersonalName() + "`.");
        }

        UnityAttackStepProbe probe = new UnityAttackStepProbe();
        probe.scenarioName = scenario == null ? "" : scenario.ScenarioName();
        probe.pairIndex = pairIndex;
        probe.runIndex = runIndex;
        probe.seed = seed;
        probe.turnsAdvanced = turnsAdvanced;
        probe.round = manager.GetRoundNumber();
        probe.turnIndex = manager.GetTurnIndex();
        probe.actorName = actor.GetPersonalName();
        probe.initialRandomState = JsonUtility.ToJson(UnityEngine.Random.state);
        probe.initialActors = BuildTurnActors(manager);
        probe.initialTarget = actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName();
        probe.initialTargetLocation = actor.GetTarget() == null ? -1 : actor.GetTarget().GetLocation();
        probe.initialLocation = actor.GetLocation();
        probe.initialActions = actor.GetActions();

        manager.moveManager.GetAllMoveCosts(actor, manager.map.battlingActors);
        TacticActor target = actor.GetTarget();
        if (target == null || target.GetHealth() <= 0 || target.invisible || target.GetTeam() == actor.GetTeam())
        {
            target = manager.actorAI.GetClosestEnemy(manager.map.battlingActors, actor, manager.moveManager);
            actor.SetTarget(target);
        }

        probe.directAttackAvailable = manager.actorAI.EnemyInAttackRange(actor, actor.GetTarget(), manager.map) && actor.AttackActionsLeft();
        probe.attackableAfterMoveAvailable = manager.actorAI.EnemyInAttackableRange(actor, actor.GetTarget(), manager.map, manager.moveManager);
        probe.beforePathRandomState = JsonUtility.ToJson(UnityEngine.Random.state);
        probe.targetBeforePath = actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName();
        probe.targetBeforePathLocation = actor.GetTarget() == null ? -1 : actor.GetTarget().GetLocation();
        probe.targetHpBeforePath = actor.GetTarget() == null ? -1 : actor.GetTarget().GetHealth();

        bool pathFailed = false;
        if (probe.directAttackAvailable)
        {
            probe.branch = "DirectAttack";
        }
        else if (probe.attackableAfterMoveAvailable)
        {
            probe.branch = "MoveThenAttack";
            pathFailed = (bool)typeof(BattleManager).GetMethod("AIPathToTarget", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(manager, null);
        }
        else
        {
            probe.branch = "MoveToward";
            typeof(BattleManager).GetMethod("AIPathTowardTarget", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(manager, null);
        }
        probe.afterPathRandomState = JsonUtility.ToJson(UnityEngine.Random.state);
        probe.pathFailed = pathFailed;
        probe.locationAfterPath = actor.GetLocation();
        probe.actionsAfterPath = actor.GetActions();
        probe.targetAfterPath = actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName();
        probe.targetAfterPathLocation = actor.GetTarget() == null ? -1 : actor.GetTarget().GetLocation();
        probe.targetHpAfterPath = actor.GetTarget() == null ? -1 : actor.GetTarget().GetHealth();
        probe.actorsAfterPath = BuildTurnActors(manager);

        if (probe.branch != "MoveToward" && !pathFailed && actor.GetHealth() > 0 && actor.AttackActionsLeft() && actor.TargetValid())
        {
            probe.beforeAttackRandomState = JsonUtility.ToJson(UnityEngine.Random.state);
            probe.targetHpBeforeAttack = actor.GetTarget() == null ? -1 : actor.GetTarget().GetHealth();
            probe.targetBeforeAttackLocation = actor.GetTarget() == null ? -1 : actor.GetTarget().GetLocation();
            probe.beforeAttackMapContext = BuildAttackMapContext(manager, actor, actor.GetTarget());
            probe.beforeAttackAttackerPassives = BuildBattlePassiveRows(manager.attackManager, actor, "Attack");
            probe.beforeAttackDefenderPassives = BuildBattlePassiveRows(manager.attackManager, actor.GetTarget(), "Defend");
            probe.beforeAttackAuras = BuildBattleAuraRows(manager, actor.GetTarget(), actor);
            int previousLogCount = manager.combatLog.allLogs == null ? 0 : manager.combatLog.allLogs.Count;
            MethodInfo attackMethod = typeof(BattleManager).GetMethod("NPCAttackAction", BindingFlags.Instance | BindingFlags.NonPublic);
            if (attackMethod == null)
            {
                throw new InvalidOperationException("Could not find BattleManager protected method NPCAttackAction.");
            }
            attackMethod.Invoke(manager, new object[] { false });
            probe.afterAttackRandomState = JsonUtility.ToJson(UnityEngine.Random.state);
            probe.targetHpAfterAttack = actor.GetTarget() == null ? -1 : actor.GetTarget().GetHealth();
            probe.targetAfterAttackLocation = actor.GetTarget() == null ? -1 : actor.GetTarget().GetLocation();
            int currentLogCount = manager.combatLog.allLogs == null ? 0 : manager.combatLog.allLogs.Count;
            probe.combatLogDelta = SliceCombatLogs(manager.combatLog, previousLogCount, currentLogCount);
            probe.actorsAfterAttack = BuildTurnActors(manager);
            ApplyAttackManagerSnapshot(manager.attackManager, probe);
        }

        return probe;
    }

    static List<string> BuildAttackMapContext(BattleManager manager, TacticActor attacker, TacticActor target)
    {
        List<string> context = new List<string>();
        if (manager == null || manager.map == null || attacker == null || target == null)
        {
            return context;
        }

        context.Add("weather=" + manager.map.GetWeather());
        context.Add("time=" + manager.map.GetTime());
        context.Add("attackerTile=" + attacker.GetLocation());
        context.Add("targetTile=" + target.GetLocation());
        context.Add("attackerTerrain=" + SafeMapInfo(manager.map, attacker.GetLocation()));
        context.Add("targetTerrain=" + SafeMapInfo(manager.map, target.GetLocation()));
        context.Add("attackerTerrainEffect=" + manager.map.GetTerrainEffectOnTile(attacker.GetLocation()));
        context.Add("targetTerrainEffect=" + manager.map.GetTerrainEffectOnTile(target.GetLocation()));
        context.Add("attackerBuilding=" + manager.map.GetBuildingOnTile(attacker.GetLocation()));
        context.Add("targetBuilding=" + manager.map.GetBuildingOnTile(target.GetLocation()));
        return context;
    }

    static string SafeMapInfo(BattleMap map, int tile)
    {
        if (map == null || map.mapInfo == null || tile < 0 || tile >= map.mapInfo.Count)
        {
            return "";
        }
        return map.mapInfo[tile];
    }

    static List<string> BuildBattlePassiveRows(AttackManager attackManager, TacticActor actor, string timing)
    {
        if (attackManager == null || actor == null || attackManager.buffStatusData == null)
        {
            return new List<string>();
        }

        if (timing == "Attack")
        {
            return new List<string>(actor.GetAttackingPassives(attackManager.buffStatusData));
        }
        if (timing == "Defend")
        {
            return new List<string>(actor.GetDefendingPassives(attackManager.buffStatusData));
        }
        return new List<string>();
    }

    static List<string> BuildBattleAuraRows(BattleManager manager, TacticActor target, TacticActor attacker)
    {
        List<string> rows = new List<string>();
        if (manager == null || manager.map == null || manager.map.auras == null || target == null || attacker == null)
        {
            return rows;
        }

        for (int i = 0; i < manager.map.auras.Count; i++)
        {
            AuraEffect aura = manager.map.auras[i];
            if (aura == null)
            {
                continue;
            }
            rows.Add(aura.GetAuraName() + "|teamTarget=" + aura.teamTarget + "|trigger=" + aura.trigger + "|triggerType=" + aura.triggerType + "|battleTeamCheck=" + aura.BattleTeamCheck(target, attacker, manager.map) + "|passive=" + string.Join("|", aura.ReturnPassiveStats()));
        }
        return rows;
    }

    static void ApplyAttackManagerSnapshot(AttackManager attackManager, UnityAttackStepProbe probe)
    {
        if (attackManager == null || probe == null)
        {
            return;
        }

        probe.attackManagerDamageRolls = ReadAttackManagerField<string>(attackManager, "damageRolls");
        probe.attackManagerPassiveEffectString = ReadAttackManagerField<string>(attackManager, "passiveEffectString");
        probe.attackManagerFinalDamageCalculation = ReadAttackManagerField<string>(attackManager, "finalDamageCalculation");
        probe.attackManagerAdvantage = ReadAttackManagerField<int>(attackManager, "advantage");
        probe.attackManagerBaseDamage = ReadAttackManagerField<int>(attackManager, "baseDamage");
        probe.attackManagerDamageMultiplier = ReadAttackManagerField<int>(attackManager, "damageMultiplier");
        probe.attackManagerAttackDamageMultiplier = ReadAttackManagerField<int>(attackManager, "attackDamageMultiplier");
        probe.attackManagerBonusDamage = ReadAttackManagerField<int>(attackManager, "bonusDamage");
        probe.attackManagerDefenseMultiplier = ReadAttackManagerField<int>(attackManager, "defenseMultiplier");
        probe.attackManagerBonusDefense = ReadAttackManagerField<int>(attackManager, "bonusDefense");
        probe.attackManagerDodgeChance = ReadAttackManagerField<int>(attackManager, "dodgeChance");
        probe.attackManagerDefenseValue = ReadAttackManagerField<int>(attackManager, "defenseValue");
        probe.attackManagerAttackValue = ReadAttackManagerField<int>(attackManager, "attackValue");
        probe.attackManagerHitChance = ReadAttackManagerField<int>(attackManager, "hitChance");
        probe.attackManagerCritDamage = ReadAttackManagerField<int>(attackManager, "critDamage");
        probe.attackManagerCritChance = ReadAttackManagerField<int>(attackManager, "critChance");
        probe.attackManagerCounterAttack = attackManager.counterAttack;
    }

    static void ApplyAttackManagerSnapshot(AttackManager attackManager, UnityActualTurnProbe probe)
    {
        if (attackManager == null || probe == null)
        {
            return;
        }

        probe.attackManagerDamageRolls = ReadAttackManagerField<string>(attackManager, "damageRolls");
        probe.attackManagerPassiveEffectString = ReadAttackManagerField<string>(attackManager, "passiveEffectString");
        probe.attackManagerFinalDamageCalculation = ReadAttackManagerField<string>(attackManager, "finalDamageCalculation");
        probe.attackManagerAdvantage = ReadAttackManagerField<int>(attackManager, "advantage");
        probe.attackManagerBaseDamage = ReadAttackManagerField<int>(attackManager, "baseDamage");
        probe.attackManagerDamageMultiplier = ReadAttackManagerField<int>(attackManager, "damageMultiplier");
        probe.attackManagerAttackDamageMultiplier = ReadAttackManagerField<int>(attackManager, "attackDamageMultiplier");
        probe.attackManagerBonusDamage = ReadAttackManagerField<int>(attackManager, "bonusDamage");
        probe.attackManagerDefenseMultiplier = ReadAttackManagerField<int>(attackManager, "defenseMultiplier");
        probe.attackManagerBonusDefense = ReadAttackManagerField<int>(attackManager, "bonusDefense");
        probe.attackManagerDodgeChance = ReadAttackManagerField<int>(attackManager, "dodgeChance");
        probe.attackManagerDefenseValue = ReadAttackManagerField<int>(attackManager, "defenseValue");
        probe.attackManagerAttackValue = ReadAttackManagerField<int>(attackManager, "attackValue");
        probe.attackManagerHitChance = ReadAttackManagerField<int>(attackManager, "hitChance");
        probe.attackManagerCritDamage = ReadAttackManagerField<int>(attackManager, "critDamage");
        probe.attackManagerCritChance = ReadAttackManagerField<int>(attackManager, "critChance");
        probe.attackManagerCounterAttack = attackManager.counterAttack;
    }

    static T ReadAttackManagerField<T>(AttackManager attackManager, string fieldName)
    {
        FieldInfo field = typeof(AttackManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field == null)
        {
            return default(T);
        }

        object value = field.GetValue(attackManager);
        if (value == null)
        {
            return default(T);
        }

        return (T)value;
    }

    static void InvokeBattleManagerProtected(BattleManager manager, string methodName)
    {
        MethodInfo method = typeof(BattleManager).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException("Could not find BattleManager protected method " + methodName + ".");
        }
        method.Invoke(manager, null);
    }

    static void InvokeBattleMapProtected(BattleMap map, string methodName, TacticActor actor, int tile)
    {
        MethodInfo method = typeof(BattleMap).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException("BattleMap." + methodName + " could not be found.");
        }
        method.Invoke(map, new object[] { actor, tile });
    }

    static List<UnityTurnActorState> BuildTurnActors(BattleManager manager)
    {
        List<UnityTurnActorState> actors = new List<UnityTurnActorState>();
        if (manager == null || manager.map == null || manager.map.battlingActors == null)
        {
            return actors;
        }

        for (int i = 0; i < manager.map.battlingActors.Count; i++)
        {
            TacticActor actor = manager.map.battlingActors[i];
            if (actor == null)
            {
                continue;
            }

            UnityTurnActorState state = new UnityTurnActorState();
            state.personalName = actor.GetPersonalName();
            state.spriteName = actor.GetSpriteName();
            state.team = actor.GetTeam();
            state.location = actor.GetLocation();
            state.direction = actor.GetDirection();
            state.health = actor.GetHealth();
            state.baseEnergy = actor.GetBaseEnergy();
            state.energy = actor.GetEnergy();
            state.actions = actor.GetActions();
            state.moveType = actor.GetMoveType();
            state.target = PeekTargetName(actor);
            state.summoned = actor.summoned;
            state.summonedBy = actor.summonedBy == null ? "" : actor.summonedBy.GetPersonalName();
            state.statuses = new List<string>(actor.GetStatuses());
            state.elements = new List<string>(actor.GetElements());
            state.passiveSkills = new List<string>(actor.GetPassiveSkills());
            state.passiveLevels = new List<string>(actor.GetPassiveLevels());
            state.waterResistance = actor.ReturnDamageResistanceOfType("Water");
            if (actor.hurtByList != null && actor.hurtAmount != null)
            {
                int count = Mathf.Min(actor.hurtByList.Count, actor.hurtAmount.Count);
                for (int j = 0; j < count; j++)
                {
                    TacticActor hurtActor = actor.hurtByList[j];
                    if (hurtActor == null)
                    {
                        continue;
                    }
                    state.hurtBy.Add(hurtActor.GetPersonalName() + ":" + actor.hurtAmount[j]);
                }
            }
            actors.Add(state);
        }

        return actors;
    }

    static List<string> SliceCombatLogs(CombatLog combatLog, int previousLogCount, int currentLogCount)
    {
        List<string> delta = new List<string>();
        if (combatLog == null || combatLog.allLogs == null)
        {
            return delta;
        }

        int safeStart = Mathf.Clamp(previousLogCount, 0, combatLog.allLogs.Count);
        int safeEnd = Mathf.Clamp(currentLogCount, safeStart, combatLog.allLogs.Count);
        for (int i = safeStart; i < safeEnd; i++)
        {
            int round = i < combatLog.combatRoundTracker.Count ? combatLog.combatRoundTracker[i] : 0;
            int turn = i < combatLog.combatTurnTracker.Count ? combatLog.combatTurnTracker[i] : 0;
            delta.Add("R" + round + " T" + turn + " :: " + combatLog.allLogs[i]);
        }
        return delta;
    }

    static TacticActor PeekTarget(TacticActor actor)
    {
        if (actor == null)
        {
            return null;
        }

        return actor.target;
    }

    static string PeekTargetName(TacticActor actor)
    {
        TacticActor target = PeekTarget(actor);
        if (target == null)
        {
            return "";
        }

        return target.GetPersonalName();
    }

    static List<UnityMapActorEntry> BuildActorEntries(BattleManager manager)
    {
        List<UnityMapActorEntry> actors = new List<UnityMapActorEntry>();
        if (manager == null || manager.gameObject == null)
        {
            return actors;
        }

        Scene scene = manager.gameObject.scene;
        TacticActor[] sceneActors = Resources.FindObjectsOfTypeAll<TacticActor>();
        for (int i = 0; i < sceneActors.Length; i++)
        {
            TacticActor actor = sceneActors[i];
            if (actor == null)
            {
                continue;
            }
            if (actor.gameObject == null || actor.gameObject.scene != scene)
            {
                continue;
            }

            UnityMapActorEntry entry = new UnityMapActorEntry();
            entry.personalName = actor.GetPersonalName();
            entry.spriteName = actor.GetSpriteName();
            entry.team = actor.GetTeam();
            entry.location = actor.GetLocation();
            entry.direction = actor.GetDirection();
            entry.moveType = actor.GetMoveType();
            actors.Add(entry);
        }

        return actors;
    }

    static List<UnityMapBuildingEntry> BuildBuildingEntries(BattleMap map)
    {
        List<UnityMapBuildingEntry> buildings = new List<UnityMapBuildingEntry>();
        if (map == null || map.buildings == null || map.buildingLocations == null)
        {
            return buildings;
        }

        for (int i = 0; i < map.buildings.Count && i < map.buildingLocations.Count; i++)
        {
            UnityMapBuildingEntry entry = new UnityMapBuildingEntry();
            entry.name = map.buildings[i];
            entry.location = map.buildingLocations[i];
            entry.health = map.buildingHealths != null && i < map.buildingHealths.Count ? map.buildingHealths[i] : 0;
            entry.defense = map.buildingDefenses != null && i < map.buildingDefenses.Count ? map.buildingDefenses[i] : 0;
            buildings.Add(entry);
        }

        return buildings;
    }

    static void WritePrepSnapshot(string tracePath, TracePrepSnapshot snapshot)
    {
        string prepPath = tracePath + ".prep.json";
        string directory = Path.GetDirectoryName(prepPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(prepPath, JsonUtility.ToJson(snapshot, true));
    }

    static TracePrepSnapshot BuildPrepSnapshot(BattleSimulator simulator, string scenarioName, int runIndex)
    {
        BattleManager manager = simulator == null ? null : simulator.battleManager;
        BattleMap map = manager == null ? null : manager.map;
        PartyDataManager partyData = manager == null ? null : manager.partyData;
        Inventory inventory = partyData == null ? null : partyData.inventory;
        return new TracePrepSnapshot
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            scenarioName = scenarioName,
            runIndex = runIndex,
            simulatorFound = simulator != null,
            battleManagerFound = manager != null,
            mapFound = map != null,
            mapBattleManagerFound = map != null && map.battleManager != null,
            partyDataFound = partyData != null,
            inventoryFound = inventory != null,
            inventoryItemsFound = inventory != null && inventory.items != null,
            inventoryAssignedIdsFound = inventory != null && inventory.assignedActorIDs != null
        };
    }
}

[Serializable]
public class StartupTrace
{
    public string scenarioName;
    public int seed;
    public string simulatorStateName;
    public string simulatorStateType;
    public string managerStateName;
    public string managerStateType;
    public string startManagerStateName;
    public string startManagerStateType;
    public List<string> allowedTerrains = new List<string>();
    public List<string> allowedWeather = new List<string>();
    public List<string> allowedTimes = new List<string>();
    public List<string> allowedFormations = new List<string>();
    public string terrainCall;
    public string selectedTerrainAfterCall;
    public string startBattleTimeCall;
    public string selectedTimeAfterStartBattle;
    public string startBattleWeatherCall;
    public string selectedWeatherAfterStartBattle;
    public string initializeMapWeatherCall;
    public string selectedWeatherAfterInitializeMap;
    public string initializeMapWeatherLogCall;
    public string selectedWeatherAfterWeatherLog;
    public string initializeMapTimeCall;
    public string selectedTimeAfterInitializeMap;
    public string initializeMapTimeLogCall;
    public string selectedTimeAfterTimeLog;
    public string startingFormationCall;
    public string startManagerWeatherCall;
    public string startManagerWeatherLogCall;
    public string startManagerTimeCall;
    public string startManagerTimeLogCall;
    public string startManagerAllySpawnPattern;
    public string startManagerEnemySpawnPattern;
}

[Serializable]
public class EnemyMatchupStartupSetting
{
    public int PairIndex;
    public string ScenarioName;
    public int Seed;
    public string Terrain;
    public string Weather;
    public string Time;
    public string Formation;
    public string AllySpawnPattern;
    public string EnemySpawnPattern;
}

[Serializable]
public class TracePrepSnapshot
{
    public string timestamp;
    public string scenarioName;
    public int runIndex;
    public bool simulatorFound;
    public bool battleManagerFound;
    public bool mapFound;
    public bool mapBattleManagerFound;
    public bool partyDataFound;
    public bool inventoryFound;
    public bool inventoryItemsFound;
    public bool inventoryAssignedIdsFound;
}

[Serializable]
public class UnityMapTrace
{
    public string scenarioName;
    public int pairIndex;
    public int runIndex;
    public int seed;
    public string terrain;
    public string weather;
    public string time;
    public string startingFormation;
    public string battleStartRandomStateJson;
    public string firstTurnRandomStateJson;
    public int mapSize;
    public List<string> mapInfo = new List<string>();
    public List<string> terrainEffectTiles = new List<string>();
    public List<int> elevations = new List<int>();
    public List<string> borderDetails = new List<string>();
    public List<UnityMapBuildingEntry> buildings = new List<UnityMapBuildingEntry>();
    public List<UnityMapActorEntry> actors = new List<UnityMapActorEntry>();
}

[Serializable]
public class UnityMapBuildingEntry
{
    public string name;
    public int location;
    public int health;
    public int defense;
}

[Serializable]
public class UnityMapActorEntry
{
    public string personalName;
    public string spriteName;
    public int team;
    public int location;
    public int direction;
    public string moveType;
}

[Serializable]
public class UnityMoveProbe
{
    public string scenarioName;
    public int pairIndex;
    public int runIndex;
    public int seed;
    public int turnsAdvanced;
    public string randomStateBeforeTargetSelection;
    public string actorName;
    public int actorLocation;
    public string actorMoveType;
    public int actorMoveRange;
    public int actorAttackRange;
    public int actorWeaponReach;
    public string targetName;
    public int targetLocation;
    public int approachTile;
    public int pathCostToApproach;
    public int fullPathCost;
    public List<int> currentMoveCosts = new List<int>();
    public List<int> pathCosts = new List<int>();
    public List<int> fullPath = new List<int>();
    public List<int> trimmedPath = new List<int>();
    public List<int> reachableTiles = new List<int>();
    public List<int> attackableTilesFromStart = new List<int>();
    public List<int> targetAttackableTiles = new List<int>();
    public List<UnityMoveProbeTile> fullPathTiles = new List<UnityMoveProbeTile>();
    public List<UnityMoveProbeTile> trimmedPathTiles = new List<UnityMoveProbeTile>();
    public List<UnityEnemyChoiceProbe> enemyChoices = new List<UnityEnemyChoiceProbe>();
}

[Serializable]
public class UnityMoveProbeTile
{
    public int tile;
    public string terrain;
    public string terrainEffect;
    public int elevation;
    public int stepCost;
    public int directionFromPrevious;
}

[Serializable]
public class UnityEnemyChoiceProbe
{
    public string personalName;
    public int location;
    public int pathCost;
    public List<int> path = new List<int>();
}

[Serializable]
public class UnityPostSkillEnemyChoiceProbe
{
    public string scenarioName;
    public int pairIndex;
    public int runIndex;
    public int seed;
    public int turnsAdvanced;
    public string actorName;
    public string skillName;
    public int chosenTile = -1;
    public string stateBeforeSkillTargetSelection;
    public string stateAfterSkillTargetSelection;
    public string targetName;
    public List<UnityEnemyChoiceProbe> enemyChoices = new List<UnityEnemyChoiceProbe>();
    public List<UnityTurnActorState> actors = new List<UnityTurnActorState>();
}

[Serializable]
public class UnityBossFallbackProbe
{
    public string scenarioName;
    public int pairIndex;
    public int runIndex;
    public int seed;
    public int turnsAdvanced;
    public string actorName;
    public string initialRandomState;
    public string initialTarget;
    public bool skillResult;
    public string afterSkillRandomState;
    public string afterSkillTarget;
    public string afterBasicRandomState;
    public string afterBasicTarget;
    public List<UnityTurnActorState> initialActors = new List<UnityTurnActorState>();
    public List<UnityTurnActorState> afterSkillActors = new List<UnityTurnActorState>();
    public List<UnityTurnActorState> afterBasicActors = new List<UnityTurnActorState>();
}

[Serializable]
public class UnityTurnTrace
{
    public string scenarioName;
    public int pairIndex;
    public int runIndex;
    public int seed;
    public string terrain;
    public string weather;
    public string time;
    public string startingFormation;
    public int mapSize;
    public List<UnityTurnActorState> initialActors = new List<UnityTurnActorState>();
    public List<UnityTurnFrame> turns = new List<UnityTurnFrame>();
    public int finalWinningTeam = -1;
    public int finalRound;
    public int finalTurnIndex;
    public List<UnityTurnActorState> finalActors = new List<UnityTurnActorState>();
}

[Serializable]
public class UnityTurnFrame
{
    public int step;
    public int round;
    public int turnIndex;
    public string turnActor;
    public int turnActorTeam;
    public int turnActorLocation;
    public int turnActorActions;
    public string turnActorTarget;
    public string randomStateBeforeTurn;
    public string randomStateAfterTurn;
    public List<UnityTurnActorState> beforeActors = new List<UnityTurnActorState>();
    public List<string> combatLogDelta = new List<string>();
    public List<UnityTurnActorState> afterActors = new List<UnityTurnActorState>();
    public int nextRound;
    public int nextTurnIndex;
    public string nextTurnActor;
}

[Serializable]
public class UnityTurnActorState
{
    public string personalName;
    public string spriteName;
    public int team;
    public int location;
    public int direction;
    public int health;
    public int baseEnergy;
    public int energy;
    public int actions;
    public string moveType;
    public string target;
    public bool summoned;
    public string summonedBy;
    public List<string> statuses = new List<string>();
    public List<string> elements = new List<string>();
    public List<string> passiveSkills = new List<string>();
    public List<string> passiveLevels = new List<string>();
    public int waterResistance;
    public List<string> hurtBy = new List<string>();
}

[Serializable]
public class UnitySkillTargetProbe
{
    public string scenarioName;
    public int pairIndex;
    public int runIndex;
    public int seed;
    public int turnsAdvanced;
    public int round;
    public int turnIndex;
    public string actorName;
    public int actorLocation;
    public string skillName;
    public string currentTarget;
    public List<int> targetableTiles = new List<int>();
    public List<int> emptyTargetableTiles = new List<int>();
    public int chosenTile = -1;
    public string randomStateBeforeTargetSelection;
    public string randomStateBeforeTileChoice;
    public string randomStateAfterTileChoice;
    public List<UnityTurnActorState> actors = new List<UnityTurnActorState>();
}

[Serializable]
public class UnityNpcTurnProbe
{
    public string scenarioName;
    public int pairIndex;
    public int runIndex;
    public int seed;
    public int turnsAdvanced;
    public int round;
    public int turnIndex;
    public string actorName;
    public string initialRandomState;
    public string finalRandomState;
    public string passiveSkillsRaw;
    public string passiveLevelsRaw;
    public string movingPassivesRaw;
    public List<int> path = new List<int>();
    public List<UnityTurnActorState> initialActors = new List<UnityTurnActorState>();
    public List<UnityTurnActorState> finalActors = new List<UnityTurnActorState>();
    public List<UnityAdvanceSnapshot> advanceSnapshots = new List<UnityAdvanceSnapshot>();
    public List<UnityNpcTurnProbeState> states = new List<UnityNpcTurnProbeState>();
}

[Serializable]
public class UnityAdvanceSnapshot
{
    public int step;
    public int roundBefore;
    public int turnIndexBefore;
    public string actorBefore;
    public int actorLocationBefore;
    public string randomStateBefore;
    public List<string> actorsBefore = new List<string>();
    public int roundAfter;
    public int turnIndexAfter;
    public string actorAfter;
    public int actorLocationAfter;
    public string randomStateAfter;
    public List<string> actorsAfter = new List<string>();
}

[Serializable]
public class UnityActorStateDump
{
    public string scenarioName;
    public int pairIndex;
    public int runIndex;
    public int seed;
    public int turnsAdvanced;
    public int round;
    public int turnIndex;
    public string actorName;
    public int location;
    public int actions;
    public int movement;
    public string moveType;
    public string targetName;
    public string randomState;
    public string passiveSkillsRaw;
    public string passiveLevelsRaw;
    public string movingPassivesRaw;
    public List<int> currentMoveCosts = new List<int>();
    public List<int> pathCosts = new List<int>();
    public List<int> attackableTiles = new List<int>();
    public List<int> reachableTiles = new List<int>();
}

[Serializable]
public class UnityNpcTurnProbeState
{
    public UnityNpcTurnProbeState() {}

    public UnityNpcTurnProbeState(string label, string randomState, int actorLocation, string targetName)
    {
        this.label = label;
        this.randomState = randomState;
        this.actorLocation = actorLocation;
        this.targetName = targetName;
    }

    public string label;
    public string randomState;
    public int actorLocation;
    public string targetName;
    public int stepTile = -1;
    public bool inAttackRange;
    public bool inAttackableRange;
    public string passiveId;
    public bool passivePassed;
}

[Serializable]
public class UnityActualTurnProbe
{
    public string scenarioName;
    public int pairIndex;
    public int runIndex;
    public int seed;
    public int turnsAdvanced;
    public int round;
    public int turnIndex;
    public string turnActor;
    public int turnActorTeam;
    public int turnActorLocation;
    public string turnActorTarget;
    public string randomStateBeforeTurn;
    public string randomStateAfterTurn;
    public List<UnityTurnActorState> beforeActors = new List<UnityTurnActorState>();
    public List<string> combatLogDelta = new List<string>();
    public List<UnityTurnActorState> afterActors = new List<UnityTurnActorState>();
    public List<UnityAdvanceSnapshot> advanceSnapshots = new List<UnityAdvanceSnapshot>();
    public int nextRound;
    public int nextTurnIndex;
    public string nextTurnActor;
    public string attackManagerDamageRolls;
    public string attackManagerPassiveEffectString;
    public string attackManagerFinalDamageCalculation;
    public int attackManagerAdvantage;
    public int attackManagerBaseDamage;
    public int attackManagerDamageMultiplier;
    public int attackManagerAttackDamageMultiplier;
    public int attackManagerBonusDamage;
    public int attackManagerDefenseMultiplier;
    public int attackManagerBonusDefense;
    public int attackManagerDodgeChance;
    public int attackManagerDefenseValue;
    public int attackManagerAttackValue;
    public int attackManagerHitChance;
    public int attackManagerCritDamage;
    public int attackManagerCritChance;
    public bool attackManagerCounterAttack;
}

[Serializable]
public class UnityAttackSkillProbe
{
    public string scenarioName;
    public int pairIndex;
    public int runIndex;
    public int seed;
    public int turnsAdvanced;
    public int round;
    public int turnIndex;
    public string actorName;
    public string targetName;
    public int targetWaterResistance;
    public string rawAttackSkill;
    public bool skillWouldHealTarget;
    public string returnedAttackSkill;
    public string randomState;
    public List<UnityTurnActorState> actors = new List<UnityTurnActorState>();
}

[Serializable]
public class UnitySpellAttemptProbe
{
    public string scenarioName;
    public int pairIndex;
    public int runIndex;
    public int seed;
    public int turnsAdvanced;
    public int round;
    public int turnIndex;
    public string actorName;
    public string spellName;
    public string initialRandomState;
    public string randomStateAfterTargetChoice;
    public string spellType;
    public string spellEffect;
    public int spellRange;
    public int chosenTile = -1;
    public bool costOk;
    public bool validTargets;
    public List<int> targetedTiles = new List<int>();
    public List<UnityTurnActorState> beforeActors = new List<UnityTurnActorState>();
    public List<UnityTurnActorState> afterActors = new List<UnityTurnActorState>();
}

[Serializable]
public class UnitySkillAttemptProbe
{
    public string scenarioName;
    public int pairIndex;
    public int runIndex;
    public int seed;
    public int turnsAdvanced;
    public int round;
    public int turnIndex;
    public string actorName;
    public string skillName;
    public string initialRandomState;
    public string randomStateAfterTargetChoice;
    public string skillType;
    public string skillEffect;
    public int skillRange;
    public int skillActionCost;
    public int skillEnergyCost;
    public int chosenTile = -1;
    public bool costOk;
    public bool validTargets;
    public List<int> targetedTiles = new List<int>();
    public List<UnityTurnActorState> beforeActors = new List<UnityTurnActorState>();
    public List<UnityTurnActorState> afterActors = new List<UnityTurnActorState>();
}

[Serializable]
public class UnityTurnDecisionProbe
{
    public string scenarioName;
    public int pairIndex;
    public int runIndex;
    public int seed;
    public int turnsAdvanced;
    public int round;
    public int turnIndex;
    public string actorName;
    public string spriteName;
    public int team;
    public int location;
    public int actions;
    public string initialRandomState;
    public bool bossTurn;
    public string actorSkillRotation;
    public string actorAttackSkill;
    public string bossAction;
    public string bossSpecifics;
    public string randomStateAfterBossDecision;
    public bool normalTurnReturnedTrue;
    public string activeSkillNameAfterNormalTurn;
    public string randomStateAfterNormalDecision;
    public List<string> activeSkills = new List<string>();
    public List<string> spells = new List<string>();
    public List<UnityTurnActorState> beforeActors = new List<UnityTurnActorState>();
    public List<UnityTurnActorState> afterActors = new List<UnityTurnActorState>();
}

[Serializable]
public class UnityAttackStepProbe
{
    public string scenarioName;
    public int pairIndex;
    public int runIndex;
    public int seed;
    public int turnsAdvanced;
    public int round;
    public int turnIndex;
    public string actorName;
    public string initialRandomState;
    public string initialTarget;
    public int initialTargetLocation = -1;
    public int initialLocation;
    public int initialActions;
    public bool directAttackAvailable;
    public bool attackableAfterMoveAvailable;
    public string branch;
    public int targetHpBeforePath = -1;
    public string targetBeforePath;
    public int targetBeforePathLocation = -1;
    public string beforePathRandomState;
    public bool pathFailed;
    public string afterPathRandomState;
    public int locationAfterPath;
    public int actionsAfterPath;
    public string targetAfterPath;
    public int targetAfterPathLocation = -1;
    public int targetHpAfterPath = -1;
    public string beforeAttackRandomState;
    public int targetHpBeforeAttack = -1;
    public int targetBeforeAttackLocation = -1;
    public List<string> beforeAttackMapContext = new List<string>();
    public List<string> beforeAttackAttackerPassives = new List<string>();
    public List<string> beforeAttackDefenderPassives = new List<string>();
    public List<string> beforeAttackAuras = new List<string>();
    public string afterAttackRandomState;
    public int targetHpAfterAttack = -1;
    public int targetAfterAttackLocation = -1;
    public List<string> combatLogDelta = new List<string>();
    public List<UnityTurnActorState> initialActors = new List<UnityTurnActorState>();
    public List<UnityTurnActorState> actorsAfterPath = new List<UnityTurnActorState>();
    public List<UnityTurnActorState> actorsAfterAttack = new List<UnityTurnActorState>();
    public string attackManagerDamageRolls;
    public string attackManagerPassiveEffectString;
    public string attackManagerFinalDamageCalculation;
    public int attackManagerAdvantage;
    public int attackManagerBaseDamage;
    public int attackManagerDamageMultiplier;
    public int attackManagerAttackDamageMultiplier;
    public int attackManagerBonusDamage;
    public int attackManagerDefenseMultiplier;
    public int attackManagerBonusDefense;
    public int attackManagerDodgeChance;
    public int attackManagerDefenseValue;
    public int attackManagerAttackValue;
    public int attackManagerHitChance;
    public int attackManagerCritDamage;
    public int attackManagerCritChance;
    public bool attackManagerCounterAttack;
}

[Serializable]
public class UnityRandomSequenceProbe
{
    public int seed;
    public List<int> minInclusive = new List<int>();
    public List<int> maxExclusive = new List<int>();
    public List<int> values = new List<int>();
    public string requestedStateJson;
    public string overwrittenStateJson;
    public List<string> reflectionFields = new List<string>();
    public string initialStateJson;
    public List<string> stateBeforeEachDraw = new List<string>();
    public List<string> stateAfterEachDraw = new List<string>();
}

[Serializable]
public class UnityBuffStatusValueDump
{
    public string statusId;
    public string raw;
}

public static class JsonHelper
{
    [Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }

    public static string ToJson<T>(T[] array, bool prettyPrint)
    {
        Wrapper<T> wrapper = new Wrapper<T> { Items = array };
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }
}
