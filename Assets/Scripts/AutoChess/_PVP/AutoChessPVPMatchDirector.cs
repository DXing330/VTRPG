using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// In charge of handling gameplay loop: player prep phase -> finish -> ai prep -> start battle -> finish AI battles -> watch player battle -> prep phase
public class AutoChessPVPMatchDirector : MonoBehaviour
{
    public bool autoAssignAIGenomes = false;
    public List<AutoChessPVPGenome> matchAIGenomes;
    public bool matchOver = false;
    public int roundCount = 0;
    public AutoChessPVPDataManager allPlayers;
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
        allPlayers.Load();
        // TODO Start The PrepManager.
        StartPlayerPrepPhase();
    }
    public void StartPlayerPrepPhase()
    {
        AutoChessDataManager playerTeam = GetPlayerTeam();
        // Player is dead only AI left or game over.
        if (playerTeam == null){return;}
        prepManager.SetDataManager(playerTeam);
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
    protected float totalOtherTime;
    public int testRunCount = 6;
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
        matchOver = false;
        roundCount = 0;
        allPlayers.fullAI = true;
        allPlayers.NewGameAllDataManagers();
        allPlayers.Load();
        totalAIPrepTime = 0f;
        totalBattleTime = 0f;
        totalOtherTime = 0f;
        while (!matchOver && roundCount < 30)
        {
            TimedAIPrepPhase();
        }
        Debug.Log("Rounds: " + roundCount +
            "\nAI Prep: " + totalAIPrepTime +
            "\nBattle: " + totalBattleTime);
    }
    public void TimedAIPrepPhase()
    {
        float start = Time.realtimeSinceStartup;
        List<AutoChessDataManager> allTeams = allPlayers.GetAllTeams();
        for (int i = 0; i < allTeams.Count; i++)
        {
            if (allTeams[i].PlayerData() || allTeams[i].GetHealth() <= 0){continue;}
            var entry = GenomeProvider.Get(allTeams[i]);
            if (autoAssignAIGenomes && i < matchAIGenomes.Count)
            {
                AIManager.genome = matchAIGenomes[i];
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
        totalAIPrepTime += Time.realtimeSinceStartup - start;
        PrepareBattleLists();
        TimedAutoRunAllBattles();
    }
    // Do Prep For All AI -> Move Into Battle.
    public void AIPrepPhase(bool fullAI = false)
    {
        List<AutoChessDataManager> allTeams = allPlayers.GetAllTeams();
        for (int i = 0; i < allTeams.Count; i++)
        {
            if (allTeams[i].PlayerData() || allTeams[i].GetHealth() <= 0){continue;}
            var entry = GenomeProvider.Get(allTeams[i]);
            if (autoAssignAIGenomes && i < matchAIGenomes.Count)
            {
                AIManager.genome = matchAIGenomes[i];
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
        roundCount++;
        // Go In Order.
        int playerIndex = -1;
        for (int i = 0; i < roundLeftTeams.Count; i++)
        {
            if (roundLeftTeams[i].PlayerData() || roundRightTeams[i].PlayerData())
            {
                playerIndex = i;
                continue;
            }
            battleManager.SetTeams(roundLeftTeams[i], roundRightTeams[i]);
            battleManager.SetInstantBattle();
            battleManager.StartBattle();
        }
        if (playerIndex >= 0)
        {
            battleManager.SetTeams(roundLeftTeams[playerIndex], roundRightTeams[playerIndex]);
            battleManager.SetInstantBattle(false);
            battleManager.StartBattle();
            yield return new WaitUntil(() => battleManager.EndBattle() >= 0);
        }
        CheckMatchOver();
    }
    protected void CheckMatchOver()
    {
        matchOver = false;
        List<AutoChessDataManager> allTeams = allPlayers.GetAllTeams();
        int aliveCount = 0;
        for (int i = 0; i < allTeams.Count; i++)
        {
            if (allTeams[i].GetHealth() > 0){aliveCount++;}
        }
        if (aliveCount <= 1){matchOver = true;}
    }
}
