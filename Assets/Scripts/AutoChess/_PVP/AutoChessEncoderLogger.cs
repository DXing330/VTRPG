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
    public void LogDecision(float[] state, bool[] validMask, int action)
    {
        if (!logging) return;
        if (currentMatch == null || currentMatch.steps == null) return;
        currentMatch.steps[writeIndex] = new DecisionStep
        {
            state = state,
            action = action,
            validMask = validMask
        };
        writeIndex = (writeIndex + 1) % MAX_STORED;
    }
    public void LogDecision(AutoChessPrepManager prep, AutoChessDataManager data, AutoChessShopManager shop, AutoChessFactionManager factions, int action)
    {
        if (!logging){return;}
        if (currentMatch == null || encoder == null || currentMatch.steps == null) return;
        float[] state = encoder.Encode(prep, data, shop, factions);
        bool[] validMask = encoder.BuildValidMask(prep, data, shop,factions);
        currentMatch.steps[writeIndex] = new DecisionStep
        {
            state = state,
            action = action,
            validMask = validMask
        };
        writeIndex = (writeIndex + 1) % MAX_STORED;
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