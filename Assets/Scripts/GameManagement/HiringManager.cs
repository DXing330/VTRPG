using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HiringManager : MonoBehaviour
{
    void Start()
    {
        GenerateHirelings();
        hirelingList.ResetSelected();
    }
    public StatDatabase firstNames;
    public StatDatabase middleNames;
    public StatDatabase lastNames;
    public string GenerateRandomName()
    {
        return firstNames.ReturnRandomValue()+" "+middleNames.ReturnRandomValue()+" "+lastNames.ReturnRandomValue();
    }
    public StatDatabase actorData;
    public TacticActor dummyActor;
    public PartyDataManager partyData;
    public Inventory inventory;
    // Classes
    public List<string> hireableActors;
    public List<string> basePrices;
    public List<string> possibleNames;
    // Only so many hireables appear each day/week/etc?
    public SelectStatTextList hirelingList;
    public StatTextList hirelingStats;
    public PopUpMessage popUp;
    // As you perform you can hire more/better hirelings.
    public List<string> currentHirelingClasses;
    public List<string> currentHirelingNames;

    protected void GenerateHirelings()
    {
        currentHirelingClasses.Clear();
        currentHirelingNames.Clear();
        for (int i = 0; i < Mathf.Max(hireableActors.Count, 6); i++)
        {
            currentHirelingClasses.Add(hireableActors[Random.Range(0, hireableActors.Count)]);
            currentHirelingNames.Add(GenerateRandomName());
        }
        hirelingList.SetStatsAndData(currentHirelingClasses, currentHirelingNames);
    }

    protected string GetPrice()
    {
        int index = hirelingList.GetSelected();
        string className = currentHirelingClasses[index];
        string price = basePrices[hireableActors.IndexOf(className)];
        return price;
    }

    public void ViewStats()
    {
        int index = hirelingList.GetSelected();
        if (index == -1){return;}
        string className = currentHirelingClasses[index];
        string price = GetPrice();
        dummyActor.ResetStatsBeforeLoading();
        // Changing the delimiter introduces bugs, we should try to resolve this somehow.
        dummyActor.SetInitialStats(actorData.ReturnValue(className).Split(dummyActor.delimiter).ToList());
        List<string> stats = new List<string>();
        List<string> data = new List<string>();
        stats.Add("Price");
        stats.Add("Health");
        stats.Add("Attack");
        stats.Add("Defense");
        stats.Add("Energy");
        stats.Add("Move Speed");
        stats.Add("Initiative");
        stats.Add("Class");
        data.Add(price);
        data.Add(dummyActor.GetBaseHealth().ToString());
        data.Add(dummyActor.GetBaseAttack().ToString());
        data.Add(dummyActor.GetBaseDefense().ToString());
        data.Add(dummyActor.GetBaseEnergy().ToString());
        data.Add(dummyActor.GetMoveSpeed().ToString());
        data.Add(dummyActor.GetInitiative().ToString());
        data.Add(className);
        hirelingStats.SetStatsAndData(stats, data);
    }

    public void TryToHire()
    {
        if (!partyData.OpenSlots())
        {
            popUp.SetMessage("You aren't allowed to hire any more hirelings. Rank up more in order to be trusted with more men.");
            return;
        }
        int selected = hirelingList.GetSelected();
        if (selected < 0){return;}
        int price = int.Parse(GetPrice());
        if (inventory.EnoughGold(price))
        {
            string className = currentHirelingClasses[selected];
            inventory.SpendGold(price);
            partyData.HireMember(actorData.ReturnValue(className), currentHirelingNames[selected]);
            currentHirelingClasses.RemoveAt(selected);
            currentHirelingNames.RemoveAt(selected);
            hirelingList.ResetSelected();
            hirelingList.SetStatsAndData(currentHirelingClasses, currentHirelingNames);
        }
        else
        {
            popUp.SetMessage("You can't afford to hire them.");
        }
    }
}
