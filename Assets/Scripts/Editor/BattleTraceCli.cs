using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
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
            simulator.StartBattle();
            if (simulator.battleManager == null)
            {
                throw new InvalidOperationException("BattleSimulator has no BattleManager reference.");
            }
            simulator.battleManager.ForceStart();

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

            UnityMoveProbe probe = BuildMoveProbe(simulator.battleManager, actorName, pairIndex, scenario, runIndex, seed);
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

    public static void RunEnemyMatchupTurnTrace()
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
            simulator.StartBattle();
            if (simulator.battleManager == null)
            {
                throw new InvalidOperationException("BattleSimulator has no BattleManager reference.");
            }
            simulator.battleManager.ForceStart();

            UnityTurnTrace trace = BuildTurnTrace(simulator.battleManager, scenario, pairIndex, runIndex, seed, maxTurns);
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
            List<int> maxes = ParseIntCsv(GetRequiredArg("-intMaxes"));
            if (maxes.Count == 0)
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
                return args[i + 1];
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
        trace.weather = map.GetWeather();
        trace.time = map.GetTime();
        trace.startingFormation = simulator.simulatorState == null ? "" : simulator.simulatorState.selectedStartingFormation;
        trace.battleStartRandomStateJson = JsonUtility.ToJson(UnityEngine.Random.state);
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
        return probe;
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
        trace.weather = manager.map.GetWeather();
        trace.time = manager.map.GetTime();
        trace.startingFormation = manager.battleState == null ? "" : manager.battleState.GetAllySpawnPattern();
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
            frame.turnActorTarget = turnActor.GetTarget() == null ? "" : turnActor.GetTarget().GetPersonalName();
            frame.beforeActors = BuildTurnActors(manager);

            npcTurn.Invoke(manager, null);

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
        List<int> targetableTiles = manager.activeManager.GetTargetableTiles(actor.GetLocation(), manager.moveManager.actorPathfinder);
        List<int> emptyTargetableTiles = manager.map.ReturnEmptyTiles(targetableTiles);
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
        probe.currentTarget = actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName();
        probe.targetableTiles = new List<int>(targetableTiles);
        probe.emptyTargetableTiles = new List<int>(emptyTargetableTiles);
        probe.chosenTile = chosenTile;
        probe.randomStateBeforeTargetSelection = stateBeforeTargetSelection;
        probe.randomStateBeforeTileChoice = stateBeforeTileChoice;
        probe.randomStateAfterTileChoice = stateAfterTileChoice;
        probe.actors = BuildTurnActors(manager);
        return probe;
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
            state.actions = actor.GetActions();
            state.moveType = actor.GetMoveType();
            state.target = actor.GetTarget() == null ? "" : actor.GetTarget().GetPersonalName();
            state.summoned = actor.summoned;
            state.summonedBy = actor.summonedBy == null ? "" : actor.summonedBy.GetPersonalName();
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
    public int actions;
    public string moveType;
    public string target;
    public bool summoned;
    public string summonedBy;
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
public class UnityRandomSequenceProbe
{
    public int seed;
    public List<int> maxExclusive = new List<int>();
    public List<int> values = new List<int>();
    public string initialStateJson;
    public List<string> stateBeforeEachDraw = new List<string>();
    public List<string> stateAfterEachDraw = new List<string>();
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
