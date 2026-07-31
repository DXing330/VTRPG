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
    public AutoChessShopDataManager shopData;
    public List<AutoChessDataManager> dataManagers;
    public List<AutoChessFactionDataManager> factionDataManagers;
    public List<AutoChessTactician> tacticians;
    public List<AutoChessLogDataManager> logDataManagers;
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
    [ContextMenu("New Game All Data Managers")]
    public void NewGameAllDataManagers()
    {
        for (int i = 0; i < dataManagers.Count; i++)
        {
            dataManagers[i].NewGame();
            tacticians[i].NewGame();
        }
        shopData.NewGame();
    }
    public override void Load()
    {
        for (int i = 0; i < dataManagers.Count; i++)
        {
            dataManagers[i].Load();
            tacticians[i].Load();
        }
        shopData.Load();
    }
}
