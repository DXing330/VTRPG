using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// In charge of handling gameplay loop: player prep phase -> finish -> ai prep -> start battle -> finish AI battles -> watch player battle -> prep phase
public class AutoChessPVPMatchDirector : MonoBehaviour
{
    public bool useNeuralNet = false;
    public int neuralNetCount = 8;
    public bool[] neuralNetSlots = new bool[8];
    public AutoChessNeuralNetPrepController neuralNetController;
    void CheckNeuralNetPlacement(List<AutoChessDataManager> rankedTeams)
    {
        bool won = false;
        for (int i = 0; i < rankedTeams.Count; i++)
        {
            int slot = allPlayers.GetAllTeams().IndexOf(rankedTeams[i]);
            if (slot >= 0 && slot < neuralNetSlots.Length && neuralNetSlots[slot])
            {
                int placement = i + 1; // 1st = 1, 2nd = 2, etc.
                won = (placement <= 1 || won);
                Debug.Log($"[Benchmark] Neural net (slot {slot}) placed #{placement} — {(won ? "WIN" : "LOSS")}");
                // Could log to a file here for aggregate stats across many runs
            }
        }
        // Don't Record Entries Of Loser Neural Nets.
        if (!won)
        {
            logMatches = false;
        }
    }
    public bool autoAssignAIGenomes = false;
    public bool matchOver = false;
    // Determine Placements.
    public void MatchOver()
    {
        matchOver = true;
        var allTeams = allPlayers.GetAllTeams();
        allTeams = allTeams.OrderByDescending(t => t.GetRound()).ThenByDescending(t => t.GetHealth()).ToList();
        for (int i = 0; i < allTeams.Count; i++)
        {
            allTeams[i].encoderLogger.SetFinalRank(i + 1);
        }
        if (useNeuralNet)
        {
            CheckNeuralNetPlacement(allTeams);
        }
        allPlayers.Save();
        if (logMatches)
        {
            decisionDB.CompileDecisions();
        }
    }
    public int roundCount = 0;
    // Result Is Only Used When Player Defeated Or Match Over.
    public int GetPlayerPlacement()
    {
        allPlayers.Save();
        AutoChessDataManager playerData = GetPlayerTeam();
        // No Player In The Game.
        if (playerData == null){return -1;}
        // Player Is Dead.
        if (playerData.GetHealth() <= 0)
        {
            // Count Alive AI Left.
            return GetAliveTeams().Count + 1;
        }
        // Player Is Alive -> First Place.
        return 1;
    }
    public AutoChessPVPDataManager allPlayers;
    public bool logMatches = false;
    public AutoChessLogDataManager matchHistoryLog;
    public AutoChessDecisionDatabase decisionDB;
    public void AddLog(string newLog)
    {
        if (!logMatches){return;}
        matchHistoryLog.AddLog(newLog);
    }
    public void EnableLogs()
    {
        logMatches = true;
        allPlayers.EnableLogs();
        prepManager.logging = true;
    }
    public void DisableLogs()
    {
        logMatches = false;
        allPlayers.DisableLogs();
        prepManager.logging = false;
    }
    public List<AutoChessDataManager> GetAliveTeams()
    {
        List<AutoChessDataManager> allTeams = allPlayers.GetAllTeams();
        List<AutoChessDataManager> aliveTeams = new();
        for (int i = 0; i < allTeams.Count; i++)
        {
            if (allTeams[i].GetHealth() > 0)
            {
                aliveTeams.Add(allTeams[i]);
            }
        }
        return aliveTeams;
    }
    public AutoChessDataManager GetPlayerTeam()
    {
        List<AutoChessDataManager> allTeams = allPlayers.GetAllTeams();
        for (int i = 0; i < allTeams.Count; i++)
        {
            if (allTeams[i].PlayerData()){return allTeams[i];}
        }
        return null;
    }
    public RNGUtility RNG;
    public AutoChessPrepManager prepManager;
    public GameObject gameOverObject;
    public AutoChessPVPBattleManager battleManager;
    public AutoChessAIPrepController AIManager;
    public List<AutoChessDataManager> roundLeftTeams;
    public List<AutoChessDataManager> roundRightTeams;
    public AutoChessDataManager ghostTeam;
    public void DetermineGhostTeam()
    {
        // Get The Dead Team That Spent The Most Gold Ie The Longest Latest (And Hopefully The Strongest)
        int index = 0;
        int maxGoldSpent = -1;
        List<AutoChessDataManager> allTeams = allPlayers.GetAllTeams();
        for (int i = 0; i < allTeams.Count; i++)
        {
            if (allTeams[i].GetHealth() > 0){continue;}
            if (allTeams[i].GetTotalGoldSpent() > maxGoldSpent)
            {
                index = i;
                maxGoldSpent = allTeams[i].GetTotalGoldSpent();
            }
        }
        ghostTeam = allTeams[index];
    }
    void Start()
    {
        // Only Start For Player Matches, Else Do Nothing Until Called.
        if (autoAssignAIGenomes)
        {
            allPlayers.Load();
            if (logMatches)
            {
                EnableLogs();
                matchHistoryLog.Load();
            }
            StartPlayerPrepPhase();
        }
    }
    public void StartPlayerPrepPhase()
    {
        AutoChessDataManager playerTeam = GetPlayerTeam();
        // Ignore Very Early Matches.
        if (playerTeam.GetRound() < 2){matchHistoryLog.NewGame();}
        allPlayers.Save();
        // Player is dead, only AI left or game over.
        if (playerTeam == null || playerTeam.GetHealth() <= 0 || GetAliveTeams().Count == 1)
        {
            MatchOver();
            if (gameOverObject != null)
            {
                gameOverObject.SetActive(true);
            }
            return;
        }
        prepManager.SetDataManager(playerTeam);
        prepManager.EnableUI();
    }
    [ContextMenu("Test AI Run")]
    public void TestAI()
    {
        allPlayers.fullAI = true;
        allPlayers.Load();
        AIPrepPhase(true);
    }
    protected float totalAIPrepTime;
    protected float totalBattleTime;
    public int testRunCount = 6;
    [ContextMenu("Test Multiple NN Full Run")]
    public void TestMultipleNNFullRun()
    {
        for (int i = 0; i < testRunCount; i++)
        {
            TestNeuralNetFullRun();
        }
    }
    public void SetupMatch()
    {
        matchOver = false;
        roundCount = 0;
        allPlayers.fullAI = true;
        allPlayers.NewGameAllDataManagers();
        allPlayers.Load();
        AIManager.equipmentController.itemValueDatabase.Load();
    }
    public void SetupNNMatch()
    {
        SetupMatch();
        useNeuralNet = true;
        neuralNetSlots = new bool[8];
        for (int i = 0; i < neuralNetSlots.Length; i++)
        {
            if (i < neuralNetCount)
            {
                neuralNetSlots[i] = true;
            }
            else
            {
                neuralNetSlots[i] = false;
            }
        }
        neuralNetController.LoadModel();
        neuralNetController.ResetTimers();
        allPlayers.AssignChampionGenomes(neuralNetSlots);
        autoAssignAIGenomes = true;
    }
    [ContextMenu("Test NeuralNet Full Run")]
    public void TestNeuralNetFullRun()
    {
        EnableLogs();
        SetupNNMatch();
        totalAIPrepTime = 0f;
        totalBattleTime = 0f;
        while (!matchOver && roundCount < 30)
        {
            TimedAIPrepPhase();
        }
        Debug.Log("Rounds: " + roundCount + "\nAI Prep: " + totalAIPrepTime + "\nBattle: " + totalBattleTime);
        neuralNetController.LogTiming();
        autoAssignAIGenomes = false;
        neuralNetController.loaded = false;
        useNeuralNet = false;
        DisableLogs();
    }
    [ContextMenu("Test Multiple AI Full Run")]
    public void TestMultipleAIFullRun()
    {
        for (int i = 0; i < testRunCount; i++)
        {
            TestAIFullRun();
        }
    }
    [ContextMenu("Test AI Full Run")]
    public void TestAIFullRun()
    {
        autoAssignAIGenomes = true;
        EnableLogs();
        SetupMatch();
        allPlayers.AssignMatchAIGenomes();
        totalAIPrepTime = 0f;
        totalBattleTime = 0f;
        AIManager.ResetTimingData();
        while (!matchOver && roundCount < 30)
        {
            TimedAIPrepPhase();
        }
        AIManager.LogTimingSummary();
        Debug.Log("Rounds: " + roundCount + "\nAI Prep: " + totalAIPrepTime + "\nBattle: " + totalBattleTime);
        autoAssignAIGenomes = false;
        DisableLogs();
    }
    public void TimedAIPrepPhase()
    {
        float start = Time.realtimeSinceStartup;
        List<AutoChessDataManager> allTeams = allPlayers.GetAllTeams();
        for (int i = 0; i < allTeams.Count; i++)
        {
            if (allTeams[i].PlayerData() || allTeams[i].GetHealth() <= 0){continue;}
            if (useNeuralNet && neuralNetSlots[i])
            {
                neuralNetController.RunAIPrepPhase(allTeams[i]);
                continue;
            }
            var entry = GenomeProvider.Get(allTeams[i]);
            if (autoAssignAIGenomes)
            {
                AIManager.genome = allTeams[i].GetGenome();
            }
            else if (entry != null)
            {
                AIManager.genome = entry.genome;  
            } 
            else
            {
                AIManager.DefaultGenome();
            }
            AIManager.AIPrepPhase(allTeams[i], true);
        }
        totalAIPrepTime += Time.realtimeSinceStartup - start;
        PrepareBattleLists();
        TimedAutoRunAllBattles();
    }
    // Do Prep For All AI -> Move Into Battle.
    public void AIPrepPhase(bool fullAI = false)
    {
        float start = Time.realtimeSinceStartup;
        prepManager.DisableUI();
        List<AutoChessDataManager> allTeams = allPlayers.GetAllTeams();
        for (int i = 0; i < allTeams.Count; i++)
        {
            if (allTeams[i].PlayerData() || allTeams[i].GetHealth() <= 0){continue;}
            if (useNeuralNet && neuralNetSlots[i])
            {
                neuralNetController.RunAIPrepPhase(allTeams[i]);
                continue;
            }
            var entry = GenomeProvider.Get(allTeams[i]);
            if (autoAssignAIGenomes)
            {
                AIManager.genome = allTeams[i].GetGenome();
            }
            else if (entry != null)
            {
                AIManager.genome = entry.genome;
            }
            else
            {
                AIManager.DefaultGenome();
            }
            AIManager.AIPrepPhase(allTeams[i]);
        }
        PrepareBattleLists();
        if (!fullAI)
        {
            StartCoroutine(RunAllBattles());
        }
        else
        {
            AutoRunAllBattles();
        }
    }
    public void PrepareBattleLists()
    {
        roundLeftTeams.Clear();
        roundRightTeams.Clear();
        List<AutoChessDataManager> allTeams = allPlayers.GetAllTeams();
        // Remove Dead Teams.
        for (int i = allTeams.Count - 1; i >= 0; i--)
        {
            if (allTeams[i].GetHealth() <= 0)
            {
                allTeams.RemoveAt(i);
            }
        }
        // Odd Teams -> Get The Ghost.
        if (allTeams.Count % 2 == 1)
        {
            DetermineGhostTeam();
            allTeams.Add(ghostTeam);
        }
        RNG.ShuffleList(allTeams);
        for (int i = 0; i < allTeams.Count; i++)
        {
            if (i % 2 == 0)
            {
                roundLeftTeams.Add(allTeams[i]);
            }
            else
            {
                roundRightTeams.Add(allTeams[i]);
            }
        }
    }
    protected void TimedAutoRunAllBattles()
    {
        roundCount++;
        float start = Time.realtimeSinceStartup;
        for (int i = 0; i < roundLeftTeams.Count; i++)
        {
            battleManager.SetTeams(roundLeftTeams[i], roundRightTeams[i]);
            battleManager.SetInstantBattle();
            battleManager.StartBattle();
        }
        totalBattleTime += Time.realtimeSinceStartup - start;
        CheckMatchOver();
    }
    public void AutoRunAllBattles()
    {
        roundCount++;
        for (int i = 0; i < roundLeftTeams.Count; i++)
        {
            battleManager.SetTeams(roundLeftTeams[i], roundRightTeams[i]);
            battleManager.SetInstantBattle();
            battleManager.StartBattle();
        }
        CheckMatchOver();
    }
    public IEnumerator RunAllBattles()
    {
        // Go In Order.
        float start = Time.realtimeSinceStartup;
        int playerIndex = -1;
        for (int i = 0; i < roundLeftTeams.Count; i++)
        {
            if (roundLeftTeams[i].PlayerData() || roundRightTeams[i].PlayerData())
            {
                playerIndex = i;
                continue;
            }
            battleManager.SetTeams(roundLeftTeams[i], roundRightTeams[i]);
            if (logMatches)
            {
                AddLog("ROUND:" + roundLeftTeams[i].GetRound() + " | BATTLE:" + (i + 1));
                AddLog("LEFT TEAM:");
                AddLog(roundLeftTeams[i].GetFieldActorState());
                AddLog("RIGHT TEAM:");
                AddLog(roundRightTeams[i].GetFieldActorState());
            }
            battleManager.SetInstantBattle();
            battleManager.StartBattle();
        }
        if (playerIndex >= 0)
        {
            AddLog("ROUND " + roundLeftTeams[playerIndex].GetRound() + " | PLAYER BATTLE");
            AddLog("LEFT TEAM:");
            AddLog(roundLeftTeams[playerIndex].GetFieldActorState());
            AddLog("RIGHT TEAM:");
            AddLog(roundRightTeams[playerIndex].GetFieldActorState());
            battleManager.SetTeams(roundLeftTeams[playerIndex], roundRightTeams[playerIndex]);
            battleManager.SetInstantBattle(false);
            battleManager.StartBattle();
            yield return new WaitUntil(() => battleManager.EndBattle() >= 0);
        }
        CheckMatchOver();
        if (!matchOver && playerIndex >= 0)
        {
            allPlayers.shopData.NewRound();
            StartPlayerPrepPhase();
            matchHistoryLog.Save();
        }
    }
    protected void CheckMatchOver()
    {
        matchOver = false;
        List<AutoChessDataManager> allTeams = allPlayers.GetAllTeams();
        if (GetAliveTeams().Count <= 1)
        {
            MatchOver();
        }
    }
}