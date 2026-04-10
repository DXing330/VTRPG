using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ElementalTrainingUI : MonoBehaviour
{
    public PartyDataManager partyData;
    public TacticActor selectedActor;
    public string resourceCost = "Mana";
    public int baseCost;
    public int selectedStat = -1;
    public void SelectTrainedStat(int index)
    {
        selectedStat = index;
        UpdateTrainingDisplay();
    }
    public int selectedTraining = -1;
    public void SelectTraining(int index)
    {
        selectedTraining = index;
        UpdateTrainingDisplay();
    }
    public string allTrainablePassives;
    [ContextMenu("Load")]
    public void Load()
    {
        trainablePassives = allTrainablePassives.Split("|").ToList();
    }
    public List<string> trainablePassives;
    public string GetTrainedPassive()
    {
        if (selectedStat < 0 || selectedTraining < 0)
        {
            return "";
        }
        int index = selectedStat + (selectedTraining * trainablePassives.Count / 2);
        return trainablePassives[index];
    }
    protected void UpdateTrainingCostDetails()
    {
        trainedStatText.text = GetTrainedPassive();
        int stat = selectedActor.GetLevelFromPassive(GetTrainedPassive());
        trainingEffectText.text = stat + " > " + (stat + 1);
        int totalLevel = selectedActor.GetTotalPassiveLevelsOfPassiveGroup(trainablePassives);
        trainingCostText.text = ((totalLevel + 1) * baseCost).ToString();
    }
    protected void UpdateTrainingDisplay()
    {
        partyManaText.text = partyData.inventory.ReturnQuantityOfItem(resourceCost).ToString();
        if (traineeSelect.GetSelected() >= 0 && selectedStat >= 0 && selectedTraining >= 0)
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
    public ActorSpriteHPList traineeSelect;
    public void SelectTrainee()
    {
        if (traineeSelect.GetSelected() < 0){return;}
        selectedActor = partyData.ReturnActorAtIndex(traineeSelect.GetSelected());
        UpdateTrainingDisplay();
    }
    public TMP_Text partyManaText;
    public TMP_Text trainingCostText;
    public TMP_Text trainedStatText;
    public TMP_Text trainingEffectText;
    public void TrainStat()
    {
        if (traineeSelect.GetSelected() < 0 || selectedStat < 0 || selectedTraining < 0){return;}
        if (int.Parse(partyManaText.text) < int.Parse(trainingCostText.text))
        {
            return;
        }
        else
        {
            partyData.inventory.RemoveItemQuantity(int.Parse(trainingCostText.text), resourceCost);
        }
        selectedActor.AddPassiveSkill(GetTrainedPassive(), "1");
        partyData.UpdatePartyMember(selectedActor, traineeSelect.GetSelected());
        UpdateTrainingDisplay();
    }
}
