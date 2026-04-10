using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Used to train standard skills: weapons, basic tile types and basic classes
// Step 1. Select Actor (Can View Current Passives)
// Step 2. Select Passive (View Upgrade Effect) (View Price)
// Step 3. Confirm and Pay
public class BasicPassiveTraining : MonoBehaviour
{
    public int baseCost = 100;
    public GeneralUtility utility;
    public PartyDataManager partyData;
    public ActorSpriteHPList allActors;
    public TacticActor selectedActor;
    public TextList sActorAllPassives;
    protected void UpdateCurrentPassives()
    {
        if (selectedActor == null){return;}
        sActorAllPassives.ResetTextList();
        sActorAllPassives.SetTextList(detailViewer.ReturnAllPassiveDetails(selectedActor));
    }
    public PassiveDetailViewer detailViewer;
    void Start()
    {
        SetState(0);
        ResetDetails();
    }
    public SelectStatTextList passiveSelect;
    public string factionName; // Affects max trainable levels.
    public void SetFactionName(string newInfo)
    {
        factionName = newInfo;
    }
    public StatDatabase trainablePassives;
    public List<string> passiveNames;
    public List<string> passiveMaxLevels;
    public void UpdateTrainablePassives()
    {
        passiveNames.Clear();
        passiveMaxLevels.Clear();
        string[] tP = trainablePassives.ReturnValue(factionName).Split("|");
        for (int i = 0; i < tP.Length; i++)
        {
            string[] blocks = tP[i].Split("-");
            if (blocks.Length < 2){continue;}
            passiveNames.Add(blocks[0]);
            passiveMaxLevels.Add(blocks[1]);
        }
        passiveSelect.SetStatsAndData(passiveNames, passiveMaxLevels);
    }
    // State details.
    public List<int> stateToObjectMapping;
    public List<GameObject> stateObjects;
    public int state;
    public void SetState(int newState)
    {
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
        UpdateDetails();
    }
    // Select logic.
    public void SelectActor()
    {
        if (allActors.GetSelected() < 0){return;}
        selectedActor = partyData.ReturnActorAtIndex(allActors.GetSelected());
        UpdateCurrentPassives();
        ResetSelectedTraining();
        UpdateTrainablePassives();
        SetState(1);
    }
    public void ResetSelectedActor()
    {
        selectedActor = null;
        SetState(0);
    }
    public string selectedTraining;
    public int selectedMaxLevel;
    public void SelectTraining()
    {
        if (passiveSelect.GetSelected() < 0){return;}
        selectedTraining = passiveSelect.GetSelectedStat();
        int indexOf = passiveNames.IndexOf(selectedTraining);
        selectedMaxLevel = int.Parse(passiveMaxLevels[indexOf]);
        SetState(2);
        UpdateDetails();
    }
    public void ResetSelectedTraining()
    {
        selectedTraining = "";
        selectedMaxLevel = 0;
        ResetDetails();
    }
    // Display details.
    public TMP_Text currentLevel;
    public TMP_Text nextLevelDetails;
    public TMP_Text cost;
    protected int GetCost()
    {
        if (selectedActor == null){return baseCost;}
        int totalCount = 1 + selectedActor.GetTotalPassiveLevelsOfPassiveGroup(passiveNames);
        return baseCost * totalCount;
    }
    public TMP_Text gold;
    public void ResetDetails()
    {
        currentLevel.text = "";
        nextLevelDetails.text = "";
        cost.text = GetCost().ToString();
        gold.text = partyData.inventory.GetGold().ToString();
    }
    public void UpdateDetails()
    {
        ResetDetails();
        if (selectedTraining == "" || selectedActor == null){return;}
        currentLevel.text = (selectedActor.GetLevelFromPassive(selectedTraining)).ToString();
        nextLevelDetails.text = detailViewer.ReturnSpecificPassiveLevelEffect(selectedTraining, selectedActor.GetLevelFromPassive(selectedTraining) + 1);
    }
    public void ViewAllPassiveLevels()
    {
        detailViewer.UpdatePassiveNames(selectedTraining, selectedMaxLevel.ToString());
    }
    // Confirm, pay and add the passive.
    public PopUpMessage errorMessage;
    public void ConfirmTraining()
    {
        int selectedIndex = allActors.GetSelected();
        selectedActor.SetInitialStatsFromString(partyData.ReturnPartyMemberStatsAtIndex(selectedIndex));
        int cLevel = selectedActor.GetLevelFromPassive(selectedTraining);
        // Check if you're below max level.
        if (cLevel >= selectedMaxLevel)
        {
            errorMessage.SetMessage("We can't help you improve this skill any further.");
            SetState(1);
            return;
        }
        int cost = GetCost();
        int gold = partyData.inventory.GetGold();
        // Check if you can afford it.
        if (cost > gold)
        {
            errorMessage.SetMessage("You can't afford this training at this time.");
            SetState(0);
            return;
        }
        // Subtract gold.
        partyData.inventory.SpendGold(cost);
        // Gain skill.
        selectedActor.AddPassiveSkill(selectedTraining, "1");
        partyData.UpdatePartyMember(selectedActor, selectedIndex);
        // Refresh.
        SetState(1);
    }
}
