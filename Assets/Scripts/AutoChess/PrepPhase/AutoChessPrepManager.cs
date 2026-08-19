using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Main Manager For AutoChess, Since Prep Phase Is Almost All Player Actions.
public class AutoChessPrepManager : ClickTileManager
{
    public bool PVP = false;
    public AutoChessDataManager dataManager;
    public void SetDataManager(AutoChessDataManager newDataManager)
    {
        dataManager = newDataManager;
        dataManager.Load();
        factionManager.SetDataManager(dataManager);
        tactician = dataManager.tactician;
        shopManager.shopData.logData = dataManager.logData;
        shopManager.shopData.SetShopLevel(dataManager.GetLevel());
        LoadSlots();
    }
    public void SaveToDataManager()
    {
        dataManager.SaveFromPrepManager(this);
        factionManager.SetDataManager(dataManager);
        factionManager.UpdateActiveFactions();
    }
    public bool SpendGold(int amount)
    {
        bool spent = dataManager.SpendGold(amount);
        if (spent && tactician != null)
        {
            tactician.Load();
            tactician.ApplySpendGoldEffect(this, amount);
        }
        return spent;
    }
    public AutoChessTactician tactician;

    [ContextMenu("New Game")]
    public void NewGame()
    {
        dataManager.NewGame();
    }
    public AutoChessPrepUIManager UIManager;
    public bool UI = true;
    public void EnableUI()
    {
        UI = true;
        UpdateAllUI();
    }
    public void DisableUI(){UI = false;}
    // Put The Equipment UI Reset Here For Now.
    public void UpdateAllUI()
    {
        if (UIManager == null || !UI){return;}
        UIManager.UpdateAutoChessUI(this);
        shopManager.UpdateAutoChessShopUI();
        equipManager.StopManagingEquipment();
        factionManager.UpdateActiveFactions();
    }
    void Start()
    {
        GenerateSpawnTiles();
        ResetSelected();
        // Load From Data Manager.
        dataManager.Load();
        factionManager.SetDataManager(dataManager);
        LoadSlots();
        UpdateAllUI();
    }
    public void LoadSlots()
    {
        benchSlots.Clear();
        fieldSlots.Clear();
        for (int i = 0; i < dataManager.benchActorData.Count; i++)
        {
            if (dataManager.benchActorData[i].Length <= 0){continue;}
            AutoActorRollUpData newBenchActor = new AutoActorRollUpData();
            newBenchActor.LoadRollUpData(dataManager.benchActorData[i]);
            newBenchActor.LoadBaseStats(actorData, newBenchActor.GetLevel());
            benchSlots.Add(newBenchActor);
        }
        for (int i = 0; i < dataManager.fieldActorData.Count; i++)
        {
            if (dataManager.fieldActorData[i].Length <= 0){continue;}
            AutoActorRollUpData newFieldActor = new AutoActorRollUpData();
            newFieldActor.LoadRollUpData(dataManager.fieldActorData[i]);
            newFieldActor.LoadBaseStats(actorData, newFieldActor.GetLevel());
            fieldSlots.Add(newFieldActor);
        }
    }
    // MAP
    public MapUtility mapUtility;
    public AutoActorRollUpData GetFrontActor(AutoActorRollUpData actor)
    {
        int location = actor.GetLocation();
        int direction = actor.GetDirection();
        int target = mapUtility.PointInDirection(location, direction, mapSize);
        if (target < 0){return null;}
        for (int i = 0; i < fieldSlots.Count; i++)
        {
            if (fieldSlots[i].GetLocation() == target){return fieldSlots[i];}
        }
        return null;
    }
    public AutoActorRollUpData GetBackActor(AutoActorRollUpData actor)
    {
        int location = actor.GetLocation();
        int direction = (actor.GetDirection() + 3) % 6;
        int target = mapUtility.PointInDirection(location, direction, mapSize);
        if (target < 0){return null;}
        for (int i = 0; i < fieldSlots.Count; i++)
        {
            if (fieldSlots[i].GetLocation() == target){return fieldSlots[i];}
        }
        return null;
    }
    public List<AutoActorRollUpData> GetActorsInLineDirection(AutoActorRollUpData actor)
    {
        List<AutoActorRollUpData> lineActors = new();
        int location = actor.GetLocation();
        int direction = actor.GetDirection();
        List<int> lineTiles = mapUtility.GetTilesInLineDirection(location, direction, mapSize, mapSize);
        for (int j = 0; j < lineTiles.Count; j++)
        {
            for (int i = 0; i < fieldSlots.Count; i++)
            {
                if (fieldSlots[i].GetLocation() == lineTiles[j]){lineActors.Add(fieldSlots[i]);}
            }
        }
        return lineActors;
    }
    public int mapSize = 7;
    public int GetCastleTile()
    {
        if (PVP){return -1;}
        int column = 0; // Left Side.
        int row = mapSize / 2; // Middle.
        int castleTile = mapUtility.ReturnTileNumberFromRowCol(row, column, mapSize);
        return castleTile;
    }
    protected List<int> spawnTiles;
    public void GenerateSpawnTiles()
    {
        spawnTiles = new List<int>();
        if (!PVP)
        {
            for (int i = 0; i < mapSize; i++)
            {
                spawnTiles.Add(mapUtility.ReturnTileNumberFromRowCol(i, mapSize - 1, mapSize));
            }
            return;
        }
        for (int i = 3; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                spawnTiles.Add(mapUtility.ReturnTileNumberFromRowCol(j, i, mapSize));
            }
        }
    }
    public List<int> GetSpawnTiles()
    {
        return spawnTiles;
    }
    public bool ValidActorTile(int tileNumber)
    {
        if (tileNumber == GetCastleTile()){return false;}
        if (GetSpawnTiles().Contains(tileNumber)){return false;}
        return true;
    }
    // Factions/Traits
    public AutoChessFactionManager factionManager;
    public AutoChessTraitManager traitManager;
    public void CheckTraitTiming(AutoActorRollUpData actor, string timing)
    {
        AutoChessTrait trait = actor.trait;
        if (trait == null){return;}
        if (trait.timing == timing)
        {
            dataManager.AddLog(actor.GetName() + " [" + timing + "]");
            if (timing == "OnPurchase" && factionManager.FactionActive("Investor"))
            {
                ApplyActorTrait(actor, trait);
                if (factionManager.GetStacksOfFaction("Investor") >= 70)
                {
                    ApplyActorTrait(actor, trait);
                }
            }
            ApplyActorTrait(actor, trait);
        }
    }
    public void ApplyActorTrait(AutoActorRollUpData actor, AutoChessTrait trait, bool copied = false)
    {
        if (trait == null){return;}
        int intSpecifics = traitManager.ReturnTraitSpecificsInt(actor, this);
        AutoActorRollUpData backActor = GetBackActor(actor);
        AutoActorRollUpData frontActor = GetFrontActor(actor);
        List<string> factionsToAdd = new List<string>();
        switch (trait.effect)
        {
            // Self/SelfActive/HighestActive/RandomActive
            default:
            factionManager.GainStacksSwitch(actor, intSpecifics);
            break;
            case "SelfAndBackActive":
            factionsToAdd.AddRange(actor.GetFactions().Distinct().ToList());
            if (backActor != null)
            {
                factionsToAdd.AddRange(backActor.GetFactions().Distinct().ToList());
            }
            factionManager.GainActiveStacks(factionsToAdd, intSpecifics);
            break;
            case "SelfAndFrontActive":
            factionsToAdd.AddRange(actor.GetFactions().Distinct().ToList());
            if (frontActor != null)
            {
                factionsToAdd.AddRange(frontActor.GetFactions().Distinct().ToList());
            }
            factionManager.GainActiveStacks(factionsToAdd, intSpecifics);
            break;
            case "SelfAndFrontLineActive":
            List<AutoActorRollUpData> lineActors = GetActorsInLineDirection(actor);
            for (int i = 0; i < lineActors.Count; i++)
            {
                factionsToAdd.AddRange(lineActors[i].GetFactions().Distinct().ToList());
            }
            factionManager.GainActiveStacks(factionsToAdd, intSpecifics);
            break;
            case "AllActiveBench":
            for (int i = 0; i < benchSlots.Count; i++)
            {
                factionsToAdd.AddRange(benchSlots[i].GetFactions().Distinct().ToList());
            }
            factionManager.GainActiveStacks(factionsToAdd, intSpecifics);
            break;
            // Don't Unlimited Copy.
            case "CopyFront":
            if (copied || frontActor == null){break;}
            dataManager.AddLog("Copied " + frontActor.GetName() + "'s trait.");
            ApplyActorTrait(actor, frontActor.trait, true);
            break;
            case "CopyBack":
            if (copied || backActor == null){break;}
            dataManager.AddLog("Copied " + backActor.GetName() + "'s trait.");
            ApplyActorTrait(actor, backActor.trait, true);
            break;
            case "Gold":
            dataManager.GainGold(intSpecifics);
            break;
            case "NextRoundGold":
            dataManager.GainNextRoundGold(intSpecifics);
            break;
            case "Equipment":
            dataManager.GainEquipment(trait.specifics);
            break;
            case "AegirUnit":
            for (int i = 0; i < Mathf.Max(1, intSpecifics); i++)
            {
                List<string> randomAegirUnits = new List<string>{"Specter", "Skadi", "Andreana"};
                string randomGainedAegir = randomAegirUnits[shopManager.shopData.autoChessShopRNG.SeedRange(0, randomAegirUnits.Count)];
                AutoActorRollUpData gainedAegir = new AutoActorRollUpData();
                gainedAegir.SetName(randomGainedAegir);
                if (shopManager.RemoveFromPool(randomGainedAegir))
                {
                    GainActor(gainedAegir);
                }
            }
            break;
            case "Unit":
            for (int i = 0; i < Mathf.Max(1, intSpecifics); i++)
            {
                string gainedUnitName = trait.specifics;
                AutoActorRollUpData gainedUnit = new AutoActorRollUpData();
                gainedUnit.SetName(gainedUnitName);
                if (shopManager.RemoveFromPool(gainedUnitName))
                {
                    GainActor(gainedUnit);
                }
            }
            break;
            case "HighestActiveUnit":
            string faction = factionManager.HighestUnitCountFaction();
            if (faction == "")
            {
                GainRandomActor();
                break;
            }
            for (int i = 0; i < Mathf.Max(1, intSpecifics); i++)
            {
                GainActorOfFaction(faction);
            }
            break;
        }
    }
    public void StartBattle()
    {
        ResetBonusSlots();
        fieldSlots.Sort((a, b) =>
        {
            int colA = mapUtility.GetColumn(a.GetLocation(), 9);
            int colB = mapUtility.GetColumn(b.GetLocation(), 9);
            return colB.CompareTo(colA);
        });
        SaveToDataManager();
        ApplyStartBattleActorTraits();
        // Check The Tactician Effect.
        if (tactician != null)
        {
            tactician.Load();
            tactician.ApplyEndRoundEffect(this);
        }
        dataManager.LogBattleStart();
    }
    public void ApplyStartBattleActorTraits()
    {
        ResetSelected();
        dataManager.AddLog("-- Applying StartBattle Traits --");
        for (int i = 0; i < fieldSlots.Count; i++)
        {
            CheckTraitTiming(fieldSlots[i], "StartBattle");
        }
        List<string> activeFactions = factionManager.factionData.GetActiveFactions();
        if (activeFactions.Contains("Aid"))
        {
            int aidCount = factionManager.factionData.GetCountOfFaction("Aid");
            int aidBonus = 2;
            if (aidCount >= 3)
            {
                aidBonus = 4;
            }
            for (int i = 0; i < activeFactions.Count; i++)
            {
                factionManager.factionData.GainFactionStacks(activeFactions[i], aidBonus);
            }
        }
        // TODO Check The Swire Thing On Bench.
        UpdateAllUI();
    }
    // EQUIP
    public AutoChessPrepEquipmentManager equipManager;
    public void ManageActorEquipment()
    {
        equipManager.ManageActorEquipment(ReturnSelectedActor());
    }
    // SHOP
    public StatDatabase actorData;
    public AutoChessShopManager shopManager;
    public void Select(int index)
    {
        shopManager.Select(index);
        UIManager.UpdateActorDisplay(shopManager.GetSelectedActor());
    }
    public void PVPRerollShop()
    {
        if (!SpendGold(1)){return;}
        shopManager.PVPReroll();
        dataManager.Reroll();
    }
    public void RerollShop()
    {
        if (!SpendGold(1)){return;}
        shopManager.Reroll();
        dataManager.Reroll();
        UpdateAllUI();
    }
    protected int expCost = 4;
    public void BuyExp()
    {
        if (dataManager.MaxLevel()){return;}
        if (!SpendGold(expCost)){return;}
        dataManager.GainExp(expCost);
        UpdateAllUI();
    }
    public void FreezeShop()
    {
        shopManager.FreezeShop();
    }
    // This Will Remove It From The Pool.
    public void GainRandomActor()
    {
        string newActorName = shopManager.shopData.ReturnRandomActor();
        GainActor(newActorName);
    }
    // Generate A Random Actor Of That Faction.
    public void GainActorOfFactionAndRarity(string faction, int rarity, int level = 1)
    {
        string newActorName = shopManager.shopData.ReturnRandomActorFromFactionAndRarity(faction, rarity);
        AutoActorRollUpData gainedActor = new AutoActorRollUpData();
        gainedActor.SetName(newActorName);
        GainActor(gainedActor, level);
        if (level == 2)
        {
            // Remove Two Extra Copies From The Store.
            shopManager.shopData.RemoveFromPool(newActorName);
            shopManager.shopData.RemoveFromPool(newActorName);
        }
    }
    public void GainActorOfFaction(string faction)
    {
        string newActorName = shopManager.shopData.ReturnRandomActorFromFaction(faction);
        GainActor(newActorName);
    }
    public void GainActor(string newActorName, bool removeFromPool = false)
    {
        if (removeFromPool)
        {
            shopManager.shopData.RemoveFromPool(newActorName);
        }
        AutoActorRollUpData gainedActor = new AutoActorRollUpData();
        gainedActor.SetName(newActorName);
        GainActor(gainedActor);
    }
    // New Actors Go To The Bench.
    public void GainActor(AutoActorRollUpData newActor, int level = 1)
    {
        dataManager.AddLog("Gained " + newActor.GetName());
        // Check For Merging To Level Up.
        if (level == 1 && GetLevelOneActorsWithName(newActor.GetName()) >= 2)
        {
            CheckTraitTiming(newActor, "OnPurchase");
            MergeIntoLevelTwoActor(newActor.GetName());
            // Don't Add The Actor Since It Will Be Merged Away Along With One Actor On The Bench/Field.
            UpdateAllUI();
            return;
        }
        dataManager.GainActor(newActor);
        newActor.LoadBaseStats(actorData, level);
        // Determine If A Trait Is Triggered.
        CheckTraitTiming(newActor, "OnPurchase");
        // Determine If Go To Bench Vs Bonus Area.
        int newSlot = AvailableBenchSlot();
        if (newSlot < 0)
        {
            dataManager.AddLog(newActor.GetName() + " placed into bonus zone.");
            bonusSlots.Add(newActor);
            return;
        }
        newActor.SetLocation(newSlot);
        benchSlots.Add(newActor);
        UpdateAllUI();
    }
    public void PVPBuySelectedActor(int cost)
    {
        if (factionManager.FactionActive("Foresight") && factionManager.GetStacksOfFaction("Foresight") >= 100)
        {
            cost--;
        }
        if (!SpendGold(cost)){return;}
        AutoActorRollUpData boughtActor = shopManager.GetSelectedActor();
        dataManager.AddLog("Bought " + boughtActor.GetName());
        GainActor(boughtActor);
        shopManager.PVPBuySelectedActor();
    }
    public void BuySelectedActor()
    {
        int newSlot = AvailableBenchSlot();
        if (newSlot < 0){return;}
        int cost = shopManager.SelectedCost();
        if (cost < 0){return;}
        if (!SpendGold(cost)){return;}
        // Remove The Actor From The Shop.
        AutoActorRollUpData boughtActor = shopManager.GetSelectedActor();
        dataManager.AddLog("Bought " + boughtActor.GetName());
        GainActor(boughtActor);
        shopManager.BuySelectedActor();
        SaveToDataManager();
        UpdateAllUI();
    }
    protected void CommercialPackagingPlan()
    {
        int sellCount = dataManager.GetRoundSold();
        if (sellCount <= 0 || sellCount % 8 != 0){return;}
        // Iterate Through Field And Bench.
        for (int i = 0; i < fieldSlots.Count; i++)
        {
            if (fieldSlots[i].EquipmentExists("Commercial Packaging Plan"))
            {
                dataManager.AddLog("Commercial Packaging Plan Grants A Copy Of " + fieldSlots[i].GetName());
                GainActor(fieldSlots[i].GetName(), true);
            }
        }
        for (int i = 0; i < benchSlots.Count; i++)
        {
            if (benchSlots[i].EquipmentExists("Commercial Packaging Plan"))
            {
                dataManager.AddLog("Commercial Packaging Plan Grants A Copy Of " + benchSlots[i].GetName());
                GainActor(benchSlots[i].GetName(), true);
            }
        }
    }
    public void FastSellSelectedActor(AutoActorRollUpData soldActor)
    {
        if (soldActor == null){return;}
        dataManager.AddLog("Sold " + soldActor.GetName());
        CheckTraitTiming(soldActor, "OnSold");
        dataManager.ReclaimEquipmentFromActor(soldActor);
        dataManager.SellActor();
        shopManager.SellActor(soldActor);
        if (benchSlots.Contains(soldActor))
        {
            benchSlots.Remove(soldActor);
        }
        else if (fieldSlots.Contains(soldActor))
        {
            fieldSlots.Remove(soldActor);
        }
        ResetSelected();
        dataManager.GainGold(1);
        CommercialPackagingPlan();
        UpdateBonusSlots();
    }
    public void FastSellSelectedActor()
    {
        AutoActorRollUpData soldActor = ReturnSelectedActor();
        FastSellSelectedActor(soldActor);
    }
    public void SellSelectedActor()
    {
        FastSellSelectedActor();
        SaveToDataManager();
        UpdateAllUI();
    }
    // Only For When The Bench Is Full But You Still Gain An Actor.
    public List<AutoActorRollUpData> bonusSlots;
    public void ResetBonusSlots()
    {
        for (int i = 0; i < bonusSlots.Count; i++)
        {
            dataManager.AddLog("Sold " + bonusSlots[i].GetName() + " automatically (No Bench Space)");
            shopManager.SellActor(bonusSlots[i]);
            dataManager.GainGold(1);
        }
        bonusSlots.Clear();
    }
    public void UpdateBonusSlots()
    {
        if (bonusSlots.Count <= 0){return;}
        int openBenchSlots = maxBenchSlots - benchSlots.Count;
        for (int i = 0; i < openBenchSlots; i++)
        {
            if (bonusSlots.Count <= 0){return;}
            AutoActorRollUpData actor = bonusSlots[0];
            actor.SetLocation(AvailableBenchSlot());
            benchSlots.Add(actor);
            bonusSlots.RemoveAt(0);
        }
    }
    // Spending Gold, Unit Placement (Location/Direction/TurnOrder), Etc.
    protected int maxBenchSlots = 12;
    public List<AutoActorRollUpData> benchSlots;
    public AutoActorRollUpData GetBenchSlotActorOnSlot(int slotNumber)
    {
        for (int i = 0; i < benchSlots.Count; i++)
        {
            if (benchSlots[i].GetLocation() == slotNumber)
            {
                return benchSlots[i];
            }
        }
        return null;
    }
    public int GetBenchSlotIndexOnSlot(int slotNumber)
    {
        for (int i = 0; i < benchSlots.Count; i++)
        {
            if (benchSlots[i].GetLocation() == slotNumber)
            {
                return i;
            }
        }
        return -1;
    }
    public int AvailableBenchSlot()
    {
        int available = 0;
        List<int> taken = new List<int>();
        for (int i = 0; i < benchSlots.Count; i++)
        {
            taken.Add(benchSlots[i].GetLocation());
        }
        for (int i = 0; i < maxBenchSlots; i++)
        {
            if (taken.Contains(available))
            {
                available++;
            }
            else
            {
                return available;
            }
        }
        return -1;
    }
    public void MoveFromBenchToField(AutoActorRollUpData benchActor)
    {
        if (!benchSlots.Contains(benchActor)){return;}
        fieldSlots.Add(benchActor);
        benchSlots.Remove(benchActor);
    }
    public List<AutoActorRollUpData> fieldSlots;
    public bool UnitExists(AutoActorRollUpData newUnit, bool includeBench = true)
    {
        for (int i = 0; i < fieldSlots.Count; i++)
        {
            if (fieldSlots[i].GetName() == newUnit.GetName()){return true;}
        }
        if (!includeBench){return false;}
        for (int i = 0; i < benchSlots.Count; i++)
        {
            if (benchSlots[i].GetName() == newUnit.GetName()){return true;}
        }
        return false;
    }
    public List<string> GetAllFieldEquipment()
    {
        List<string> allEquipment = new List<string>();
        for (int i = 0; i < fieldSlots.Count; i++)
        {
            allEquipment.AddRange(fieldSlots[i].GetEquipmentNames());
        }
        return allEquipment;
    }
    public void MoveFromFieldToBench(AutoActorRollUpData fieldActor)
    {
        if (!fieldSlots.Contains(fieldActor)){return;}
        benchSlots.Add(fieldActor);
        fieldSlots.Remove(fieldActor);
    }
    public List<int> GetTakenSpots()
    {
        List<int> spots = new List<int>();
        for (int i = 0; i < fieldSlots.Count; i++)
        {
            spots.Add(fieldSlots[i].GetLocation());
        }
        return spots;
    }
    public int GetMaxFieldSlots()
    {
        int fieldSlotEquipCount = 0;
        for (int i = 0; i < fieldSlots.Count; i++)
        {
            if (fieldSlots[i].EquipmentExists("HR File"))
            {
                fieldSlotEquipCount++;
            }
        }
        int bonusSlots = Mathf.Min(2, fieldSlotEquipCount);
        return 3 + bonusSlots +(dataManager.GetLevel() / 2);
    }
    public AutoActorRollUpData GetFieldSlotActorOnTileNumber(int tileNumber)
    {
        for (int i = 0; i < fieldSlots.Count; i++)
        {
            if (fieldSlots[i].GetLocation() == tileNumber)
            {
                return fieldSlots[i];
            }
        }
        return null;
    }
    public int GetFieldSlotIndexOnTileNumber(int tileNumber)
    {
        for (int i = 0; i < fieldSlots.Count; i++)
        {
            if (fieldSlots[i].GetLocation() == tileNumber)
            {
                return i;
            }
        }
        return -1;
    }
    public int GetLevelOneActorsWithName(string newName)
    {
        int count = 0;
        for (int i = 0; i < benchSlots.Count; i++)
        {
            if (benchSlots[i].GetName() == newName && benchSlots[i].GetLevel() == 1)
            {
                count++;
            }
        }
        for (int i = 0; i < fieldSlots.Count; i++)
        {
            if (fieldSlots[i].GetName() == newName && fieldSlots[i].GetLevel() == 1)
            {
                count++;
            }
        }
        return count;
    }
    public void MergeIntoLevelTwoActor(string newName)
    {
        dataManager.AddLog("3 Level 1 [" + newName + "] Units Fused Into 1 Level 2 Unit");
        // As a bonus, get a free equipment.
        dataManager.GainEquipment("Random");
        // Need To Make Sure Equipment Returns To Equipment Inventory.
        // Determine The Actor To Level Up.
        bool merged = false;
        for (int i = 0; i < fieldSlots.Count; i++)
        {
            if (fieldSlots[i].GetName() == newName && fieldSlots[i].GetLevel() < 2)
            {
                fieldSlots[i].SetLevel(2);
                fieldSlots[i].LoadBaseStats(actorData, 2);
                // Pretend That You're Gaining The Actor And Trigger OnPurchase Traits.
                CheckTraitTiming(fieldSlots[i], "OnPurchase");
                merged = true;
                break;
            }
        }
        // Else Check The Bench.
        if (!merged)
        {
            for (int i = 0; i < benchSlots.Count; i++)
            {
                if (benchSlots[i].GetName() == newName && benchSlots[i].GetLevel() < 2)
                {
                    benchSlots[i].SetLevel(2);
                    benchSlots[i].LoadBaseStats(actorData, 2);
                    // Pretend That You're Gaining The Actor And Trigger OnPurchase Traits.
                    CheckTraitTiming(benchSlots[i], "OnPurchase");
                    merged = true;
                    break;
                }
            }
        }
        for (int i = 0; i < benchSlots.Count; i++)
        {
            if (benchSlots[i].GetName() == newName && benchSlots[i].GetLevel() < 2)
            {
                // Remove Equipment.
                dataManager.ReclaimEquipmentFromActor(benchSlots[i]);
                // Remove Actor.
                benchSlots.RemoveAt(i);
                return;
            }
        }
        for (int i = 0; i < fieldSlots.Count; i++)
        {
            if (fieldSlots[i].GetName() == newName && fieldSlots[i].GetLevel() < 2)
            {
                // Remove Equipment.
                dataManager.ReclaimEquipmentFromActor(fieldSlots[i]);
                // Remove Actor.
                fieldSlots.RemoveAt(i);
                return;
            }
        }
    }
    public int selectedActorLocation;
    public int selectedActorIndex;
    public void ResetSelected()
    {
        selectedActorLocation = -1;
        selectedActorIndex = -1;
        UpdateAllUI();
    }
    public AutoActorRollUpData ReturnSelectedActor()
    {
        if (selectedActorLocation < 0 || selectedActorIndex < 0){return null;}
        if (selectedActorLocation == 0)
        {
            return benchSlots[selectedActorIndex];
        }
        else
        {
            return fieldSlots[selectedActorIndex];
        }
    }
    // Move From Map To Bench, Select Actor On Bench, Move From Bench To Bench
    public void ClickOnBenchTile(int clickedLocation)
    {
        // Select Actor On Bench.
        if (selectedActorLocation < 0)
        {
            for (int i = 0; i < benchSlots.Count; i++)
            {
                if (benchSlots[i].GetLocation() == clickedLocation)
                {
                    selectedActorIndex = i;
                    selectedActorLocation = 0;
                    UIManager.UpdateActorDisplay(benchSlots[i]);
                    UIManager.ActivateBenchActorObjects();
                    equipManager.SetCurrentActor(ReturnSelectedActor());
                    return;
                }
            }
        }
        // Move From Bench To Bench.
        else if (selectedActorLocation == 0)
        {
            selectedActorLocation = -1;
            if (selectedActorIndex < 0){return;}
            int previousLocation = benchSlots[selectedActorIndex].GetLocation();
            int newLocation = clickedLocation;
            // Move Any Actor On The New Slot To The Previous Slot.
            for (int i = 0; i < benchSlots.Count; i++)
            {
                if (benchSlots[i].GetLocation() == newLocation)
                {
                    benchSlots[i].SetLocation(previousLocation);
                    break;
                }
            }
            // Move The Previous Actor To The New Slot.
            benchSlots[selectedActorIndex].SetLocation(newLocation);
            ResetSelected();
        }
        // Move From Map To Bench (Potentially Swap).
        else if (selectedActorLocation == 1)
        {
            AutoActorRollUpData currentBenchActor = GetBenchSlotActorOnSlot(clickedLocation);
            if (currentBenchActor != null)
            {
                int fieldLocation = fieldSlots[selectedActorIndex].GetLocation();
                currentBenchActor.SetLocation(fieldLocation);
                fieldSlots.Add(currentBenchActor);
                benchSlots.Remove(currentBenchActor);
            }
            AutoActorRollUpData newAutoActor = new AutoActorRollUpData();
            newAutoActor.LoadRollUpData(fieldSlots[selectedActorIndex].ReturnRollUpData());
            newAutoActor.SetLocation(clickedLocation);
            newAutoActor.SetDirection(1);
            fieldSlots.RemoveAt(selectedActorIndex);
            benchSlots.Add(newAutoActor);
            ResetSelected();
        }
        SaveToDataManager();
        UpdateAllUI();
    }
    // Move From Bench To Map, Select Actor On Map, Move From Map To Map
    public override void ClickOnTile(int tileNumber)
    {
        if (!ValidActorTile(tileNumber)){return;}
        // Select Actor On Map.
        if (selectedActorLocation < 0)
        {
            for (int i = 0; i < fieldSlots.Count; i++)
            {
                if (fieldSlots[i].location == tileNumber)
                {
                    selectedActorIndex = i;
                    UIManager.UpdateActorDisplay(fieldSlots[i]);
                    UIManager.ActivateFieldActorObjects();
                    UIManager.HighlightSelectedAttackRange(this, fieldSlots[i]);
                    selectedActorLocation = 1;
                    equipManager.SetCurrentActor(ReturnSelectedActor());
                    return;
                }
            }
        }
        // Move From Bench To Map (Potentially Swap).
        else if (selectedActorLocation == 0)
        {
            // Check if any actor on the field is on the selected tile.
            AutoActorRollUpData currentfieldActor = GetFieldSlotActorOnTileNumber(tileNumber);
            if (currentfieldActor != null)
            {
                int benchLocation = benchSlots[selectedActorIndex].GetLocation();
                currentfieldActor.SetLocation(benchLocation);
                MoveFromFieldToBench(currentfieldActor);
            }
            // If not replacing an actor then you can't place anymore.
            else
            {
                if (fieldSlots.Count >= GetMaxFieldSlots()){return;}
            }
            benchSlots[selectedActorIndex].SetLocation(tileNumber);
            benchSlots[selectedActorIndex].SetDirection(1);
            MoveFromBenchToField(benchSlots[selectedActorIndex]);
            ResetSelected();
            UpdateBonusSlots();
        }
        // Move From Map To Map.
        else
        {
            selectedActorLocation = -1;
            if (selectedActorIndex < 0){return;}
            int previousLocation = fieldSlots[selectedActorIndex].GetLocation();
            int newLocation = tileNumber;
            // Move Any Actor On The New Slot To The Previous Slot.
            for (int i = 0; i < fieldSlots.Count; i++)
            {
                if (fieldSlots[i].GetLocation() == newLocation)
                {
                    fieldSlots[i].SetLocation(previousLocation);
                    break;
                }
            }
            // Move The Previous Actor To The New Slot.
            fieldSlots[selectedActorIndex].SetLocation(newLocation);
            ResetSelected();
        }
        SaveToDataManager();
        UpdateAllUI();
    }
    // For Changing The Direction After Selecting An Actor
    public void ChangeSelectedActorDirection(int direction)
    {
        if (selectedActorIndex < 0 || selectedActorLocation != 1){return;}
        selectedActorLocation = -1;
        fieldSlots[selectedActorIndex].SetDirection(direction);
        SaveToDataManager();
        UpdateAllUI();
    }
}