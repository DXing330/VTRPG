using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class AutoChessNeuralNetTournamentRunner : MonoBehaviour
{
    public GameObject thisGO;
    public enum TournamentMode { Master, Worker }
    [Header("Multiple Worker Support")]
    public TournamentMode tournamentMode = TournamentMode.Master;
    public int workerCount = 2;
    public int trainingWorker = 1;
    public float workerTimeoutSeconds = 7200f;
    public bool quitWhenDone = false;
    private string workerResultsPath;
    private string workerStartPath;
    private string workerDonePath;
    private bool stopRequested;
    private void Awake()
    {
        string[] args = Environment.GetCommandLineArgs();
        if (args.Contains("--train-master-nn") || args.Contains("--train-worker-nn") || args.Contains("--benchmark-neural-net") || args.Contains("--train-master") || args.Contains("--train-worker") || args.Contains("--train-master-elo") || args.Contains("--train-worker-elo"))
        {
            thisGO.SetActive(false);
            return;
        }
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--nn-tournament-master":
                    tournamentMode = TournamentMode.Master;
                    launchFromCommandLine = true;
                    break;
                case "--nn-tournament-worker":
                    tournamentMode = TournamentMode.Worker;
                    launchFromCommandLine = true;
                    break;
                case "--workers":
                    if (i + 1 < args.Length)
                        int.TryParse(args[++i], out workerCount);
                    break;
                case "--worker":
                    if (i + 1 < args.Length)
                        int.TryParse(args[++i], out trainingWorker);
                    break;
                case "--matches":
                    if (i + 1 < args.Length)
                        int.TryParse(args[++i], out matchesToRun);
                    break;
                case "--quit":
                    quitWhenDone = true;
                    break;
            }
        }
        if (tournamentMode == TournamentMode.Worker)
            TrainingWorkerStorage.SetWorker(trainingWorker);
    }
    public void StopTournament()
    {
        stopRequested = true;
        StopAllCoroutines();
        isRunning = false;
        CloseWriters();
    }
    [ContextMenu("Start Tournament")]
    public void StartTournament()
    {
        if (isRunning) return;
        stopRequested = false;
        if (tournamentMode == TournamentMode.Master)
            StartCoroutine(RunTournamentMaster());
        else
            StartCoroutine(RunTournamentWorker());
    }
    private IEnumerator RunTournamentMaster()
    {
        Application.targetFrameRate = 5;
        isRunning = true;
        if (workerCount < 1 || !ValidateSetup())
        {
            isRunning = false;
            yield break;
        }
        Directory.CreateDirectory(modelFolder);
        resultsPath = Path.Combine(modelFolder, "tournament_results.csv");
        summaryPath = Path.Combine(modelFolder, "tournament_summary.csv");
        DiscoverModels();
        if (modelPaths.Count < modelsPerMatch)
        {
            Debug.LogError($"Master found only {modelPaths.Count} models.");
            isRunning = false;
            yield break;
        }
        // Clear stale signals and result files before launching a new batch.
        for (int w = 1; w <= workerCount; w++)
        {
            string dir = TrainingWorkerStorage.GetWorkerPath(w);
            Directory.CreateDirectory(dir);
            foreach (string file in new[] {"nn_tournament_start.txt", "nn_tournament_done.txt", "nn_tournament_results.csv"
            })
            {
                string path = Path.Combine(dir, file);
                if (File.Exists(path)) File.Delete(path);
            }
        }
        // matchesToRun here means matches PER worker.
        for (int w = 1; w <= workerCount; w++)
        {
            string path = Path.Combine(TrainingWorkerStorage.GetWorkerPath(w), "nn_tournament_start.txt");
            File.WriteAllText(path, matchesToRun.ToString(CultureInfo.InvariantCulture));
        }
        Debug.Log($"Tournament master: {workerCount} workers, " + $"{matchesToRun} matches each, " + $"{workerCount * matchesToRun} total planned.");
        float startTime = Time.realtimeSinceStartup;
        bool[] done = new bool[workerCount + 1];
        while (!stopRequested)
        {
            bool allDone = true;
            for (int w = 1; w <= workerCount; w++)
            {
                if (done[w]) continue;
                string path = Path.Combine(TrainingWorkerStorage.GetWorkerPath(w), "nn_tournament_done.txt");
                if (File.Exists(path))
                {
                    done[w] = true;
                    Debug.Log($"Tournament master: worker {w} finished.");
                }
                else
                {
                    allDone = false;
                }
            }
            if (allDone) break;
            if (Time.realtimeSinceStartup - startTime > workerTimeoutSeconds)
            {
                Debug.LogError("Tournament master timed out waiting for workers.");
                break;
            }
            yield return new WaitForSeconds(2f);
        }
        // Combine worker CSVs into the main result file.
        MergeWorkerResults();
        RebuildSummaryFromExistingResults();
        WriteSummary();
        CleanupWorkerSignals();
        isRunning = false;
        Debug.Log($"Tournament master finished. Results: {resultsPath}");
        if (quitWhenDone) QuitApplication();
    }
    private IEnumerator RunTournamentWorker()
    {
        isRunning = true;
        if (!ValidateSetup())
        {
            isRunning = false;
            yield break;
        }
        string workerDir = TrainingWorkerStorage.GetPersistentPath();
        Directory.CreateDirectory(workerDir);
        workerStartPath = Path.Combine(workerDir, "nn_tournament_start.txt");
        workerDonePath = Path.Combine(workerDir, "nn_tournament_done.txt");
        workerResultsPath = Path.Combine(workerDir, "nn_tournament_results.csv");
        modelFolder = Path.Combine(Application.persistentDataPath, modelFolderName);
        Directory.CreateDirectory(modelFolder);
        DiscoverModels();
        if (modelPaths.Count < modelsPerMatch)
        {
            Debug.LogError($"Worker {trainingWorker}: only found {modelPaths.Count} models in {modelFolder}.");
            isRunning = false;
            yield break;
        }
        // Wait for the master to assign the batch size.
        while (!stopRequested && !File.Exists(workerStartPath))
            yield return new WaitForSeconds(1f);
        if (stopRequested)
        {
            isRunning = false;
            yield break;
        }
        if (!int.TryParse(File.ReadAllText(workerStartPath).Trim(),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int count) ||
            count < 1)
        {
            Debug.LogError($"Worker {trainingWorker}: invalid start signal.");
            isRunning = false;
            yield break;
        }
        File.Delete(workerStartPath);
        if (File.Exists(workerDonePath)) File.Delete(workerDonePath);
        if (File.Exists(workerResultsPath)) File.Delete(workerResultsPath);
        resultsPath = workerResultsPath;
        summaryPath = Path.Combine(workerDir, "nn_tournament_summary.csv");
        resultWriter = new StreamWriter(workerResultsPath, false);
        resultWriter.WriteLine(ResultsHeader);
        resultWriter.AutoFlush = true;
        // Make worker schedules reproducible but different between workers.
        rng = new System.Random(randomSeed + trainingWorker * 1000003);
        int completed = 0;
        for (int match = 0; match < count && !stopRequested; match++)
        {
            Debug.Log($"[Tournament] Starting match {match + 1}/{count}");
            List<string> selected = SelectModelsForMatch(match + (trainingWorker - 1) * count);
            if (shuffleModelSlots) Shuffle(selected);
            Debug.Log($"[Tournament] Loading {selected.Count} models for match {match + 1}/{count}");
            if (selected.Count != modelsPerMatch || !LoadSelectedModels(selected))
            {
                Debug.LogError($"Worker {trainingWorker}: failed to load models for match {match + 1}.");
                break;
            }
            Debug.Log($"[Tournament] All {modelsPerMatch} models loaded. Calling SetupMatch() for match {match + 1}.");
            director.SetupMatch();
            Debug.Log($"[Tournament] SetupMatch() returned for match {match + 1}. Starting gameplay.");
            director.useNeuralNet = true;
            director.neuralNetCount = modelsPerMatch;
            director.neuralNetWon = false;
            director.matchOver = false;
            director.roundCount = 0;
            director.autoAssignAIGenomes = true;
            if (director.neuralNetSlots == null || director.neuralNetSlots.Length < modelsPerMatch)
            {
                Debug.LogError("Match director neuralNetSlots array is too small.");
                break;
            }
            Array.Clear(director.neuralNetSlots, 0, director.neuralNetSlots.Length);
            var teams = director.allPlayers.GetAllTeams();
            if (teams.Count < modelsPerMatch)
            {
                Debug.LogError($"Worker {trainingWorker}: only {teams.Count} teams available.");
                break;
            }
            modelByTeam.Clear();
            for (int i = 0; i < modelsPerMatch; i++)
            {
                director.neuralNetSlots[i] = true;
                modelByTeam[teams[i]] = selected[i];
            }
            director.neuralNetControllers = controllers;
            int lastLoggedRound = -1;
            while (!stopRequested && !director.matchOver && director.roundCount < maxRounds)
            {
                if (director.roundCount != lastLoggedRound)
                {
                    lastLoggedRound = director.roundCount;
                    Debug.Log($"[Tournament] Match {match + 1}/{count}: round {lastLoggedRound}");
                }
                director.TimedAIPrepPhase();
                yield return null;
            }
            if (stopRequested) break;
            if (!director.matchOver) director.MatchOver();
            // Worker results use local match IDs; each worker has its own file.
            RecordMatch(match + 1);
            completed++;
            if (completed % 10 == 0)
            {
                Debug.Log($"Worker {trainingWorker}: {completed}/{count} matches complete.");
            }
        }
        CloseWriters();
        director.autoAssignAIGenomes = false;
        director.useNeuralNet = false;
        // Publish completion only after results have been flushed and closed.
        File.WriteAllText(workerDonePath, completed.ToString(CultureInfo.InvariantCulture));
        Debug.Log($"Worker {trainingWorker}: completed {completed}/{count} matches.");
        isRunning = false;
        if (quitWhenDone) QuitApplication();
    }
    private void MergeWorkerResults()
    {
        string tempPath = resultsPath + ".tmp";
        int nextMatchId = 1;
        var outputLines = new List<string>();
        // Ensure the output always has a header.
        if (outputLines.Count == 0)
        {
            outputLines.Add(ResultsHeader);
        }
        // Each worker has its own local match IDs starting at 1.
        // Remap each worker-local match ID to one unique global ID.
        for (int worker = 1; worker <= workerCount; worker++)
        {
            string workerResults = Path.Combine(TrainingWorkerStorage.GetWorkerPath(worker), "nn_tournament_results.csv");
            if (!File.Exists(workerResults))
            {
                Debug.LogWarning($"Tournament master: worker {worker} results file missing.");
                continue;
            }
            var lines = File.ReadAllLines(workerResults);
            // Local match ID -> globally unique match ID.
            var matchIdMap = new Dictionary<int, int>();
            // Start at 1 to skip the worker CSV header.
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;
                List<string> cells = ParseCsvLine(lines[i]);
                if (cells.Count < 14 || !int.TryParse(cells[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int localMatchId))
                {
                    Debug.LogWarning($"Skipping malformed row in worker {worker} CSV, line {i + 1}.");
                    continue;
                }
                // Assign an ID once per match, not once per team row.
                if (!matchIdMap.TryGetValue(localMatchId, out int globalMatchId))
                {
                    globalMatchId = nextMatchId++;
                    matchIdMap[localMatchId] = globalMatchId;
                }
                cells[0] = globalMatchId.ToString(CultureInfo.InvariantCulture);
                outputLines.Add(string.Join(",", cells.Select(Csv)));
            }
            Debug.Log($"Tournament master: merged {matchIdMap.Count} matches " + $"from worker {worker}.");
        }
        File.WriteAllLines(tempPath, outputLines);
        File.Copy(tempPath, resultsPath, true);
        File.Delete(tempPath);
        Debug.Log($"Tournament master: merged CSV contains " + $"{outputLines.Count - 1} data rows.");
    }
    private void CleanupWorkerSignals()
    {
        for (int w = 1; w <= workerCount; w++)
        {
            string dir = TrainingWorkerStorage.GetWorkerPath(w);
            foreach (string file in new[] {"nn_tournament_start.txt", "nn_tournament_done.txt"})
            {
                string path = Path.Combine(dir, file);
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
    private static void QuitApplication()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
    [Header("Existing Match System")]
    public AutoChessPVPMatchDirector director;
    [Header("Independent Model Controllers - Exactly 8")]
    public AutoChessNeuralNetPrepController[] controllers = new AutoChessNeuralNetPrepController[8];
    [Header("Tournament Settings")]
    public int matchesToRun = 100;
    public int modelsPerMatch = 8;
    public int maxRounds = 30;
    public int randomSeed = 12345;
    public bool shuffleModelSlots = true;
    public bool runOnStart = false;
    [Header("Progress")]
    public bool isRunning;
    private bool launchFromCommandLine;
    public int completedMatches;
    public string modelFolder;
    public string modelFolderName = "nn_history";
    public string resultsPath;
    public string summaryPath;
    private System.Random rng = new System.Random();
    private readonly List<string> modelPaths = new List<string>();
    private readonly Dictionary<AutoChessDataManager, string> modelByTeam = new Dictionary<AutoChessDataManager, string>();
    private readonly Dictionary<string, ModelSummary> summaries = new Dictionary<string, ModelSummary>();
    private StreamWriter resultWriter;
    private const string ResultsHeader = "MatchId,Model,Placement,Won,RoundSurvived,FinalHealth,FinalGold,TotalGoldSpent,Level,FieldUnits,BenchUnits,Factions,FactionStacks,Equipment";
    private const string SummaryHeader = "Model,Matches,Wins,WinRate,AveragePlacement,AverageRounds,AverageFinalGold,AverageGoldSpent,AverageLevel";
    [Serializable]
    private class ModelSummary
    {
        public int matches;
        public int wins;
        public double placementTotal;
        public double roundsTotal;
        public double goldTotal;
        public double goldSpentTotal;
        public double levelTotal;
    }
    private void Start()
    {
        if (runOnStart || launchFromCommandLine)
        {
            StartTournament();
        }
    }
    private bool ValidateSetup()
    {
        if (director == null)
        {
            Debug.LogError("Tournament runner needs a match director.");
            return false;
        }
        if (modelsPerMatch != 8)
        {
            Debug.LogError("This version requires exactly 8 models per match.");
            return false;
        }
        if (controllers == null || controllers.Length < modelsPerMatch)
        {
            Debug.LogError($"Assign at least {modelsPerMatch} independent controllers.");
            return false;
        }
        for (int i = 0; i < modelsPerMatch; i++)
        {
            if (controllers[i] == null)
            {
                Debug.LogError($"Controller slot {i} is not assigned.");
                return false;
            }
        }
        return true;
    }
    private void DiscoverModels()
    {
        modelPaths.Clear();
        modelPaths.AddRange(Directory.GetFiles(modelFolder, "*.hnn").OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase));
    }
    private readonly Dictionary<string, int> modelAppearances = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private List<string> SelectModelsForMatch(int matchIndex)
    {
        var shuffled = new List<string>(modelPaths);
        // Randomize tie-breaking.
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        // Least-played models first.
        shuffled.Sort((a, b) =>
        {
            int countA = modelAppearances.TryGetValue(a, out int ca) ? ca : 0;
            int countB = modelAppearances.TryGetValue(b, out int cb) ? cb : 0;
            return countA.CompareTo(countB);
        });
        var selected = shuffled.Take(modelsPerMatch).ToList();
        // Count appearances immediately.
        foreach (string model in selected)
        {
            if (!modelAppearances.ContainsKey(model))
                modelAppearances[model] = 0;
            modelAppearances[model]++;
        }
        return selected;
    }
    private bool LoadSelectedModels(List<string> selected)
    {
        for (int i = 0; i < modelsPerMatch; i++)
        {
            if (!controllers[i].LoadModelFromPath(selected[i])) return false;
        }
        return true;
    }
    private void RecordMatch(int matchId)
    {
        var teams = director.allPlayers.GetAllTeams();
        var ranked = teams.Where(team => !team.PlayerData()).OrderByDescending(team => team.GetRound()).ThenByDescending(team => team.GetHealth()).ToList();
        var placementByTeam = new Dictionary<AutoChessDataManager, int>();
        for (int i = 0; i < ranked.Count; i++) placementByTeam[ranked[i]] = i + 1;
        foreach (var pair in modelByTeam)
        {
            AutoChessDataManager team = pair.Key;
            if (!placementByTeam.TryGetValue(team, out int placement)) continue;
            string modelName = Path.GetFileName(pair.Value);
            int rounds = team.GetRound();
            int health = team.GetHealth();
            int gold = team.GetGold();
            int goldSpent = team.GetTotalGoldSpent();
            int level = team.GetLevel();
            string field = string.Join("|", team.GetFieldActorNames());
            string bench = string.Join("|", team.GetBenchActorNames());
            string factions = string.Join("|", team.factionData.GetActiveFactions());
            string factionStacks = string.Join("|", team.factionData.GetActiveFactionStacks());
            string equipment = string.Join("|", team.GetFieldActorEquipment());
            resultWriter.WriteLine(string.Join(",", Csv(matchId.ToString(CultureInfo.InvariantCulture)), Csv(modelName), Csv(placement.ToString(CultureInfo.InvariantCulture)), Csv((placement == 1 ? 1 : 0).ToString(CultureInfo.InvariantCulture)), Csv(rounds.ToString(CultureInfo.InvariantCulture)), Csv(health.ToString(CultureInfo.InvariantCulture)), Csv(gold.ToString(CultureInfo.InvariantCulture)), Csv(goldSpent.ToString(CultureInfo.InvariantCulture)), Csv(level.ToString(CultureInfo.InvariantCulture)), Csv(field), Csv(bench), Csv(factions), Csv(factionStacks), Csv(equipment)));

            if (!summaries.TryGetValue(modelName, out ModelSummary summary))
            {
                summary = new ModelSummary();
                summaries[modelName] = summary;
            }
            summary.matches++;
            if (placement == 1) summary.wins++;
            summary.placementTotal += placement;
            summary.roundsTotal += rounds;
            summary.goldTotal += gold;
            summary.goldSpentTotal += goldSpent;
            summary.levelTotal += level;
        }
    }
    private void RebuildSummaryFromExistingResults()
    {
        summaries.Clear();
        completedMatches = 0;
        if (!File.Exists(resultsPath)) return;
        foreach (string line in File.ReadLines(resultsPath).Skip(1))
        {
            List<string> cells = ParseCsvLine(line);
            if (cells.Count < 14 ||
                !int.TryParse(cells[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int matchId) ||
                !int.TryParse(cells[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int placement) ||
                !int.TryParse(cells[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int won) ||
                !int.TryParse(cells[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rounds) ||
                !double.TryParse(cells[6], NumberStyles.Any, CultureInfo.InvariantCulture, out double gold) ||
                !double.TryParse(cells[7], NumberStyles.Any, CultureInfo.InvariantCulture, out double goldSpent) ||
                !double.TryParse(cells[8], NumberStyles.Any, CultureInfo.InvariantCulture, out double level))
                continue;

            string modelName = cells[1];
            if (!summaries.TryGetValue(modelName, out ModelSummary summary))
            {
                summary = new ModelSummary();
                summaries[modelName] = summary;
            }
            summary.matches++;
            summary.wins += won;
            summary.placementTotal += placement;
            summary.roundsTotal += rounds;
            summary.goldTotal += gold;
            summary.goldSpentTotal += goldSpent;
            summary.levelTotal += level;
            completedMatches = Math.Max(completedMatches, matchId);
        }
    }

    private void WriteSummary()
    {
        using (var writer = new StreamWriter(summaryPath, false))
        {
            writer.WriteLine(SummaryHeader);
            foreach (var pair in summaries.OrderBy(p => p.Key))
            {
                ModelSummary s = pair.Value;
                if (s.matches == 0) continue;
                writer.WriteLine(string.Join(",", Csv(pair.Key), s.matches.ToString(CultureInfo.InvariantCulture), s.wins.ToString(CultureInfo.InvariantCulture), (s.wins / (double)s.matches).ToString("F4", CultureInfo.InvariantCulture), (s.placementTotal / s.matches).ToString("F3", CultureInfo.InvariantCulture), (s.roundsTotal / s.matches).ToString("F3", CultureInfo.InvariantCulture), (s.goldTotal / s.matches).ToString("F3", CultureInfo.InvariantCulture), (s.goldSpentTotal / s.matches).ToString("F3", CultureInfo.InvariantCulture), (s.levelTotal / s.matches).ToString("F3", CultureInfo.InvariantCulture)));
            }
        }
    }

    private static string Csv(string value)
    {
        value = value ?? "";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static List<string> ParseCsvLine(string line)
    {
        var cells = new List<string>();
        var cell = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else quoted = !quoted;
            }
            else if (c == ',' && !quoted)
            {
                cells.Add(cell.ToString());
                cell.Clear();
            }
            else cell.Append(c);
        }
        cells.Add(cell.ToString());
        return cells;
    }
    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    private void CloseWriters()
    {
        resultWriter?.Flush();
        resultWriter?.Dispose();
        resultWriter = null;
    }
}
