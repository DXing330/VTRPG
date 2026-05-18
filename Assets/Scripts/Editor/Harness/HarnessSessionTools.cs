using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class HarnessSessionTools
{
    private const string ProgressTemplatePath = "Assets/Docs/Harness/progress_log_template.md";
    private const string SessionLogFolder = "Assets/Docs/Harness/SessionLogs";
    private const string ScenarioChecklistPath = "Assets/Docs/Harness/scenario_checklist.template.json";

    [MenuItem("Tools/Harness/Create Session Log From Template")]
    public static void CreateSessionLogFromTemplate()
    {
        if (!File.Exists(ProgressTemplatePath))
        {
            Debug.LogError("Harness template not found: " + ProgressTemplatePath);
            return;
        }

        Directory.CreateDirectory(SessionLogFolder);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        var targetPath = Path.Combine(SessionLogFolder, "session_log_" + timestamp + ".md");
        File.Copy(ProgressTemplatePath, targetPath, overwrite: false);
        AssetDatabase.Refresh();
        Debug.Log("Created session log: " + targetPath);
    }

    [MenuItem("Tools/Harness/Stamp Scenario Checklist last_updated")]
    public static void StampScenarioChecklistLastUpdated()
    {
        if (!File.Exists(ScenarioChecklistPath))
        {
            Debug.LogError("Scenario checklist not found: " + ScenarioChecklistPath);
            return;
        }

        var content = File.ReadAllText(ScenarioChecklistPath);
        var stamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var updated = Regex.Replace(
            content,
            "\"last_updated\"\\s*:\\s*\"[^\"]*\"",
            "\"last_updated\": \"" + stamp + "\"");

        File.WriteAllText(ScenarioChecklistPath, updated);
        AssetDatabase.Refresh();
        Debug.Log("Updated last_updated fields in: " + ScenarioChecklistPath + " to " + stamp);
    }
}
