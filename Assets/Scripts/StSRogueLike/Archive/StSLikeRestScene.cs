using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class StSLikeRestScene : MonoBehaviour
{
    public enum RestMenuState
    {
        Default,
        ActionSelect,
        MendTargetSelect,
        TrainSelect
    }

    // --- Scene References ---
    public StSStateManager stsManager;
    public GeneralUtility utility;
    public PartyDataManager partyData;
    public TacticActor dummyActor;
    public ActorSpriteHPList actorSelectList;
    public GameObject restChoiceSelectObject;
    public GameObject actorChoiceObject;
    public SelectList restChoiceSelect;
    public GameObject mendTargetSelectObject;
    public ActorSpriteHPList mendTargetSelect;
    public TMP_Text actorChoiceText;
    public TMP_Text actorChoiceSpecificsText;
    public List<TMP_Text> restEffects;

    // --- Rest Choice Data ---
    public List<string> possibleChoices;
    public List<bool> choiceAvailable;
    public List<string> restChoices;
    public List<string> restChoiceSpecifics;
    public int selectedActorIndex = -1;
    protected string workingChoice = "";
    protected string workingSpecifics = "";
    protected RestMenuState currentMenuState = RestMenuState.Default;
    protected List<int> currentMendTargetIDs = new List<int>();

    // --- Train UI Data ---
    public StatDatabase activeData;
    public ActiveSkill previewActive;
    public GameObject trainChoiceObject;
    public BasicSkillTrainingUI skillTraining;

    // --- Scene Setup ---
    void Start()
    {
        partyData.Load();
        actorSelectList.RefreshData();
        if (mendTargetSelect != null){mendTargetSelect.startUp = false;}
        UpdateAvailableChoices();
        InitializeChoices();
        ReturnToDefaultMenu();
    }

    // --- Actor Selection ---
    public void SelectActor()
    {
        if (GetSelectedActorIndex() < 0){return;}
        selectedActorIndex = GetSelectedActorIndex();
        LoadWorkingChoiceForSelectedActor();
        OpenActionSelectMenu();
    }

    protected int GetSelectedActorIndex()
    {
        return actorSelectList.GetSelected();
    }

    protected TacticActor GetSelectedPartyActor()
    {
        if (selectedActorIndex < 0){return null;}
        return partyData.ReturnActorAtIndex(selectedActorIndex);
    }

    protected void InitializeChoices()
    {
        int actorCount = actorSelectList.allActorNames.Count;
        if (restChoices == null){restChoices = new List<string>();}
        if (restChoiceSpecifics == null){restChoiceSpecifics = new List<string>();}
        restChoices.Clear();
        restChoiceSpecifics.Clear();
        for (int i = 0; i < actorCount; i++)
        {
            restChoices.Add("");
            restChoiceSpecifics.Add("");
        }
    }

    // --- Default Menu / Summaries ---
    protected void ReturnToDefaultMenu()
    {
        currentMenuState = RestMenuState.Default;
        workingChoice = "";
        workingSpecifics = "";
        if (restChoiceSelectObject != null){restChoiceSelectObject.SetActive(false);}
        if (actorChoiceObject != null){actorChoiceObject.SetActive(false);}
        if (mendTargetSelectObject != null){mendTargetSelectObject.SetActive(false);}
        if (trainChoiceObject != null){trainChoiceObject.SetActive(false);}
        UpdateRestSummary();
    }

    protected void UpdateRestSummary()
    {
        for (int i = 0; i < restEffects.Count; i++)
        {
            if (i >= actorSelectList.allActorNames.Count)
            {
                restEffects[i].text = "";
                continue;
            }
            string summary = actorSelectList.allActorNames[i];
            if (restChoices[i].Length <= 0)
            {
                summary += ": No Choice";
                restEffects[i].text = summary;
                continue;
            }
            summary += ": " + restChoices[i];
            string specificsText = ReturnChoiceSummaryText(restChoices[i], restChoiceSpecifics[i], i);
            if (specificsText.Length > 0)
            {
                summary += " (" + specificsText + ")";
            }
            restEffects[i].text = summary;
        }
    }

    // --- Working Choice State ---
    protected void LoadWorkingChoiceForSelectedActor()
    {
        if (selectedActorIndex < 0){return;}
        workingChoice = restChoices[selectedActorIndex];
        workingSpecifics = restChoiceSpecifics[selectedActorIndex];
    }

    protected void SaveWorkingChoiceForSelectedActor()
    {
        if (selectedActorIndex < 0){return;}
        restChoices[selectedActorIndex] = workingChoice;
        restChoiceSpecifics[selectedActorIndex] = workingSpecifics;
    }

    protected bool ChoiceValid(string choice, string specifics)
    {
        switch (choice)
        {
            case "Rest":
                return true;
            case "Mend":
                return specifics.Length > 0;
            case "Train":
                return specifics.Length > 0;
        }
        return false;
    }

    protected bool CurrentActorChoiceValid()
    {
        return ChoiceValid(workingChoice, workingSpecifics);
    }

    protected bool AllChoicesConfirmed()
    {
        for (int i = 0; i < restChoices.Count; i++)
        {
            if (!ChoiceValid(restChoices[i], restChoiceSpecifics[i]))
            {
                return false;
            }
        }
        return true;
    }

    public void ConfirmChoice()
    {
        SyncWorkingChoiceFromCurrentMenu();
        if (!CurrentActorChoiceValid()){return;}
        SaveWorkingChoiceForSelectedActor();
        ReturnToDefaultMenu();
    }

    public void ConfirmAllChoices()
    {
        if (!AllChoicesConfirmed()){return;}
        ResolveRestChoices();
        stsManager.ReturnToMap();
    }

    public void CancelCurrentChoice()
    {
        ReturnToDefaultMenu();
    }

    // --- Action Selection ---
    protected void UpdateAvailableChoices()
    {
        List<string> availableChoices = new List<string>();
        for (int i = 0; i < possibleChoices.Count; i++)
        {
            if (choiceAvailable[i]){availableChoices.Add(possibleChoices[i]);}
        }
        restChoiceSelect.SetSelectables(availableChoices);
    }

    protected void OpenActionSelectMenu()
    {
        currentMenuState = RestMenuState.ActionSelect;
        if (restChoiceSelectObject != null){restChoiceSelectObject.SetActive(true);}
        if (actorChoiceObject != null){actorChoiceObject.SetActive(true);}
        if (mendTargetSelectObject != null){mendTargetSelectObject.SetActive(false);}
        if (trainChoiceObject != null){trainChoiceObject.SetActive(false);}
        restChoiceSelect.SetSelectedString(workingChoice);
        UpdateActorChoiceText();
    }

    public void SelectRestChoice()
    {
        if (restChoiceSelect.GetSelected() < 0){return;}
        string selectedChoice = restChoiceSelect.GetSelectedString();
        switch (selectedChoice)
        {
            case "Rest":
                SelectRest();
                break;
            case "Mend":
                BeginMend();
                break;
            case "Train":
                BeginTrain();
                break;
        }
    }

    protected void SelectRest()
    {
        workingChoice = "Rest";
        workingSpecifics = "";
        UpdateActorChoiceText();
    }

    public void UpdateActorChoiceText()
    {
        actorChoiceText.text = workingChoice;
        actorChoiceSpecificsText.text = ReturnChoiceDetailText(workingChoice, workingSpecifics, selectedActorIndex);
    }

    // --- Choice Display ---
    protected string ReturnChoiceSummaryText(string choice, string specifics, int actorIndex)
    {
        switch (choice)
        {
            case "Rest":
                return "30% HP, 40% MP, clear statuses";
            case "Mend":
                int targetID = utility.SafeParseInt(specifics, -1);
                if (targetID < 0){return "";}
                return ReturnActorNameFromID(targetID) + " - 30% HP, 40% MP, clear statuses";
            case "Train":
                string skillName;
                string upgradeType;
                if (!TryParseTrainSpecifics(specifics, out skillName, out upgradeType))
                {
                    return "";
                }
                return skillName + " - " + upgradeType;
        }
        return "";
    }

    protected string ReturnChoiceDetailText(string choice, string specifics, int actorIndex)
    {
        switch (choice)
        {
            case "Rest":
                return "Restore 30% of max HP.\nRestore 40% of max MP.\nClear statuses.";
            case "Mend":
                int targetID = utility.SafeParseInt(specifics, -1);
                if (targetID < 0){return "";}
                return "Target: " + ReturnActorNameFromID(targetID) + "\nRestore 30% of max HP.\nRestore 40% of max MP.\nClear statuses.";
            case "Train":
                return ReturnTrainChoiceDetailText(specifics, actorIndex);
        }
        return "";
    }

    protected void SyncWorkingChoiceFromCurrentMenu()
    {
        if (currentMenuState == RestMenuState.TrainSelect && skillTraining != null)
        {
            if (skillTraining.CanConfirmTraining())
            {
                workingChoice = "Train";
                workingSpecifics = skillTraining.GetTrainingSpecifics();
            }
        }
    }

    // --- Mend Choice ---
    protected void BeginMend()
    {
        currentMenuState = RestMenuState.MendTargetSelect;
        workingChoice = "Mend";
        if (restChoiceSelectObject != null){restChoiceSelectObject.SetActive(false);}
        if (mendTargetSelectObject != null){mendTargetSelectObject.SetActive(true);}
        UpdateMendTargetOptions();
        UpdateActorChoiceText();
    }

    protected void UpdateMendTargetOptions()
    {
        if (mendTargetSelect == null){return;}
        List<string> mendTargets = new List<string>();
        List<string> mendSprites = new List<string>();
        List<string> mendStats = new List<string>();
        currentMendTargetIDs.Clear();
        for (int i = 0; i < actorSelectList.allActorNames.Count; i++)
        {
            if (i == selectedActorIndex){continue;}
            mendTargets.Add(actorSelectList.allActorNames[i]);
            mendSprites.Add(partyData.ReturnActorAtIndex(i).GetSpriteName());
            mendStats.Add(partyData.ReturnPartyMemberStatsAtIndex(i));
            currentMendTargetIDs.Add(partyData.ReturnIDAtIndex(i));
        }
        mendTargetSelect.SetData(mendSprites, mendTargets, mendStats);
    }

    public void SelectMendTarget()
    {
        if (mendTargetSelect == null || mendTargetSelect.GetSelected() < 0){return;}
        int selectedMendIndex = mendTargetSelect.GetSelected();
        if (selectedMendIndex >= currentMendTargetIDs.Count){return;}
        workingChoice = "Mend";
        workingSpecifics = currentMendTargetIDs[selectedMendIndex].ToString();
        UpdateActorChoiceText();
    }

    // --- Train Choice ---
    protected void BeginTrain()
    {
        currentMenuState = RestMenuState.TrainSelect;
        workingChoice = "Train";
        if (restChoiceSelectObject != null){restChoiceSelectObject.SetActive(false);}
        if (trainChoiceObject != null){trainChoiceObject.SetActive(true);}
        OpenTrainMenu();
        UpdateActorChoiceText();
    }

    protected void OpenTrainMenu()
    {
        if (skillTraining == null){return;}
        TacticActor selectedActor = GetSelectedPartyActor();
        if (selectedActor == null){return;}
        skillTraining.SetActor(selectedActor);
        string skillName;
        string upgradeType;
        if (TryParseTrainSpecifics(workingSpecifics, out skillName, out upgradeType))
        {
            skillTraining.LoadTraining(skillName, upgradeType);
        }
    }

    public void UpdateWorkingTrainChoice()
    {
        if (skillTraining == null){return;}
        if (!skillTraining.CanConfirmTraining()){return;}
        workingChoice = "Train";
        workingSpecifics = skillTraining.GetTrainingSpecifics();
        UpdateActorChoiceText();
    }

    protected string ReturnTrainChoiceDetailText(string specifics, int actorIndex)
    {
        if (actorIndex < 0){return "";}
        string skillName;
        string upgradeType;
        if (!TryParseTrainSpecifics(specifics, out skillName, out upgradeType))
        {
            return "";
        }
        TacticActor actor = partyData.ReturnActorAtIndex(actorIndex);
        string activeDetails = activeData.ReturnValue(skillName);
        if (activeDetails.Length <= 0){return skillName + " - " + upgradeType;}
        string currentText = ReturnActiveDetailText(activeDetails, actor);
        string upgradedText = ReturnUpgradedActiveDetailText(activeDetails, actor, skillName, upgradeType);
        return skillName + "\nCurrent:\n" + currentText + "\n\nAfter " + upgradeType + ":\n" + upgradedText;
    }

    protected string ReturnActiveDetailText(string activeDetails, TacticActor actor)
    {
        previewActive.LoadSkillFromString(activeDetails, actor);
        return skillTraining.activeViewer.ReturnActiveDescription(previewActive);
    }

    protected string ReturnUpgradedActiveDetailText(string activeDetails, TacticActor actor, string skillName, string upgradeType)
    {
        List<string> originalMods = new List<string>(actor.GetActiveMods());
        actor.AddActiveMod(skillName, upgradeType, GetChoiceDelimiter());
        previewActive.LoadSkillFromString(activeDetails, actor);
        actor.SetActiveMods(originalMods);
        return skillTraining.activeViewer.ReturnActiveDescription(previewActive);
    }

    // --- Train Specifics Parsing ---
    protected string GetChoiceDelimiter()
    {
        if (previewActive == null){return "_";}
        return previewActive.activeSkillDelimiter;
    }

    protected string BuildTrainSpecifics(string skillName, string upgradeType)
    {
        return skillName + GetChoiceDelimiter() + upgradeType;
    }

    protected bool TryParseTrainSpecifics(string specifics, out string skillName, out string upgradeType)
    {
        string[] blocks = specifics.Split(new string[] { GetChoiceDelimiter() }, System.StringSplitOptions.None);
        if (blocks.Length < 2)
        {
            skillName = "";
            upgradeType = "";
            return false;
        }
        skillName = blocks[0];
        upgradeType = blocks[1];
        return true;
    }

    // --- Actor Lookup ---
    protected string ReturnActorNameFromID(int actorID)
    {
        for (int i = 0; i < partyData.ReturnTotalPartyCount(); i++)
        {
            if (partyData.ReturnIDAtIndex(i) == actorID)
            {
                return partyData.GetAllPartyNames()[i];
            }
        }
        return actorID.ToString();
    }

    protected int ReturnActorIndexFromID(int actorID)
    {
        for (int i = 0; i < partyData.ReturnTotalPartyCount(); i++)
        {
            if (partyData.ReturnIDAtIndex(i) == actorID)
            {
                return i;
            }
        }
        return -1;
    }

    // --- Rest Resolution ---
    protected void RestoreActor(TacticActor actor)
    {
        int hpRestore = Mathf.Max(1, (actor.GetBaseHealth() * 30) / 100);
        int mpRestore = Mathf.Max(1, (actor.GetMaxMana() * 40) / 100);
        actor.UpdateHealth(hpRestore, false);
        actor.SetMana(Mathf.Min(actor.GetMaxMana(), actor.GetMana() + mpRestore));
        actor.ClearStatuses();
    }

    protected void ApplyTraining(TacticActor actor, string specifics)
    {
        string skillName;
        string upgradeType;
        if (!TryParseTrainSpecifics(specifics, out skillName, out upgradeType))
        {
            return;
        }
        actor.AddActiveMod(skillName, upgradeType, GetChoiceDelimiter());
    }

    protected void ResolveRestChoices()
    {
        List<string> resolvedActorStats = new List<string>();
        for (int i = 0; i < partyData.ReturnTotalPartyCount(); i++)
        {
            resolvedActorStats.Add(partyData.ReturnPartyMemberStatsAtIndex(i));
        }
        // Apply direct rests first.
        for (int i = 0; i < restChoices.Count; i++)
        {
            if (restChoices[i] != "Rest"){continue;}
            dummyActor.SetInitialStatsFromString(resolvedActorStats[i]);
            RestoreActor(dummyActor);
            resolvedActorStats[i] = dummyActor.GetInitialStats();
        }
        // Then mend targets.
        for (int i = 0; i < restChoices.Count; i++)
        {
            if (restChoices[i] != "Mend"){continue;}
            int targetIndex = ReturnActorIndexFromID(utility.SafeParseInt(restChoiceSpecifics[i], -1));
            if (targetIndex < 0){continue;}
            dummyActor.SetInitialStatsFromString(resolvedActorStats[targetIndex]);
            RestoreActor(dummyActor);
            resolvedActorStats[targetIndex] = dummyActor.GetInitialStats();
        }
        // Finally apply skill training.
        for (int i = 0; i < restChoices.Count; i++)
        {
            if (restChoices[i] != "Train"){continue;}
            dummyActor.SetInitialStatsFromString(resolvedActorStats[i]);
            ApplyTraining(dummyActor, restChoiceSpecifics[i]);
            resolvedActorStats[i] = dummyActor.GetInitialStats();
        }
        for (int i = 0; i < resolvedActorStats.Count; i++)
        {
            dummyActor.SetInitialStatsFromString(resolvedActorStats[i]);
            partyData.UpdatePartyMember(dummyActor, i);
        }
    }

    // --- Testing ---
    // Test data for validating staged Rest / Mend / Train flows in this scene.
    public string testActorOne = "Test Hero!Humanoid!!!99!12!1!6!2!Walking!1!10!6!0!0!200!100!0!Dash!1!!Double Slash,Gravity,Pain Split,Fire Attack,Cleave!Cleave_Energy,Cleave_Actions,Cleave_Power!!2!0!0!10!!!!2!4!40!";
    public string testActorTwo = "Test Hero!Humanoid!!!80!10!1!8!2!Walking!1!8!5!0!0!200!100!0!!!!Bloodletting,Attack Blessing,Mass Attack Order!Attack Blessing_Power,Mass Attack Order_Actions!!2!0!0!8!!!!2!2!15!";
    public string testActorThree = "Test Mage!Humanoid!!!70!8!2!5!2!Walking!1!12!7!0!0!200!100!0!Dash!1!!Double Slash,Gravity!!2!0!0!12!!!!2!1!9!";
}
