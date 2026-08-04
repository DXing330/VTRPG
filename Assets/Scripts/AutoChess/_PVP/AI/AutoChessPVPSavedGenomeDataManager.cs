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
    public int generation;
    public float fitness;
    public int wins;
    public int matchesPlayed;
    public float avgPlacement;
    public string lastTeam;
    public string lastFactions;
    public string lastStacks;
    public string championTeam;
    public string championFactions;
    public string championStacks;
    public AutoChessPVPGenome genome;
    public float WinRate => matchesPlayed > 0 ? (float)wins / matchesPlayed : 0f;
    public GenomeEntry Clone()
    {
        return new GenomeEntry
        {
            id = System.Guid.NewGuid().ToString(),
            tag = tag,
            generation = generation,
            fitness = fitness,
            wins = wins,
            matchesPlayed = matchesPlayed,
            genome = genome?.Copy()
        };
    }
}
[CreateAssetMenu(fileName = "SavedGenomes", menuName = "ScriptableObjects/PVPAI/SavedGenomes", order = 1)]
public class AutoChessPVPSavedGenomeDataManager : ScriptableObject
{
    public List<GenomeEntry> entries = new();
    public List<GenomeEntry> AllEntries => entries;
    public GenomeEntry GetById(string id) => entries.FirstOrDefault(e => e.id == id);
    public List<GenomeEntry> GetByTag(string tag) => entries.Where(e => e.tag == tag).ToList();
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
        var child = new GenomeEntry
        {
            id = System.Guid.NewGuid().ToString(),
            tag = tag,
            generation = generation,
            genome = AutoChessPVPGenome.Crossover(parentA.genome, parentB.genome)
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
    public void SaveToDisk(string filename = "autochess_genome_database.json")
    {
        var wrapper = new GenomeDatabaseWrapper { entries = this.entries };
        string json = JsonUtility.ToJson(wrapper, true);
        string path = Path.Combine(Application.persistentDataPath, filename);
        File.WriteAllText(path, json);
        Debug.Log($"GenomeDatabase saved: {path} ({entries.Count} entries)");
    }
    public void LoadFromDisk(string filename = "autochess_genome_database.json")
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
        Debug.Log($"GenomeDatabase loaded: {entries.Count} entries");
    }
    // --- Quick init for training ---
    public void InitPopulation(int size, int anchorCount = 5)
    {
        Clear();
        for (int i = 0; i < anchorCount; i++) AddDefaultAnchor(generation: 0);
        for (int i = anchorCount; i < size; i++) AddRandom("gen0", generation: 0);
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
}