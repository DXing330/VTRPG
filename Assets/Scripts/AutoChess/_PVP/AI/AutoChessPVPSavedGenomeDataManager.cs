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
    public float elo = 1500f;
    public int eloMatches = 0;
    public float peakElo = 1500f;
    public int wins;
    public int matchesPlayed;
    public float avgPlacement;
    public float avgRoundsSurvived;
    public float avgFinalGold;
    public float avgFinalLevel;
    public float avgGoldSpent;
    public List<string> tacticianHistory = new();
    public List<string> teamHistory = new();
    public List<string> benchHistory = new();
    public List<string> factionHistory = new();
    public List<string> stackHistory = new();
    public List<string> equipmentHistory = new();
    public AutoChessPVPGenome genome;
    public float WinRate => matchesPlayed > 0 ? (float)wins / matchesPlayed : 0f;
    public GenomeEntry Clone()
    {
        return new GenomeEntry
        {
            id = id,
            tag = tag,
            genePool = genePool,
            preferredFaction = preferredFaction,
            generation = generation,
            fitness = fitness,
            elo = elo,
            eloMatches = eloMatches,
            peakElo = peakElo,
            wins = wins,
            matchesPlayed = matchesPlayed,
            avgPlacement = avgPlacement,
            avgRoundsSurvived = avgRoundsSurvived,
            avgFinalGold = avgFinalGold,
            avgFinalLevel = avgFinalLevel,
            avgGoldSpent = avgGoldSpent,
            tacticianHistory = tacticianHistory != null ? new List<string>(tacticianHistory) : new List<string>(),
            teamHistory = teamHistory != null ? new List<string>(teamHistory) : new List<string>(),
            benchHistory = benchHistory != null ? new List<string>(benchHistory) : new List<string>(),
            factionHistory = factionHistory != null ? new List<string>(factionHistory) : new List<string>(),
            stackHistory = stackHistory != null ? new List<string>(stackHistory) : new List<string>(),
            equipmentHistory = equipmentHistory != null ? new List<string>(equipmentHistory) : new List<string>(),
            genome = genome?.Copy()
        };
    }
    // For Use By Parallel Trainers
    public GenomeEntry CreateEvaluationCopy()
    {
        GenomeEntry copy = Clone();
        // Evaluation starts from zero.
        copy.fitness = 0;
        copy.wins = 0;
        copy.matchesPlayed = 0;
        copy.avgPlacement = 0;
        copy.avgRoundsSurvived = 0;
        copy.avgFinalGold = 0;
        copy.avgFinalLevel = 0;
        copy.avgGoldSpent = 0;
        copy.tacticianHistory.Clear();
        copy.teamHistory.Clear();
        copy.benchHistory.Clear();
        copy.factionHistory.Clear();
        copy.stackHistory.Clear();
        copy.equipmentHistory.Clear();
        return copy;
    }
    public void MergeEvaluation(GenomeEntry other)
    {
        if (other == null)
            return;
        if (id != other.id)
        {
            Debug.LogError($"Cannot merge GenomeEntries with different IDs: " + $"{id} != {other.id}"
            );
            return;
        }
        int oldMatches = matchesPlayed;
        int addedMatches = other.matchesPlayed;
        int totalMatches = oldMatches + addedMatches;
        fitness += other.fitness;
        wins += other.wins;
        matchesPlayed = totalMatches;
        if (totalMatches > 0)
        {
            avgPlacement = ((avgPlacement * oldMatches) + (other.avgPlacement * addedMatches)) / totalMatches;
            avgRoundsSurvived = ((avgRoundsSurvived * oldMatches) + (other.avgRoundsSurvived * addedMatches)) / totalMatches;
            avgFinalGold = ((avgFinalGold * oldMatches) + (other.avgFinalGold * addedMatches)) / totalMatches;
            avgFinalLevel = ((avgFinalLevel * oldMatches) + (other.avgFinalLevel * addedMatches)) / totalMatches;
            avgGoldSpent = ((avgGoldSpent * oldMatches) + (other.avgGoldSpent * addedMatches)) / totalMatches;
        }
        if (other.tacticianHistory != null)
            tacticianHistory.AddRange(other.tacticianHistory);
        if (other.teamHistory != null)
            teamHistory.AddRange(other.teamHistory);
        if (other.benchHistory != null)
            benchHistory.AddRange(other.benchHistory);
        if (other.factionHistory != null)
            factionHistory.AddRange(other.factionHistory);
        if (other.stackHistory != null)
            stackHistory.AddRange(other.stackHistory);
        if (other.equipmentHistory != null)
            equipmentHistory.AddRange(other.equipmentHistory);
    }
}
[CreateAssetMenu(fileName = "SavedGenomes", menuName = "ScriptableObjects/PVPAI/SavedGenomes", order = 1)]
public class AutoChessPVPSavedGenomeDataManager : ScriptableObject
{
    public string filename = "autochess_genome_database.json";
    public string geneSchemaFilename = "autochess_genome_schema.json";
    public List<GenomeEntry> entries = new();
    public List<GenomeEntry> AllEntries => entries;
    public GenomeEntry GetById(string id) => entries.FirstOrDefault(e => e.id == id);
    public List<GenomeEntry> GetByTag(string tag) => entries.Where(e => e.tag == tag).ToList();
    public List<GenomeEntry> GetByGeneration(int generation) => entries.Where(e => e.generation == generation).ToList();
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
    [System.Serializable]
    public class GeneSchemaWrapper
    {
        public List<string> geneNames;
    }
    protected string GetSavePath(bool master = false)
    {
        if (master)
        {
            return Application.persistentDataPath + "/" + filename;
        }
        return TrainingWorkerStorage.GetFilePath(filename);
    }
    public void SaveToDisk(bool master = false)
    {
        var wrapper = new GenomeDatabaseWrapper { entries = this.entries };
        string json = JsonUtility.ToJson(wrapper, master);
        string path = GetSavePath(master);
        string tempPath = path + ".tmp";
        File.WriteAllText(tempPath, json);
        if (File.Exists(path)){File.Delete(path);}
        File.Move(tempPath, path);
        Debug.Log($"GenomeDatabase saved: {path} ({entries.Count} entries)");
        // Save current gene schema separately
        var schemaWrapper = new GeneSchemaWrapper
        {
            geneNames = new List<string>(AutoChessPVPGenome.GeneNames)
        };
        string schemaJson = JsonUtility.ToJson(schemaWrapper, true);
        string schemaPath = TrainingWorkerStorage.GetFilePath(geneSchemaFilename);
        File.WriteAllText(schemaPath, schemaJson);
    }
    // Used To Store A Small Slice Of Champions For Use In PVP
    [ContextMenu("Filtered Load")]
    public void FilteredLoadFromDisk()
    {
        string path = GetSavePath(true);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"No save found at {path}");
            return;
        }
        string json = File.ReadAllText(path);
        var wrapper = JsonUtility.FromJson<GenomeDatabaseWrapper>(json);
        List<GenomeEntry> savedEntries = wrapper.entries;
        // Select The Top 100 By Elo?
        // Automatically discover factions from the database.
        List<string> factions = savedEntries.Select(e => e.preferredFaction).Distinct().ToList();
        int championsPerFaction = 12;
        List<GenomeEntry> selected = new List<GenomeEntry>();
        for (int i = 0; i < factions.Count; i++)
        {
            List<GenomeEntry> factionChampions = savedEntries.Where(e => e.preferredFaction == factions[i]).OrderByDescending(e => e.elo).Take(championsPerFaction).ToList();
            for (int j = 0; j < factionChampions.Count; j++)
            {
                selected.Add(factionChampions[j].Clone());
            }
        }
        entries = selected;
        #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        #endif
    }
    public void LoadFromDisk(bool master = false)
    {
        string path = GetSavePath(master);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"No save found at {path}");
            return;
        }
        string json = File.ReadAllText(path);
        var wrapper = JsonUtility.FromJson<GenomeDatabaseWrapper>(json);
        this.entries = wrapper.entries ?? new List<GenomeEntry>();
        // Auto-heal old genomes whenever the database is loaded
        if (master)
        {
            bool changed = MigrateGenomeSizes();
            string schemaPath = TrainingWorkerStorage.GetFilePath(geneSchemaFilename);
            if (File.Exists(schemaPath))
            {
                string schemaJson = File.ReadAllText(schemaPath);
                GeneSchemaWrapper schema = JsonUtility.FromJson<GeneSchemaWrapper>(schemaJson);
                List<string> oldGeneNames = schema.geneNames;
                changed |= MigrateGenomeGeneNames(oldGeneNames);
            }
            changed |= MigratePreferredFactions();
            if (changed)
            {
                SaveToDisk(master);
            }
        }
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
    public void AddGenePool(AutoChessPVPGenomeTemplate template, int size, int generation = 0, float initialMutationRate = 0.15f, float initialMutationStrength = 1f, bool overwriteExisting = true)
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
        if (!overwriteExisting)
        {
            if (HasGenePool(poolName))
            {
                Debug.Log($"Gene pool '{poolName}' already exists. Skipping creation.");
                return;
            }
        }
        else
        {
            int removedCount = entries.RemoveAll(entry => entry != null && entry.genePool == poolName);
            if (removedCount > 0)
            {
                Debug.Log($"Overwriting gene pool '{poolName}'. " + $"Removed {removedCount} existing genomes.");
            }
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
    public bool MigrateGenomeSizes()
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
        }
        return changed;
    }
    public bool MigrateGenomeGeneNames(List<string> oldGeneNames)
    {
        bool changed = false;
        List<string> currentGeneNames = AutoChessPVPGenome.GeneNames;
        for (int i = 0; i < currentGeneNames.Count; i++)
        {
            if (oldGeneNames[i] != currentGeneNames[i])
            {
                Debug.Log($"Migrated {oldGeneNames[i]} genome to {currentGeneNames[i]} with {AutoChessPVPGenome.Defaults[i]} default value.");
                changed = true;
                foreach (var entry in entries)
                {
                    entry.genome.genes[i] = AutoChessPVPGenome.Defaults[i];
                }
            }
        }
        return changed;
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
    public bool MigratePreferredFactions()
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
            Debug.Log($"Preferred faction migration complete. " + $"Repaired {repaired} entries.");
        }
        return changed;
    }
    [ContextMenu("Manually Migrate Old Gene Names")]
    private void MigrateUsingOldGeneNames()
    {
        string path = GetSavePath();
        string json = File.ReadAllText(path);
        var wrapper = JsonUtility.FromJson<GenomeDatabaseWrapper>(json);
        this.entries = wrapper.entries ?? new List<GenomeEntry>();
        List<string> oldGeneNames = new List<string>
        {
        };
        bool changed = MigrateGenomeGeneNames(oldGeneNames);
        if (changed)
        {
            SaveToDisk();
        }
    }
}