using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessShopManager : MonoBehaviour
{
    public AutoChessShopDataManager shopData;
    public bool RemoveFromPool(string actorName)
    {
        return shopData.RemoveFromPool(actorName);
    }
    public AutoChessShopDisplay UI;
    public StatDatabase actorData;
    public string ReturnActorFactions(AutoActorRollUpData actor)
    {
        string[] blocks = actorData.ReturnValue(actor.GetName()).Split("|");
        return blocks[0];
    }
    public StatDatabase actorCost;
    public int ReturnActorCost(string actorName)
    {
        return int.Parse(actorCost.ReturnValue(actorName));
    }
    public int ReturnActorCost(AutoActorRollUpData actor)
    {
        return ReturnActorCost(actor.GetName());
    }
    public StatDatabase actorRarity;
    public int ReturnActorRarity(AutoActorRollUpData actor)
    {
        return int.Parse(actorRarity.ReturnValue(actor.GetName()));
    }
    public List<AutoActorRollUpData> shopActors;
    void Start()
    {
        ResetSelected();
        // Load The Data.
        shopData.Load();
        // Update The UI.
        UpdateAutoChessShopUI();
    }
    public void Save()
    {
        shopData.Save();
    }
    public void RefreshData()
    {
        shopActors = new List<AutoActorRollUpData>();
        List<string> currentListing = shopData.GetCurrentListing();
        for (int i = 0; i < currentListing.Count; i++)
        {
            if (currentListing[i].Length <= 0){continue;}
            AutoActorRollUpData newActor = new AutoActorRollUpData();
            newActor.SetName(currentListing[i]);
            newActor.LoadBaseStats(actorData);
            shopActors.Add(newActor);
        }
    }
    public void PVPRefreshData()
    {
        shopActors = new List<AutoActorRollUpData>();
        List<string> currentListing = shopData.GetPVPCurrentListing();
        for (int i = 0; i < currentListing.Count; i++)
        {
            if (currentListing[i].Length <= 0){continue;}
            AutoActorRollUpData newActor = new AutoActorRollUpData();
            newActor.SetName(currentListing[i]);
            newActor.LoadBaseStats(actorData);
            shopActors.Add(newActor);
        }
    }
    public void UpdateAutoChessShopUI()
    {
        RefreshData();
        UI.UpdateAutoChessShopUI(this);
    }
    public void PVPReroll()
    {
        shopData.GeneratePVPCurrentListing(true);
    }
    public void Reroll()
    {
        ResetSelected();
        shopData.GenerateCurrentListing(true);
        UpdateAutoChessShopUI();
    }
    public int GetFrozen(){return shopData.frozenShop;}
    public void FreezeShop()
    {
        ResetSelected();
        shopData.FreezeShop();
        UpdateAutoChessShopUI();
    }
    public int selectedIndex = -1;
    public void ResetSelected(){selectedIndex = -1;}
    public void Select(int index)
    {
        selectedIndex = index;
    }
    public int SelectedCost()
    {
        if (selectedIndex < 0){return -1;}
        AutoActorRollUpData selectedActor = shopActors[selectedIndex];
        return ReturnActorCost(selectedActor);
    }
    public AutoActorRollUpData GetSelectedActor()
    {
        return shopActors[selectedIndex];
    }
    public AutoActorRollUpData GetActorOfFaction(string faction)
    {
        return null;
    }
    public AutoActorRollUpData GetActorOfRarity(int rarity)
    {
        return null;
    }
    public void PVPBuySelectedActor()
    {
        shopActors.RemoveAt(selectedIndex);
        shopData.RemoveFromPVPListing(selectedIndex);
        ResetSelected();
        PVPRefreshData();
    }
    public void BuySelectedActor()
    {
        shopActors.RemoveAt(selectedIndex);
        shopData.RemoveFromListing(selectedIndex);
        ResetSelected();
        UpdateAutoChessShopUI();
    }
    public void SellActor(AutoActorRollUpData soldActor)
    {
        string name = soldActor.GetName();
        int level = soldActor.GetLevel();
        shopData.AddToPool(name, level);
    }
}
