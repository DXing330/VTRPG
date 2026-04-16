using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        ResetConfirmPanel();
        activeViewerPanel.ResetMessage();
    }
    protected void ResetConfirmPanel()
    {
        skillTypeText.text = "";
        skillNameText.text = "";
        skillEffectText.text = "";
        confirmObject.SetActive(false);
        confirmButtonObject.SetActive(false);
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
            int passiveLevelToShow = 1;
            if (selectedActor != null)
            {
                passiveLevelToShow = selectedActor.GetLevelFromPassive(skillName) + 1;
            }
            string passiveLevelName = passiveDetailViewer.passiveNameLevels.GetMultiKeyValue(skillName, passiveLevelToShow.ToString());
            string passiveDetails = passiveDetailViewer.ReturnPassiveDetailsFromName(passiveLevelName);
            skillBookEffectText.text = passiveDetails;
            if (passiveDetailViewer.SpellGrantingPassive(passiveLevelName))
            {
                skillBookEffectText.text += "\n" + activeDetailViewer.ReturnSpellDescriptionFromName(passiveLevelName);
            }
            else if (passiveDetailViewer.SkillGrantingPassive(passiveLevelName))
            {
                skillBookEffectText.text += "\n" + activeDetailViewer.ReturnActiveDescriptionFromName(passiveLevelName);
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
        UpdateSkillBookEffectText();
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
    public GameObject confirmButtonObject;
    public TMP_Text skillTypeText;
    public TMP_Text skillNameText;
    public TMP_Text skillEffectText;
    protected void UpdateConfirmPanel()
    {
        ResetConfirmPanel();
        if (selectedActor == null || skillName == "" || skillType == "")
        {
            return;
        }
        skillTypeText.text = skillType;
        skillNameText.text = skillName;
        confirmObject.SetActive(true);
        bool canUseBook = CanUseSelectedBookOnActor();
        switch (skillType)
        {
            case "Passive":
            if (!canUseBook)
            {
                skillEffectText.text = skillName + " is already at max level.";
                break;
            }
            skillEffectText.text = skillName + " level: " + selectedActor.GetLevelFromPassive(skillName) + " > " + (selectedActor.GetLevelFromPassive(skillName) + 1);
            break;
            case "Skill":
            if (!canUseBook)
            {
                skillEffectText.text = skillName + " is already learned.";
                break;
            }
            skillEffectText.text = "Learn the " + skillName + " skill.";
            break;
            case "Spell":
            if (!canUseBook)
            {
                skillEffectText.text = skillName + " is already learned.";
                break;
            }
            skillEffectText.text = "Learn the " + skillName + " spell.";
            break;
        }
        confirmButtonObject.SetActive(canUseBook);
    }

    protected bool CanUseSelectedBookOnActor()
    {
        if (selectedActor == null){return false;}
        switch (skillType)
        {
            case "Passive":
            return selectedActor.GetLevelFromPassive(skillName) < passiveDetailViewer.GetMaxLevelFromPassiveName(skillName);
            case "Skill":
            return !selectedActor.GetActiveSkills().Contains(skillName);
            case "Spell":
            return !selectedActor.GetSpells().Contains(skillName);
        }
        return false;
    }
    public void ConfirmUse()
    {
        if (!CanUseSelectedBookOnActor()){return;}
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

    // --- Testing ---
    // Test setup for validating skillbook use rules. Does not save.
    public string testActorOne;
    public string testActorTwo;
    public string testActorThree;
    public string testBooks = "Dash|Dash|Apparition|Apparition|Attack Up|Attack Up|Attack Up|Attack Up|Attack Up|Attack Up|Attack Up|Bhavati-Vishnu|Bhavati-Kali|Bhavati-Shiva|Bottomless Trap Hole|Power Word Kill";

    [ContextMenu("Load Use SkillBook Test Data")]
    public void LoadUseSkillBookTestData()
    {
        if (partyData.ReturnTotalPartyCount() < 3)
        {
            Debug.Log("Need at least 3 party members to load the skillbook test data.");
            return;
        }
        testActorOne = BuildSkillBookTestActorOne();
        testActorTwo = BuildSkillBookTestActorTwo();
        testActorThree = BuildSkillBookTestActorThree();
        LoadTestActorAtIndex(0, testActorOne);
        LoadTestActorAtIndex(1, testActorTwo);
        LoadTestActorAtIndex(2, testActorThree);
        partyData.spellBook.SetBooks(testBooks.Split('|').ToList());
        actorSelect.SetSelectables(partyData.GetAllPartyNames());
        InitialSetUp();
    }

    protected void LoadTestActorAtIndex(int index, string actorStats)
    {
        TacticActor actor = partyData.ReturnActorAtIndex(index);
        actor.SetInitialStatsFromString(actorStats);
        partyData.UpdatePartyMember(actor, index);
    }

    protected string BuildSkillBookTestActorOne()
    {
        string[] fields = new string[35];
        fields[0] = "Test Assassin";
        fields[1] = "Humanoid";
        fields[4] = "60";
        fields[5] = "12";
        fields[6] = "1";
        fields[7] = "3";
        fields[8] = "2";
        fields[9] = "Walking";
        fields[10] = "1";
        fields[11] = "12";
        fields[12] = "6";
        fields[13] = "0";
        fields[14] = "1";
        fields[15] = "200";
        fields[16] = "99";
        fields[17] = "1";
        fields[18] = "Assassin";
        fields[19] = "6";
        fields[21] = "Dash";
        fields[24] = "2";
        fields[25] = "0";
        fields[26] = "0";
        fields[27] = "10";
        fields[31] = "2";
        fields[32] = "10";
        fields[33] = "60";
        return string.Join("!", fields);
    }

    protected string BuildSkillBookTestActorTwo()
    {
        string[] fields = new string[35];
        fields[0] = "Test Mage";
        fields[1] = "Humanoid";
        fields[4] = "70";
        fields[5] = "8";
        fields[6] = "2";
        fields[7] = "5";
        fields[8] = "2";
        fields[9] = "Walking";
        fields[10] = "1";
        fields[11] = "12";
        fields[12] = "7";
        fields[13] = "0";
        fields[14] = "0";
        fields[15] = "200";
        fields[16] = "100";
        fields[17] = "0";
        fields[18] = "Dash";
        fields[19] = "1";
        fields[21] = "Double Slash,Gravity";
        fields[24] = "2";
        fields[25] = "0";
        fields[26] = "0";
        fields[27] = "12";
        fields[31] = "2";
        fields[32] = "12";
        fields[33] = "70";
        return string.Join("!", fields);
    }

    protected string BuildSkillBookTestActorThree()
    {
        string[] fields = new string[35];
        fields[0] = "Test Scholar";
        fields[1] = "Humanoid";
        fields[4] = "55";
        fields[5] = "6";
        fields[6] = "1";
        fields[7] = "4";
        fields[8] = "2";
        fields[9] = "Walking";
        fields[10] = "1";
        fields[11] = "10";
        fields[12] = "6";
        fields[13] = "0";
        fields[14] = "0";
        fields[15] = "200";
        fields[16] = "100";
        fields[17] = "0";
        fields[18] = "Bhavati-Vishnu+Bhavati-Kali";
        fields[19] = "1+1";
        fields[21] = "Double Slash";
        fields[24] = "2";
        fields[25] = "0";
        fields[26] = "0";
        fields[27] = "10";
        fields[31] = "2";
        fields[32] = "10";
        fields[33] = "55";
        return string.Join("!", fields);
    }
}
