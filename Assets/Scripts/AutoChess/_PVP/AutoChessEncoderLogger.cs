using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// In charge of logging state + action for each player in the autochess game.
[CreateAssetMenu(fileName = "AutoChessEncoderLogger", menuName = "ScriptableObjects/AutoChess/AutoChessEncoderLogger", order = 1)]
public class AutoChessEncoderLogger : SavedData
{
    public string folderName = "AutoChessDecisionLogs";
    public bool player = false;
    public bool logging = false;
    public void EnableLogs(){logging = true;}
    public void DisableLogs(){logging = false;}
    public AutoChessStateEncoder encoder;
    [System.Serializable]
    public class DecisionStep
    {
        public float[] state;
        public int action; // 0-43
        public bool[] validMask; // 44 booleans
        // ----------------------------------------
        // HIERARCHICAL ACTION LABELS
        // ----------------------------------------
        // 0 = Done
        // 1 = LevelUp
        // 2 = Reroll
        // 3 = Buy
        // 4 = SellBench
        // 5 = PlaceBench
        // 6 = ReturnField
        public int actionType;
        // Child action index.
        // Buy:
        //   0-5 = shop slot
        // SellBench / PlaceBench:
        //   0-11 = bench slot
        // ReturnField:
        //   0-10 = field slot
        // Done / LevelUp / Reroll:
        //   -1
        public int actionIndex;
        // Hierarchical validity masks.
        public bool[] actionTypeMask; // 7
        public bool[] buyMask; // 6
        public bool[] sellBenchMask; // 12
        public bool[] placeBenchMask; // 12
        public bool[] fieldMask;      // 11
    }
    private string GetActionTypeName(int action)
    {
        if (action == 0) return "Done";
        if (action == 1) return "LevelUp";
        if (action == 2) return "Reroll";
        if (action >= 3 && action <= 8) return $"Buy({action - 3})";
        if (action >= 9 && action <= 20) return $"SellBench({action - 9})";
        if (action >= 21 && action <= 32) return $"PlaceBench({action - 21})";
        if (action >= 33 && action <= 43) return $"ReturnField({action - 33})";
        return "Unknown";
    }
    public int MAX_STORED = 1024;
    private int writeIndex = 0;
    [System.Serializable]
    public class MatchLog
    {
        public bool isAI = true;
        public int finalRank; // 1-8, set at match end
        public DecisionStep[] steps;
    }
    public MatchLog currentMatch;
    public override void NewGame()
    {
        if (!logging){return;}
        writeIndex = 0;
        currentMatch = new MatchLog
        {
            isAI = !player,
            finalRank = -1,// unknown until end
            steps = new DecisionStep[MAX_STORED]
        };
        Save();
    }
    private DecisionStep BuildDecisionStep(float[] state, bool[] validMask, int action)
    {
        encoder.DecodeAction(action, out int actionType, out int actionIndex);
        HierarchicalMask hierarchicalMask = encoder.BuildHierarchicalMask(validMask);
        return new DecisionStep
        {
            state = state,
            // Keep old representation.
            action = action,
            validMask = validMask,
            // New hierarchical representation.
            actionType = actionType,
            actionIndex = actionIndex,
            actionTypeMask = hierarchicalMask.actionTypeMask,
            buyMask = hierarchicalMask.buyMask,
            sellBenchMask = hierarchicalMask.sellBenchMask,
            placeBenchMask = hierarchicalMask.placeBenchMask,
            fieldMask = hierarchicalMask.fieldMask
        };
    }
    public void LogDecision(float[] state, bool[] validMask, int action)
    {
        if (!logging) return;
        if (currentMatch == null || currentMatch.steps == null) return;
        if (action < 0 || action >= validMask.Length)
        {
            Debug.LogError($"INVALID AI ACTION INDEX: action={action}, " + $"maskLength={validMask.Length}");
            return;
        }
        if (!validMask[action])
        {
            Debug.LogError($"AI ACTION/MASK MISMATCH: " + $"action={GetActionTypeName(action)}");
        }
        currentMatch.steps[writeIndex] = BuildDecisionStep(state, validMask, action);
        writeIndex = (writeIndex + 1) % MAX_STORED;
    }
    public void LogDecision(AutoChessPrepManager prep, AutoChessDataManager data, AutoChessShopManager shop, AutoChessFactionManager factions, int action)
    {
        if (!logging){return;}
        if (currentMatch == null || encoder == null || currentMatch.steps == null) return;
        float[] state = encoder.Encode(prep, data, shop, factions);
        bool[] validMask = encoder.BuildValidMask(prep, data, shop, factions);
        if (action < 0 || action >= validMask.Length)
        {
            Debug.LogError($"INVALID AI ACTION INDEX: " + $"action={action}, " + $"maskLength={validMask.Length}");
            return;
        }
        if (!validMask[action])
        {
            string actionTypeName = GetActionTypeName(action);
            Debug.LogError($"AI ACTION/MASK MISMATCH: " + $"action={action}, " +$"actionType={actionTypeName}" + $" | benchCount={prep.benchSlots.Count}" + $" | fieldCount={prep.fieldSlots.Count}" + $" | maxField={prep.GetMaxFieldSlots()}" + $" | maxBench={prep.GetMaxBenchSlots()}");
            Debug.LogError($"Full validMask: [{string.Join(", ", validMask)}]");
            Debug.LogError($"Bench=[{string.Join(", ", prep.benchSlots.ConvertAll(x => x.GetName()))}]");
            Debug.LogError($"Field=[{string.Join(", ", prep.fieldSlots.ConvertAll(x => x.GetName()))}]");
        }
        LogDecision(state, validMask, action);
    }
    public void SetFinalRank(int rank)
    {
        if (currentMatch != null)
        {
            currentMatch.finalRank = rank;
        }
    }
    public override void Save()
    {
        if (!logging){return;}
        if (currentMatch == null) return;
        string dir = Path.Combine(TrainingWorkerStorage.GetPersistentPath(), folderName);
        Directory.CreateDirectory(dir);  // ensure folder exists
        string path = Path.Combine(dir, $"{filename}.json");
        string json = JsonUtility.ToJson(currentMatch, false); // compact
        File.WriteAllText(path, json);
    }
    public override void Load()
    {
        if (!logging){return;}
        string dir = Path.Combine(TrainingWorkerStorage.GetPersistentPath(), folderName);
        string path = Path.Combine(dir, $"{filename}.json");
        if (!File.Exists(path)){return;}
        string json = File.ReadAllText(path);
        currentMatch = JsonUtility.FromJson<MatchLog>(json);
    }
}