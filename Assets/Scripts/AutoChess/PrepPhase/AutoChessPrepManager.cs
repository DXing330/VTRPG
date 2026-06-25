using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Main Manager For AutoChess, Since Prep Phase Is Almost All Player Actions.
public class AutoChessPrepManager : ClickTileManager
{
    public AutoChessDataManager dataManager;
    public void Save()
    {
        dataManager.SaveFromPrepManager(this);
        shopManager.Save();
    }
    [ContextMenu("New Game")]
    public void NewGame()
    {
        dataManager.NewGame();
        shopManager.shopData.NewGame();
    }
    public AutoChessPrepUIManager UIManager;
    void Start()
    {
        ResetSelected();
        // Load From Data Manager.
        dataManager.Load();
        LoadSlots();
        UIManager.UpdateUI(this);
    }
    // MAP
    public MapUtility mapUtility;
    public int mapSize = 7;
    public int GetCastleTile()
    {
        int column = 0; // Left Side.
        int row = mapSize / 2; // Middle.
        int castleTile = mapUtility.ReturnTileNumberFromRowCol(row, column, mapSize);
        return castleTile;
    }
    public List<int> GetSpawnTiles()
    {
        List<int> spawnTiles = new List<int>();
        for (int i = 0; i < mapSize; i++)
        {
            spawnTiles.Add(mapUtility.ReturnTileNumberFromRowCol(i, mapSize - 1, mapSize));
        }
        return spawnTiles;
    }
    public bool ValidActorTile(int tileNumber)
    {
        if (tileNumber == GetCastleTile()){return false;}
        if (GetSpawnTiles().Contains(tileNumber)){return false;}
        return true;
    }
    // SHOP
    public StatDatabase actorData;
    public AutoChessShopManager shopManager;
    public void Select(int index)
    {
        shopManager.Select(index);
        UIManager.UpdateActorDisplay(shopManager.GetSelectedActor());
    }
    public void RerollShop()
    {
        if (!dataManager.SpendGold(1))
        {
            return;
        }
        shopManager.Reroll();
        UIManager.UpdateUI(this);
    }
    protected int expCost = 4;
    public void BuyExp()
    {
        if (dataManager.MaxLevel()){return;}
        if (!dataManager.SpendGold(expCost))
        {
            return;
        }
        dataManager.GainExp(expCost);
        UIManager.UpdateUI(this);
    }
    public void BuySelectedActor()
    {
        int newSlot = AvailableBenchSlot();
        if (newSlot < 0){return;}
        int cost = shopManager.SelectedCost();
        if (cost < 0){return;}
        if (!dataManager.SpendGold(cost))
        {
            return;
        }
        // Remove The Actor From The Shop.
        AutoActorRollUpData boughtActor = shopManager.GetSelectedActor();
        boughtActor.SetLocation(newSlot);
        boughtActor.LoadBaseStats(actorData);
        // Add The Actor To The Bench In The Earliest Open Slot.
        benchSlots.Add(boughtActor);
        shopManager.BuySelectedActor();
        // TODO Apply OnPurchase Traits.
        Save();
        UIManager.UpdateUI(this);
    }
    public void SellSelectedActor()
    {
        if (selectedActorLocation < 0 || selectedActorIndex < 0){return;}
        // Determine The Actor.
        // TODO Apply OnSold Traits.
        if (selectedActorLocation == 0)
        {
            AutoActorRollUpData soldBenchActor = benchSlots[selectedActorIndex];
            shopManager.SellActor(soldBenchActor);
            benchSlots.RemoveAt(selectedActorIndex);
        }
        else if (selectedActorLocation == 1)
        {
            AutoActorRollUpData soldFieldActor = fieldSlots[selectedActorIndex];
            shopManager.SellActor(soldFieldActor);
            fieldSlots.RemoveAt(selectedActorIndex);
        }
        ResetSelected();
        dataManager.GainGold(1);
        Save();
        UIManager.UpdateUI(this);
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
    public List<AutoActorRollUpData> fieldSlots;
    public int GetMaxFieldSlots()
    {
        return 2 + dataManager.GetLevel();
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
    public void LoadSlots()
    {
        benchSlots.Clear();
        fieldSlots.Clear();
        for (int i = 0; i < dataManager.benchActorData.Count; i++)
        {
            if (dataManager.benchActorData[i].Length <= 0){continue;}
            AutoActorRollUpData newBenchActor = new AutoActorRollUpData();
            newBenchActor.LoadRollUpData(dataManager.benchActorData[i]);
            newBenchActor.LoadBaseStats(actorData);
            benchSlots.Add(newBenchActor);
        }
        for (int i = 0; i < dataManager.fieldActorData.Count; i++)
        {
            if (dataManager.fieldActorData[i].Length <= 0){continue;}
            AutoActorRollUpData newFieldActor = new AutoActorRollUpData();
            newFieldActor.LoadRollUpData(dataManager.fieldActorData[i]);
            newFieldActor.LoadBaseStats(actorData);
            fieldSlots.Add(newFieldActor);
        }
    }
    public int selectedActorLocation;
    public int selectedActorIndex;
    public void ResetSelected()
    {
        selectedActorLocation = -1;
        selectedActorIndex = -1;
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
                    UIManager.ActivateSellObject();
                    break;
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
            UIManager.UpdateUI(this);
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
            newAutoActor.SetDirection(-1);
            fieldSlots.RemoveAt(selectedActorIndex);
            benchSlots.Add(newAutoActor);
            ResetSelected();
            UIManager.UpdateUI(this);
        }
        Save();
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
                    UIManager.ActivateSellObject();
                    UIManager.ActivateRotateObject();
                    UIManager.HighlightSelectedAttackRange(this, fieldSlots[i]);
                    selectedActorLocation = 1;
                    break;
                }
            }
        }
        // Move From Bench To Map (Potentially Swap).
        else if (selectedActorLocation == 0)
        {
            if (fieldSlots.Count >= GetMaxFieldSlots()){return;}
            // Check if any actor on the field is on the selected tile.
            AutoActorRollUpData currentfieldActor = GetFieldSlotActorOnTileNumber(tileNumber);
            if (currentfieldActor != null)
            {
                int benchLocation = benchSlots[selectedActorIndex].GetLocation();
                currentfieldActor.SetLocation(benchLocation);
                benchSlots.Add(currentfieldActor);
                fieldSlots.Remove(currentfieldActor);
            }
            // If not replacing an actor then you can't place anymore.
            else
            {
                if (fieldSlots.Count >= GetMaxFieldSlots()){return;}
            }
            // Make A New Copy Of An Actor On The Bench.
            AutoActorRollUpData newAutoActor = new AutoActorRollUpData();
            newAutoActor.LoadRollUpData(benchSlots[selectedActorIndex].ReturnRollUpData());
            newAutoActor.SetLocation(tileNumber);
            newAutoActor.SetDirection(1);
            benchSlots.RemoveAt(selectedActorIndex);
            fieldSlots.Add(newAutoActor);
            ResetSelected();
            UIManager.UpdateUI(this);
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
            UIManager.UpdateUI(this);
        }
        Save();
    }
    // For Changing The Direction After Selecting An Actor
    public void ChangeSelectedActorDirection(int direction)
    {
        if (selectedActorIndex < 0 || selectedActorLocation != 1){return;}
        selectedActorLocation = -1;
        fieldSlots[selectedActorIndex].SetDirection(direction);
        UIManager.UpdateUI(this);
        Save();
    }
}