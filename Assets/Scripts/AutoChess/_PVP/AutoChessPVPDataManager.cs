using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CreateAssetMenu(fileName = "AutoChessPVPDataManager", menuName = "ScriptableObjects/AutoChessPVP/AutoChessPVPDataManager", order = 1)]
public class AutoChessPVPDataManager : SavedData
{
    public bool fullAI = false;
    public List<AutoChessDataManager> GetAllTeams()
    {
        if (fullAI)
        {
            return new List<AutoChessDataManager>(dataManagers);
        }
        List<AutoChessDataManager> playerAndAI = new List<AutoChessDataManager>();
        playerAndAI.Add(playerData);
        for (int i = 1; i < dataManagers.Count; i++)
        {
            playerAndAI.Add(dataManagers[i]);
        }
        return playerAndAI;
    }
    public AutoChessDataManager playerData;
    public AutoChessFactionDataManager playerFactions;
    public AutoChessTactician playerTactician;
    public AutoChessLogDataManager playerLogs;
    public RNGUtility playerSeed;
    public AutoChessSettingsDataManager playerSettings;
    public AutoChessShopDataManager shopData;
    public AutoChessPVPSavedGenomeDataManager championGenomes;
    public List<AutoChessPVPGenome> matchAIGenomes;
    public StatDatabase tacticianFactions;
    public List<AutoChessDataManager> dataManagers;
    public List<AutoChessFactionDataManager> factionDataManagers;
    public List<AutoChessTactician> tacticians;
    public List<AutoChessLogDataManager> logDataManagers;
    public void DisableLogs()
    {
        for (int i = 0; i < dataManagers.Count; i++)
        {
            dataManagers[i].DisableLogs();
            factionDataManagers[i].DisableLogs();
        }
    }
    public List<RNGUtility> seedManagers;
    [ContextMenu("Initialize All Data Managers")]
    public void InitializeAllDataManagers()
    {
        for (int i = 0; i < dataManagers.Count; i++)
        {
            // Set The File Names For The Data Paths.
            dataManagers[i].filename = "AIPVPAUTOCHESSDATA_" + (i+1);
            tacticians[i].filename = "AIPVPAUTOCHESSTACTICIAN_" + (i+1);
            factionDataManagers[i].filename = "AIPVPAUTOCHESSFACTIONDATA_" + (i+1);
            logDataManagers[i].filename = "AIPVPAUTOCHESSLOGDATA_" + (i+1);
            seedManagers[i].filename = "AIPVPAUTOCHESSRNGSEED_" + (i+1);
            // Update The Linkage Of Each.
            dataManagers[i].subDataManagers.Clear();
            dataManagers[i].subDataManagers.Add(factionDataManagers[i]);
            dataManagers[i].subDataManagers.Add(logDataManagers[i]);
            dataManagers[i].subDataManagers.Add(tacticians[i]);
            dataManagers[i].subDataManagers.Add(seedManagers[i]);
            dataManagers[i].tactician = tacticians[i];
            dataManagers[i].factionData = factionDataManagers[i];
            dataManagers[i].logData = logDataManagers[i];
            dataManagers[i].RNG = seedManagers[i];
            tacticians[i].dataManager = dataManagers[i];
            tacticians[i].factionData = factionDataManagers[i];
            tacticians[i].RNG = seedManagers[i];
            factionDataManagers[i].logData = logDataManagers[i];
            factionDataManagers[i].RNG = seedManagers[i];
            #if UNITY_EDITOR
                EditorUtility.SetDirty(dataManagers[i]);
                EditorUtility.SetDirty(tacticians[i]);
                EditorUtility.SetDirty(factionDataManagers[i]);
                EditorUtility.SetDirty(logDataManagers[i]);
                EditorUtility.SetDirty(seedManagers[i]);
            #endif
        }
    }
    public void AssignMatchAIGenomes()
    {
        if (championGenomes == null || championGenomes.entries.Count == 0)
        {
            Debug.LogError("No champion genomes available.");
            return;
        }
        List<GenomeEntry> available = championGenomes.entries.Where(e => e != null && e.genome != null).OrderBy(e => Random.value).ToList();
        int genomeIndex = 0;
        for (int i = 0; i < dataManagers.Count; i++)
        {
            if (!fullAI && i == 0)
            {
                continue;
            }
            if (genomeIndex >= available.Count)
            {
                Debug.LogWarning("Not enough champion genomes.");
                break;
            }
            GenomeEntry entry = available[genomeIndex++];
            AutoChessPVPGenome genome = entry.genome.Copy();
            dataManagers[i].SetGenome(genome);
        }
    }
    public void AssignMatchTacticians(bool player = false)
    {
        List<string> availableTacticians = tacticianFactions.GetAllKeys();
        if (player)
        {
            // Player Gets Theirs From The Settings, Others Get Random Appropriate Ones Based On Their Genomes' Preferred Faction.
            string playerTactician = playerSettings.GetTactician();
            playerData.tactician.SetTactician(playerTactician);
            availableTacticians.Remove(playerTactician);
        }
        // Remove Those That Are None.
        for (int i = availableTacticians.Count - 1; i >= 0; i--)
        {
            if (tacticianFactions.ReturnValue(availableTacticians[i]) == "None"){availableTacticians.RemoveAt(i);}
        }
        playerSeed.ShuffleList(availableTacticians);
        // TODO Later Let AI Pick Their Own Tactician.
        for (int i = 0; i < dataManagers.Count; i++)
        {
            if (player && i == 0){continue;}
            dataManagers[i].tactician.ResetTactician();
            for (int j = availableTacticians.Count - 1; j >= 0; j--)
            {
                string tacticianFaction = tacticianFactions.ReturnValue(availableTacticians[j]);
                if (tacticianFaction == "Any" || tacticianFaction == dataManagers[i].GetGenome().GetPreferredFaction())
                {
                    dataManagers[i].tactician.SetTactician(availableTacticians[j]);
                    availableTacticians.RemoveAt(j);
                    break;
                }
            }
        }
    }
    [ContextMenu("New Game All Data Managers")]
    public void NewGameAllDataManagers(bool player = false)
    {
        // In PVP Assign Genomes + Tacticians Randomly.
        if (player)
        {
            // Need This Specific Order For Player Games: Genomes Determine Tacticians And New Game Loads Tacticians So Tactician Assigment Must Be After Genomes But Before New Game.
            AssignMatchAIGenomes();
            AssignMatchTacticians(true);
            playerData.NewGame();
        }
        else
        {
            AssignMatchTacticians(false);
        }
        for (int i = 0; i < dataManagers.Count; i++)
        {
            dataManagers[i].NewGame();
        }
        shopData.NewGame();
    }
    public override void Save()
    {
        playerData.Save();
        for (int i = 0; i < dataManagers.Count; i++)
        {
            dataManagers[i].Save();
        }
        shopData.Save();
    }
    public override void Load()
    {
        playerData.Load();
        for (int i = 0; i < dataManagers.Count; i++)
        {
            dataManagers[i].Load();
        }
        shopData.Load();
    }
}
