using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessPrepManager : MonoBehaviour
{
    public AutoChessDataManager dataManager;
    public void Save()
    {
        dataManager.SaveFromPrepManager(this);
        shopManager.Save();
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
        boughtActor.LoadBaseTrait(actorData);
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
    public void LoadSlots()
    {
        benchSlots.Clear();
        fieldSlots.Clear();
        for (int i = 0; i < dataManager.benchActorData.Count; i++)
        {
            if (dataManager.benchActorData[i].Length <= 0){continue;}
            AutoActorRollUpData newBenchActor = new AutoActorRollUpData();
            newBenchActor.LoadRollUpData(dataManager.benchActorData[i]);
            newBenchActor.LoadBaseTrait(actorData);
            benchSlots.Add(newBenchActor);
        }
        for (int i = 0; i < dataManager.fieldActorData.Count; i++)
        {
            if (dataManager.fieldActorData[i].Length <= 0){continue;}
            AutoActorRollUpData newFieldActor = new AutoActorRollUpData();
            newFieldActor.LoadRollUpData(dataManager.fieldActorData[i]);
            newFieldActor.LoadBaseTrait(actorData);
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
            selectedActorLocation = 0;
            for (int i = 0; i < benchSlots.Count; i++)
            {
                if (benchSlots[i].GetLocation() == clickedLocation)
                {
                    selectedActorIndex = i;
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
        // Move From Map To Bench.
        else if (selectedActorLocation == 1)
        {
            selectedActorLocation = -1;
        }
    }
    // Move From Bench To Map, Select Actor On Map, Move From Map To Map
    public void ClickOnTile(int index)
    {
        // Select Actor On Map.
        if (selectedActorLocation < 0)
        {
            selectedActorLocation = 1;
            for (int i = 0; i < fieldSlots.Count; i++)
            {
                if (fieldSlots[i].location == index)
                {
                    selectedActorIndex = i;
                    break;
                }
            }
        }
        // Move From Bench To Map.
        else if (selectedActorLocation == 0)
        {

        }
        // Move From Map To Map.
        else
        {

        }
    }
}