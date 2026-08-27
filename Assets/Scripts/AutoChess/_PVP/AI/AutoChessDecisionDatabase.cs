using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DecisionEntry
{
    public float[] state;
    public int action;
    public bool[] validMask;
    public float weight;
}
[CreateAssetMenu(fileName = "AutoChessDecisionDatabase", menuName = "ScriptableObjects/AutoChess/AutoChessDecisionDatabase", order = 1)]
public class AutoChessDecisionDatabase : SavedData
{
    public string logFolder = "AutoChessDecisionLogs";
    public string compiledFolder = "datasets";
    public string outputFile = "master_dataset_{0}.bin";
    public int chunkSize = 10000;
    [ContextMenu("Gather Worker Datasets")]
    public void GatherWorkerDatasets()
    {
        string masterCompileDir = Path.Combine(TrainingWorkerStorage.GetPersistentPath(), compiledFolder);
        Directory.CreateDirectory(masterCompileDir);
        string trainingDir = Path.Combine(Application.persistentDataPath, "Training");
        if (!Directory.Exists(trainingDir))
        {
            Debug.Log("No Training folder found");
            return;
        }
        int filesMoved = 0;
        long totalBytes = 0;
        foreach (string workerDir in Directory.GetDirectories(trainingDir, "Worker_*"))
        {
            string workerCompileDir = Path.Combine(workerDir, compiledFolder);
            if (!Directory.Exists(workerCompileDir)) continue;
            foreach (string file in Directory.GetFiles(workerCompileDir, "master_dataset_*.bin"))
            {
                string fileName = Path.GetFileName(file);
                string destPath = Path.Combine(masterCompileDir, fileName);
                // Handle name collisions by appending worker number
                if (File.Exists(destPath))
                {
                    string workerName = Path.GetFileName(workerDir);
                    destPath = Path.Combine(masterCompileDir, $"{workerName}_{fileName}");
                }
                File.Move(file, destPath);  // or File.Copy if you want to keep worker copies
                filesMoved++;
                totalBytes += new FileInfo(destPath).Length;
            }
        }
        Debug.Log($"Gathered {filesMoved} dataset files ({totalBytes / 1024f / 1024f:F2} MB) to {masterCompileDir}");
    }
    [ContextMenu("Compile Decisions")]
    public void CompileDecisions()
    {
        string logDir = Path.Combine(TrainingWorkerStorage.GetPersistentPath(), logFolder);
        string compileDir = Path.Combine(TrainingWorkerStorage.GetPersistentPath(), compiledFolder);
        Directory.CreateDirectory(compileDir);
        var allFiles = Directory.GetFiles(logDir, "*.json");
        var entries = new List<DecisionEntry>();
        foreach (var file in allFiles)
        {
            string json = File.ReadAllText(file);
            var match = JsonUtility.FromJson<AutoChessEncoderLogger.MatchLog>(json);
            if (match?.finalRank < 1 || match?.finalRank > 8) continue;
            float weight = (1f / match.finalRank);
            foreach (var step in match.steps)
            {
                if (step?.state == null || step.state.Length == 0) continue;
                entries.Add(new DecisionEntry
                {
                    state = step.state,
                    action = step.action,
                    validMask = step.validMask ?? new bool[44],
                    weight = weight
                });
            }
        }
        if (entries.Count == 0)
        {
            Debug.Log("No valid decisions to compile");
            return;
        }
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string outPath = Path.Combine(compileDir, string.Format(outputFile, timestamp));
        SaveBinary(entries, outPath);
        Debug.Log($"Saved {entries.Count} decisions to {outPath} ({new FileInfo(outPath).Length / 1024f:F0} KB)");
    }
    void SaveBinary(List<DecisionEntry> entries, string path)
    {
        const int STATE_SIZE = 1619;
        const int MASK_SIZE = 44;
        using (var fs = new FileStream(path, FileMode.Create))
        using (var writer = new BinaryWriter(fs))
        {
            writer.Write(entries.Count);
            writer.Write(STATE_SIZE);
            writer.Write(MASK_SIZE);
            foreach (var e in entries)
            {
                writer.Write(e.action);
                writer.Write(e.weight);
                for (int i = 0; i < STATE_SIZE; i++)
                    writer.Write(e.state[i]);
                for (int i = 0; i < MASK_SIZE; i++)
                    writer.Write(e.validMask[i] ? (byte)1 : (byte)0);
            }
        }
    }
    [ContextMenu("List Compiled Datasets")]
    public void ListCompiledDatasets()
    {
        string compileDir = Path.Combine(TrainingWorkerStorage.GetPersistentPath(), compiledFolder);
        if (!Directory.Exists(compileDir))
        {
            Debug.Log("No compiled datasets folder found");
            return;
        }
        var files = Directory.GetFiles(compileDir, "master_dataset_*.bin")
            .OrderBy(f => f)
            .ToArray();
        Debug.Log($"Found {files.Length} compiled dataset(s):");
        foreach (var file in files)
        {
            var info = new FileInfo(file);
            Debug.Log($"  {Path.GetFileName(file)}: {info.Length / 1024f:F0} KB");
        }
    }
    [ContextMenu("Load Latest Binary Test")]
    public void LoadLatestBinaryTest()
    {
        string compileDir = Path.Combine(TrainingWorkerStorage.GetPersistentPath(), compiledFolder);
        if (!Directory.Exists(compileDir))
        {
            Debug.Log("No compiled datasets folder found");
            return;
        }
        var files = Directory.GetFiles(compileDir, "master_dataset_*.bin")
            .OrderBy(f => f)
            .ToArray();
        if (files.Length == 0)
        {
            Debug.Log("No compiled datasets found");
            return;
        }
        string latest = files.Last();
        Debug.Log($"Loading: {Path.GetFileName(latest)}");
        using (var fs = new FileStream(latest, FileMode.Open))
        using (var reader = new BinaryReader(fs))
        {
            int count = reader.ReadInt32();
            int stateSize = reader.ReadInt32();
            int maskSize = reader.ReadInt32();
            Debug.Log($"Entries: {count}, State: {stateSize}, Mask: {maskSize}");
            int action = reader.ReadInt32();
            float weight = reader.ReadSingle();
            float[] state = new float[stateSize];
            for (int i = 0; i < stateSize; i++) state[i] = reader.ReadSingle();
            bool[] mask = new bool[maskSize];
            for (int i = 0; i < maskSize; i++) mask[i] = reader.ReadByte() > 0;
            Debug.Log($"First entry: action={action}, weight={weight:F3}, gold={state[0]:F3}, maskTrue={mask.Count(x => x)}");
        }
    }
}