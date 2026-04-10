using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Load Skillbooks/Party From Party Data > Select Skillbook (Shows Skillbook Details, Allows Selecting Actor To Teach) > Select Actor (Allows Viewing Actor Details, Allows Confirming) > Confirm > Refresh > Restart
public class UseSkillBookUI : MonoBehaviour
{
    // Actual Game Stuff.
    public PartyDataManager partyData;
    public StatDatabase skillBookData;
    // UI Stuff.
    void Start()
    {
        actorSelect.SetSelectables(partyData.GetAllPartyNames());
        InitialSetUp();
    }
    public void InitialSetUp()
    {
        selectedData = "";
        skillName = "";
        skillType = "";
        skillBookSelect.SetSelectables(partyData.spellBook.GetBooks());
        selectedBookEffectObject.SetActive(false);
        actorSelectObject.SetActive(false);
        actorSkillSelectObject.SetActive(false);
        confirmObject.SetActive(false);
        activeViewerPanel.ResetMessage();
    }
    public SelectList skillBookSelect;
    public GameObject selectedBookEffectObject;
    public TMP_Text skillBookEffectText;
    protected void UpdateSkillBookEffectText()
    {
        if (selectedData == "")
        {
            skillBookEffectText.text = "";
            return;
        }
        switch (skillType)
        {
            default:
            skillBookEffectText.text = "";
            return;
            case "Passive":
            skillBookEffectText.text = passiveDetailViewer.ReturnPassiveDetailsFromName(skillName);
            // If the passive grants a skill/spell then show that skill/spell effect as well.
            if (passiveDetailViewer.SpellGrantingPassive(skillName))
            {
                skillBookEffectText.text += "\n" + activeDetailViewer.ReturnSpellDescriptionFromName(skillName);
                return;
            }
            else if (passiveDetailViewer.SkillGrantingPassive(skillName))
            {
                skillBookEffectText.text += "\n" + activeDetailViewer.ReturnActiveDescriptionFromName(skillName);
                return;
            }
            return;
            case "Skill":
            skillBookEffectText.text = activeDetailViewer.ReturnActiveDescriptionFromName(skillName);
            return;
            case "Spell":
            skillBookEffectText.text = activeDetailViewer.ReturnSpellDescriptionFromName(skillName);
            return;
        }
    }
    public string selectedData;
    public string skillName;
    // Skill / Passive / Spell
    public string skillType;
    public void SelectSkillBook()
    {
        if (skillBookSelect.GetSelected() < 0)
        {
            InitialSetUp();
            return;
        }
        string skillBookName = skillBookSelect.GetSelectedString();
        selectedData = skillBookData.ReturnValue(skillBookName);
        if (selectedData == "")
        {
            InitialSetUp();
            return;
        }
        string[] dataBlocks = selectedData.Split("_");
        skillName = dataBlocks[1];
        skillType = dataBlocks[0];
        actorSelectObject.SetActive(true);
        selectedBookEffectObject.SetActive(true);
        UpdateSkillBookEffectText();
    }
    public GameObject actorSelectObject;
    public SelectList actorSelect;
    public TacticActor selectedActor;
    public void SelectActor()
    {
        if (actorSelect.GetSelected() < 0)
        {
            InitialSetUp();
            return;
        }
        selectedActor = partyData.ReturnActorAtIndex(actorSelect.GetSelected());
        actorSkillSelectObject.SetActive(true);
        confirmObject.SetActive(true);
        UpdateConfirmPanel();
        List<string> allPassives = passiveDetailViewer.ReturnAllPassiveInfo(selectedActor.GetPassiveSkills(), selectedActor.GetPassiveLevels());
        switch (skillType)
        {
            case "Passive":
            actorSkillSelect.SetSelectables(selectedActor.GetPassiveSkillsAndLevels());
            break;
            case "Skill":
            List<string> actorSkills = selectedActor.GetActiveSkills();
            // Check if any passives also grant skills.
            for (int i = 0; i < allPassives.Count; i++)
            {
                string[] aBlocks = allPassives[i].Split("|");
                if (aBlocks.Length < 4){break;}
                if (aBlocks[4].Contains("Skill"))
                {
                    actorSkills.Add(aBlocks[5]);
                }
            }
            actorSkillSelect.SetSelectables(actorSkills);
            break;
            case "Spell":
            List<string> actorSpells = selectedActor.GetSpells();
            // Check if any passives also grant spells.
            for (int i = 0; i < allPassives.Count; i++)
            {
                string[] sBlocks = allPassives[i].Split("|");
                if (sBlocks.Length < 4){break;}
                if (sBlocks[4].Contains("Spell"))
                {
                    actorSpells.Add(sBlocks[5]);
                }
            }
            actorSkillSelect.SetSelectables(actorSpells);
            break;
        }
    }
    public GameObject actorSkillSelectObject;
    public SelectList actorSkillSelect;
    public PopUpMessage activeViewerPanel;
    public void SelectActorSkill()
    {
        if (actorSkillSelect.GetSelected() < 0)
        {
            return;
        }
        string selectedSkillName = actorSkillSelect.GetSelectedString();
        switch (skillType)
        {
            case "Passive":
            passiveViewerObject.SetActive(true);
            string[] passiveAndLevel = selectedSkillName.Split(" - ");
            passiveDetailViewer.UpdatePassiveNames(passiveAndLevel[0], passiveAndLevel[1]);
            break;
            case "Skill":
            activeViewerPanel.SetMessage(activeDetailViewer.ReturnActiveDescriptionFromName(selectedSkillName));
            break;
            case "Spell":
            activeViewerPanel.SetMessage(activeDetailViewer.ReturnSpellDescriptionFromName(selectedSkillName));
            break;
        }
    }
    public GameObject confirmObject;
    public TMP_Text skillTypeText;
    public TMP_Text skillNameText;
    public TMP_Text skillEffectText;
    protected void UpdateConfirmPanel()
    {
        skillTypeText.text = skillType;
        skillNameText.text = skillName;
        switch (skillType)
        {
            case "Passive":
            skillEffectText.text = skillName + " level: " + selectedActor.GetLevelFromPassive(skillName) + " > " + (selectedActor.GetLevelFromPassive(skillName) + 1);
            break;
            case "Skill":
            skillEffectText.text = "Learn the " + skillName + " skill.";
            break;
            case "Spell":
            skillEffectText.text = "Learn the " + skillName + " spell.";
            break;
        }
    }
    public void ConfirmUse()
    {
        // Teach the actor.
        switch (skillType)
        {
            case "Passive":
            selectedActor.AddPassiveSkill(skillName);
            partyData.UpdatePartyMember(selectedActor, actorSelect.GetSelected());
            break;
            case "Skill":
            selectedActor.AddActiveSkill(skillName);
            partyData.UpdatePartyMember(selectedActor, actorSelect.GetSelected());
            break;
            case "Spell":
            partyData.AddSpellToPartyMember(skillName, actorSelect.GetSelected());
            break;
        }
        // Remove the skillbook from the party.
        partyData.spellBook.LoseBookAtIndex(skillBookSelect.GetSelected());
        InitialSetUp();
    }
    public SpellDetailViewer activeDetailViewer;
    public PassiveDetailViewer passiveDetailViewer;
    public GameObject passiveViewerObject;
}
