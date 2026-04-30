using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class BattleSimulationReportWriter
{
    public static string WriteSuiteResult(BattleTestSuite suite, BattleSimulationSuiteResult result)
    {
        string reportRoot = ProjectRelativePath(suite.reportRoot);
        string reportDirectory = Path.Combine(reportRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(reportDirectory);
        Dictionary<string, ScenarioStats> currentStats = BuildScenarioStats(result);
        BattleSimulationSuiteResult previousResult = LoadPreviousSuiteResult(reportRoot, reportDirectory, result.suiteName);
        Dictionary<string, ScenarioStats> previousStats = previousResult == null ? new Dictionary<string, ScenarioStats>() : BuildScenarioStats(previousResult);
        Dictionary<string, BattleTestScenario> scenarioMap = BuildScenarioMap(suite);
        MatchupMatrixSummary matchupMatrix = BuildMatchupMatrixSummary(result, scenarioMap);

        File.WriteAllText(Path.Combine(reportDirectory, "results.json"), JsonUtility.ToJson(result, true));
        File.WriteAllText(Path.Combine(reportDirectory, "failures.json"), BuildFailuresJson(result));
        File.WriteAllText(Path.Combine(reportDirectory, "summary.md"), BuildMarkdown(result, currentStats));
        File.WriteAllText(Path.Combine(reportDirectory, "review.md"), BuildReviewMarkdown(result, currentStats, previousStats, scenarioMap));
        File.WriteAllText(Path.Combine(reportDirectory, "comparison.md"), BuildComparisonMarkdown(result, previousResult, currentStats, previousStats));
        File.WriteAllText(Path.Combine(reportDirectory, "matchup-matrix.md"), BuildMatchupMatrixMarkdown(result, matchupMatrix));
        File.WriteAllText(Path.Combine(reportDirectory, "matchup-matrix.json"), JsonUtility.ToJson(matchupMatrix, true));
        File.WriteAllText(Path.Combine(reportDirectory, "skill-usage.md"), BuildSkillUsageMarkdown(result));
        File.WriteAllText(Path.Combine(reportDirectory, "ai-reasoning.md"), BuildAiReasoningMarkdown(result));
        File.WriteAllText(Path.Combine(reportDirectory, "debug.md"), BuildDebugMarkdown(result));
        File.WriteAllText(Path.Combine(reportDirectory, "boss-ai-debug.md"), BuildBossAiDebugMarkdown(result));
        File.WriteAllText(Path.Combine(reportDirectory, "dashboard.html"), BuildDashboardHtml(result, currentStats, matchupMatrix));
        WriteCombatLogs(reportDirectory, result);
        WriteFailureLogs(reportDirectory, result);

        return reportDirectory;
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

    static string BuildFailuresJson(BattleSimulationSuiteResult result)
    {
        BattleSimulationFailureReport failureReport = new BattleSimulationFailureReport();
        failureReport.failed = result.failed;
        failureReport.failures = new List<string>(result.failures);
        return JsonUtility.ToJson(failureReport, true);
    }

    static string BuildMarkdown(BattleSimulationSuiteResult result, Dictionary<string, ScenarioStats> statsByScenario)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# Battle Simulation Summary");
        builder.AppendLine();
        builder.AppendLine("- Suite: " + result.suiteName);
        builder.AppendLine("- Started: " + result.startedAt);
        builder.AppendLine("- Finished: " + result.finishedAt);
        builder.AppendLine("- Runs: " + result.runs.Count);
        builder.AppendLine("- Status: " + (result.failed ? "FAILED" : "PASSED"));
        builder.AppendLine();

        if (result.failures.Count > 0)
        {
            builder.AppendLine("## Failures");
            builder.AppendLine();
            for (int i = 0; i < result.failures.Count; i++)
            {
                builder.AppendLine("- " + result.failures[i]);
            }
            builder.AppendLine();
        }

        builder.AppendLine("## Scenario Results");
        builder.AppendLine();
        foreach (KeyValuePair<string, ScenarioStats> entry in statsByScenario)
        {
            ScenarioStats stats = entry.Value;
            builder.AppendLine("### " + entry.Key);
            builder.AppendLine();
            builder.AppendLine("- Runs: " + stats.runs);
            builder.AppendLine("- Failed runs: " + stats.failures);
            builder.AppendLine("- Team 0 wins: " + stats.teamZeroWins);
            builder.AppendLine("- Team 1 wins: " + stats.teamOneWins);
            builder.AppendLine("- Other/unknown wins: " + stats.otherWins);
            builder.AppendLine("- Average rounds: " + stats.AverageRounds().ToString("0.00"));
            builder.AppendLine("- Average log entries: " + stats.AverageLogEntries().ToString("0.00"));
            builder.AppendLine();
            AppendActorAverages(builder, stats);
            builder.AppendLine();
        }

        builder.AppendLine("## Runs");
        builder.AppendLine();
        builder.AppendLine("| Scenario | Run | Seed | Winner | Rounds | Logs | Status |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | --- |");
        for (int i = 0; i < result.runs.Count; i++)
        {
            BattleSimulationRunResult run = result.runs[i];
            builder.AppendLine("| " + EscapeMarkdown(run.scenarioName) + " | " + run.runIndex + " | " + run.seed + " | " + run.winningTeam + " | " + run.rounds + " | " + run.logEntries + " | " + (run.failed ? "FAILED" : "PASSED") + " |");
        }

        return builder.ToString();
    }

    static Dictionary<string, ScenarioStats> BuildScenarioStats(BattleSimulationSuiteResult result)
    {
        Dictionary<string, ScenarioStats> statsByScenario = new Dictionary<string, ScenarioStats>();
        for (int i = 0; i < result.runs.Count; i++)
        {
            BattleSimulationRunResult run = result.runs[i];
            if (!statsByScenario.ContainsKey(run.scenarioName))
            {
                statsByScenario.Add(run.scenarioName, new ScenarioStats());
            }
            statsByScenario[run.scenarioName].Add(run);
        }
        return statsByScenario;
    }

    static Dictionary<string, BattleTestScenario> BuildScenarioMap(BattleTestSuite suite)
    {
        Dictionary<string, BattleTestScenario> scenarioMap = new Dictionary<string, BattleTestScenario>();
        for (int i = 0; i < suite.scenarios.Count; i++)
        {
            if (suite.scenarios[i] == null)
            {
                continue;
            }
            string name = suite.scenarios[i].ScenarioName();
            if (!scenarioMap.ContainsKey(name))
            {
                scenarioMap.Add(name, suite.scenarios[i]);
            }
        }
        return scenarioMap;
    }

    static MatchupMatrixSummary BuildMatchupMatrixSummary(BattleSimulationSuiteResult result, Dictionary<string, BattleTestScenario> scenarioMap)
    {
        MatchupMatrixSummary summary = new MatchupMatrixSummary();
        summary.labelMatrix = BuildMatchupMatrix(result, scenarioMap, false);
        summary.groupMatrix = BuildMatchupMatrix(result, scenarioMap, true);
        return summary;
    }

    static MatchupMatrixData BuildMatchupMatrix(BattleSimulationSuiteResult result, Dictionary<string, BattleTestScenario> scenarioMap, bool useGroups)
    {
        MatchupMatrixData matrix = new MatchupMatrixData();
        matrix.matrixKind = useGroups ? "group" : "label";

        Dictionary<string, MatrixCellStats> cells = new Dictionary<string, MatrixCellStats>();
        HashSet<string> labels = new HashSet<string>();
        for (int i = 0; i < result.runs.Count; i++)
        {
            BattleSimulationRunResult run = result.runs[i];
            if (run == null || run.failed)
            {
                continue;
            }

            BattleTestScenario scenario;
            if (!scenarioMap.TryGetValue(run.scenarioName, out scenario) || scenario == null || !scenario.includeInMatchupMatrix)
            {
                continue;
            }

            string left = useGroups ? scenario.MatrixGroup(0) : scenario.MatrixLabel(0);
            string right = useGroups ? scenario.MatrixGroup(1) : scenario.MatrixLabel(1);
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            {
                continue;
            }

            labels.Add(left);
            labels.Add(right);
            AddMatrixObservation(cells, left, right, run, 0);
            if (left != right)
            {
                AddMatrixObservation(cells, right, left, run, 1);
            }
        }

        matrix.labels = new List<string>(labels);
        matrix.labels.Sort(StringComparer.OrdinalIgnoreCase);

        foreach (string row in matrix.labels)
        {
            foreach (string column in matrix.labels)
            {
                string key = row + "|" + column;
                MatrixCellStats cell;
                if (cells.TryGetValue(key, out cell))
                {
                    matrix.cells.Add(cell);
                }
            }
        }
        return matrix;
    }

    static void AddMatrixObservation(Dictionary<string, MatrixCellStats> cells, string rowLabel, string columnLabel, BattleSimulationRunResult run, int rowTeam)
    {
        string key = rowLabel + "|" + columnLabel;
        MatrixCellStats cell;
        if (!cells.TryGetValue(key, out cell))
        {
            cell = new MatrixCellStats();
            cell.rowLabel = rowLabel;
            cell.columnLabel = columnLabel;
            cells.Add(key, cell);
        }

        cell.runs++;
        cell.totalRounds += run.rounds;
        if (run.winningTeam == rowTeam)
        {
            cell.rowWins++;
        }
        else if (run.winningTeam == 0 || run.winningTeam == 1)
        {
            cell.rowLosses++;
        }
        else
        {
            cell.unknown++;
        }
    }

    static BattleSimulationSuiteResult LoadPreviousSuiteResult(string reportRoot, string currentReportDirectory, string suiteName)
    {
        if (!Directory.Exists(reportRoot))
        {
            return null;
        }

        string[] directories = Directory.GetDirectories(reportRoot);
        Array.Sort(directories);
        for (int i = directories.Length - 1; i >= 0; i--)
        {
            if (Path.GetFullPath(directories[i]) == Path.GetFullPath(currentReportDirectory))
            {
                continue;
            }
            string resultsPath = Path.Combine(directories[i], "results.json");
            if (!File.Exists(resultsPath))
            {
                continue;
            }
            try
            {
                BattleSimulationSuiteResult previous = JsonUtility.FromJson<BattleSimulationSuiteResult>(File.ReadAllText(resultsPath));
                if (previous != null && previous.suiteName == suiteName)
                {
                    return previous;
                }
            }
            catch
            {
                // Ignore malformed or old report files.
            }
        }
        return null;
    }

    static string EscapeMarkdown(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }
        return value.Replace("|", "\\|");
    }

    static void AppendActorAverages(StringBuilder builder, ScenarioStats stats)
    {
        builder.AppendLine("Actor averages:");
        builder.AppendLine();
        builder.AppendLine("| Actor | Team | Base Stats | Avg Damage Dealt | Avg Damage Taken | Runs Seen |");
        builder.AppendLine("| --- | ---: | --- | ---: | ---: | ---: |");
        foreach (KeyValuePair<string, ActorAggregateStats> entry in stats.actorStats)
        {
            ActorAggregateStats actor = entry.Value;
            builder.AppendLine("| "
                + EscapeMarkdown(actor.actorName) + " | "
                + actor.team + " | "
                + EscapeMarkdown(actor.baseStats) + " | "
                + actor.AverageDamageDealt().ToString("0.00") + " | "
                + actor.AverageDamageTaken().ToString("0.00") + " | "
                + actor.runsSeen + " |");
        }
    }

    static void WriteFailureLogs(string reportDirectory, BattleSimulationSuiteResult result)
    {
        for (int i = 0; i < result.runs.Count; i++)
        {
            BattleSimulationRunResult run = result.runs[i];
            if (!run.failed || run.combatLogs.Count == 0)
            {
                continue;
            }
            string fileName = SanitizeFileName(run.scenarioName + "-run-" + run.runIndex + "-combat-log.txt");
            File.WriteAllLines(Path.Combine(reportDirectory, fileName), run.combatLogs.ToArray());
        }
    }

    static void WriteCombatLogs(string reportDirectory, BattleSimulationSuiteResult result)
    {
        string combatLogDirectory = Path.Combine(reportDirectory, "combat-logs");
        Directory.CreateDirectory(combatLogDirectory);
        for (int i = 0; i < result.runs.Count; i++)
        {
            BattleSimulationRunResult run = result.runs[i];
            string fileName = CombatLogFileName(run);
            File.WriteAllText(Path.Combine(combatLogDirectory, fileName), BuildRunCombatLogMarkdown(run));
        }
    }

    static string BuildRunCombatLogMarkdown(BattleSimulationRunResult run)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# Combat Log");
        builder.AppendLine();
        builder.AppendLine("- Scenario: " + run.scenarioName);
        builder.AppendLine("- Run: " + run.runIndex);
        builder.AppendLine("- Seed: " + run.seed);
        builder.AppendLine("- Winner: Team " + run.winningTeam);
        builder.AppendLine("- Terrain: " + run.terrain);
        builder.AppendLine("- Weather: " + run.weather);
        builder.AppendLine("- Time: " + run.time);
        builder.AppendLine();

        int currentRound = int.MinValue;
        int currentTurn = int.MinValue;
        bool[] usedReasoning = run.aiReasoning == null ? new bool[0] : new bool[run.aiReasoning.Count];
        for (int i = 0; i < run.combatLogEntries.Count; i++)
        {
            BattleSimulationCombatLogEntry entry = run.combatLogEntries[i];
            if (entry.round != currentRound)
            {
                currentRound = entry.round;
                currentTurn = int.MinValue;
                builder.AppendLine("## Round " + entry.round);
                builder.AppendLine();
            }
            if (entry.turn != currentTurn)
            {
                currentTurn = entry.turn;
                builder.AppendLine("### Turn " + (entry.turn + 1));
                builder.AppendLine();
            }

            if (!string.IsNullOrEmpty(entry.actionType))
            {
                builder.AppendLine("- **" + entry.actorName + "** " + entry.actionType.ToLower() + ": `" + entry.skillName + "`");
                BattleSimulationAiReasoningEntry reasoning = FindReasoningForLogEntry(run, entry, usedReasoning);
                if (reasoning != null)
                {
                    builder.AppendLine("  - AI: " + ReasoningSentence(reasoning));
                }
            }
            else
            {
                builder.AppendLine("- " + entry.text);
            }
        }

        return builder.ToString();
    }

    static BattleSimulationAiReasoningEntry FindReasoningForLogEntry(BattleSimulationRunResult run, BattleSimulationCombatLogEntry entry, bool[] usedReasoning)
    {
        if (run.aiReasoning == null)
        {
            return null;
        }

        for (int i = 0; i < run.aiReasoning.Count; i++)
        {
            if (i < usedReasoning.Length && usedReasoning[i])
            {
                continue;
            }

            BattleSimulationAiReasoningEntry reasoning = run.aiReasoning[i];
            if (reasoning.round == entry.round
                && reasoning.turn == entry.turn
                && reasoning.actorName == entry.actorName
                && reasoning.actionType == entry.actionType
                && reasoning.skillName == entry.skillName)
            {
                if (i < usedReasoning.Length)
                {
                    usedReasoning[i] = true;
                }
                return reasoning;
            }
        }
        return null;
    }

    static string ReasoningSentence(BattleSimulationAiReasoningEntry reasoning)
    {
        if (reasoning == null)
        {
            return "";
        }

        string form = string.IsNullOrEmpty(reasoning.inferredForm) ? "unknown form" : "`" + reasoning.inferredForm + "`";
        string condition = string.IsNullOrEmpty(reasoning.condition) ? "unknown condition" : "`" + reasoning.condition + "|" + reasoning.conditionSpecifics + "`";
        string rule = string.IsNullOrEmpty(reasoning.rotationRule) ? "no matched rotation block" : "`" + reasoning.rotationRule + "`";
        return reasoning.confidence + " from " + form + " using " + condition + " -> " + rule + ". " + reasoning.note;
    }

    static string BuildMatchupMatrixMarkdown(BattleSimulationSuiteResult result, MatchupMatrixSummary matchupMatrix)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# Matchup Matrix");
        builder.AppendLine();
        builder.AppendLine("- Suite: " + result.suiteName);
        builder.AppendLine("- Status: row-side win rate against the column side.");
        builder.AppendLine("- Cell format: `win rate | row wins-losses-unknown | avg rounds | runs`.");
        builder.AppendLine();

        AppendMatchupMatrixSection(builder, "Roster Matchups", matchupMatrix.labelMatrix);
        AppendMatchupMatrixSection(builder, "Group Matchups", matchupMatrix.groupMatrix);
        return builder.ToString();
    }

    static void AppendMatchupMatrixSection(StringBuilder builder, string title, MatchupMatrixData matrix)
    {
        builder.AppendLine("## " + title);
        builder.AppendLine();
        if (matrix == null || matrix.labels == null || matrix.labels.Count == 0 || matrix.cells == null || matrix.cells.Count == 0)
        {
            builder.AppendLine("- No matchup data was collected.");
            builder.AppendLine();
            return;
        }

        Dictionary<string, MatrixCellStats> cellMap = new Dictionary<string, MatrixCellStats>();
        for (int i = 0; i < matrix.cells.Count; i++)
        {
            MatrixCellStats cell = matrix.cells[i];
            cellMap[cell.rowLabel + "|" + cell.columnLabel] = cell;
        }

        builder.Append("| Row vs Column |");
        for (int i = 0; i < matrix.labels.Count; i++)
        {
            builder.Append(" " + EscapeMarkdown(matrix.labels[i]) + " |");
        }
        builder.AppendLine();

        builder.Append("| --- |");
        for (int i = 0; i < matrix.labels.Count; i++)
        {
            builder.Append(" --- |");
        }
        builder.AppendLine();

        for (int rowIndex = 0; rowIndex < matrix.labels.Count; rowIndex++)
        {
            string rowLabel = matrix.labels[rowIndex];
            builder.Append("| " + EscapeMarkdown(rowLabel) + " |");
            for (int colIndex = 0; colIndex < matrix.labels.Count; colIndex++)
            {
                string colLabel = matrix.labels[colIndex];
                MatrixCellStats cell;
                if (cellMap.TryGetValue(rowLabel + "|" + colLabel, out cell))
                {
                    builder.Append(" " + EscapeMarkdown(MatrixCellValue(cell)) + " |");
                }
                else
                {
                    builder.Append(" - |");
                }
            }
            builder.AppendLine();
        }
        builder.AppendLine();

        builder.AppendLine("| Row | Column | Row Win Rate | Record | Avg Rounds | Runs |");
        builder.AppendLine("| --- | --- | ---: | --- | ---: | ---: |");
        for (int i = 0; i < matrix.cells.Count; i++)
        {
            MatrixCellStats cell = matrix.cells[i];
            builder.AppendLine("| "
                + EscapeMarkdown(cell.rowLabel) + " | "
                + EscapeMarkdown(cell.columnLabel) + " | "
                + (cell.RowWinRate() * 100f).ToString("0.0") + "% | "
                + cell.rowWins + "-" + cell.rowLosses + "-" + cell.unknown + " | "
                + cell.AverageRounds().ToString("0.00") + " | "
                + cell.runs + " |");
        }
        builder.AppendLine();
    }

    static string MatrixCellValue(MatrixCellStats cell)
    {
        if (cell == null || cell.runs <= 0)
        {
            return "-";
        }
        return (cell.RowWinRate() * 100f).ToString("0")
            + "% | "
            + cell.rowWins + "-" + cell.rowLosses + "-" + cell.unknown
            + " | "
            + cell.AverageRounds().ToString("0.0")
            + "r | "
            + cell.runs;
    }

    static string BuildSkillUsageMarkdown(BattleSimulationSuiteResult result)
    {
        Dictionary<string, SkillUsageStats> skillStats = new Dictionary<string, SkillUsageStats>();
        for (int i = 0; i < result.runs.Count; i++)
        {
            BattleSimulationRunResult run = result.runs[i];
            for (int j = 0; j < run.combatLogEntries.Count; j++)
            {
                BattleSimulationCombatLogEntry entry = run.combatLogEntries[j];
                if (string.IsNullOrEmpty(entry.actionType) || string.IsNullOrEmpty(entry.skillName))
                {
                    continue;
                }
                string key = run.scenarioName + "|" + entry.actorName + "|" + entry.actionType + "|" + entry.skillName;
                if (!skillStats.ContainsKey(key))
                {
                    skillStats.Add(key, new SkillUsageStats(run.scenarioName, entry.actorName, entry.actionType, entry.skillName));
                }
                skillStats[key].count++;
            }
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# Skill Usage Summary");
        builder.AppendLine();
        builder.AppendLine("| Scenario | Actor | Action | Skill | Uses |");
        builder.AppendLine("| --- | --- | --- | --- | ---: |");
        foreach (KeyValuePair<string, SkillUsageStats> entry in skillStats)
        {
            SkillUsageStats usage = entry.Value;
            builder.AppendLine("| "
                + EscapeMarkdown(usage.scenarioName) + " | "
                + EscapeMarkdown(usage.actorName) + " | "
                + usage.actionType + " | "
                + EscapeMarkdown(usage.skillName) + " | "
                + usage.count + " |");
        }
        return builder.ToString();
    }

    static string BuildAiReasoningMarkdown(BattleSimulationSuiteResult result)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# AI Reasoning Trace");
        builder.AppendLine();
        builder.AppendLine("- Suite: " + result.suiteName);
        builder.AppendLine("- Note: this is inferred from observed combat-log actions and boss rotation data. It does not instrument runtime condition values.");
        builder.AppendLine();

        bool foundReasoning = false;
        for (int i = 0; i < result.runs.Count; i++)
        {
            BattleSimulationRunResult run = result.runs[i];
            if (run.aiReasoning == null || run.aiReasoning.Count == 0)
            {
                continue;
            }

            foundReasoning = true;
            builder.AppendLine("## " + run.scenarioName + " run " + run.runIndex);
            builder.AppendLine();
            builder.AppendLine("- Seed: " + run.seed);
            builder.AppendLine("- Winner: Team " + run.winningTeam);
            builder.AppendLine();
            builder.AppendLine("| Round | Turn | Actor | Observed Action | Inferred Form | Condition | Rotation Action | Rule | Confidence |");
            builder.AppendLine("| ---: | ---: | --- | --- | --- | --- | --- | --- | --- |");
            for (int j = 0; j < run.aiReasoning.Count; j++)
            {
                BattleSimulationAiReasoningEntry entry = run.aiReasoning[j];
                builder.AppendLine("| "
                    + entry.round + " | "
                    + (entry.turn + 1) + " | "
                    + EscapeMarkdown(entry.actorName) + " | "
                    + EscapeMarkdown(entry.actionType + " " + entry.skillName) + " | "
                    + EscapeMarkdown(entry.inferredForm) + " | "
                    + EscapeMarkdown(entry.condition + "|" + entry.conditionSpecifics) + " | "
                    + EscapeMarkdown(entry.rotationAction + " " + entry.rotationTarget) + " | "
                    + EscapeMarkdown(entry.rotationRule) + " | "
                    + EscapeMarkdown(entry.confidence) + " |");
            }
            builder.AppendLine();
        }

        if (!foundReasoning)
        {
            builder.AppendLine("No inferred AI reasoning was captured.");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    static string BuildReviewMarkdown(BattleSimulationSuiteResult result, Dictionary<string, ScenarioStats> currentStats, Dictionary<string, ScenarioStats> previousStats, Dictionary<string, BattleTestScenario> scenarioMap)
    {
        List<string> baselineWarnings = BuildBaselineWarnings(currentStats, scenarioMap);
        List<string> comparisonWarnings = BuildComparisonHighlights(currentStats, previousStats);
        List<string> outliers = BuildOutlierHighlights(result, currentStats);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# Battle Test Review");
        builder.AppendLine();
        builder.AppendLine("- Suite: " + result.suiteName);
        builder.AppendLine("- Status: " + (result.failed || baselineWarnings.Count > 0 ? "REVIEW NEEDED" : "OK"));
        builder.AppendLine("- Technical failures: " + result.failures.Count);
        builder.AppendLine("- Baseline warnings: " + baselineWarnings.Count);
        builder.AppendLine("- Comparison highlights: " + comparisonWarnings.Count);
        builder.AppendLine("- Outliers: " + outliers.Count);
        builder.AppendLine();

        AppendReviewSection(builder, "Technical Failures", result.failures);
        AppendReviewSection(builder, "Baseline Warnings", baselineWarnings);
        AppendReviewSection(builder, "Largest Changes From Previous Run", comparisonWarnings);
        AppendReviewSection(builder, "Outliers To Inspect", outliers);

        builder.AppendLine("## Useful Files");
        builder.AppendLine();
        builder.AppendLine("- `summary.md`: full scenario and actor averages");
        builder.AppendLine("- `comparison.md`: latest-vs-previous deltas");
        builder.AppendLine("- `matchup-matrix.md`: roster and group win-rate matrices");
        builder.AppendLine("- `skill-usage.md`: skill/action counts by actor");
        builder.AppendLine("- `ai-reasoning.md`: inferred boss rotation reasoning from combat-log actions");
        builder.AppendLine("- `debug.md`: exception stack traces and setup diagnostics for failed runs");
        builder.AppendLine("- `boss-ai-debug.md`: boss rotation, form-change, live turn-actor state, and inferred crash context");
        builder.AppendLine("- `combat-logs/`: per-run round and turn timelines");
        builder.AppendLine("- `dashboard.html`: browser-friendly overview");
        return builder.ToString();
    }

    static string BuildDebugMarkdown(BattleSimulationSuiteResult result)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# Battle Test Debug");
        builder.AppendLine();
        builder.AppendLine("- Suite: " + result.suiteName);
        builder.AppendLine("- Failed runs: " + result.failures.Count);
        builder.AppendLine();

        bool foundFailure = false;
        for (int i = 0; i < result.runs.Count; i++)
        {
            BattleSimulationRunResult run = result.runs[i];
            if (!run.failed)
            {
                continue;
            }
            foundFailure = true;
            AppendRunDebug(builder, run);
        }

        if (!foundFailure)
        {
            builder.AppendLine("No failed runs.");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    static string BuildBossAiDebugMarkdown(BattleSimulationSuiteResult result)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# Boss AI Debug");
        builder.AppendLine();
        builder.AppendLine("- Suite: " + result.suiteName);
        builder.AppendLine("- Runs: " + result.runs.Count);
        builder.AppendLine();

        bool foundBossDebug = false;
        for (int i = 0; i < result.runs.Count; i++)
        {
            BattleSimulationRunResult run = result.runs[i];
            if ((run.bossAiDebug == null || run.bossAiDebug.Count == 0)
                && (run.crashInference == null || run.crashInference.Count == 0)
                && (run.aiReasoning == null || run.aiReasoning.Count == 0))
            {
                continue;
            }

            foundBossDebug = true;
            builder.AppendLine("## " + run.scenarioName + " run " + run.runIndex);
            builder.AppendLine();
            builder.AppendLine("- Seed: " + run.seed);
            builder.AppendLine("- Status: " + (run.failed ? "FAILED" : "PASSED"));
            builder.AppendLine("- Failure: " + run.failureReason);
            builder.AppendLine("- Stage: " + run.failureStage);
            builder.AppendLine();
            AppendDebugList(builder, "Boss AI Data", run.bossAiDebug);
            AppendAiReasoningDebug(builder, run);
            AppendDebugList(builder, "Crash Inference", run.crashInference);
        }

        if (!foundBossDebug)
        {
            builder.AppendLine("No boss AI debug data was captured.");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    static void AppendAiReasoningDebug(StringBuilder builder, BattleSimulationRunResult run)
    {
        if (run.aiReasoning == null || run.aiReasoning.Count == 0)
        {
            return;
        }

        builder.AppendLine("### Inferred AI Trace");
        builder.AppendLine();
        for (int i = 0; i < run.aiReasoning.Count; i++)
        {
            BattleSimulationAiReasoningEntry entry = run.aiReasoning[i];
            builder.AppendLine("- Round " + entry.round
                + " turn " + (entry.turn + 1)
                + ": " + entry.actorName
                + " observed `" + entry.actionType + " " + entry.skillName + "` -> "
                + ReasoningSentence(entry));
        }
        builder.AppendLine();
    }

    static void AppendRunDebug(StringBuilder builder, BattleSimulationRunResult run)
    {
        builder.AppendLine("## " + run.scenarioName + " run " + run.runIndex);
        builder.AppendLine();
        builder.AppendLine("- Seed: " + run.seed);
        builder.AppendLine("- Failure: " + run.failureReason);
        builder.AppendLine("- Stage: " + run.failureStage);
        builder.AppendLine("- Exception type: " + run.exceptionType);
        builder.AppendLine("- Exception message: " + run.exceptionMessage);
        builder.AppendLine("- Winner: " + run.winningTeam);
        builder.AppendLine("- Rounds: " + run.rounds);
        builder.AppendLine("- Log entries: " + run.logEntries);
        builder.AppendLine();

        AppendDebugList(builder, "Debug Steps", run.debugSteps);
        AppendDebugList(builder, "Scenario", run.scenarioDebug);
        AppendActorDebug(builder, run);
        AppendDebugList(builder, "Boss AI Data", run.bossAiDebug);
        AppendDebugList(builder, "Crash Inference", run.crashInference);
        AppendDebugList(builder, "Simulator Snapshots", run.simulatorDebug);
        AppendDebugList(builder, "Party Lists", run.partyDebug);
        AppendDebugList(builder, "Combat Log Tail", run.combatLogTail);
        AppendDebugList(builder, "Unity Errors", run.errors);

        if (!string.IsNullOrEmpty(run.exceptionStackTrace))
        {
            builder.AppendLine("### Stack Trace");
            builder.AppendLine();
            builder.AppendLine("```");
            builder.AppendLine(run.exceptionStackTrace);
            builder.AppendLine("```");
            builder.AppendLine();
        }
    }

    static void AppendActorDebug(StringBuilder builder, BattleSimulationRunResult run)
    {
        builder.AppendLine("### Actor Data");
        builder.AppendLine();
        if (run.actorDebug.Count == 0)
        {
            builder.AppendLine("- None");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Party | Index | Display | Sprite | Key Exists | Override | Fields | Stats Length | Equipment Length |");
        builder.AppendLine("| --- | ---: | --- | --- | --- | --- | ---: | ---: | ---: |");
        for (int i = 0; i < run.actorDebug.Count; i++)
        {
            BattleSimulationActorDebugInfo actor = run.actorDebug[i];
            builder.AppendLine("| "
                + EscapeMarkdown(actor.partyName) + " | "
                + actor.index + " | "
                + EscapeMarkdown(actor.displayName) + " | "
                + EscapeMarkdown(actor.spriteName) + " | "
                + actor.actorStatsKeyExists + " | "
                + actor.usesStatsOverride + " | "
                + actor.statsFieldCount + " | "
                + actor.statsLength + " | "
                + actor.equipmentLength + " |");
        }
        builder.AppendLine();

        for (int i = 0; i < run.actorDebug.Count; i++)
        {
            BattleSimulationActorDebugInfo actor = run.actorDebug[i];
            builder.AppendLine("#### " + actor.partyName + " " + actor.index + ": " + actor.displayName);
            builder.AppendLine();
            builder.AppendLine("- Sprite: `" + actor.spriteName + "`");
            builder.AppendLine("- ID: `" + actor.id + "`");
            builder.AppendLine("- Equipment: `" + EscapeBackticks(actor.equipment) + "`");
            builder.AppendLine("- Stats preview: `" + EscapeBackticks(actor.statsPreview) + "`");
            builder.AppendLine();
            builder.AppendLine("Stats fields:");
            builder.AppendLine();
            if (actor.indexedStats.Count == 0)
            {
                builder.AppendLine("- None");
            }
            else
            {
                for (int statIndex = 0; statIndex < actor.indexedStats.Count; statIndex++)
                {
                    builder.AppendLine("- `" + EscapeBackticks(actor.indexedStats[statIndex]) + "`");
                }
            }
            builder.AppendLine();
        }
    }

    static void AppendDebugList(StringBuilder builder, string title, List<string> lines)
    {
        builder.AppendLine("### " + title);
        builder.AppendLine();
        if (lines == null || lines.Count == 0)
        {
            builder.AppendLine("- None");
            builder.AppendLine();
            return;
        }
        for (int i = 0; i < lines.Count; i++)
        {
            builder.AppendLine("- " + EscapeMarkdown(lines[i]));
        }
        builder.AppendLine();
    }

    static string EscapeBackticks(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }
        return value.Replace("`", "'");
    }

    static void AppendReviewSection(StringBuilder builder, string title, List<string> items)
    {
        builder.AppendLine("## " + title);
        builder.AppendLine();
        if (items.Count == 0)
        {
            builder.AppendLine("- None");
            builder.AppendLine();
            return;
        }
        for (int i = 0; i < items.Count; i++)
        {
            builder.AppendLine("- " + items[i]);
        }
        builder.AppendLine();
    }

    static string BuildComparisonMarkdown(BattleSimulationSuiteResult result, BattleSimulationSuiteResult previousResult, Dictionary<string, ScenarioStats> currentStats, Dictionary<string, ScenarioStats> previousStats)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# Battle Test Comparison");
        builder.AppendLine();
        if (previousResult == null)
        {
            builder.AppendLine("No previous report for this suite was found.");
            return builder.ToString();
        }

        builder.AppendLine("- Current started: " + result.startedAt);
        builder.AppendLine("- Previous started: " + previousResult.startedAt);
        builder.AppendLine();
        builder.AppendLine("| Scenario | Team 0 Win Rate | Team 1 Win Rate | Avg Rounds | Avg Logs |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: |");
        foreach (KeyValuePair<string, ScenarioStats> entry in currentStats)
        {
            ScenarioStats current = entry.Value;
            ScenarioStats previous;
            if (!previousStats.TryGetValue(entry.Key, out previous))
            {
                builder.AppendLine("| " + EscapeMarkdown(entry.Key) + " | new | new | new | new |");
                continue;
            }
            builder.AppendLine("| "
                + EscapeMarkdown(entry.Key) + " | "
                + FormatDelta(current.TeamZeroWinRate(), previous.TeamZeroWinRate(), true) + " | "
                + FormatDelta(current.TeamOneWinRate(), previous.TeamOneWinRate(), true) + " | "
                + FormatDelta(current.AverageRounds(), previous.AverageRounds(), false) + " | "
                + FormatDelta(current.AverageLogEntries(), previous.AverageLogEntries(), false) + " |");
        }

        builder.AppendLine();
        builder.AppendLine("## Actor Damage Changes");
        builder.AppendLine();
        builder.AppendLine("| Scenario | Actor | Avg Dealt | Avg Taken |");
        builder.AppendLine("| --- | --- | ---: | ---: |");
        foreach (KeyValuePair<string, ScenarioStats> entry in currentStats)
        {
            ScenarioStats previousScenario;
            if (!previousStats.TryGetValue(entry.Key, out previousScenario))
            {
                continue;
            }
            foreach (KeyValuePair<string, ActorAggregateStats> actorEntry in entry.Value.actorStats)
            {
                ActorAggregateStats previousActor;
                if (!previousScenario.actorStats.TryGetValue(actorEntry.Key, out previousActor))
                {
                    continue;
                }
                ActorAggregateStats currentActor = actorEntry.Value;
                builder.AppendLine("| "
                    + EscapeMarkdown(entry.Key) + " | "
                    + EscapeMarkdown(currentActor.actorName) + " | "
                    + FormatDelta(currentActor.AverageDamageDealt(), previousActor.AverageDamageDealt(), false) + " | "
                    + FormatDelta(currentActor.AverageDamageTaken(), previousActor.AverageDamageTaken(), false) + " |");
            }
        }
        return builder.ToString();
    }

    static string BuildDashboardHtml(BattleSimulationSuiteResult result, Dictionary<string, ScenarioStats> currentStats, MatchupMatrixSummary matchupMatrix)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("<!doctype html><html><head><meta charset=\"utf-8\"><title>Battle Test Dashboard</title>");
        builder.AppendLine("<style>body{font-family:Arial,sans-serif;margin:24px;}table{border-collapse:collapse;margin-bottom:24px;}td,th{border:1px solid #ccc;padding:6px 8px;}th{background:#eee;} .fail{color:#b00020;font-weight:bold;} .ok{color:#006b3c;font-weight:bold;}</style>");
        builder.AppendLine("</head><body>");
        builder.AppendLine("<h1>Battle Test Dashboard</h1>");
        builder.AppendLine("<p>Suite: " + Html(result.suiteName) + "</p>");
        builder.AppendLine("<p>Status: <span class=\"" + (result.failed ? "fail" : "ok") + "\">" + (result.failed ? "FAILED" : "PASSED") + "</span></p>");
        builder.AppendLine("<table><tr><th>Scenario</th><th>Runs</th><th>Team 0 Wins</th><th>Team 1 Wins</th><th>Avg Rounds</th><th>Avg Logs</th></tr>");
        foreach (KeyValuePair<string, ScenarioStats> entry in currentStats)
        {
            ScenarioStats stats = entry.Value;
            builder.AppendLine("<tr><td>" + Html(entry.Key) + "</td><td>" + stats.runs + "</td><td>" + stats.teamZeroWins + "</td><td>" + stats.teamOneWins + "</td><td>" + stats.AverageRounds().ToString("0.00") + "</td><td>" + stats.AverageLogEntries().ToString("0.00") + "</td></tr>");
        }
        builder.AppendLine("</table>");
        AppendDashboardMatrix(builder, "Roster Matchups", matchupMatrix == null ? null : matchupMatrix.labelMatrix);
        AppendDashboardMatrix(builder, "Group Matchups", matchupMatrix == null ? null : matchupMatrix.groupMatrix);
        builder.AppendLine("<p>Open <code>review.md</code>, <code>matchup-matrix.md</code>, <code>skill-usage.md</code>, or <code>combat-logs/</code> for deeper inspection.</p>");
        builder.AppendLine("</body></html>");
        return builder.ToString();
    }

    static List<string> BuildBaselineWarnings(Dictionary<string, ScenarioStats> currentStats, Dictionary<string, BattleTestScenario> scenarioMap)
    {
        List<string> warnings = new List<string>();
        foreach (KeyValuePair<string, ScenarioStats> entry in currentStats)
        {
            BattleTestScenario scenario;
            if (!scenarioMap.TryGetValue(entry.Key, out scenario) || !scenario.baselineEnabled)
            {
                continue;
            }
            ScenarioStats stats = entry.Value;
            CheckRange(warnings, entry.Key, "Team 0 win rate", stats.TeamZeroWinRate(), scenario.minTeamZeroWinRate, scenario.maxTeamZeroWinRate, true);
            CheckRange(warnings, entry.Key, "Team 1 win rate", stats.TeamOneWinRate(), scenario.minTeamOneWinRate, scenario.maxTeamOneWinRate, true);
            CheckRange(warnings, entry.Key, "average rounds", stats.AverageRounds(), scenario.minAverageRounds, scenario.maxAverageRounds, false);
            CheckRange(warnings, entry.Key, "failure rate", stats.FailureRate(), -1f, scenario.maxFailureRate, true);
            CheckRange(warnings, entry.Key, "unknown win rate", stats.UnknownWinRate(), -1f, scenario.maxUnknownWinRate, true);
            CheckActorBaselines(warnings, entry.Key, stats, scenario);
        }
        return warnings;
    }

    static void CheckActorBaselines(List<string> warnings, string scenarioName, ScenarioStats stats, BattleTestScenario scenario)
    {
        for (int i = 0; i < scenario.actorBaselines.Count; i++)
        {
            BattleTestActorBaseline baseline = scenario.actorBaselines[i];
            ActorAggregateStats actor = FindActorStats(stats, baseline.actorName, baseline.team);
            if (actor == null)
            {
                warnings.Add(scenarioName + ": actor baseline target not found: " + baseline.actorName);
                continue;
            }
            CheckRange(warnings, scenarioName, actor.actorName + " avg damage dealt", actor.AverageDamageDealt(), baseline.minAverageDamageDealt, baseline.maxAverageDamageDealt, false);
            CheckRange(warnings, scenarioName, actor.actorName + " avg damage taken", actor.AverageDamageTaken(), baseline.minAverageDamageTaken, baseline.maxAverageDamageTaken, false);
        }
    }

    static ActorAggregateStats FindActorStats(ScenarioStats stats, string actorName, int team)
    {
        foreach (KeyValuePair<string, ActorAggregateStats> entry in stats.actorStats)
        {
            if (team >= 0 && entry.Value.team != team)
            {
                continue;
            }
            if (string.IsNullOrEmpty(actorName) || entry.Value.actorName == actorName)
            {
                return entry.Value;
            }
        }
        return null;
    }

    static void CheckRange(List<string> warnings, string scenarioName, string label, float value, float min, float max, bool rate)
    {
        min = NormalizeRateThreshold(min, rate);
        max = NormalizeRateThreshold(max, rate);
        if (min >= 0f && value < min)
        {
            warnings.Add(scenarioName + ": " + label + " " + FormatValue(value, rate) + " below baseline " + FormatValue(min, rate));
        }
        if (max >= 0f && value > max)
        {
            warnings.Add(scenarioName + ": " + label + " " + FormatValue(value, rate) + " above baseline " + FormatValue(max, rate));
        }
    }

    static List<string> BuildComparisonHighlights(Dictionary<string, ScenarioStats> currentStats, Dictionary<string, ScenarioStats> previousStats)
    {
        List<ChangeHighlight> changes = new List<ChangeHighlight>();
        foreach (KeyValuePair<string, ScenarioStats> entry in currentStats)
        {
            ScenarioStats previous;
            if (!previousStats.TryGetValue(entry.Key, out previous))
            {
                continue;
            }
            AddChange(changes, entry.Key + ": Team 0 win rate", entry.Value.TeamZeroWinRate(), previous.TeamZeroWinRate(), true);
            AddChange(changes, entry.Key + ": Team 1 win rate", entry.Value.TeamOneWinRate(), previous.TeamOneWinRate(), true);
            AddChange(changes, entry.Key + ": average rounds", entry.Value.AverageRounds(), previous.AverageRounds(), false);
            foreach (KeyValuePair<string, ActorAggregateStats> actorEntry in entry.Value.actorStats)
            {
                ActorAggregateStats previousActor;
                if (!previous.actorStats.TryGetValue(actorEntry.Key, out previousActor))
                {
                    continue;
                }
                AddChange(changes, entry.Key + ": " + actorEntry.Value.actorName + " avg damage dealt", actorEntry.Value.AverageDamageDealt(), previousActor.AverageDamageDealt(), false);
                AddChange(changes, entry.Key + ": " + actorEntry.Value.actorName + " avg damage taken", actorEntry.Value.AverageDamageTaken(), previousActor.AverageDamageTaken(), false);
            }
        }
        changes.Sort((a, b) => b.absoluteDelta.CompareTo(a.absoluteDelta));
        List<string> highlights = new List<string>();
        for (int i = 0; i < Mathf.Min(10, changes.Count); i++)
        {
            highlights.Add(changes[i].label + " changed " + FormatSigned(changes[i].delta, changes[i].rate) + " (" + FormatValue(changes[i].previous, changes[i].rate) + " -> " + FormatValue(changes[i].current, changes[i].rate) + ")");
        }
        return highlights;
    }

    static List<string> BuildOutlierHighlights(BattleSimulationSuiteResult result, Dictionary<string, ScenarioStats> currentStats)
    {
        List<string> outliers = new List<string>();
        foreach (KeyValuePair<string, ScenarioStats> entry in currentStats)
        {
            BattleSimulationRunResult shortest = null;
            BattleSimulationRunResult longest = null;
            BattleSimulationRunResult highestDamageRun = null;
            int highestDamage = -1;
            for (int i = 0; i < result.runs.Count; i++)
            {
                BattleSimulationRunResult run = result.runs[i];
                if (run.scenarioName != entry.Key)
                {
                    continue;
                }
                if (shortest == null || run.rounds < shortest.rounds)
                {
                    shortest = run;
                }
                if (longest == null || run.rounds > longest.rounds)
                {
                    longest = run;
                }
                for (int j = 0; j < run.actors.Count; j++)
                {
                    if (run.actors[j].damageDealt > highestDamage)
                    {
                        highestDamage = run.actors[j].damageDealt;
                        highestDamageRun = run;
                    }
                }
                AddNoActionWarnings(outliers, run);
            }
            if (shortest != null)
            {
                outliers.Add(entry.Key + ": shortest run was run " + shortest.runIndex + " at " + shortest.rounds + " rounds (`combat-logs/" + CombatLogFileName(shortest) + "`)");
            }
            if (longest != null)
            {
                outliers.Add(entry.Key + ": longest run was run " + longest.runIndex + " at " + longest.rounds + " rounds (`combat-logs/" + CombatLogFileName(longest) + "`)");
            }
            if (highestDamageRun != null)
            {
                outliers.Add(entry.Key + ": highest single-run actor damage was " + highestDamage + " in run " + highestDamageRun.runIndex + " (`combat-logs/" + CombatLogFileName(highestDamageRun) + "`)");
            }
            AddUpsetWarnings(outliers, entry.Key, entry.Value, result);
        }
        return outliers;
    }

    static string SanitizeFileName(string fileName)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalid.Length; i++)
        {
            fileName = fileName.Replace(invalid[i], '_');
        }
        return fileName;
    }

    static string CombatLogFileName(BattleSimulationRunResult run)
    {
        return SanitizeFileName(run.scenarioName + "-run-" + run.runIndex + "-seed-" + run.seed + ".md");
    }

    static void AddNoActionWarnings(List<string> outliers, BattleSimulationRunResult run)
    {
        for (int i = 0; i < run.actors.Count; i++)
        {
            bool foundAction = false;
            for (int j = 0; j < run.combatLogEntries.Count; j++)
            {
                if (!string.IsNullOrEmpty(run.combatLogEntries[j].actionType) && run.combatLogEntries[j].actorName == run.actors[i].actorName)
                {
                    foundAction = true;
                    break;
                }
            }
            if (!foundAction)
            {
                outliers.Add(run.scenarioName + ": " + run.actors[i].actorName + " had no parsed skill/attack usage in run " + run.runIndex + " (`combat-logs/" + CombatLogFileName(run) + "`)");
            }
        }
    }

    static void AddUpsetWarnings(List<string> outliers, string scenarioName, ScenarioStats stats, BattleSimulationSuiteResult result)
    {
        int dominantWinner = -1;
        if (stats.teamZeroWins > stats.teamOneWins && stats.teamZeroWins > stats.otherWins)
        {
            dominantWinner = 0;
        }
        else if (stats.teamOneWins > stats.teamZeroWins && stats.teamOneWins > stats.otherWins)
        {
            dominantWinner = 1;
        }
        if (dominantWinner < 0)
        {
            return;
        }

        for (int i = 0; i < result.runs.Count; i++)
        {
            BattleSimulationRunResult run = result.runs[i];
            if (run.scenarioName == scenarioName && run.winningTeam >= 0 && run.winningTeam != dominantWinner)
            {
                outliers.Add(scenarioName + ": upset win by team " + run.winningTeam + " in run " + run.runIndex + " (`combat-logs/" + CombatLogFileName(run) + "`)");
            }
        }
    }

    static void AddChange(List<ChangeHighlight> changes, string label, float current, float previous, bool rate)
    {
        float delta = current - previous;
        if (Mathf.Abs(delta) <= 0.0001f)
        {
            return;
        }
        ChangeHighlight change = new ChangeHighlight();
        change.label = label;
        change.current = current;
        change.previous = previous;
        change.delta = delta;
        change.absoluteDelta = Mathf.Abs(delta);
        change.rate = rate;
        changes.Add(change);
    }

    static string FormatDelta(float current, float previous, bool rate)
    {
        return FormatValue(current, rate) + " (" + FormatSigned(current - previous, rate) + ")";
    }

    static string FormatSigned(float value, bool rate)
    {
        string sign = value >= 0f ? "+" : "";
        return sign + FormatValue(value, rate);
    }

    static string FormatValue(float value, bool rate)
    {
        if (rate)
        {
            return (value * 100f).ToString("0.0") + "%";
        }
        return value.ToString("0.00");
    }

    static float NormalizeRateThreshold(float value, bool rate)
    {
        if (!rate || value < 0f)
        {
            return value;
        }
        if (value > 1f)
        {
            return value / 100f;
        }
        return value;
    }

    static string Html(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }
        return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }

    static void AppendDashboardMatrix(StringBuilder builder, string title, MatchupMatrixData matrix)
    {
        if (matrix == null || matrix.labels == null || matrix.labels.Count == 0 || matrix.cells == null || matrix.cells.Count == 0)
        {
            return;
        }

        Dictionary<string, MatrixCellStats> cellMap = new Dictionary<string, MatrixCellStats>();
        for (int i = 0; i < matrix.cells.Count; i++)
        {
            MatrixCellStats cell = matrix.cells[i];
            cellMap[cell.rowLabel + "|" + cell.columnLabel] = cell;
        }

        builder.AppendLine("<h2>" + Html(title) + "</h2>");
        builder.AppendLine("<table><tr><th>Row vs Column</th>");
        for (int i = 0; i < matrix.labels.Count; i++)
        {
            builder.AppendLine("<th>" + Html(matrix.labels[i]) + "</th>");
        }
        builder.AppendLine("</tr>");

        for (int rowIndex = 0; rowIndex < matrix.labels.Count; rowIndex++)
        {
            string rowLabel = matrix.labels[rowIndex];
            builder.AppendLine("<tr><th>" + Html(rowLabel) + "</th>");
            for (int colIndex = 0; colIndex < matrix.labels.Count; colIndex++)
            {
                string colLabel = matrix.labels[colIndex];
                MatrixCellStats cell;
                if (cellMap.TryGetValue(rowLabel + "|" + colLabel, out cell))
                {
                    builder.AppendLine("<td>" + Html(MatrixCellValue(cell)) + "</td>");
                }
                else
                {
                    builder.AppendLine("<td>-</td>");
                }
            }
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</table>");
    }

    [Serializable]
    class BattleSimulationFailureReport
    {
        public bool failed;
        public List<string> failures = new List<string>();
    }

    [Serializable]
    class MatchupMatrixSummary
    {
        public MatchupMatrixData labelMatrix = new MatchupMatrixData();
        public MatchupMatrixData groupMatrix = new MatchupMatrixData();
    }

    [Serializable]
    class MatchupMatrixData
    {
        public string matrixKind;
        public List<string> labels = new List<string>();
        public List<MatrixCellStats> cells = new List<MatrixCellStats>();
    }

    [Serializable]
    class MatrixCellStats
    {
        public string rowLabel;
        public string columnLabel;
        public int runs;
        public int rowWins;
        public int rowLosses;
        public int unknown;
        public int totalRounds;

        public float RowWinRate()
        {
            if (runs <= 0)
            {
                return 0f;
            }
            return (float)rowWins / runs;
        }

        public float AverageRounds()
        {
            if (runs <= 0)
            {
                return 0f;
            }
            return (float)totalRounds / runs;
        }
    }

    class ScenarioStats
    {
        public int runs;
        public int failures;
        public int teamZeroWins;
        public int teamOneWins;
        public int otherWins;
        public int totalRounds;
        public int totalLogEntries;
        public Dictionary<string, ActorAggregateStats> actorStats = new Dictionary<string, ActorAggregateStats>();

        public void Add(BattleSimulationRunResult run)
        {
            runs++;
            if (run.failed)
            {
                failures++;
            }
            if (run.winningTeam == 0)
            {
                teamZeroWins++;
            }
            else if (run.winningTeam == 1)
            {
                teamOneWins++;
            }
            else
            {
                otherWins++;
            }
            totalRounds += run.rounds;
            totalLogEntries += run.logEntries;
            for (int i = 0; i < run.actors.Count; i++)
            {
                AddActor(run.actors[i]);
            }
        }

        void AddActor(BattleSimulationActorResult actor)
        {
            string key = actor.team + "|" + actor.actorName;
            if (!actorStats.ContainsKey(key))
            {
                actorStats.Add(key, new ActorAggregateStats(actor));
            }
            actorStats[key].Add(actor);
        }

        public float AverageRounds()
        {
            if (runs <= 0)
            {
                return 0f;
            }
            return (float)totalRounds / runs;
        }

        public float AverageLogEntries()
        {
            if (runs <= 0)
            {
                return 0f;
            }
            return (float)totalLogEntries / runs;
        }

        public float TeamZeroWinRate()
        {
            if (runs <= 0)
            {
                return 0f;
            }
            return (float)teamZeroWins / runs;
        }

        public float TeamOneWinRate()
        {
            if (runs <= 0)
            {
                return 0f;
            }
            return (float)teamOneWins / runs;
        }

        public float UnknownWinRate()
        {
            if (runs <= 0)
            {
                return 0f;
            }
            return (float)otherWins / runs;
        }

        public float FailureRate()
        {
            if (runs <= 0)
            {
                return 0f;
            }
            return (float)failures / runs;
        }
    }

    class ActorAggregateStats
    {
        public string actorName;
        public int team;
        public string baseStats;
        public int runsSeen;
        public int totalDamageDealt;
        public int totalDamageTaken;

        public ActorAggregateStats(BattleSimulationActorResult actor)
        {
            actorName = actor.actorName;
            team = actor.team;
            baseStats = actor.baseStats;
        }

        public void Add(BattleSimulationActorResult actor)
        {
            runsSeen++;
            totalDamageDealt += actor.damageDealt;
            totalDamageTaken += actor.damageTaken;
        }

        public float AverageDamageDealt()
        {
            if (runsSeen <= 0)
            {
                return 0f;
            }
            return (float)totalDamageDealt / runsSeen;
        }

        public float AverageDamageTaken()
        {
            if (runsSeen <= 0)
            {
                return 0f;
            }
            return (float)totalDamageTaken / runsSeen;
        }
    }

    class SkillUsageStats
    {
        public string scenarioName;
        public string actorName;
        public string actionType;
        public string skillName;
        public int count;

        public SkillUsageStats(string scenarioName, string actorName, string actionType, string skillName)
        {
            this.scenarioName = scenarioName;
            this.actorName = actorName;
            this.actionType = actionType;
            this.skillName = skillName;
        }
    }

    class ChangeHighlight
    {
        public string label;
        public float current;
        public float previous;
        public float delta;
        public float absoluteDelta;
        public bool rate;
    }
}
