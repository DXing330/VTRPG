using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Select a trainer.
// Check if the trainer has a relevant passive based on their sprite name.
// Determine the max passive level as relevant passive / 2.
// Determine if they can learn anymore by checking their relevant passive.
// Determine the price based on total class passive levels.
public class SubclassTrainingUI : MonoBehaviour
{
    public PartyDataManager partyData;
    public TacticActor selectedActor;
    public ActorSpriteHPList trainerSelect;
    public ActorSpriteHPList traineeSelect;
    public TrainClassDisplay trainerDisplay;
    public string trainedClass;
    public int maxTrainedLevel;
    public TrainClassDisplay traineeDisplay;
    public int totalTraineeLevels;
    public PassiveDetailViewer detailViewer;
    public TMP_Text partyGoldText;
    public TMP_Text trainingCostText;
    public TMP_Text trainedSkillText;
    public TMP_Text trainingEffectText;
    public PopUpMessage errorMessage;
    protected void UpdateTrainingCostDetails()
    {
        int currentLevel = partyData.ReturnActorAtIndex(traineeSelect.GetSelected()).GetLevelFromPassive(trainedClass);trainingCostText.text = ((totalTraineeLevels * pricePerLevel) + ((currentLevel + 1) * baseCostPerLevel[trainableClasses.IndexOf(trainedClass)])).ToString();
        trainedSkillText.text = trainedClass;
        trainingEffectText.text = currentLevel + " > " + Mathf.Min((currentLevel + 1), maxTrainedLevel);
    }
    protected void UpdateTrainingDisplay()
    {
        partyGoldText.text = partyData.inventory.GetGold().ToString();
        if (trainerSelect.GetSelected() >= 0 && traineeSelect.GetSelected() >= 0)
        {
            UpdateTrainingCostDetails();
        }
        else
        {
            trainingCostText.text = "";
            trainedSkillText.text = "";
            trainingEffectText.text = "";
        }
    }
    public List<string> trainableClasses;
    public List<int> baseCostPerLevel;
    public int pricePerLevel;
    public void SelectTrainer()
    {
        if (trainerSelect.GetSelected() < 0){return;}
        selectedActor = partyData.ReturnActorAtIndex(trainerSelect.GetSelected());
        trainerDisplay.UpdateDisplay(selectedActor, trainableClasses);
        trainedClass = selectedActor.GetSpriteName();
        if (trainableClasses.Contains(trainedClass))
        {
            maxTrainedLevel = selectedActor.GetLevelFromPassive(trainedClass) / 2;
        }
        else
        {
            maxTrainedLevel = 0;
        }
        UpdateTrainingDisplay();
    }
    public void ResetTrainer()
    {
        trainerSelect.ResetSelected();
        trainedClass = "";
        maxTrainedLevel = -1;
        UpdateTrainingDisplay();
    }
    public void SelectTrainee()
    {
        if (traineeSelect.GetSelected() < 0){return;}
        selectedActor = partyData.ReturnActorAtIndex(traineeSelect.GetSelected());
        traineeDisplay.UpdateDisplay(selectedActor, trainableClasses);
        totalTraineeLevels = selectedActor.GetTotalPassiveLevelsOfPassiveGroup(trainableClasses);
        UpdateTrainingDisplay();
    }
    public void ResetTrainee()
    {
        traineeSelect.ResetSelected();
        totalTraineeLevels = 0;
        UpdateTrainingDisplay();
    }
    public void TrainSubClass()
    {
        // Didn't select a trainer.
        if (trainerSelect.GetSelected() < 0 || traineeSelect.GetSelected() < 0)
        {
            errorMessage.SetMessage("Please select an appropriate trainer and trainee before training.");
            return;
        }
        // Not enough gold.
        if (int.Parse(partyGoldText.text) < int.Parse(trainingCostText.text))
        {
            errorMessage.SetMessage("Not enough gold to pay for this training.");
            return;
        }
        // Training would have no effect.
        string[] levelChange = trainingEffectText.text.Split(">");
        if (levelChange.Length < 2)
        {
            errorMessage.SetMessage("This training would have no effect.");
            return;
        }
        else if (int.Parse(levelChange[0]) >= int.Parse(levelChange[1]))
        {
            errorMessage.SetMessage("This training would have no effect.");
            return;
        }
        selectedActor = partyData.ReturnActorAtIndex(traineeSelect.GetSelected());
        selectedActor.AddPassiveSkill(trainedClass, "1");
        partyData.UpdatePartyMember(selectedActor, traineeSelect.GetSelected());
        partyData.inventory.SpendGold(int.Parse(trainingCostText.text));
        // Reset after updating.
        trainerDisplay.DisableDisplay();
        traineeDisplay.DisableDisplay();
        traineeSelect.RefreshData();
        trainerSelect.RefreshData();
        ResetTrainee();
        ResetTrainer();
    }
}