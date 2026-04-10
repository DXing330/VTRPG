using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StatTrainingUI : MonoBehaviour
{
    public PartyDataManager partyData;
    public TacticActor selectedActor;
    public List<string> trainableStats;
    public List<int> baseCosts;
    public List<int> maxStats;
    public int selectedStat = -1;
    public ActorSpriteHPList traineeSelect;
    public TMP_Text partyGoldText;
    public TMP_Text trainingCostText;
    public TMP_Text trainedStatText;
    public TMP_Text trainingEffectText;
    protected void UpdateTrainingCostDetails()
    {
        trainedStatText.text = trainableStats[selectedStat];
        int stat = 0;
        switch (selectedStat)
        {
            case 0:
                stat = selectedActor.GetBaseHealth();
                break;
            case 1:
                stat = selectedActor.GetBaseAttack();
                break;
            case 2:
                stat = selectedActor.GetBaseDefense();
                break;
            case 3:
                stat = selectedActor.GetBaseEnergy();
                break;
            case 4:
                stat = selectedActor.GetInitiative();
                break;
        }
        trainingEffectText.text = stat + " > " + Mathf.Min((stat + 1), maxStats[selectedStat]);
        trainingCostText.text = (stat * stat * baseCosts[selectedStat]).ToString();
    }
    protected void UpdateTrainingDisplay()
    {
        partyGoldText.text = partyData.inventory.GetGold().ToString();
        if (traineeSelect.GetSelected() >= 0 && selectedStat >= 0)
        {
            UpdateTrainingCostDetails();
        }
        else
        {
            trainingCostText.text = "";
            trainedStatText.text = "";
            trainingEffectText.text = "";
        }
    }
    public void SelectTrainee()
    {
        if (traineeSelect.GetSelected() < 0){return;}
        selectedActor = partyData.ReturnActorAtIndex(traineeSelect.GetSelected());
        UpdateTrainingDisplay();
    }
    public void SelectTrainedStat(int index)
    {
        selectedStat = index;
        UpdateTrainingDisplay();
    }
    public void TrainStat()
    {
        if (traineeSelect.GetSelected() < 0 || selectedStat < 0){return;}
        // Check gold.
        if (int.Parse(partyGoldText.text) < int.Parse(trainingCostText.text))
        {
            return;
        }
        else
        {
            partyData.inventory.SpendGold(int.Parse(trainingCostText.text));
        }
        // Increase stat.
        int maxStat = maxStats[selectedStat];
        switch (selectedStat)
        {
            case 0:
                selectedActor.SetBaseHealth(Mathf.Min(selectedActor.GetBaseHealth() + 1, maxStat));
                break;
            case 1:
                selectedActor.SetBaseAttack(Mathf.Min(selectedActor.GetBaseAttack() + 1, maxStat));
                break;
            case 2:
                selectedActor.SetBaseDefense(Mathf.Min(selectedActor.GetBaseDefense() + 1, maxStat));
                break;
            case 3:
                selectedActor.SetBaseEnergy(Mathf.Min(selectedActor.GetBaseEnergy() + 1, maxStat));
                break;
            case 4:
                selectedActor.SetInitiative(Mathf.Min(selectedActor.GetInitiative() + 1, maxStat));
                break;
        }
        partyData.UpdatePartyMember(selectedActor, traineeSelect.GetSelected());
        UpdateTrainingDisplay();
    }
}
