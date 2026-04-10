using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CustomPassiveTraining : MonoBehaviour
{
    public int baseCost = 50;
    // Later add a discount rate based on city reputation.
    public GeneralUtility utility;
    public PassiveStats passiveStats;
    public PassiveDetailViewer detailViewer;
    public string GetPassiveDetails(string passiveDetails)
    {
        return detailViewer.ReturnPassiveDetails(passiveDetails);
    }
    void Start()
    {
        passiveStats = new PassiveStats();
        passiveStats.ResetStats();
        ResetState();
    }
    public PopUpMessage errorPopUp;
    // Step 1: select which actor from the party to train.
    public PartyDataManager partyData;
    public ActorSpriteHPList allActors;
    public void ResetState()
    {
        SetState(0);
        selectedActor = null;
        allActors.ResetSelected();
        UpdatePossibleTimings();
        selectedTiming = "";
        selectedEffect = "";
        selectedCondition = "";
    }
    // Current Custom Passives
    public TextList actorCCPDetails;
    protected void UpdateCurrentCustomPassives()
    {
        actorCCPDetails.ResetTextList();
        List<string> ccpDetails = new List<string>();
        List<string> aCP = selectedActor.GetCustomPassives();
        for (int i = 0; i < aCP.Count; i++)
        {
            ccpDetails.Add(GetPassiveDetails(aCP[i]));
        }
        actorCCPDetails.SetTextList(ccpDetails);
    }
    public void SelectActor()
    {
        if (allActors.GetSelected() < 0){return;}
        selectedActor = partyData.ReturnActorAtIndex(allActors.GetSelected());
        SetState(1);
        UpdateCurrentCustomPassives();
        UpdatePossibleTimings();
        selectedEffect = "";
        selectedCondition = "";
    }
    public TacticActor selectedActor;
    // Step 2: select the timing of the new passive.
    public List<string> passiveTimings;
    public SelectList timingSelect;
    public void UpdatePossibleTimings()
    {
        timingSelect.SetSelectables(passiveTimings);
        selectedTiming = "";
    }
    public void SelectTiming()
    {
        if (timingSelect.GetSelected() < 0){return;}
        selectedTiming = timingSelect.GetSelectedString();
        SetState(2);
        UpdatePossibleEffects();
    }
    public string selectedTiming;
    // Step 3/4: select the effect and condition in any order.
    public StatDatabase passiveEffects;
    public List<string> possibleEffects;
    public void UpdatePossibleEffects()
    {
        string allPossible = passiveEffects.ReturnValue(selectedTiming);
        List<string> possibleList = allPossible.Split("|").ToList();
        effectSelect.SetSelectables(possibleList);
        selectedEffect = "";
    }
    public void SelectEffect()
    {
        if (effectSelect.GetSelected() < 0){return;}
        selectedEffect = effectSelect.GetSelectedString();
        if (state < 3)
        {
            SetState(3);
            UpdatePossibleConditions();
        }
        else
        {
            UpdateResults();
        }
    }
    public SelectList effectSelect;
    public string selectedEffect; // Also tracks the target being effected and the effect specifics.
    public string factionName; // Affects bonus conditions.
    public void SetFactionName(string newInfo)
    {
        factionName = newInfo;
    }
    public StatDatabase factionConditions;
    public StatDatabase factionFilters;
    public StatDatabase passiveConditionFilters;
    public GameObject conditionFilterObject;
    public SelectList conditionFilterSelect;
    public string filteredCondition;
    protected void ResetCondition()
    {
        filteredCondition = "";
        selectedCondition = "";
    }
    public void UpdatePossibleFilters()
    {
        string allPossible = passiveConditionFilters.ReturnValue(selectedTiming);
        List<string> possibleList = allPossible.Split("|").ToList();
        if (factionName != "")
        {
            possibleList.AddRange(factionFilters.ReturnValue(selectedTiming).Split("|").ToList());
        }
        conditionFilterObject.SetActive(true);
        conditionFilterSelect.SetSelectables(possibleList);
    }
    public void ResetFilter()
    {
        filteredCondition = "";
        conditionFilterObject.SetActive(false);
        UpdatePossibleConditions();
    }
    public void SelectFilter()
    {
        if (conditionFilterSelect.GetSelected() < 0){return;}
        filteredCondition = conditionFilterSelect.GetSelectedString();
        conditionFilterObject.SetActive(false);
        UpdatePossibleConditions();
    }
    public StatDatabase passiveConditions;
    public StatDatabase passiveConditionMultipliers;
    public List<string> possibleConditions;
    public void UpdatePossibleConditions()
    {
        string allPossible = passiveConditions.ReturnValue(selectedTiming);
        List<string> possibleList = allPossible.Split("|").ToList();
        if (factionName != "")
        {
            possibleList.AddRange(factionConditions.ReturnValue(factionName + "-" + selectedTiming).Split("|").ToList());
        }
        if (filteredCondition != "")
        {
            possibleList = utility.ReturnFilteredList(possibleList, filteredCondition);
        }
        conditionSelect.SetSelectables(possibleList);
        ResetCondition();
    }
    public void SelectCondition()
    {
        if (conditionSelect.GetSelected() < 0){return;}
        selectedCondition = conditionSelect.GetSelectedString();
        UpdateResults();
    }
    public SelectList conditionSelect;
    public string selectedCondition; // Also tracks condition specifics.
    public int state; // 0 - selecting actor, 1 - selecting timing, 2 - selecting effect and conditions
    public void SetState(int newState)
    {
        ResetResults();
        state = newState;
        for (int i = 0; i < stateToObjectMapping.Count; i++)
        {
            if (stateToObjectMapping[i] > state)
            {
                stateObjects[i].SetActive(false);
            }
            else
            {
                stateObjects[i].SetActive(true);
            }
        }
    }
    public List<int> stateToObjectMapping;
    public List<GameObject> stateObjects;
    // Results.
    public void ResetResults()
    {
        passiveDetailText.text = "";
        currentGold.text = "";
        cost.text = "";
    }
    public void UpdateResults()
    {
        if (selectedTiming == "" || selectedCondition == "" || selectedEffect == "")
        {
            ResetResults();
            return;
        }
        passiveStats.ResetStats();
        passiveStats.SetTiming(selectedTiming);
        passiveStats.SetConditionAndSpecifics(selectedCondition);
        passiveStats.SetTargetEffectAndSpecifics(selectedEffect, " ", int.Parse(passiveConditionMultipliers.ReturnValue(selectedCondition)));
        passiveDetailText.text = GetPassiveDetails(passiveStats.ReturnStats());
        currentGold.text = partyData.inventory.GetGold().ToString();
        cost.text = ((selectedActor.CustomPassiveCount() + 1) * baseCost).ToString();
    }
    public TMP_Text passiveDetailText;
    public TMP_Text currentGold;
    public TMP_Text cost;
    public PopUpMessage errorMessage;
    public void ConfirmTraining()
    {
        int selectedIndex = allActors.GetSelected();
        selectedActor.SetInitialStatsFromString(partyData.ReturnPartyMemberStatsAtIndex(selectedIndex));
        // Check if you already have the passive.
        string newCP = passiveStats.ReturnStats();
        if (selectedActor.CustomPassiveExists(newCP))
        {
            errorMessage.SetMessage("Already know this passive. Try training something new.");
            return;
        }
        // Check if you can afford it.
        int trainingFee = (selectedActor.CustomPassiveCount() + 1) * baseCost;
        if (partyData.inventory.GetGold() < trainingFee)
        {
            errorMessage.SetMessage("You cannot afford this training at this time.");
            return;
        }
        // Add the passive to the actor.
        partyData.inventory.SpendGold(trainingFee);
        selectedActor.AddCustomPassive(passiveStats.ReturnStats());
        partyData.UpdatePartyMember(selectedActor, selectedIndex);
        // Refresh data.
        SelectActor();
    }
}