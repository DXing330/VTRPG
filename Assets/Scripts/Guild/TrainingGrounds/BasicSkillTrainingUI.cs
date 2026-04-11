using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

// Used to apply basic trainings to actor skills.
// Can train energy/power/actions once each for each skill.
public class BasicSkillTrainingUI : MonoBehaviour
{
    public PartyDataManager partyData;
    public TacticActor selectedActor;
    public int selectedActorIndex = -1;
    public StatDatabase activeData;
    public ActiveSkill selectedActive;
    public ActiveSkill previewActive;
    public ActiveDescriptionViewer activeViewer;
    public PassiveDetailViewer passiveViewer;
    public SelectList selectSkillList;
    public List<string> basicUpgrades;
    public GameObject selectUpgradeObject;
    public SelectList selectUpgradeList;
    public string selectedSkillName;
    public string selectedUpgrade;
    public PopUpMessage currentActiveStats;
    public PopUpMessage trainedActiveStats;

    protected virtual void Awake()
    {
        if (basicUpgrades == null)
        {
            basicUpgrades = new List<string>();
        }
        if (basicUpgrades.Count <= 0)
        {
            basicUpgrades.Add("Energy");
            basicUpgrades.Add("Actions");
            basicUpgrades.Add("Power");
        }
    }

    protected virtual void Start()
    {
        InitializeUI();
    }

    public void InitializeUI()
    {
        ResetSelection();
        if (selectedActor != null)
        {
            UpdateTrainableSkills();
        }
        else if (selectSkillList != null)
        {
            selectSkillList.SetSelectables(new List<string>());
        }
    }

    protected void SetActor(TacticActor actor = null)
    {
        selectedActor = actor;
        ResetSelection();
        UpdateTrainableSkills();
    }

    [ContextMenu("Test Set Actor From Party")]
    public void TestSetActor()
    {
        SetActorFromPartyIndex(0);
    }

    // Should be called to activate this.
    public void SetActorFromPartyIndex(int actorIndex)
    {
        selectedActorIndex = actorIndex;
        if (partyData == null || actorIndex < 0)
        {
            SetActor(null);
            return;
        }
        SetActor(partyData.ReturnActorAtIndex(actorIndex));
        selectedActorIndex = actorIndex;
    }

    public void ResetSelection()
    {
        selectedSkillName = "";
        selectedUpgrade = "";
        if (selectSkillList != null)
        {
            selectSkillList.ResetSelected();
        }
        if (selectUpgradeList != null)
        {
            selectUpgradeList.ResetSelected();
            selectUpgradeList.SetSelectables(new List<string>());
        }
        SetUpgradeSelectionVisible(false);
        UpdateSkillTexts();
    }

    public List<string> GetTrainableSkills()
    {
        List<string> skills = new List<string>();
        if (selectedActor == null)
        {
            return skills;
        }
        List<string> actorSkills = GetAllActorTrainableActives();
        for (int i = 0; i < actorSkills.Count; i++)
        {
            if (actorSkills[i].Length <= 0)
            {
                continue;
            }
            if (activeData == null || activeData.ReturnValue(actorSkills[i]).Length <= 0)
            {
                continue;
            }
            if (GetAvailableUpgrades(actorSkills[i]).Count <= 0)
            {
                continue;
            }
            skills.Add(actorSkills[i]);
        }
        return skills;
    }

    protected List<string> GetAllActorTrainableActives()
    {
        List<string> allActives = new List<string>(selectedActor.GetActiveSkills());
        List<string> allPassives = passiveViewer.ReturnAllPassiveInfo(selectedActor.GetPassiveSkills(), selectedActor.GetPassiveLevels());
        for (int i = 0; i < allPassives.Count; i++)
        {
            string[] blocks = allPassives[i].Split("|");
            if (blocks.Length < 6){continue;}
            if (!blocks[4].Contains("Skill")){continue;}
            allActives.Add(blocks[5]);
        }
        return allActives.Distinct().ToList();
    }

    public void UpdateTrainableSkills()
    {
        if (selectSkillList == null)
        {
            return;
        }
        selectSkillList.SetSelectables(GetTrainableSkills());
        SetUpgradeSelectionVisible(false);
    }

    public void SelectSkillFromList()
    {
        if (selectSkillList == null || selectSkillList.GetSelected() < 0)
        {
            return;
        }
        SetSkill(selectSkillList.GetSelectedString());
    }

    public void SetSkill(string skillName)
    {
        selectedSkillName = skillName;
        selectedUpgrade = "";
        List<string> availableUpgrades = GetAvailableUpgrades(skillName);
        if (selectUpgradeList != null)
        {
            selectUpgradeList.ResetSelected();
            selectUpgradeList.SetSelectables(availableUpgrades);
        }
        SetUpgradeSelectionVisible(availableUpgrades.Count > 0);
        if (selectedActive != null)
        {
            string activeDetails = ReturnActiveDetails(skillName);
            selectedActive.LoadSkillFromString(activeDetails, selectedActor);
        }
        UpdateSkillTexts();
    }

    public List<string> GetAvailableUpgrades(string skillName)
    {
        List<string> validUpgrades = new List<string>();
        if (selectedActor == null || skillName.Length <= 0)
        {
            return validUpgrades;
        }
        string activeDetails = ReturnActiveDetails(skillName);
        if (activeDetails.Length <= 0)
        {
            return validUpgrades;
        }
        selectedActive.LoadSkillFromString(activeDetails, selectedActor);
        for (int i = 0; i < basicUpgrades.Count; i++)
        {
            if (CanUpgradeSkill(skillName, basicUpgrades[i]))
            {
                validUpgrades.Add(basicUpgrades[i]);
            }
        }
        return validUpgrades;
    }

