using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BattleUnitTestCli
{
    const string TestBattleScenePath = "Assets/Scenes/TestScenes/TestBattle.unity";
    const string ManualReportPath = "Assets/output/battle-tests/manual-run/battle-unit-test-report.txt";
    const string AutoReportPath = "Assets/output/battle-tests/auto-run/battle-unit-test-report.txt";

    [MenuItem("Window/Battle Tests/Run Battle Unit Tests")]
    public static void RunAllFromWindowMenu()
    {
        RunSuite("all", ManualReportPath, true);
    }

    [MenuItem("Window/Battle Tests/Run Combat Unit Tests")]
    public static void RunCombatFromWindowMenu()
    {
        RunSuite("combat", ManualReportPath, true);
    }

    [MenuItem("Window/Battle Tests/Run Map/Movement Unit Tests")]
    public static void RunMapMovementFromWindowMenu()
    {
        RunSuite("map", ManualReportPath, true);
    }

    [MenuItem("Window/Battle Tests/Run Active Skill Effect Unit Tests")]
    public static void RunActiveSkillEffectFromWindowMenu()
    {
        RunSuite("active-effects", ManualReportPath, true);
    }

    [MenuItem("Window/Battle Tests/Run Passive Unit Tests")]
    public static void RunPassiveFromWindowMenu()
    {
        RunSuite("passive", ManualReportPath, true);
    }

    public static void RunAll()
    {
        RunFromCommandLine("all");
    }

    public static void RunCombat()
    {
        RunFromCommandLine("combat");
    }

    public static void RunMapMovement()
    {
        RunFromCommandLine("map");
    }

    public static void RunForcedMovement()
    {
        RunFromCommandLine("forced");
    }

    public static void RunActiveSkillEffect()
    {
        RunFromCommandLine("active-effects");
    }

    public static void RunPassive()
    {
        RunFromCommandLine("passive");
    }

    public static void RunSkillPassive()
    {
        RunFromCommandLine("active-effects");
    }

    public static void RunMapPassive()
    {
        RunFromCommandLine("map-passive");
    }

    public static void RunTurnLifecycle()
    {
        RunFromCommandLine("turn");
    }

    public static void RunStatusBuff()
    {
        RunFromCommandLine("status");
    }

    public static void RunSelected()
    {
        RunFromCommandLine(GetArg("-suite", "all"));
    }

    static void RunFromCommandLine(string fallbackSuite)
    {
        string suite = GetArg("-suite", fallbackSuite);
        string reportPath = GetArg("-reportPath", AutoReportPath);
        bool logPassed = GetBoolArg("-logPassed", false);
        int exitCode = RunSuite(suite, reportPath, logPassed);
        if (Application.isBatchMode)
            EditorApplication.Exit(exitCode);
    }

    static int RunSuite(string suite, string reportPath, bool logPassed)
    {
        try
        {
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(TestBattleScenePath, OpenSceneMode.Single);
            BattleUnitTestRunner runner = FindRunner();
            runner.reportPath = reportPath;
            runner.logPassedTests = logPassed;

            switch (suite.Trim().ToLowerInvariant())
            {
                case "":
                case "all":
                    runner.RunAllBattleUnitTests();
                    break;
                case "combat":
                    runner.RunCombatUnitTests();
                    break;
                case "turn":
                case "turn-lifecycle":
                    runner.RunTurnLifecycleUnitTests();
                    break;
                case "status":
                case "status-buff":
                    runner.RunStatusBuffUnitTests();
                    break;
                case "map":
                case "movement":
                case "map-movement":
                    runner.RunMapMovementUnitTests();
                    break;
                case "forced":
                case "forced-movement":
                    runner.RunForcedMovementUnitTests();
                    break;
                case "active":
                case "active-effects":
                case "active-skill-effects":
                    runner.RunActiveSkillEffectUnitTests();
                    break;
                case "passive":
                case "passives":
                    runner.RunPassiveUnitTests();
                    break;
                case "skill":
                case "skills":
                case "skill-passive":
                    runner.RunActiveSkillEffectUnitTests();
                    break;
                case "map-passive":
                case "passive-map":
                    runner.RunMapPassiveUnitTests();
                    break;
                default:
                    throw new InvalidOperationException("Unknown battle unit test suite '" + suite + "'.");
            }

            string fullReportPath = FullReportPath(reportPath);
            int failures = ReadReportCount(fullReportPath, "Total failed:");
            int skipped = ReadReportCount(fullReportPath, "Total skipped:");
            Debug.Log("Battle unit test CLI complete. Suite: " + suite + ", failed: " + failures + ", skipped: " + skipped + ", report: " + fullReportPath);
            return failures == 0 ? 0 : 1;
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
            return 1;
        }
    }

    static BattleUnitTestRunner FindRunner()
    {
        BattleUnitTestRunner[] runners = Resources.FindObjectsOfTypeAll<BattleUnitTestRunner>();
        for (int i = 0; i < runners.Length; i++)
        {
            if (runners[i].gameObject.scene.IsValid() && runners[i].gameObject.scene.isLoaded)
                return runners[i];
        }
        throw new InvalidOperationException("Could not find BattleUnitTestRunner in " + TestBattleScenePath + ".");
    }

    static string FullReportPath(string reportPath)
    {
        if (Path.IsPathRooted(reportPath))
            return reportPath;
        string root = Application.dataPath;
        if (reportPath.StartsWith("Assets/") || reportPath.StartsWith("Assets\\"))
            return Path.Combine(root.Substring(0, root.Length - "Assets".Length), reportPath);
        return Path.Combine(root, reportPath);
    }

    static int ReadReportCount(string reportPath, string prefix)
    {
        if (!File.Exists(reportPath))
            return -1;
        string[] lines = File.ReadAllLines(reportPath);
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            if (!lines[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;
            int value;
            if (int.TryParse(lines[i].Substring(prefix.Length).Trim(), out value))
                return value;
        }
        return -1;
    }

    static string GetArg(string argName, string fallback)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], argName, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return fallback;
    }

    static bool GetBoolArg(string argName, bool fallback)
    {
        string value = GetArg(argName, fallback ? "true" : "false");
        bool parsed;
        return bool.TryParse(value, out parsed) ? parsed : fallback;
    }
}
