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
    public float value;
    public int actionType;
    public int actionIndex;
    public bool[] actionTypeMask; // 7
    public bool[] buyMask; // 6
    public bool[] sellBenchMask; // 12
    public bool[] placeBenchMask; // 12
    public bool[] fieldMask;      // 11
    public float oldLogProb;
    public float oldValue;
    public int finalRank;
}
[CreateAssetMenu(fileName = "AutoChessDecisionDatabase", menuName = "ScriptableObjects/AutoChess/AutoChessDecisionDatabase", order = 1)]
public class AutoChessDecisionDatabase : SavedData
{
    public string logFolder = "AutoChessDecisionLogs";
    public string compiledFolder = "CompiledDatasets";
    public string championCompiledFolder = "ChampionDatasets";  // NEW: separate folder for champion data
    public string outputFile = "master_dataset_{0}.bin";
    public int chunkSize = 10000;
    [ContextMenu("Clear Worker Decision Data")]
    public void ClearWorkerDecisionData()
    {
        string basePath = TrainingWorkerStorage.GetPersistentPath();
        // Don't Remove The Master Files.
        if (basePath == Application.persistentDataPath){return;}
        string[] folders =
        {
            logFolder,
            compiledFolder
        };
        int deletedFiles = 0;
        foreach (string folder in folders)
        {
            string path = Path.Combine(basePath, folder);
            if (!Directory.Exists(path))
                continue;
            foreach (string file in Directory.GetFiles(path))
            {
                try
                {
                    File.Delete(file);
                    deletedFiles++;
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        $"Could not delete decision file {file}: {e.Message}");
                }
            }
        }
        Debug.Log(
            $"Cleared worker decision data: {deletedFiles} files deleted.");
    }
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
    [ContextMenu("Gather Champion Datasets")]
    public void GatherChampionDatasets()
    {
        string masterChampionDir = Path.Combine(TrainingWorkerStorage.GetPersistentPath(), championCompiledFolder);
        Directory.CreateDirectory(masterChampionDir);
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
            // Champion tournament workers compile normally into CompiledDatasets.
            string workerCompileDir = Path.Combine(workerDir, compiledFolder);
            if (!Directory.Exists(workerCompileDir))
                continue;
            foreach (string file in Directory.GetFiles(workerCompileDir, "master_dataset_*.bin"))
            {
                string fileName = Path.GetFileName(file);
                // Put the worker's compiled dataset into the MASTER ChampionDatasets.
                string destPath = Path.Combine(masterChampionDir, fileName);
                // Prevent workers with identical timestamps from overwriting each other.
                if (File.Exists(destPath))
                {
                    string workerName = Path.GetFileName(workerDir);
                    destPath = Path.Combine(masterChampionDir, $"{workerName}_{fileName}");
                }
                long fileSize = new FileInfo(file).Length;
                File.Move(file, destPath);
                filesMoved++;
                totalBytes += fileSize;
            }
        }
        Debug.Log($"Gathered {filesMoved} champion dataset files " + $"({totalBytes / 1024f / 1024f:F2} MB) to {masterChampionDir}");
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
            if (match == null || match.finalRank < 1 || match.finalRank > 8)
            {
                Debug.LogWarning($"Skipping {file}: invalid finalRank={match?.finalRank}");
                continue;
            }
            float weight = (0.125f * (9 - match.finalRank));
            float value = 1f - 2f * ((match.finalRank - 1) / 7f);
            foreach (var step in match.steps)
            {
                if (step?.state == null || step.state.Length == 0) continue;
                entries.Add(new DecisionEntry
                {
                    state = step.state,
                    action = step.action,
                    validMask = step.validMask ?? new bool[44],
                    weight = weight,
                    value = value,
                    actionType = step.actionType,
                    actionIndex = step.actionIndex,
                    actionTypeMask = step.actionTypeMask ?? new bool[7],
                    buyMask = step.buyMask ?? new bool[6],
                    sellBenchMask = step.sellBenchMask ?? new bool[12],
                    placeBenchMask = step.placeBenchMask ?? new bool[12],
                    fieldMask = step.fieldMask ?? new bool[11],
                    oldLogProb = step.oldLogProb,
                    oldValue = step.oldValue,
                    finalRank = match.finalRank
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
        const int STATE_SIZE = 1918;
        const int MASK_SIZE = 44;
        const int ACTION_TYPE_COUNT = 7;
        const int BUY_COUNT = 6;
        const int BENCH_COUNT = 12;
        const int FIELD_COUNT = 11;
        using (var fs = new FileStream(path, FileMode.Create))
        using (var writer = new BinaryWriter(fs))
        {
            writer.Write(entries.Count);
            writer.Write(STATE_SIZE);
            writer.Write(MASK_SIZE);
            writer.Write(ACTION_TYPE_COUNT);
            writer.Write(BUY_COUNT);
            writer.Write(BENCH_COUNT);
            writer.Write(FIELD_COUNT);
            foreach (var e in entries)
            {
                writer.Write(e.action);
                writer.Write(e.actionType);
                writer.Write(e.actionIndex);
                writer.Write(e.weight);
                writer.Write(e.value);
                writer.Write(e.oldLogProb);
                writer.Write(e.oldValue);
                writer.Write(e.finalRank);
                for (int i = 0; i < STATE_SIZE; i++)
                    writer.Write(e.state[i]);
                for (int i = 0; i < MASK_SIZE; i++)
                    writer.Write(e.validMask[i] ? (byte)1 : (byte)0);
                for (int i = 0; i < ACTION_TYPE_COUNT; i++)
                {
                    bool value = e.actionTypeMask != null && i < e.actionTypeMask.Length && e.actionTypeMask[i];
                    writer.Write(value ? (byte)1 : (byte)0);
                }
                // ------------------------------------
                // BUY MASK
                // ------------------------------------
                for (int i = 0; i < BUY_COUNT; i++)
                {
                    bool value = e.buyMask != null && i < e.buyMask.Length && e.buyMask[i];
                    writer.Write(value ? (byte)1 : (byte)0);
                }
                // ------------------------------------
                // SELL BENCH MASK
                // ------------------------------------
                for (int i = 0; i < BENCH_COUNT; i++)
                {
                    bool value = e.sellBenchMask != null && i < e.sellBenchMask.Length && e.sellBenchMask[i];
                    writer.Write(value ? (byte)1 : (byte)0);
                }
                // ------------------------------------
                // PLACE BENCH MASK
                // ------------------------------------
                for (int i = 0; i < BENCH_COUNT; i++)
                {
                    bool value = e.placeBenchMask != null && i < e.placeBenchMask.Length && e.placeBenchMask[i];
                    writer.Write(value ? (byte)1 : (byte)0);
                }
                // ------------------------------------
                // FIELD MASK
                // ------------------------------------
                for (int i = 0; i < FIELD_COUNT; i++)
                {
                    bool value = e.fieldMask != null && i < e.fieldMask.Length && e.fieldMask[i];
                    writer.Write(value ? (byte)1 : (byte)0);
                }
            }
        }
    }
    [ContextMenu("Load Latest Binary Test")]
    public void LoadLatestBinaryTest()
    {
        string compileDir =
            Path.Combine(
                TrainingWorkerStorage.GetPersistentPath(),
                compiledFolder
            );

        if (!Directory.Exists(compileDir))
        {
            Debug.Log("No compiled datasets folder found");
            return;
        }


        var files = Directory
            .GetFiles(
                compileDir,
                "master_dataset_*.bin"
            )
            .OrderBy(f => f)
            .ToArray();


        if (files.Length == 0)
        {
            Debug.Log("No compiled datasets found");
            return;
        }


        string latest = files.Last();

        Debug.Log(
            $"Loading: {Path.GetFileName(latest)}"
        );


        using (var fs = new FileStream(
            latest,
            FileMode.Open
        ))
        using (var reader = new BinaryReader(fs))
        {
            // ========================================
            // HEADER
            // ========================================
            int count = reader.ReadInt32();
            int stateSize = reader.ReadInt32();
            int flatMaskSize = reader.ReadInt32();
            int actionTypeCount = reader.ReadInt32();
            int buyCount = reader.ReadInt32();
            int benchCount = reader.ReadInt32();
            int fieldCount = reader.ReadInt32();
            Debug.Log($"Entries: {count}");
            Debug.Log($"State: {stateSize}");
            Debug.Log($"Flat Mask: {flatMaskSize}");
            Debug.Log($"Action Types: {actionTypeCount}");
            Debug.Log($"Buy Slots: {buyCount}");
            Debug.Log($"Bench Slots: {benchCount}");
            Debug.Log($"Field Slots: {fieldCount}");
            // ========================================
            // FIRST ENTRY
            // ========================================
            int action = reader.ReadInt32();
            int actionType = reader.ReadInt32();
            int actionIndex = reader.ReadInt32();
            float weight = reader.ReadSingle();
            float value = reader.ReadSingle();
            float oldLogProb = reader.ReadSingle();
            float oldValue = reader.ReadSingle();
            int finalRank = reader.ReadInt32();
            // ----------------------------------------
            // STATE
            // ----------------------------------------
            float[] state = new float[stateSize];
            for (int i = 0; i < stateSize; i++)
            {
                state[i] = reader.ReadSingle();
            }
            // ----------------------------------------
            // FLAT MASK
            // ----------------------------------------
            bool[] flatMask = new bool[flatMaskSize];
            for (int i = 0; i < flatMaskSize; i++)
            {
                flatMask[i] = reader.ReadByte() > 0;
            }
            // ----------------------------------------
            // ACTION TYPE MASK
            // ----------------------------------------
            bool[] actionTypeMask = new bool[actionTypeCount];
            for (int i = 0; i < actionTypeCount; i++)
            {
                actionTypeMask[i] = reader.ReadByte() > 0;
            }
            // ----------------------------------------
            // BUY MASK
            // ----------------------------------------
            bool[] buyMask = new bool[buyCount];
            for (int i = 0; i < buyCount; i++)
            {
                buyMask[i] = reader.ReadByte() > 0;
            }
            // ----------------------------------------
            // SELL MASK
            // ----------------------------------------
            bool[] sellBenchMask = new bool[benchCount];
            for (int i = 0; i < benchCount; i++)
            {
                sellBenchMask[i] = reader.ReadByte() > 0;
            }
            // ----------------------------------------
            // PLACE MASK
            // ----------------------------------------
            bool[] placeBenchMask = new bool[benchCount];
            for (int i = 0; i < benchCount; i++)
            {
                placeBenchMask[i] = reader.ReadByte() > 0;
            }
            // ----------------------------------------
            // FIELD MASK
            // ----------------------------------------
            bool[] fieldMask = new bool[fieldCount];
            for (int i = 0; i < fieldCount; i++)
            {
                fieldMask[i] = reader.ReadByte() > 0;
            }
            // DEBUG OUTPUT
            Debug.Log($"First entry:");
            Debug.Log($"  Flat Action: {action}");
            Debug.Log($"  Action Type: {actionType}");
            Debug.Log($"  Action Index: {actionIndex}");
            Debug.Log($"  Weight: {weight:F3}");
            Debug.Log($"  Value: {value:F3}");
            Debug.Log($"  Old Log Prob: {oldLogProb:F6}");
            Debug.Log($"  Old Value: {oldValue:F3}");
            Debug.Log($"  Final Rank: {finalRank}");
            Debug.Log($"  Gold: {state[0]:F3}");
            Debug.Log($"  Flat Valid: {flatMask.Count(x => x)}");
            Debug.Log($"  Valid Action Types: {actionTypeMask.Count(x => x)}");
            Debug.Log($"  Valid Buy Slots: {buyMask.Count(x => x)}");
            Debug.Log($"  Valid Sell Slots: {sellBenchMask.Count(x => x)}");
            Debug.Log($"  Valid Place Slots: {placeBenchMask.Count(x => x)}");
            Debug.Log($"  Valid Field Slots: {fieldMask.Count(x => x)}");
        }
    }
}