    protected bool CanUpgradeSkill(string skillName, string upgrade)
    {
        if (selectedActor == null || skillName.Length <= 0 || upgrade.Length <= 0)
        {
            return false;
        }
        string delimiter = selectedActive.activeSkillDelimiter;
        if (selectedActor.ActiveHasModType(skillName, upgrade, delimiter))
        {
            return false;
        }
        switch (upgrade)
        {
            case "Energy":
                return selectedActive.GetEnergyCost() > 0;
            case "Actions":
                return selectedActive.GetActionCost() > 0;
            case "Power":
                return selectedActive.CanTrainPower();
        }
        return false;
    }

    public void SelectUpgradeFromList()
    {
        if (selectUpgradeList == null || selectUpgradeList.GetSelected() < 0)
        {
            return;
        }
        SelectUpgrade(selectUpgradeList.GetSelectedString());
    }

    // Power/Energy/Action
    public void SelectUpgrade(string upgrade)
    {
        if (selectedSkillName.Length <= 0)
        {
            return;
        }
        if (!CanUpgradeSkill(selectedSkillName, upgrade))
        {
            selectedUpgrade = "";
            UpdateSkillTexts();
            return;
        }
        selectedUpgrade = upgrade;
        UpdateSkillTexts();
    }

    public string GetTrainingSpecifics()
    {
        if (selectedSkillName.Length <= 0 || selectedUpgrade.Length <= 0)
        {
            return "";
        }
        return selectedSkillName + selectedActive.activeSkillDelimiter + selectedUpgrade;
    }

    public void LoadTraining(string skillName, string upgrade)
    {
        SetSkill(skillName);
        SelectUpgrade(upgrade);
    }

    public bool CanConfirmTraining()
    {
        return selectedActor != null && selectedSkillName.Length > 0 && selectedUpgrade.Length > 0;
    }

    public void ConfirmTraining()
    {
        if (!CanConfirmTraining())
        {
            return;
        }
        string delimiter = selectedActive != null ? selectedActive.activeSkillDelimiter : "_";
        if (partyData != null && selectedActorIndex >= 0)
        {
            selectedActor = partyData.ReturnActorAtIndex(selectedActorIndex);
        }
        selectedActor.AddActiveMod(selectedSkillName, selectedUpgrade, delimiter);
        if (partyData != null && selectedActorIndex >= 0)
        {
            partyData.UpdatePartyMember(selectedActor, selectedActorIndex);
            partyData.Save();
            selectedActor = partyData.ReturnActorAtIndex(selectedActorIndex);
        }
        SetSkill(selectedSkillName);
        UpdateTrainableSkills();
    }

    protected string ReturnActiveDetails(string skillName)
    {
        if (activeData == null || skillName.Length <= 0)
        {
            return "";
        }
        return activeData.ReturnValue(skillName);
    }

    protected void UpdateSkillTexts()
    {
        if (currentActiveStats != null)
        {
            string currentText = ReturnCurrentSkillText();
            if (currentText.Length > 0)
            {
                currentActiveStats.SetMessage(currentText);
            }
            else
            {
                currentActiveStats.ResetMessage();
            }
        }
        if (trainedActiveStats != null)
        {
            string previewText = ReturnPreviewSkillText();
            if (previewText.Length > 0)
            {
                trainedActiveStats.SetMessage(previewText);
            }
            else
            {
                trainedActiveStats.ResetMessage();
            }
        }
    }

    protected string ReturnCurrentSkillText()
    {
        if (selectedActor == null)
        {
            return "";
        }
        if (selectedSkillName.Length <= 0)
        {
            return "";
        }
        string activeDetails = ReturnActiveDetails(selectedSkillName);
        if (activeDetails.Length <= 0)
        {
            return selectedSkillName;
        }
        selectedActive.LoadSkillFromString(activeDetails, selectedActor);
        return ReturnSkillText(selectedActive);
    }

    protected string ReturnPreviewSkillText()
    {
        if (selectedSkillName.Length <= 0)
        {
            return "";
        }
        if (selectedUpgrade.Length <= 0)
        {
            return "";
        }
        string activeDetails = ReturnActiveDetails(selectedSkillName);
        if (activeDetails.Length <= 0)
        {
            return "";
        }
        List<string> originalMods = new List<string>(selectedActor.GetActiveMods());
        string delimiter = previewActive.activeSkillDelimiter;
        selectedActor.AddActiveMod(selectedSkillName, selectedUpgrade, delimiter);
        previewActive.LoadSkillFromString(activeDetails, selectedActor);
        selectedActor.SetActiveMods(originalMods);
        return ReturnSkillText(previewActive);
    }

    protected void SetUpgradeSelectionVisible(bool visible)
    {
        if (selectUpgradeObject != null)
        {
            selectUpgradeObject.SetActive(visible);
        }
    }

    protected string ReturnSkillText(ActiveSkill activeSkill)
    {
        if (activeSkill == null)
        {
            return "";
        }
        if (activeViewer != null)
        {
            return activeSkill.GetSkillName() + "\n" + activeViewer.ReturnActiveDescription(activeSkill);
        }
        string text = activeSkill.GetSkillName();
        text += "\nEffect: " + activeSkill.GetEffect();
        if (activeSkill.GetSpecifics().Length > 0)
        {
            text += "\nSpecifics: " + activeSkill.GetSpecifics();
        }
        text += "\nPower: " + activeSkill.GetPower();
        text += "\nAction Cost: " + activeSkill.GetActionCost();
        text += "\nEnergy Cost: " + activeSkill.GetEnergyCost();
        text += "\nRange: " + activeSkill.GetRangeShape() + "-" + activeSkill.GetRangeString();
        text += "\nSpan: " + activeSkill.GetShape() + "-" + activeSkill.GetSpan();
        return text;
    }
}
