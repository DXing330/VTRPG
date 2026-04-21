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
    const string SimulatorScenePath = "Assets/Scenes/DebugScenes/BattleSimulator.unity";

    [MenuItem("Tools/Battle Tests/Run Fast Suite")]
    public static void RunFastSuite()
    {
        RunSuiteAtPath(FastSuitePath, "");
    }

    [MenuItem("Window/Battle Tests/Run Fast Suite")]
    public static void RunFastSuiteFromWindowMenu()
    {
        RunFastSuite();
    }

    [MenuItem("Window/Battle Tests/Run Smoke Scenarios")]
    public static void RunSmokeScenarios()
    {
        RunSuiteAtPath(FastSuitePath, "smoke");
    }

    [MenuItem("Window/Battle Tests/Run Balance Scenarios")]
    public static void RunBalanceScenarios()
    {
        RunSuiteAtPath(FastSuitePath, "balance");
    }

    [MenuItem("Window/Battle Tests/Run Selected Suite Or Scenario")]
    public static void RunSelectedSuiteOrScenario()
    {
        BattleTestSuite suite = Selection.activeObject as BattleTestSuite;
        if (suite != null)
        {
            RunSuite(suite, "");
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
            RunSuite(transientSuite, "");
            ScriptableObject.DestroyImmediate(transientSuite);
            return;
        }

        Debug.LogError("Select a BattleTestSuite or BattleTestScenario asset in the Project window first.");
    }

    public static void RunSuiteAtPath(string suitePath)
    {
        RunSuiteAtPath(suitePath, "");
    }

    public static void RunSuiteAtPath(string suitePath, string requiredTag)
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
        RunSuite(suite, requiredTag);
    }

    static void RunSuite(BattleTestSuite suite, string requiredTag)
    {
        BattleSimulationSuiteResult suiteResult = new BattleSimulationSuiteResult();
        string reportDirectory = "";
        int exitCode = 0;

        try
        {
            suiteResult.suiteName = suite.SuiteName();
            if (!string.IsNullOrEmpty(requiredTag))
            {
                suiteResult.suiteName += " [" + requiredTag + "]";
            }
            suiteResult.startedAt = DateTime.Now.ToString("o");

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("Battle simulation suite cancelled because modified scenes were not saved.");
                return;
            }

            RunSuiteScenarios(suite, suiteResult, requiredTag);

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

    static void RunSuiteScenarios(BattleTestSuite suite, BattleSimulationSuiteResult suiteResult, string requiredTag)
    {
        if (!File.Exists(ProjectRelativePath(SimulatorScenePath)))
        {
            throw new FileNotFoundException("Simulator scene was not found.", SimulatorScenePath);
        }

        var scenarios = suite.EnabledScenariosWithTag(requiredTag);
        if (scenarios.Count == 0)
        {
            throw new InvalidOperationException("Battle test suite has no enabled scenarios" + (string.IsNullOrEmpty(requiredTag) ? "." : " tagged '" + requiredTag + "'."));
        }

        for (int scenarioIndex = 0; scenarioIndex < scenarios.Count; scenarioIndex++)
        {
            BattleTestScenario scenario = scenarios[scenarioIndex];
            for (int runIndex = 0; runIndex < scenario.RunCount(); runIndex++)
            {
                EditorSceneManager.OpenScene(SimulatorScenePath, OpenSceneMode.Single);
                BattleSimulationRunResult run = BattleSimulationRunner.RunScenarioInLoadedScene(scenario, runIndex);
                suiteResult.AddRun(run);
                if (suite.stopOnFailure && run.failed)
                {
                    return;
                }
            }
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
