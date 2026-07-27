using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Shop/Prep/Battle/Etc Will Call This When Needed.
// Tactician Is Set At Start Of Game.
[CreateAssetMenu(fileName = "AutoChessTactician", menuName = "ScriptableObjects/AutoChess/AutoChessTactician", order = 1)]
public class AutoChessTactician : SavedData
{
    public AutoChessDataManager dataManager;
    public StatDatabase unitData;
    public StatDatabase unitRarity;
    public void AddLog()
    {
        dataManager.AddLog(tacticianName + "'s trait activates.");
    }
    public AutoChessFactionDataManager factionData;
    public StatDatabase tacticianDatabase;
    public RNGUtility shopRNG;
    public string tacticianName;
    public string tacticianTiming;
    public string tacticianDescription;
    public void SetTactician(string newName)
    {
        tacticianName = newName;
        string[] details = tacticianDatabase.ReturnValue(tacticianName).Split("|");
        tacticianTiming = details[0];
        tacticianDescription = details[1];
    }
    public override void NewGame()
    {
        List<string> allTacticians = tacticianDatabase.GetAllKeys();
        SetTactician(allTacticians[0]);
        Save();
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "Tactician=" + tacticianName + delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        if (File.Exists(dataPath))
        {
            allData = File.ReadAllText(dataPath);
        }
        else
        {
            NewGame();
            return;
        }
        string[] blocks = allData.Split(delimiter);
        for (int i = 0; i < blocks.Length; i++)
        {
            LoadStat(blocks[i]);
        }
    }
    public override void LoadStat(string data)
    {
        string[] blocks = data.Split("=");
        if (blocks.Length < 2){return;}
        string key = blocks[0];
        string value = blocks[1];
        switch (key)
        {
            default:
            return;
            case "Tactician":
            SetTactician(value);
            return;
        }
    }
    // DuYaoye, Orchid, Pepe
    public void ApplyRerollEffect(AutoChessShopDataManager shopData)
    {
        if (tacticianTiming != "Reroll"){return;}
        int roundRerollCount = dataManager.GetRoundRerolls();
        switch (tacticianName)
        {
            case "DuYaoye":
            if (roundRerollCount > 0){return;}
            AddLog();
            shopData.ModifyCurrentList(0, "Faction", "Yan");
            return;
            case "Orchid":
            if (roundRerollCount > 1){return;}
            AddLog();
            shopData.ModifyCurrentList(1, "Copy", "0");
            return;
            case "Pepe":
            if (roundRerollCount > 0){return;}
            AddLog();
            shopData.ModifyCurrentList(0, "Faction", "Sargon");
            return;
        }
    }
    // Kirara, Paganini
    public void ApplySpendGoldEffect()
    {
        if (tacticianTiming != "SpendGold"){return;}
        switch (tacticianName)
        {
            case "Kirara":
            break;
            case "Paganini":
            break;
        }
    }
    // Warfarin, Justin, Harold, Quintus, Yu, DamaztiIsomorph
    public void ApplyEndRoundEffect(AutoChessPrepManager prepManager = null)
    {
        if (tacticianTiming != "EndRound"){return;}
        int round = dataManager.GetRound();
        switch (tacticianName)
        {
            case "Warfarin":
            ApplyWarfarinEffect();
            break;
            case "Justin":
            if ((round - 1) % 3 != 0){return;}
            AddLog();
            dataManager.GainGold(shopRNG.SeedRange(1, 6));
            break;
            case "Harold":
            if (round < 4 || round % 2 != 0){return;}
            if (prepManager == null){return;}
            AddLog();
            prepManager.GainActorOfFaction("Victoria");
            break;
            case "Quintus":
            if (round != 3){return;}
            AddLog();
            dataManager.GainEquipment("Mutated Cells");
            break;
            case "DamaztiCluster":
            if (round != 7){return;}
            AddLog();
            dataManager.GainEquipment("Damazti Isomorph");
            break;
            case "Yu":
            if (round != 7){return;}
            AddLog();
            // Check # Of Active Factions.
            List<string> activeFactions = factionData.GetActiveFactions();
            if (activeFactions.Count == 1)
            {
                factionData.GainFactionStacks(activeFactions[0], 40);
            }
            else
            {
                for (int i = 0; i < activeFactions.Count; i++)
                {
                    factionData.GainFactionStacks(activeFactions[i], 12);
                }
            }
            break;
        }
    }
    protected void ApplyWarfarinEffect()
    {
        AddLog();
        // TODO, Get A List Of The Field Actors, Shuffle, Iterate Through Rarity 1-6, Gain Stacks For The First One Found Of Each Rarity.
        // The DataManager Knows The Most Up To Date Field Actor List.
        List<string> fieldActors = new List<string>(dataManager.GetFieldActorData());
        shopRNG.ShuffleList(fieldActors);
        List<AutoActorRollUpData> fieldActorData = new List<AutoActorRollUpData>();
        for (int i = 0; i < fieldActors.Count; i++)
        {
            AutoActorRollUpData newFieldActor = new AutoActorRollUpData();
            newFieldActor.LoadRollUpData(fieldActors[i]);
            fieldActorData.Add(newFieldActor);
        }
        // Iterate Through The Rarities 1-6 And Gain Stacks Of The First One Found.
        for (int i = 1; i < 7; i++)
        {
            for (int j = 0; j < fieldActorData.Count; j++)
            {
                int rarity = int.Parse(unitRarity.ReturnValue(fieldActorData[j].GetName()));
                if (rarity == i)
                {
                    // Gain Stacks.
                    factionData.GainActiveStacks(fieldActorData[j].GetFactions(), 2);
                    // Remove From List To Speed Up Future Iterations?
                    break;
                }
            }
        }
    }
    // Goliath, Malkiewicz
    public void ApplyStartGameEffect()
    {
        if (tacticianTiming != "StartGame"){return;}
        switch (tacticianName)
        {
            case "Goliath":
            break;
            case "Malkiewicz":
            break;
            case "Pith":
            AutoActorRollUpData pithUnit = new AutoActorRollUpData();
            pithUnit.SetName("Pith");
            pithUnit.LoadBaseStats(unitData);
            dataManager.AddActorToBench(pithUnit.ReturnRollUpData());
            break;
            case "Euden":
            AutoActorRollUpData playerUnit = new AutoActorRollUpData();
            playerUnit.SetName("Player");
            playerUnit.LoadBaseStats(unitData);
            dataManager.AddActorToBench(playerUnit.ReturnRollUpData());
            AutoActorRollUpData familiarUnit = new AutoActorRollUpData();
            familiarUnit.SetName("Familiar");
            familiarUnit.SetLocation(1);
            familiarUnit.LoadBaseStats(unitData);
            dataManager.AddActorToBench(familiarUnit.ReturnRollUpData());
            break;
        }
    }
    // Amiya, Ermengarde, Eunectes
    public void ApplyStartBattleEffect(List<TacticActor> allies)
    {
        if (tacticianTiming != "StartBattle"){return;}
        switch (tacticianName)
        {
            case "Amiya":
            break;
            case "Ermengarde":
            break;
            case "Eunectes":
            break;
        }
    }
}
