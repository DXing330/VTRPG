using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessShopManager : MonoBehaviour
{
    public AutoChessShopDataManager data;
    public AutoChessShopDisplay UI;
    public AutoActorDisplay actorDisplay;
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
        data.Load();
        RefreshData();
        // Update The UI.
        UpdateAutoChessShopUI();
    }
    public void RefreshData()
    {
        shopActors = new List<AutoActorRollUpData>();
        List<string> currentListing = data.GetCurrentListing();
        for (int i = 0; i < currentListing.Count; i++)
        {
            AutoActorRollUpData newActor = new AutoActorRollUpData();
            newActor.SetName(currentListing[i]);
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
        data.GenerateCurrentListing();
        RefreshData();
        UpdateAutoChessShopUI();
    }
    public void Freeze()
    {

    }
    public int selectedIndex = -1;
    public void Select(int index)
    {
        // Single Click To View.
        if (selectedIndex != index)
        {
            selectedIndex = index;
            actorDisplay.DisplayActor(shopActors[selectedIndex]);
        }
        // Double Click To Buy.
    }
}
