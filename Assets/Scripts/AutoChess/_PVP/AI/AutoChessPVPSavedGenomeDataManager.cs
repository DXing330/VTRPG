using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GenomeEntry
{
    public string id; // unique guid
    public string tag; // "anchor", "gen1", "champion", "mutant", etc.
    public string genePool;
    public string preferredFaction;
    public int generation;
    public float fitness;
    public int wins;
    public int matchesPlayed;
    public float avgPlacement;
    public float avgRoundsSurvived;
    public float avgFinalGold;
    public float avgFinalLevel;
    public float avgGoldSpent;
    public List<string> teamHistory = new();
    public List<string> benchHistory = new();
    public List<string> factionHistory = new();
    public List<string> stackHistory = new();
    public List<string> equipmentHistory = new();
    public string championTeam;
    public string championBench;
    public string championFactions;
    public string championStacks;
    public string championEquipment;
    public AutoChessPVPGenome genome;
    public float WinRate => matchesPlayed > 0 ? (float)wins / matchesPlayed : 0f;
    public GenomeEntry Clone()
    {
        return new GenomeEntry
        {
            id = System.Guid.NewGuid().ToString(),
            tag = tag,
            genePool = genePool,
            preferredFaction = preferredFaction,
            generation = generation,
            fitness = fitness,
            wins = wins,
            matchesPlayed = matchesPlayed,
            avgPlacement = avgPlacement,
            avgRoundsSurvived = avgRoundsSurvived,
            avgFinalGold = avgFinalGold,
            avgFinalLevel = avgFinalLevel,
            avgGoldSpent = avgGoldSpent,
            teamHistory = teamHistory != null ? new List<string>(teamHistory) : new List<string>(),
            benchHistory = benchHistory != null ? new List<string>(benchHistory) : new List<string>(),
            factionHistory = factionHistory != null ? new List<string>(factionHistory) : new List<string>(),
            stackHistory = stackHistory != null ? new List<string>(stackHistory) : new List<string>(),
            equipmentHistory = equipmentHistory != null ? new List<string>(equipmentHistory) : new List<string>(),
            championTeam = championTeam,
            championBench = championBench,
            championFactions = championFactions,
            championStacks = championStacks,
            championEquipment = championEquipment,
            genome = genome?.Copy()
        };
    }
}
[CreateAssetMenu(fileName = "SavedGenomes", menuName = "ScriptableObjects/PVPAI/SavedGenomes", order = 1)]
public class AutoChessPVPSavedGenomeDataManager : ScriptableObject
{
    public string filename = "autochess_genome_database.json";
    public List<GenomeEntry> entries = new();
    public List<GenomeEntry> AllEntries => entries;
    public GenomeEntry GetById(string id) => entries.FirstOrDefault(e => e.id == id);
    public List<GenomeEntry> GetByTag(string tag) => entries.Where(e => e.tag == tag).ToList();
    public List<GenomeEntry> GetByGenePool(string pool)
    {
        return entries.Where(e => e.genePool == pool).ToList();
    }
    public List<GenomeEntry> GetByGenePoolAndGeneration(string pool, int generation)
    {
        return entries.Where(e => e.genePool == pool && e.generation == generation).ToList();
    }
    public GenomeEntry GetRandom() => entries.Count > 0 ? entries[UnityEngine.Random.Range(0, entries.Count)] : null;
    public GenomeEntry GetTopFitness(int generationFilter = -1)
    {
        var pool = generationFilter < 0 ? entries : entries.Where(e => e.generation == generationFilter).ToList();
        if (pool.Count == 0) return null;
        return pool.OrderByDescending(e => e.fitness).First();
    }
    public List<GenomeEntry> GetTopN(int n, int generationFilter = -1)
    {
        var pool = generationFilter < 0 ? entries : entries.Where(e => e.generation == generationFilter).ToList();
        return pool.OrderByDescending(e => e.fitness).Take(n).ToList();
    }
    public GenomeEntry AddDefaultAnchor(int generation = 0)
    {
        var entry = new GenomeEntry
        {
            id = System.Guid.NewGuid().ToString(),
            tag = "anchor",
            generation = generation,
            genome = AutoChessPVPGenome.CreateDefault()
        };
        entries.Add(entry);
        return entry;
    }
    public GenomeEntry AddRandom(string tag = "random", int generation = 0)
    {
        var entry = new GenomeEntry
        {
            id = System.Guid.NewGuid().ToString(),
            tag = tag,
            generation = generation,
            genome = AutoChessPVPGenome.RandomGenome()
        };
        entries.Add(entry);
        return entry;
    }
    public GenomeEntry AddChild(GenomeEntry parentA, GenomeEntry parentB, int generation, string tag = "child")
    {
        AutoChessPVPGenome childGenome =
        AutoChessPVPGenome.Crossover(parentA.genome, parentB.genome);
        childGenome.genePool = parentA.genePool;
        childGenome.preferredFaction = parentA.preferredFaction;
        var child = new GenomeEntry
        {
            id = System.Guid.NewGuid().ToString(),
            tag = tag,
            generation = generation,
            genePool = parentA.genePool,
            preferredFaction = parentA.preferredFaction,
            genome = childGenome
        };
        entries.Add(child);
        return child;
    }
    public void Clear() => entries.Clear();
    public void RemoveWorstN(int n)
    {
        var toRemove = entries.OrderBy(e => e.fitness).Take(n).ToList();
        foreach (var r in toRemove) entries.Remove(r);
    }
    [System.Serializable]
    public class GenomeDatabaseWrapper
    {
        public List<GenomeEntry> entries;
    }
    public void SaveToDisk()
    {
        var wrapper = new GenomeDatabaseWrapper { entries = this.entries };
        string json = JsonUtility.ToJson(wrapper, true);
        string path = Path.Combine(Application.persistentDataPath, filename);
        File.WriteAllText(path, json);
        Debug.Log($"GenomeDatabase saved: {path} ({entries.Count} entries)");
    }
    public void LoadFromDisk()
    {
        string path = Path.Combine(Application.persistentDataPath, filename);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"No save found at {path}");
            return;
        }
        string json = File.ReadAllText(path);
        var wrapper = JsonUtility.FromJson<GenomeDatabaseWrapper>(json);
        this.entries = wrapper.entries ?? new List<GenomeEntry>();
        // Auto-heal old genomes whenever the database is loaded
        MigrateGenomeSizes();
        MigratePreferredFactions();
        Debug.Log($"GenomeDatabase loaded: {entries.Count} entries");
    }
    // --- Quick init for training ---
    public void InitPopulation(int size, int anchorCount = 5)
    {
        Clear();
        for (int i = 0; i < anchorCount; i++) AddDefaultAnchor(generation: 0);
        for (int i = anchorCount; i < size; i++) AddRandom("gen0", generation: 0);
    }
    public void AssignUnassignedToBasePool()
    {
        foreach (var entry in entries)
        {
            if (entry == null)
                continue;
            if (string.IsNullOrEmpty(entry.genePool))
            {
                entry.genePool = "Base";
                if (entry.genome != null)
                    entry.genome.genePool = "Base";
            }
        }
    }
    // Quick Init To Start A Population
    public void AddGenePool(AutoChessPVPGenomeTemplate template, int size, int generation = 0, float initialMutationRate = 0.15f, float initialMutationStrength = 1f)
    {
        if (template == null)
        {
            Debug.LogError("Cannot create gene pool: template is null.");
            return;
        }
        string poolName = template.templateName;
        if (string.IsNullOrEmpty(poolName))
        {
            Debug.LogError("Cannot create gene pool: template has no templateName.");
            return;
        }
        int removedCount = entries.RemoveAll(entry => entry != null && entry.genePool == poolName);
        if (removedCount > 0)
        {
            Debug.Log($"Overwriting gene pool '{poolName}'. " + $"Removed {removedCount} existing genomes.");
        }
        for (int i = 0; i < size; i++)
        {
            AutoChessPVPGenome genome = template.CreateGenome();
            // Keep one genome exactly at the template.
            // Mutate the rest to create initial diversity.
            if (i > 0)
            {
                genome.Mutate(initialMutationRate, initialMutationStrength);
            }
            genome.genePool = poolName;
            genome.preferredFaction = template.preferredFaction;
            GenomeEntry entry = new GenomeEntry
            {
                id = Guid.NewGuid().ToString(),
                tag = $"gen{generation}",
                generation = generation,
                genePool = poolName,
                preferredFaction = template.preferredFaction,
                genome = genome,
            };
            entries.Add(entry);
        }
        Debug.Log($"Created gene pool '{poolName}' with {size} genomes.");
    }
    public bool HasGenePool(string poolName)
    {
        return entries.Any(e => e != null && e.genePool == poolName);
    }
    public List<GenomeEntry> GetGenePool(string poolName)
    {
        return entries.Where(e => e != null && e.genePool == poolName).ToList();
    }
    // For Updating After Expanding Genome.
    public void MigrateGenomeSizes()
    {
        bool changed = false;
        int targetLength = AutoChessPVPGenome.Defaults.Length;
        foreach (var entry in entries)
        {
            if (entry?.genome == null) continue;
            if (entry.genome.genes.Length == targetLength) continue;
            float[] old = entry.genome.genes;
            float[] expanded = new float[targetLength];
            // Copy whatever old genes exist
            for (int i = 0; i < Mathf.Min(old.Length, targetLength); i++)
                expanded[i] = old[i];
            // Backfill any new slots with current defaults
            for (int i = old.Length; i < targetLength; i++)
                 expanded[i] = AutoChessPVPGenome.Defaults[i];
             entry.genome.genes = expanded;
             changed = true;
        }
        if (changed)
        {
            Debug.Log($"Migrated {entries.Count} genomes to {targetLength} genes.");
            SaveToDisk(); // persist immediately
        }
    }
    bool IsValidPreferredFaction(string faction)
    {
        switch (faction)
        {
            case "Yan":
            case "Sargon":
            case "Kjerag":
            case "Aegir":
            case "Victoria":
            case "Laterano":
                return true;
            default:
                return false;
        }
    }
    public void MigratePreferredFactions()
    {
        bool changed = false;
        int repaired = 0;
        foreach (var entry in entries)
        {
            if (entry == null)
                continue;
            // If preferred faction is already assigned,
            // make sure the genome agrees with it.
            if (!string.IsNullOrEmpty(entry.preferredFaction))
            {
                if (entry.genome != null &&
                    entry.genome.preferredFaction != entry.preferredFaction)
                {
                    entry.genome.preferredFaction = entry.preferredFaction;
                    changed = true;
                }
                continue;
            }
            // If preferred faction is missing,
            // try to recover it from the gene pool name.
            if (!string.IsNullOrEmpty(entry.genePool))
            {
                string recoveredFaction = entry.genePool;

                // Only recover if this is actually one
                // of your faction gene pools.
                if (IsValidPreferredFaction(recoveredFaction))
                {
                    entry.preferredFaction = recoveredFaction;

                    if (entry.genome != null)
                    {
                        entry.genome.preferredFaction = recoveredFaction;
                        entry.genome.genePool = entry.genePool;
                    }

                    repaired++;
                    changed = true;
                }
            }
        }
        if (changed)
        {
            SaveToDisk();
            Debug.Log(
                $"Preferred faction migration complete. " +
                $"Repaired {repaired} entries."
            );
        }
    }
}