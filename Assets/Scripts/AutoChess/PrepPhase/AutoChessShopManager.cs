using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessShopManager : MonoBehaviour
{
    public AutoChessShopDataManager shopData;
    public AutoChessShopDisplay UI;
    public StatDatabase actorData;
    public string ReturnActorFactions(AutoActorRollUpData actor)
    {
        string[] blocks = actorData.ReturnValue(actor.GetName()).Split("|");
        return blocks[0];
    }
    public StatDatabase actorCost;
    public int ReturnActorCost(AutoActorRollUpData actor)
    {
        return int.Parse(actorCost.ReturnValue(actor.GetName()));
    }
    public StatDatabase actorRarity;
    public int ReturnActorRarity(AutoActorRollUpData actor)
    {
        return int.Parse(actorRarity.ReturnValue(actor.GetName()));
    }
    public List<AutoActorRollUpData> shopActors;
    void Start()
    {
        // Load The Data.
        shopData.Load();
        RefreshData();
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
        selectedIndex = -1;
    }
    public void UpdateAutoChessShopUI()
    {
        UI.UpdateAutoChessShopUI(this);
    }
    public void Reroll()
    {
        shopData.GenerateCurrentListing();
        RefreshData();
        UpdateAutoChessShopUI();
    }
    public void Freeze()
    {
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
