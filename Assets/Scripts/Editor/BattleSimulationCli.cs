using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BattleSimulationCli
{
    const string FastSuitePath = "Assets/Scripts/Combat/TestData/BattleTestSuites/FastBattleSuite.asset";
    const string R1SuitePath = "Assets/Scripts/Combat/TestData/BattleTestSuites/RoguelikeFloor1Suite.asset";
    const string GuardianFocusScenarioPath = "Assets/Scripts/Combat/TestData/BattleTestSuites/Scenario_RL_F1_BossSSGuardian.asset";
    const string SimulatorScenePath = "Assets/Scenes/DebugScenes/BattleSimulator.unity";

    [MenuItem("Window/Battle Tests/Run R1 Suite")]
    public static void RunR1SuiteFromWindowMenu()
    {
        RunSuiteAtPath(R1SuitePath);
    }

    [MenuItem("Window/Battle Tests/Run R1 Enemy Matrix")]
    public static void RunR1EnemyMatrixFromWindowMenu()
    {
        RunEnemyMatchupSuiteAtPath(R1SuitePath, 1, true, false);
    }

    [MenuItem("Window/Battle Tests/Run R1 Enemy Matrix Quick")]
    public static void RunR1EnemyMatrixQuickFromWindowMenu()
    {
        RunEnemyMatchupSuiteAtPath(R1SuitePath, 1, true, false, 3);
    }

    [MenuItem("Window/Battle Tests/Run Fast Suite")]
    public static void RunFastSuiteFromWindowMenu()
    {
        RunSuiteAtPath(FastSuitePath);
    }

    [MenuItem("Window/Battle Tests/Run Guardian Focus")]
    public static void RunGuardianFocusFromWindowMenu()
    {
        RunScenarioAtPath(GuardianFocusScenarioPath, "Guardian Focus");
    }

    [MenuItem("Window/Battle Tests/Clean Generated Actors")]
    public static void CleanGeneratedActorsFromWindowMenu()
    {
        int cleaned = BattleSimulationRunner.CleanGeneratedActorObjectsInLoadedScenes();
        Debug.Log("Battle test cleanup removed " + cleaned + " generated actor object(s).");
    }

    public static void RunGuardianFocus()
    {
        RunScenarioAtPath(GuardianFocusScenarioPath, "Guardian Focus");
    }

    [MenuItem("Window/Battle Tests/Run Selected Suite Or Scenario")]
    public static void RunSelectedSuiteOrScenario()
    {
        BattleTestSuite suite = Selection.activeObject as BattleTestSuite;
        if (suite != null)
        {
            RunSuite(suite);
            return;
        }

        BattleTestScenario scenario = Selection.activeObject as BattleTestScenario;
        if (scenario != null)
        {
            BattleTestSuite transientSuite = ScriptableObject.CreateInstance<BattleTestSuite>();
            transientSuite.suiteName = "Selected Scenario - " + scenario.ScenarioName();
            transientSuite.reportRoot = "Assets/output/battle-tests";
            transientSuite.scenarios = new List<BattleTestScenario>();
            transientSuite.scenarios.Add(scenario);
            RunSuite(transientSuite);
            ScriptableObject.DestroyImmediate(transientSuite);
            return;
        }

        Debug.LogError("Select a BattleTestSuite or BattleTestScenario asset in the Project window first.");
    }

    [MenuItem("Window/Battle Tests/Run Selected Suite Enemy Matrix")]
    public static void RunSelectedSuiteEnemyMatrix()
    {
        BattleTestSuite suite = Selection.activeObject as BattleTestSuite;
        if (suite == null)
        {
            Debug.LogError("Select a BattleTestSuite asset in the Project window first.");
            return;
        }

        RunEnemyMatchupSuite(suite, 1, true, false);
    }

    [MenuItem("Window/Battle Tests/Run Selected Suite Enemy Matrix Quick")]
    public static void RunSelectedSuiteEnemyMatrixQuick()
    {
        BattleTestSuite suite = Selection.activeObject as BattleTestSuite;
        if (suite == null)
        {
            Debug.LogError("Select a BattleTestSuite asset in the Project window first.");
            return;
        }

        RunEnemyMatchupSuite(suite, 1, true, false, 3);
    }

    public static void RunSuiteAtPath(string suitePath)
    {
        AssetDatabase.Refresh();
        BattleTestSuite suite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
        if (suite == null)
        {
            Debug.LogError("Could not load battle test suite at " + suitePath + ".");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
            return;
        }
        RunSuite(suite);
    }

    public static void RunScenarioAtPath(string scenarioPath, string suiteName)
    {
        AssetDatabase.Refresh();
        BattleTestScenario scenario = AssetDatabase.LoadAssetAtPath<BattleTestScenario>(scenarioPath);
        if (scenario == null)
        {
            Debug.LogError("Could not load battle test scenario at " + scenarioPath + ".");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
            return;
        }

        BattleTestSuite transientSuite = ScriptableObject.CreateInstance<BattleTestSuite>();
        transientSuite.suiteName = suiteName;
        transientSuite.reportRoot = "Assets/output/battle-tests";
        transientSuite.scenarios = new List<BattleTestScenario>();
        transientSuite.scenarios.Add(scenario);
        RunSuite(transientSuite);
        ScriptableObject.DestroyImmediate(transientSuite);
    }

    public static void RunR1EnemyMatrixSuite()
    {
        RunEnemyMatchupSuiteAtPath(R1SuitePath, 1, true, false);
    }

    public static void RunR1EnemyMatrixQuickSuite()
    {
        RunEnemyMatchupSuiteAtPath(R1SuitePath, 1, true, false, 3);
    }

    public static void RunEnemyMatchupSuiteAtPath(string suitePath, int sourceTeam, bool orderedPairs, bool includeMirrorMatches)
    {
        RunEnemyMatchupSuiteAtPath(suitePath, sourceTeam, orderedPairs, includeMirrorMatches, 0);
    }

    public static void RunEnemyMatchupSuiteAtPath(string suitePath, int sourceTeam, bool orderedPairs, bool includeMirrorMatches, int runCountOverride)
    {
        AssetDatabase.Refresh();
        BattleTestSuite suite = AssetDatabase.LoadAssetAtPath<BattleTestSuite>(suitePath);
        if (suite == null)
        {
            Debug.LogError("Could not load battle test suite at " + suitePath + ".");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
            return;
        }

        RunEnemyMatchupSuite(suite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
    }

    static void RunEnemyMatchupSuite(BattleTestSuite sourceSuite, int sourceTeam, bool orderedPairs, bool includeMirrorMatches)
    {
        RunEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, 0);
    }

    static void RunEnemyMatchupSuite(BattleTestSuite sourceSuite, int sourceTeam, bool orderedPairs, bool includeMirrorMatches, int runCountOverride)
    {
        BattleTestSuite generatedSuite = null;
        try
        {
            generatedSuite = BattleMatchupSuiteBuilder.BuildEnemyMatchupSuite(sourceSuite, sourceTeam, orderedPairs, includeMirrorMatches, runCountOverride);
            RunSuite(generatedSuite);
        }
        finally
        {
            BattleMatchupSuiteBuilder.DestroyGeneratedSuite(generatedSuite);
        }
    }

    static void RunSuite(BattleTestSuite suite)
    {
        BattleSimulationSuiteResult suiteResult = new BattleSimulationSuiteResult();
        string reportDirectory = "";
        int exitCode = 0;
        try
        {
            suiteResult.suiteName = suite.SuiteName();
            suiteResult.startedAt = DateTime.Now.ToString("o");
            RunSuiteScenarios(suite, suiteResult);
            suiteResult.finishedAt = DateTime.Now.ToString("o");
            reportDirectory = BattleSimulationReportWriter.WriteSuiteResult(suite, suiteResult);
            Debug.Log("Battle simulation report written to: " + reportDirectory);
        }
        catch (Exception exception)
        {
            exitCode = 1;
            suiteResult.failed = true;
            suiteResult.finishedAt = DateTime.Now.ToString("o");
            suiteResult.failures.Add(exception.Message);
            Debug.LogError(exception);
        }
        if (suiteResult.failed)
        {
            exitCode = 1;
        }
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }

    static void RunSuiteScenarios(BattleTestSuite suite, BattleSimulationSuiteResult suiteResult)
    {
        AddSuiteReferenceFailures(suite, suiteResult);
        var scenarios = suite.EnabledScenarios();
        for (int scenarioIndex = 0; scenarioIndex < scenarios.Count; scenarioIndex++)
        {
            BattleTestScenario scenario = scenarios[scenarioIndex];
            Debug.Log(scenario);
            for (int runIndex = 0; runIndex < scenario.RunCount(); runIndex++)
            {
                EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
                Debug.Log(scenario);
                BattleSimulationRunResult run = BattleSimulationRunner.RunScenarioInLoadedScene(scenario, runIndex);
                suiteResult.AddRun(run);
                if (suite.stopOnFailure && run.failed)
                {
                    return;
                }
            }
        }
    }

    static void AddSuiteReferenceFailures(BattleTestSuite suite, BattleSimulationSuiteResult suiteResult)
    {
        if (suite == null || suite.scenarios == null)
        {
            suiteResult.failed = true;
            suiteResult.failures.Add("Suite has no scenario list.");
            return;
        }

        for (int i = 0; i < suite.scenarios.Count; i++)
        {
            if (suite.scenarios[i] != null)
            {
                continue;
            }
            suiteResult.failed = true;
            suiteResult.failures.Add("Suite scenario reference at index " + i + " is missing or could not be loaded.");
        }
    }

    static string ProjectRelativePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.GetFullPath(Path.Combine(projectRoot, path));
    }
}
