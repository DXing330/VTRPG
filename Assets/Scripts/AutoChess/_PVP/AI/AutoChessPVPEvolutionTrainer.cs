using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TournamentMatchLog
{
    public List<string> rankedIds = new();
}
[Serializable]
public class TournamentMatchLogList
{
    public List<TournamentMatchLog> matches = new();
}
public static class GenomeProvider
{
    private static Dictionary<AutoChessDataManager, GenomeEntry> map = new();
    public static void Assign(AutoChessDataManager team, GenomeEntry entry) => map[team] = entry;
    public static GenomeEntry Get(AutoChessDataManager team) => map.TryGetValue(team, out var e) ? e : null;
    public static void Clear() => map.Clear();
}
public class AutoChessPVPEvolutionTrainer : MonoBehaviour
{
    public enum TrainingMode
    {
        Master,
        Worker
    }
    [Header("Training Mode")]
    public bool autoStartTraining = true;
    public TrainingMode trainingMode = TrainingMode.Master;
    public int workerCount = 1;
    [Header("Headless Mode")]
    public int targetGenerations = 100;
    public bool quitWhenDone = false;
    public int trainingWorker = 0;
    void Awake()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--train-master":
                    trainingMode = TrainingMode.Master;
                    autoStartTraining = true;
                    autoStartEloTournament = false;
                    break;
                case "--train-worker":
                    trainingMode = TrainingMode.Worker;
                    autoStartTraining = true;
                    autoStartEloTournament = false;
                    break;
                case "--train-master-elo":
                    trainingMode = TrainingMode.Master;
                    autoStartTraining = false;
                    autoStartEloTournament = true;
                    break;
                case "--train-worker-elo":
                    trainingMode = TrainingMode.Worker;
                    autoStartTraining = false;
                    autoStartEloTournament = true;
                    break;
                case "--workers":
                    if (i + 1 < args.Length)
                        int.TryParse(args[++i], out workerCount);
                    break;
                case "--worker":
                    if (i + 1 < args.Length)
                    {
                        int.TryParse(args[++i], out trainingWorker);
                    }
                    break;
                case "--gens":
                    if (i + 1 < args.Length)
                        int.TryParse(args[++i], out targetGenerations);
                    break;
                case "--pop":
                    if (i + 1 < args.Length)
                        int.TryParse(args[++i], out populationSize);
                    break;
                case "--matches":
                    if (i + 1 < args.Length)
                        int.TryParse(args[++i], out matchesPerGenome);
                    break;
                case "--pod":
                    if (i + 1 < args.Length)
                        int.TryParse(args[++i], out podSize);
                    break;
                case "--elite":
                    if (i + 1 < args.Length)
                        int.TryParse(args[++i], out elites);
                    break;
                case "--cull":
                    if (i + 1 < args.Length)
                        int.TryParse(args[++i], out cullCount);
                    break;
                case "--crossbreedRate":
                    if (i + 1 < args.Length)
                        float.TryParse(args[++i], out crossbreedRate);
                    break;
                case "--mutRate":
                    if (i + 1 < args.Length)
                        float.TryParse(args[++i], out mutationRate);
                    break;
                case "--mutStrength":
                    if (i + 1 < args.Length)
                        float.TryParse(args[++i], out mutationStrength);
                    break;
                case "--keep":
                    if (i + 1 < args.Length)
                        int.TryParse(args[++i], out generationsToKeep);
                    break;
                case "--quit":
                    quitWhenDone = true;
                    break;
                case "--eloMatches":
                    if (i + 1 < args.Length)
                        int.TryParse(args[++i], out eloTargetMatches);
                    break;
            }
        }
        if (autoStartEloTournament)
        {
            int seed = trainingWorker * 10000 + System.DateTime.Now.DayOfYear;
            UnityEngine.Random.InitState(seed);
        }
        if (trainingWorker > 0)
        {
            TrainingWorkerStorage.SetWorker(trainingWorker);
            Debug.Log($"Training worker {trainingWorker} using isolated storage: " + TrainingWorkerStorage.GetPersistentPath());
        }
    }
    [Header("References")]
    public AutoChessPVPSavedGenomeDataManager database;
    public AutoChessPVPSavedGenomeDataManager championDatabase;
    public AutoChessPVPTournamentMatchSavedData tournamentMatchData;
    public bool newGenePools = false;
    public List<AutoChessPVPGenomeTemplate> genePoolTemplates = new();
    protected void LoadDB()
    {
        database.LoadFromDisk(true);
        championDatabase.LoadFromDisk(true);
        if (database.AllEntries.Count == 0)
        {
            database.InitPopulation(populationSize);
        }
        else
        {
            int maxGen = 0;
            foreach (var e in database.AllEntries) maxGen = Mathf.Max(maxGen, e.generation);
            currentGeneration = maxGen;
            Debug.Log($"Resuming at generation {currentGeneration} with {database.AllEntries.Count} genomes.");
        }
        if (newGenePools)
        {
            database.AssignUnassignedToBasePool();
            for (int i = 0; i < genePoolTemplates.Count; i++)
            {
                database.AddGenePool(genePoolTemplates[i], populationSize, currentGeneration, 0.15f, 1.0f, false);
            }
            database.SaveToDisk();
        }
        if (autoStartTraining)
        {
            StartCoroutine(RunTrainingLoop());
        }
    }
    public AutoChessPVPMatchDirector director;
    public AutoChessAIPrepController aiController;
    [Header("Evolution Settings")]
    public int populationSize = 8; // This Is Per Gene Pool Not Full Population
    public int matchesPerGenome = 6;
    public int podSize = 8;
    public int elites = 2;
    public int cullCount = 2;
    public float crossbreedRate = 0.1f;
    public float mutationRate = 0.2f;
    public float mutationStrength = 0.3f;
    [Header("State")]
    public int generationsToKeep = 100;
    public int currentGeneration = 0;
    public bool isTraining = false;
    public int matchesCompletedThisGen = 0;
    public GenomeEntry champion = null;
    public int championGeneration = 0;
    private List<GenomeEntry> currentGenPool = new();
    void Start()
    {
        // Don't needs logging during training.
        director.allPlayers.DisableLogs();
        try
        {
            if (database == null)
            {
                database = Resources.Load<AutoChessPVPSavedGenomeDataManager>("SavedGenomes");
            }
            if (database == null)
            {
                Debug.LogError("FATAL: database is still null after Resources.Load!");
                return;
            }
            LoadDB();
            Debug.Log("LoadDB finished.");
            if (autoStartEloTournament)
            {
                if (tournamentMatchData != null)
                {
                    tournamentMatchData.ClearHistory();
                }
                StartChampionEloTournament();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("CRASH IN START: " + e);
        }
    }
    [Header("Champion Elo Tournament")]
    public int eloTargetMatches = 50;
    public int priorityChampionsPerPod = 2;
    public float eloKFactor = 10f;
    public bool autoStartEloTournament = false;
    [ContextMenu("Start Champion Elo Tournament")]
    public void StartChampionEloTournament()
    {
        if (championDatabase == null)
        {
            Debug.LogError("Cannot start Champion Elo Tournament: championDatabase is null.");
            return;
        }
        if (trainingMode == TrainingMode.Worker)
        {
            StartCoroutine(RunTournamentWorkerLoop());
        }
        else if (trainingMode == TrainingMode.Master)
        {
            StartCoroutine(RunTournamentMasterLoop());
        }
        else
        {
            // Normal mode: single process, run locally
            StartCoroutine(RunChampionEloTournament());
        }
    }
    List<GenomeEntry> CreateTournamentPod()
    {
        List<GenomeEntry> pod = new();
        // Always take the two champions with the fewest Elo matches.
        List<GenomeEntry> lowestMatchChampions = championDatabase.entries.Where(c => c != null).OrderBy(c => c.eloMatches).ThenBy(c => UnityEngine.Random.value).Take(priorityChampionsPerPod).ToList();
        pod.AddRange(lowestMatchChampions);
        // Fill remaining slots randomly
        List<GenomeEntry> randomChampions = championDatabase.entries.Where(c => c != null && !pod.Contains(c)).OrderBy(c => UnityEngine.Random.value).Take(podSize - pod.Count).ToList();
        pod.AddRange(randomChampions);
        // Workers Don't Save So No Double Counting, This Is So That Workers Know To Not Oversample The Original Undersampled Population.
        for (int i = 0; i < pod.Count; i++)
        {
            pod[i].eloMatches++;
        }
        return pod;
    }
    TournamentMatchLogList currentMatchLogs = new();
    void SaveMatchLog(List<string> rankedIds)
    {
        currentMatchLogs.matches.Add(new TournamentMatchLog { rankedIds = rankedIds });
    }
    void SaveMatchLogFile(TournamentMatchLogList logs, string path)
    {
        string json = JsonUtility.ToJson(logs, false);
        string tempPath = path + ".tmp";
        File.WriteAllText(tempPath, json);
        if (File.Exists(path)) File.Delete(path);
        File.Move(tempPath, path);
    }
    IEnumerator RunTournamentWorkerLoop()
    {
        Debug.Log($"=== Tournament Worker {trainingWorker} starting ===");
        // Load master champion DB for current Elo values
        championDatabase.LoadFromDisk(true);
        currentMatchLogs = new TournamentMatchLogList();
        // Load match count from start signal
        string workerDir = TrainingWorkerStorage.GetPersistentPath();
        string startPath = Path.Combine(workerDir, "tournament_start.txt");
        // Wait for start signal
        while (!File.Exists(startPath))
        {
            yield return new WaitForSeconds(1f);
        }
        string countStr = File.ReadAllText(startPath).Trim();
        if (!int.TryParse(countStr, out int matchesToRun))
        {
            Debug.LogError($"Worker {trainingWorker}: Could not parse match count.");
            yield break;
        }
        try { File.Delete(startPath); } catch { }
        Debug.Log($"Worker {trainingWorker}: Running {matchesToRun} tournament matches");
        // Clear any old match logs
        string logPath = Path.Combine(workerDir, "tournament_matches.json");
        if (File.Exists(logPath))
        {
            try { File.Delete(logPath); } catch { }
        }
        float tournamentStart = Time.realtimeSinceStartup;
        int matchesRun = 0;
        for (int i = 0; i < matchesToRun; i++)
        {
            // Build pod from champion database
            List<GenomeEntry> pod = CreateTournamentPod();
            if (pod.Count < podSize)
            {
                Debug.LogWarning($"Worker {trainingWorker}: Could not build full pod, skipping.");
                continue;
            }
            yield return StartCoroutine(RunPodMatch(pod, i, true));
            matchesRun++;
            // Save incremental log every 10 matches (crash recovery)
            if (i % 10 == 0)
            {
                SaveMatchLogFile(currentMatchLogs, logPath);
            }
        }
        // Final save
        SaveMatchLogFile(currentMatchLogs, logPath);
        // Signal completion
        string donePath = Path.Combine(workerDir, "tournament_done.txt");
        try { File.WriteAllText(donePath, matchesRun.ToString()); } catch { }
        float tournamentTime = Time.realtimeSinceStartup - tournamentStart;
        Debug.Log($"{matchesRun} finished in {tournamentTime:F1}s");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    void SignalTournamentStart(int matchesPerWorker)
    {
        for (int i = 1; i <= workerCount; i++)
        {
            string startPath = Path.Combine(TrainingWorkerStorage.GetWorkerPath(i), "tournament_start.txt");
            try { File.WriteAllText(startPath, matchesPerWorker.ToString()); } catch { }
        }
    }
    IEnumerator WaitForTournamentWorkers()
    {
        float startTime = Time.realtimeSinceStartup;
        bool[] workerDone = new bool[workerCount + 1];
        while (true)
        {
            bool allDone = true;
            for (int i = 1; i <= workerCount; i++)
            {
                if (workerDone[i]) continue;
                string donePath = Path.Combine(TrainingWorkerStorage.GetWorkerPath(i), "tournament_done.txt");
                if (File.Exists(donePath))
                {
                    workerDone[i] = true;
                    Debug.Log($"MASTER: Worker {i} tournament complete.");
                }
                else{allDone = false;}
            }
            if (allDone) break;
            if (Time.realtimeSinceStartup - startTime > workerEloTimeoutSeconds)
            {
                Debug.LogError("MASTER: Timeout waiting for tournament workers.");
                for (int i = 1; i <= workerCount; i++)
                {
                    if (!workerDone[i])
                        Debug.LogWarning($"MASTER: Worker {i} never finished.");
                }
                break;
            }
            yield return new WaitForSeconds(2f);
        }
        Debug.Log("MASTER: All tournament workers done.");
    }
    void MergeTournamentMatchLogs()
    {
        var allLogs = new List<TournamentMatchLog>();
        for (int worker = 1; worker <= workerCount; worker++)
        {
            string logPath = Path.Combine(TrainingWorkerStorage.GetWorkerPath(worker), "tournament_matches.json");
            if (!File.Exists(logPath))
            {
                Debug.LogWarning($"MASTER: Worker {worker} match log missing.");
                continue;
            }
            string json = File.ReadAllText(logPath);
            var logs = JsonUtility.FromJson<TournamentMatchLogList>(json);
            if (logs?.matches != null)
            {
                allLogs.AddRange(logs.matches);
                Debug.Log($"MASTER: Loaded {logs.matches.Count} matches from worker {worker}");
            }
            // Cleanup
            try { File.Delete(logPath); } catch { }
        }
        Debug.Log($"MASTER: Total matches to replay: {allLogs.Count}");
        // Shuffle for fair ordering
        Shuffle(allLogs);
        // Replay all matches
        ReplayTournamentMatches(allLogs);
    }
    void CleanupTournamentSignals()
    {
        for (int i = 1; i <= workerCount; i++)
        {
            string dir = TrainingWorkerStorage.GetWorkerPath(i);
            foreach (var f in new[] { "tournament_start.txt", "tournament_done.txt", "tournament_matches.json" })
            {
                string path = Path.Combine(dir, f);
                if (File.Exists(path)) try { File.Delete(path); } catch { }
            }
        }
    }
    void ReplayTournamentMatches(List<TournamentMatchLog> allLogs)
    {
        foreach (var log in allLogs)
        {
            var ranked = log.rankedIds.Select(id => championDatabase.GetById(id)).Where(c => c != null).ToList();
            if (ranked.Count != podSize)
            {
                Debug.LogWarning($"Skipping match: only {ranked.Count}/{podSize} champions found.");
                continue;
            }
            UpdateElo(ranked);
            if (tournamentMatchData != null)
            {
                tournamentMatchData.AddMatch(ranked);
            }
            foreach (GenomeEntry champion in ranked)
            {
                champion.eloMatches++;
                champion.peakElo = Mathf.Max(champion.peakElo, champion.elo);
            }
        }
        Debug.Log($"MASTER: Replayed {allLogs.Count} matches.");
    }
    IEnumerator RunTournamentMasterLoop()
    {
        Application.targetFrameRate = 5;
        // Load champion database
        championDatabase.LoadFromDisk(true);
        if (championDatabase.entries.Count < podSize)
        {
            Debug.LogWarning($"Need {podSize} champions, have {championDatabase.entries.Count}.");
            yield break;
        }
        // All workers run the same number of matches
        int matchesPerWorker = eloTargetMatches;
        Debug.Log($"=== Tournament Master Starting ===");
        Debug.Log($"Champions: {championDatabase.entries.Count}");
        Debug.Log($"Matches per worker: {matchesPerWorker}");
        // Remove Old Signals.
        CleanupTournamentSignals();
        // Signal workers to start
        SignalTournamentStart(matchesPerWorker);
        // Wait for all workers
        yield return StartCoroutine(WaitForTournamentWorkers());
        // Merge and replay all match logs
        MergeTournamentMatchLogs();
        // Save final results
        championDatabase.SaveToDisk(true);
        Debug.Log("=== Tournament Master Complete ===");
        // Remove Old Signals.
        CleanupTournamentSignals();
        if (quitWhenDone)
        {
            Application.Quit();
        }
    }
    IEnumerator RunChampionEloTournament()
    {
        if (championDatabase.entries.Count < podSize)
        {
            yield break;
        }
        Debug.Log($"=== Champion Elo Tournament Starting ===");
        Debug.Log($"Champions: {championDatabase.entries.Count}");
        // Always take the two champions with the fewest Elo matches.
        for (int i = 0; i < eloTargetMatches; i++)
        {
            List<GenomeEntry> pod = CreateTournamentPod();
            if (pod.Count < podSize)
            {
                Debug.LogWarning($"Elo tournament needs {podSize} champions, " + $"but only found {pod.Count}.");
                break;
            }
            yield return StartCoroutine(RunPodMatch(pod, i, true));
        }
        Debug.Log("=== Champion Elo Tournament Complete ===");
        championDatabase.SaveToDisk(true);
        if (quitWhenDone)
        {
            Debug.Log("Quitting application.");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
    [ContextMenu("Start Training")]
    public void StartTraining() => StartCoroutine(RunTrainingLoop());
    [ContextMenu("Stop Training")]
    public void StopTraining() => isTraining = false;
    IEnumerator RunTrainingLoop()
    {
        if (trainingMode == TrainingMode.Worker)
        {
            yield return StartCoroutine(RunWorkerLoop());
            yield break;
        }
        else if (trainingMode == TrainingMode.Master)
        {
            yield return StartCoroutine(RunMasterLoop());
            yield break;
        }
    }
    [Header("Master Worker Control")]
    public string workerExecutablePath = "";
    public float workerTimeoutSeconds = 3600f;
    public float workerEloTimeoutSeconds = 36000f;
    // REPLACE LaunchWorkers() entirely:
    void SignalWorkersReady(int generation)
    {
        for (int i = 1; i <= workerCount; i++)
        {
            string readyPath = Path.Combine(TrainingWorkerStorage.GetWorkerPath(i), "next_generation_ready.txt");
            try { File.WriteAllText(readyPath, generation.ToString()); } catch { }
        }
    }
    void CleanupPreviousGen(int generation)
    {
        for (int i = 1; i <= workerCount; i++)
        {
            string workerDir = TrainingWorkerStorage.GetWorkerPath(i);
            // Delete old ready signals
            string readyPath = Path.Combine(workerDir, "next_generation_ready.txt");
            if (File.Exists(readyPath))
            {
                try { File.Delete(readyPath); } catch { }
            }
            // Delete old result file
            string resultPath = TrainingWorkerStorage.GetWorkerFilePath(i, database.filename);
            if (File.Exists(resultPath))
            {
                try { File.Delete(resultPath); } catch { }
            }
        }
    }
    void SignalWorkersDone()
    {
        for (int i = 1; i <= workerCount; i++)
        {
            string donePath = Path.Combine(TrainingWorkerStorage.GetWorkerPath(i), "generations_done.txt");
            try { File.WriteAllText(donePath, "generations_done"); } catch { }
        }
    }
    IEnumerator WaitForWorkerFiles()
    {
        float startTime = Time.realtimeSinceStartup;
        bool[] workerDone = new bool[workerCount + 1];
        while (true)
        {
            bool allDone = true;
            for (int i = 1; i <= workerCount; i++)
            {
                if (workerDone[i]) continue;
                string path = TrainingWorkerStorage.GetWorkerFilePath(i, database.filename);
                if (File.Exists(path))
                {
                    workerDone[i] = true;
                    Debug.Log($"MASTER: Worker {i} file detected.");
                }
                else
                {
                    allDone = false;
                }
            }
            if (allDone) break;
            if (Time.realtimeSinceStartup - startTime > workerTimeoutSeconds)
            {
                Debug.LogError("MASTER: Timeout waiting for worker files.");
                for (int i = 1; i <= workerCount; i++)
                {
                    if (!workerDone[i]){Debug.LogWarning($"MASTER: Worker {i} never produced a file.");}
                }
                break;
            }
            yield return new WaitForSeconds(1f);
        }
        Debug.Log("MASTER: Done waiting for worker files.");
    }
    IEnumerator RunMasterLoop()
    {
        Application.targetFrameRate = 5;
        isTraining = true;
        for (int gen = 0; gen < targetGenerations && isTraining; gen++)
        {
            Debug.Log($"=== MASTER: Generation {currentGeneration} ===");
            CleanupPreviousGen(currentGeneration);
            SignalWorkersReady(currentGeneration);
            yield return StartCoroutine(WaitForWorkerFiles());
            MergeWorkerResults();
            currentGenPool = database.GetByGeneration(currentGeneration).ToList();
            AdjustMutation();
            UpdateChampion();
            BreedNextGeneration();
            PruneDatabase();
            database.SaveToDisk(true);
            Debug.Log($"=== MASTER: {currentGeneration} complete ===");
            currentGeneration++;
        }
        isTraining = false;
        SignalWorkersDone();
        if (quitWhenDone)
        {
            Application.Quit();
        }
    }
    void MergeWorkerResults()
    {
        List<GenomeEntry> masterPopulation = database.GetByGeneration(currentGeneration);
        if (masterPopulation.Count == 0)
        {
            Debug.LogError($"MASTER: Cannot merge generation {currentGeneration}: " + "master population is empty.");
            return;
        }
        int totalMerged = 0;
        for (int worker = 1; worker <= workerCount; worker++)
        {
            string workerPath = TrainingWorkerStorage.GetWorkerFilePath(worker, database.filename);
            if (!File.Exists(workerPath))
            {
                Debug.LogError($"MASTER: Worker {worker} result missing: {workerPath}");
                continue;
            }
            string json = File.ReadAllText(workerPath);
            var wrapper = JsonUtility.FromJson<AutoChessPVPSavedGenomeDataManager.GenomeDatabaseWrapper>(json);
            if (wrapper == null || wrapper.entries == null)
            {
                Debug.LogError($"MASTER: Worker {worker} result could not be loaded.");
                continue;
            }
            Debug.Log($"MASTER: Merging worker {worker}: " + $"{wrapper.entries.Count} entries.");
            foreach (GenomeEntry workerEntry in wrapper.entries)
            {
                if (workerEntry == null){continue;}
                if (workerEntry.generation != currentGeneration)
                {
                    Debug.LogError($"MASTER: Worker {worker} returned wrong generation! " + $"Genome {workerEntry.id} is generation {workerEntry.generation}, " + $"but master expects {currentGeneration}. Skipping.");
                    continue;
                }
                GenomeEntry masterEntry = masterPopulation.FirstOrDefault(e => e != null && e.id == workerEntry.id);
                if (masterEntry == null)
                {
                    Debug.LogWarning($"MASTER: Worker {worker} returned unknown genome " + $"{workerEntry.id}");
                    continue;
                }
                masterEntry.MergeEvaluation(workerEntry);
                totalMerged++;
            }
            if (File.Exists(workerPath))
            {
                try
                {
                    File.Delete(workerPath);
                    Debug.Log($"MASTER: Cleaned up worker {worker} file.");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"MASTER: Could not delete worker {worker} file: {e.Message}");
                }
            }
        }
        Debug.Log($"MASTER: Merge complete. " + $"Merged {totalMerged} worker genome evaluations.");
    }
    IEnumerator RunWorkerLoop()
    {
        Debug.Log($"=== Worker {trainingWorker} starting ===");
        isTraining = true;
        float noSignalTimeout = 300f;
        float lastSignalTime = Time.realtimeSinceStartup;
        while (isTraining)
        {
            string workerDir = TrainingWorkerStorage.GetPersistentPath();
            string donePath = Path.Combine(workerDir, "generations_done.txt");
            if (File.Exists(donePath))
            {
                Debug.Log($"Worker {trainingWorker}: Master signaled done. Exiting.");
                try { File.Delete(donePath); } catch { }
                isTraining = false;
                break;
            }
            Application.targetFrameRate = 5;
            string readyPath = Path.Combine(workerDir, "next_generation_ready.txt");
            while (!File.Exists(readyPath))
            {
                if (File.Exists(donePath))
                {
                    Debug.Log($"Worker {trainingWorker}: Master done while waiting. Exiting.");
                    try { File.Delete(donePath); } catch { }
                    isTraining = false;
                    break;
                }
                if (Time.realtimeSinceStartup - lastSignalTime > noSignalTimeout)
                {
                    Debug.LogWarning($"Worker {trainingWorker}: No signal for {noSignalTimeout}s. Exiting.");
                    isTraining = false;
                    break;
                }
                yield return new WaitForSeconds(1f);
            }
            if (!isTraining){break;}
            Application.targetFrameRate = -1;
            string genStr = File.ReadAllText(readyPath).Trim();
            try { File.Delete(readyPath); } catch { }
            if (!int.TryParse(genStr, out currentGeneration))
            {
                Debug.LogError($"Worker {trainingWorker}: Could not parse generation from ready file.");
                break;
            }
            lastSignalTime = Time.realtimeSinceStartup;
            // Run generation
            float trainingStart = Time.realtimeSinceStartup;
            Debug.Log($"=== Worker {trainingWorker}: Generation {currentGeneration} Starting ===");
            matchesCompletedThisGen = 0;
            database.LoadFromDisk(true);
            List<GenomeEntry> sourcePopulation = database.GetByGeneration(currentGeneration).ToList();
            if (sourcePopulation.Count == 0)
            {
                Debug.LogError($"Worker {trainingWorker}: no genomes for gen {currentGeneration}");
                yield return new WaitForSeconds(3f);
                continue;
            }
            currentGenPool = sourcePopulation.Select(e => e.CreateEvaluationCopy()).ToList();
            yield return RunGenerationPods();
            database.entries = currentGenPool;
            database.SaveToDisk();
            float trainingEnd = Time.realtimeSinceStartup - trainingStart;
            Debug.Log($"Worker {trainingWorker}: Gen {currentGeneration} done in {trainingEnd:F1}s");
        }
        // Cleanup and exit
        Debug.Log($"Worker {trainingWorker}: exiting.");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
        yield break;  // Ensure coroutine ends cleanly
    }
    IEnumerator RunGenerationPods()
    {
        // Build flat schedule: every genome gets exactly matchesPerGenome slots
        List<GenomeEntry> schedule = new();
        foreach (var entry in currentGenPool)
        {
            for (int i = 0; i < matchesPerGenome; i++)
                schedule.Add(entry);
        }
        Shuffle(schedule);
        int podIndex = 0;
        for (int i = 0; i + podSize <= schedule.Count; i += podSize)
        {
            List<GenomeEntry> pod = schedule.GetRange(i, podSize);
            yield return StartCoroutine(RunPodMatch(pod, podIndex++));
            yield return new WaitForSeconds(0.1f);
        }
        int leftover = schedule.Count % podSize;
        if (leftover > 0)
        {
            Debug.LogWarning($"RunGenerationPods: {leftover} genomes left over (not enough for a full pod). " +
                $"Consider adjusting populationSize × matchesPerGenome to be divisible by podSize.");
        }
    }
    IEnumerator RunPodMatch(List<GenomeEntry> pod, int podIndex, bool elo = false)
    {
        GenomeProvider.Clear();
        var allTeams = director.allPlayers.GetAllTeams();
        for (int i = 0; i < allTeams.Count && i < pod.Count; i++)
        {
            GenomeProvider.Assign(allTeams[i], pod[i]);
        }
        float podStart = Time.realtimeSinceStartup;
        director.matchOver = false;
        director.roundCount = 0;
        director.allPlayers.fullAI = true;
        director.allPlayers.NewGameAllDataManagers();
        director.allPlayers.Load();
        while (!director.matchOver && director.roundCount < 30)
        {
            director.AIPrepPhase(true);
            yield return null;
        }
        float podElapsed = Time.realtimeSinceStartup - podStart;
        Debug.Log($"Pod {podIndex} finished in {podElapsed:F1}s over {director.roundCount} rounds");
        if (!elo)
        {
            RecordPodResults(allTeams);
        }
        else
        {
            RecordEloResults(allTeams);
        }
    }
    void RecordPodResults(List<AutoChessDataManager> allTeams)
    {
        var ranked = allTeams.Where(t => !t.PlayerData()).OrderByDescending(t => t.GetRound()).ThenByDescending(t => t.GetHealth()).ToList();
        for (int i = 0; i < ranked.Count; i++)
        {
            var entry = GenomeProvider.Get(ranked[i]);
            if (entry == null) continue;
            int placement = i + 1;
            bool won = placement == 1;
            int rounds = ranked[i].GetRound();
            float hp = ranked[i].GetHealth();
            float finalGold = ranked[i].GetGold();
            float finalLevel = ranked[i].GetLevel();
            float goldSpent = ranked[i].GetTotalGoldSpent();
            float placementScore = placement switch
            {
                1 => 30f, 2 => 25f, 3 => 20f, 4 => 15f,
                5 => 10f, 6 => 5f, 7 => 0f, _ => -5f
            };
            float matchFitness = placementScore + rounds;
            matchFitness += Mathf.Min(FactionFitnessBonus(ranked[i]), 10f);
            if (won)
            {
                matchFitness += 30f;
                matchFitness += hp * 0.5f;
            }
            // Store The Teams.
            entry.matchesPlayed++;
            entry.tacticianHistory.Add(ranked[i].tactician.GetTactician());
            entry.teamHistory.Add(String.Join(",", ranked[i].GetFieldActorNames()));
            entry.benchHistory.Add(String.Join(",", ranked[i].GetBenchActorNames()));
            entry.factionHistory.Add(String.Join(",", ranked[i].factionData.GetActiveFactions()));
            entry.stackHistory.Add(String.Join(",", ranked[i].factionData.GetActiveFactionStacks()));
            entry.equipmentHistory.Add(String.Join(",", ranked[i].GetFieldActorEquipment()));
            entry.fitness += matchFitness;
            entry.avgPlacement = ((entry.avgPlacement * (entry.matchesPlayed - 1)) + placement) / entry.matchesPlayed;
            entry.avgRoundsSurvived = ((entry.avgRoundsSurvived * (entry.matchesPlayed - 1)) + rounds) / entry.matchesPlayed;
            entry.avgFinalGold = ((entry.avgFinalGold * (entry.matchesPlayed - 1)) + finalGold) / entry.matchesPlayed;
            entry.avgFinalLevel = ((entry.avgFinalLevel * (entry.matchesPlayed - 1)) + finalLevel) / entry.matchesPlayed;
            entry.avgGoldSpent = ((entry.avgGoldSpent * (entry.matchesPlayed - 1)) + goldSpent) / entry.matchesPlayed;
            if (won) entry.wins++;
        }
    }
    void RecordEloResults(List<AutoChessDataManager> allTeams)
    {
        var rankedTeams = allTeams.Where(t => !t.PlayerData()).OrderByDescending(t => t.GetRound()).ThenByDescending(t => t.GetHealth()).ToList();
        List<GenomeEntry> rankedChampions = new();
        foreach (var team in rankedTeams)
        {
            GenomeEntry champion = GenomeProvider.Get(team);
            if (champion == null)
            {
                Debug.LogError("Could not find GenomeEntry for team.");
                return;
            }
            rankedChampions.Add(champion);
        }
        if (rankedChampions.Count != podSize)
        {
            Debug.LogError($"Expected {podSize} champions, " + $"got {rankedChampions.Count}.");
            return;
        }
        // Worker: save match log, don't update Elo
        if (trainingMode == TrainingMode.Worker)
        {
            SaveMatchLog(rankedChampions.Select(c => c.id).ToList());
            return;
        }
        UpdateElo(rankedChampions);
        // Save placement history as generations.
        if (tournamentMatchData != null)
        {
            tournamentMatchData.AddMatch(rankedChampions);
        }
        // Tournament-local + lifetime counts.
        foreach (GenomeEntry champion in rankedChampions)
        {
            champion.eloMatches++;
            champion.peakElo = Mathf.Max(champion.peakElo, champion.elo);
        }
        championDatabase.SaveToDisk(true);
    }
    void UpdateElo(List<GenomeEntry> ranked)
    {
        Dictionary<string, float> oldRatings = ranked.ToDictionary(c => c.id, c => c.elo);
        Dictionary<string, float> changes = ranked.ToDictionary(c => c.id, c => 0f);
        float pairK = eloKFactor / Mathf.Max(1, ranked.Count - 1);
        for (int i = 0; i < ranked.Count; i++)
        {
            for (int j = i + 1; j < ranked.Count; j++)
            {
                GenomeEntry winner = ranked[i];
                GenomeEntry loser = ranked[j];
                float winnerRating = oldRatings[winner.id];
                float loserRating = oldRatings[loser.id];
                float expectedWinner = 1f / (1f + Mathf.Pow(10f, (loserRating - winnerRating) / 400f));
                float expectedLoser = 1f - expectedWinner;
                float winnerChange = pairK * (1f - expectedWinner);
                float loserChange = pairK * (0f - expectedLoser);
                changes[winner.id] += winnerChange;
                changes[loser.id] += loserChange;
            }
        }
        foreach (GenomeEntry champion in ranked)
        {
            champion.elo += changes[champion.id];
        }
    }
    float FactionFitnessBonus(AutoChessDataManager team)
    {
        if (team == null){return 0f;}
        var factionData = team.factionData;
        float bonus = 0f;
        // Reward having non-econ active factions.
        // Reward faction stacks, but keep them secondary.
        List<int> stacks = team.factionData.GetActiveFactionStacks();
        for (int i = 0; i < stacks.Count; i++)
        {
            int stackAmount = Mathf.Min(stacks[i], 100);
            bonus += (stackAmount * stackAmount) * 0.001f;
        }
        return bonus;
    }
    void PruneDatabase()
    {
        int minGen = currentGeneration - generationsToKeep;
        if (minGen < 0) return;
        int removed = database.entries.RemoveAll(e => e.generation < minGen);
    }
    void AdjustMutation()
    {
        var avgs = currentGenPool.Where(e => e.matchesPlayed > 0).Select(e => e.fitness / e.matchesPlayed).ToList();
        if (avgs.Count < 2) return;
        float mean = avgs.Average();
        float variance = avgs.Sum(a => (a - mean) * (a - mean)) / avgs.Count;
        float relativeVariance = variance / Mathf.Max(1f, mean);
        if (relativeVariance < 0.15f) // < 15% of mean, population converged, fine-tune
        {
            mutationRate = Mathf.Max(0.05f, mutationRate * 0.95f);
            mutationStrength = Mathf.Max(0.10f, mutationStrength * 0.95f);
        }
        else if (relativeVariance > 0.40f) // > 40% of mean, still exploring, stay aggressive
        {
            mutationRate = Mathf.Min(0.4f, mutationRate * 1.05f);
            mutationStrength = Mathf.Min(0.6f, mutationStrength * 1.05f);
        }
    }
    List<string> GetGenePoolNames()
    {
        List<string> pools = new();
        pools.Add("Base");
        foreach (var template in genePoolTemplates)
        {
            if (!pools.Contains(template.templateName))
            {
                pools.Add(template.templateName);
            }
        }
        return pools;
    }
    string GetRandomDifferentPool(string currentPool, List<string> allPools)
    {
        List<string> candidates = allPools.Where(p => p != currentPool).ToList();
        if (candidates.Count == 0)
            return currentPool;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }
    void BreedNextGeneration()
    {
        int nextGen = currentGeneration + 1;
        List<string> pools = GetGenePoolNames();
        foreach(string pool in pools)
        {
            BreedPool(pool, nextGen, pools);
        }
    }
    void BreedPool(string pool, int nextGen, List<string> allPools)
    {
        var ranked = database.GetByGenePoolAndGeneration(pool, currentGeneration).OrderByDescending(e => e.matchesPlayed > 0 ? e.fitness / e.matchesPlayed : 0f).ToList();
        if(ranked.Count == 0){return;}
        // Elites
        for(int i = 0; i < elites && i < ranked.Count; i++)
        {
            var clone = ranked[i].Clone();
            clone.id = Guid.NewGuid().ToString();
            clone.tag = "elite";
            clone.generation = nextGen;
            database.entries.Add(clone);
        }
        var breedingPool = ranked.Take(Mathf.Max(2, ranked.Count - cullCount)).ToList();
        while(database.GetByGenePoolAndGeneration(pool,nextGen).Count < populationSize)
        {
            string tag = "child";
            var a = TournamentSelect(breedingPool, 3);
            var b = TournamentSelect(breedingPool, 3);
            bool crossbreed = UnityEngine.Random.value < crossbreedRate;
            if (crossbreed)
            {
                // Pick a different pool.
                string donorPool = GetRandomDifferentPool(pool, allPools);
                // Find genomes from that pool in the CURRENT generation.
                var donorPopulation = database.GetByGenePoolAndGeneration(donorPool, currentGeneration).OrderByDescending(e => e.matchesPlayed > 0 ? e.fitness / e.matchesPlayed : 0f).ToList();
                if (donorPopulation.Count > 0)
                {
                    var donorBreedingPool = donorPopulation.Take(Mathf.Max(2, donorPopulation.Count - cullCount)).ToList();
                    b = TournamentSelect(donorBreedingPool, 3);
                    tag = "crossbreed";
                }
            }
            var child = database.AddChild(a, b, nextGen, tag);
            child.genome.Mutate(mutationRate, mutationStrength);
            child.genome.genePool = pool;
        }
    }
    void UpdateChampion()
    {
        if (currentGenPool.Count == 0) return;
        var best = currentGenPool.OrderByDescending(e => e.matchesPlayed > 0 ? e.fitness / e.matchesPlayed : 0f).First();
        float bestAvg = best.matchesPlayed > 0 ? best.fitness / best.matchesPlayed : 0f;
        champion = best.Clone();
        champion.id = best.id;
        champion.tag = "champion";
        champion.generation = currentGeneration;
        championGeneration = currentGeneration;
        // Add this generation's champion to the separate champion database.
        championDatabase.entries.Add(champion);
        championDatabase.SaveToDisk(true);
        Debug.Log($"Champion: Gen {currentGeneration} | " + $"Pool {champion.genePool} | " + $"Avg Fitness {bestAvg:F1}");
    }
    GenomeEntry TournamentSelect(List<GenomeEntry> pool, int size)
    {
        GenomeEntry best = null;
        float bestFit = float.MinValue;
        for (int i = 0; i < size; i++)
        {
            var c = pool[UnityEngine.Random.Range(0, pool.Count)];
            float avg = c.matchesPlayed > 0 ? c.fitness / c.matchesPlayed : 0f;
            if (avg > bestFit) { bestFit = avg; best = c; }
        }
        return best ?? pool[0];
    }
    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
