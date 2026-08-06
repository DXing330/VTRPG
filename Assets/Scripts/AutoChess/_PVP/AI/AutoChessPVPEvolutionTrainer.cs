using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GenomeProvider
{
    private static Dictionary<AutoChessDataManager, GenomeEntry> map = new();
    public static void Assign(AutoChessDataManager team, GenomeEntry entry) => map[team] = entry;
    public static GenomeEntry Get(AutoChessDataManager team) => map.TryGetValue(team, out var e) ? e : null;
    public static void Clear() => map.Clear();
}

public class AutoChessPVPEvolutionTrainer : MonoBehaviour
{
    [Header("Headless Mode")]
    public bool autoStartTraining = false;
    public int targetGenerations = 100;
    public bool quitWhenDone = false;
    void Awake()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--train":
                    autoStartTraining = true;
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
            }
        }

        Debug.Log(
            $"Training Config: " +
            $"autoStart={autoStartTraining}, " +
            $"gens={targetGenerations}, " +
            $"pop={populationSize}, " +
            $"matches={matchesPerGenome}, " +
            $"pod={podSize}, " +
            $"elite={elites}, " +
            $"cull={cullCount}, " +
            $"mutRate={mutationRate}, " +
            $"mutStrength={mutationStrength}, " +
            $"keep={generationsToKeep}, " +
            $"quit={quitWhenDone}"
        );
    }
    [Header("References")]
    public AutoChessPVPSavedGenomeDataManager database;
    protected void LoadDB()
    {
        database.LoadFromDisk();
        if (database.AllEntries.Count == 0)
        {
            database.InitPopulation(populationSize);
        }
        else
        {
            int maxGen = 0;
            RestoreChampion();
            foreach (var e in database.AllEntries) maxGen = Mathf.Max(maxGen, e.generation);
            currentGeneration = maxGen;
            Debug.Log($"Resuming at generation {currentGeneration} with {database.AllEntries.Count} genomes.");
        }
        if (autoStartTraining)
        {
            StartCoroutine(RunTrainingLoop());
        }
    }
    public AutoChessPVPMatchDirector director;
    public AutoChessAIPrepController aiController;
    [Header("Evolution Settings")]
    public int populationSize = 40;
    public int matchesPerGenome = 6;
    public int podSize = 8;
    public int elites = 4;
    public int cullCount = 8;
    public float mutationRate = 0.15f;
    public float mutationStrength = 1f;
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
        }
        catch (System.Exception e)
        {
            Debug.LogError("CRASH IN START: " + e);
        }
    }
    [ContextMenu("Start Training")]
    public void StartTraining() => StartCoroutine(RunTrainingLoop());
    [ContextMenu("Stop Training")]
    public void StopTraining() => isTraining = false;
    IEnumerator RunTrainingLoop()
    {
        isTraining = true;
        Application.targetFrameRate = 10; // prevent overheating
        int startGen = currentGeneration;
        while (isTraining && (currentGeneration - startGen) < targetGenerations)
        {
            Debug.Log($"=== Generation {currentGeneration} Starting ===");
            matchesCompletedThisGen = 0;
            string genTag = $"gen{currentGeneration}";
            currentGenPool = database.GetByTag(genTag).ToList();
            if (currentGenPool.Count == 0)
            {
                currentGenPool = database.AllEntries.ToList();
                foreach (var e in currentGenPool) 
                {
                    e.generation = currentGeneration;
                    e.tag = genTag;
                }
            }
            foreach (var entry in currentGenPool)
            {
                entry.fitness = 0;
                entry.wins = 0;
                entry.matchesPlayed = 0;
                entry.avgPlacement = 0;
                entry.avgRoundsSurvived = 0;
                entry.avgFinalGold = 0;
                entry.avgFinalLevel = 0;
                entry.avgGoldSpent = 0;
                entry.teamHistory.Clear();
                entry.benchHistory.Clear();
                entry.factionHistory.Clear();
                entry.stackHistory.Clear();
                entry.equipmentHistory.Clear();
            }
            yield return RunGenerationPods();
            if (!isTraining) break;
            AdjustMutation();
            BreedNextGeneration();
            UpdateChampion();
            PersistChampion();
            PruneDatabase();
            database.SaveToDisk();
            if ((currentGeneration - startGen) >= targetGenerations - 1)
            {
                Debug.Log($"Training complete. Reached generation {currentGeneration}.");
                isTraining = false;
            }
            else
            {
                currentGeneration++;
                yield return new WaitForSeconds(0.5f); // cool-down between generations
            }
        }
        database.SaveToDisk();
        PersistChampion();
        Debug.Log("Final save complete.");
        Application.targetFrameRate = -1;
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
    IEnumerator RunGenerationPods()
    {
        var shuffled = new List<GenomeEntry>(currentGenPool);
        Shuffle(shuffled);
        int podsPerGeneration = Mathf.CeilToInt((currentGenPool.Count * matchesPerGenome) / (float)podSize);
        for (int i = 0; i < podsPerGeneration; i++)
        {
            var pod = new List<GenomeEntry>();
            for (int j = 0; j < podSize && j < shuffled.Count; j++)
            {
                if (champion != null && i == 0 && j == 0)
                {
                    pod.Add(champion);
                    continue;
                }
                int idx = (i * podSize + j) % shuffled.Count;
                pod.Add(shuffled[idx]);
            }
            yield return StartCoroutine(RunPodMatch(pod, i));
            yield return new WaitForSeconds(0.2f); // cool-down between pods
        }
    }
    IEnumerator RunPodMatch(List<GenomeEntry> pod, int podIndex)
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
        RecordPodResults(allTeams, podIndex);
    }
    void RecordPodResults(List<AutoChessDataManager> allTeams, int podIndex)
    {
        var ranked = allTeams.Where(t => !t.PlayerData()).OrderByDescending(t => t.GetRound()).ToList();
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
            entry.teamHistory.Add(String.Join(",", ranked[i].GetFieldActorNames()));
            entry.benchHistory.Add(String.Join(",", ranked[i].GetBenchActorNames()));
            entry.factionHistory.Add(String.Join(",", ranked[i].factionData.GetActiveFactions()));
            entry.stackHistory.Add(String.Join(",", ranked[i].factionData.GetActiveFactionStacks()));
            entry.equipmentHistory.Add(String.Join(",", ranked[i].GetFieldActorEquipment()));
            entry.matchesPlayed++;
            entry.fitness += matchFitness;
            entry.avgPlacement = ((entry.avgPlacement * (entry.matchesPlayed - 1)) + placement) / entry.matchesPlayed;
            entry.avgRoundsSurvived = ((entry.avgRoundsSurvived * (entry.matchesPlayed - 1)) + rounds) / entry.matchesPlayed;
            entry.avgFinalGold = ((entry.avgFinalGold * (entry.matchesPlayed - 1)) + finalGold) / entry.matchesPlayed;
            entry.avgFinalLevel = ((entry.avgFinalLevel * (entry.matchesPlayed - 1)) + finalLevel) / entry.matchesPlayed;
            entry.avgGoldSpent = ((entry.avgGoldSpent * (entry.matchesPlayed - 1)) + goldSpent) / entry.matchesPlayed;
            if (won) entry.wins++;
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
        int removed = database.entries.RemoveAll(e => e.generation < minGen && e.tag != "champion");
    }
    void PersistChampion()
    {
        if (champion == null) return;
        database.entries.RemoveAll(e => e.tag == "champion");
        var entry = champion.Clone();
        entry.tag = "champion";
        entry.generation = championGeneration;
        database.entries.Add(entry);
    }
    void RestoreChampion()
    {
        var saved = database.entries.FirstOrDefault(e => e.tag == "champion");
        if (saved != null)
        {
            champion = saved.Clone();
            champion.id = System.Guid.NewGuid().ToString();
            championGeneration = champion.generation;
            Debug.Log($"Restored champion from generation {championGeneration}");
        }
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
            mutationStrength = Mathf.Max(0.3f, mutationStrength * 0.95f);
        }
        else if (relativeVariance > 0.40f) // > 40% of mean, still exploring, stay aggressive
        {
            mutationRate = Mathf.Min(0.25f, mutationRate * 1.05f);
            mutationStrength = Mathf.Min(1.5f, mutationStrength * 1.05f);
        }
    }
    void BreedNextGeneration()
    {
        int nextGen = currentGeneration + 1;
        string nextTag = $"gen{nextGen}";
        var ranked = currentGenPool.OrderByDescending(e => e.matchesPlayed > 0 ? e.fitness / e.matchesPlayed : 0f).ToList();
        // Add the champion to the pool
        var championClone = champion?.Clone();
        if(championClone != null)
        {
            championClone.tag = nextTag;
            championClone.generation = nextGen;
            database.entries.Add(championClone);
        }
        // Elitism
        for (int i = 0; i < elites && i < ranked.Count; i++)
        {
            var clone = ranked[i].Clone();
            clone.tag = nextTag;
            clone.generation = nextGen;
            database.entries.Add(clone);
        }
        // Breeding pool (cull bottom)
        var breedingPool = ranked.Take(ranked.Count - Mathf.Min(cullCount, ranked.Count - elites)).ToList();
        if (breedingPool.Count < 2) breedingPool = ranked;
        // Fill rest
        while (database.GetByTag(nextTag).Count < populationSize)
        {
            var a = TournamentSelect(breedingPool, 3);
            var b = TournamentSelect(breedingPool, 3);
            var child = database.AddChild(a, b, nextGen, nextTag);
            child.genome.Mutate(mutationRate, mutationStrength);
        }
        // Diversity injection every 5 gens
        if (nextGen % 5 == 0)
        {
            var fresh = database.GetByTag(nextTag);
            int inject = Mathf.Min(3, fresh.Count);
            for (int i = 0; i < inject; i++)
            {
                fresh[fresh.Count - 1 - i].genome = AutoChessPVPGenome.RandomGenome();
            }
        }
        float topAvg = ranked[0].matchesPlayed > 0 ? ranked[0].fitness / ranked[0].matchesPlayed : 0f;
    }
    void UpdateChampion()
    {
        if (currentGenPool.Count == 0) return;
        var best = currentGenPool.OrderByDescending(e => e.matchesPlayed > 0 ? e.fitness / e.matchesPlayed : 0f).First();
        float bestAvg = best.matchesPlayed > 0 ? best.fitness / best.matchesPlayed : 0f;
        if (champion == null)
        {
            champion = best.Clone();
            champion.id = System.Guid.NewGuid().ToString();
            champion.tag = "champion";
            champion.generation = currentGeneration;
            champion.championTeam = best.teamHistory.Count > 0 ? best.teamHistory[^1] : "";
            champion.championBench = best.benchHistory.Count > 0 ? best.benchHistory[^1] : "";
            champion.championFactions = best.factionHistory.Count > 0 ? best.factionHistory[^1] : "";
            champion.championStacks = best.stackHistory.Count > 0 ? best.stackHistory[^1] : "";
            champion.championEquipment = best.equipmentHistory.Count > 0 ? best.equipmentHistory[^1] : "";
            return;
        }
        float champAvg = champion.matchesPlayed > 0 ? champion.fitness / champion.matchesPlayed : 0f;
        Debug.Log($"Benchmark: Gen {currentGeneration} best {bestAvg:F1} vs Champion (gen {championGeneration}) {champAvg:F1}");
        if (bestAvg > champAvg * 1.02f) // must beat by 2% to dethrone
        {
            champion = best.Clone();
            champion.id = System.Guid.NewGuid().ToString();
            champion.tag = "champion";
            champion.generation = currentGeneration;
            champion.championTeam = best.teamHistory.Count > 0 ? best.teamHistory[^1] : "";
            champion.championBench = best.benchHistory.Count > 0 ? best.benchHistory[^1] : "";
            champion.championFactions = best.factionHistory.Count > 0 ? best.factionHistory[^1] : "";
            champion.championStacks = best.stackHistory.Count > 0 ? best.stackHistory[^1] : "";
            champion.championEquipment = best.equipmentHistory.Count > 0 ? best.equipmentHistory[^1] : "";
            championGeneration = currentGeneration;
            Debug.Log($"New champion! Gen {currentGeneration}: {bestAvg:F1}");
        }
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
