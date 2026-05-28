using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Controls The UI for Selecting Skill Mods From Events/Relics
public class SkillModSelectMenu : MonoBehaviour
{
    // TESTING
    public string testMod;
    public int testModSelectCount;
    public PartyDataManager testParty;
    [ContextMenu("Initialize Test")]
    public void InitializeTestMenu()
    {
        testParty.NewGame();
        testParty.Load();
        InitializeMenu(testParty, testMod, testModSelectCount);
    }
    [ContextMenu("Reset Menu")]
    public void ResetTestMenu()
    {
        // Reset The MultiList.
        multiList.ResetSelectables();
        // Reset The Text.
        for (int i = 0; i < allText.Count; i++)
        {
            allText[i].text = "";
        }
    }
    // GAME LOGIC
    public PartyDataManager partyData;
    public Equipment dummyEquip;
    public SelectMultiList multiList;
    public ActiveDescriptionViewer activeDetailViewer;
    public PassiveDetailViewer passiveDetailViewer;
    public string mod;
    public List<string> partySkillNames;
    public List<int> partySkillIDs;
    // UI
    public GameObject thisObject;
    public List<TMP_Text> allText;
    public TMP_Text actorNameText;
    public TMP_Text modNameText;
    public TMP_Text modDetailText;
    public TMP_Text currentSkillText;
    public TMP_Text moddedSkillText;
    public void InitializeMenu(PartyDataManager newParty, string newMod, int newCount = 1)
    {
        thisObject.SetActive(true);
        partyData = newParty;
        SetMod(newMod);
        // Set the amount of skills that can be selected.
        multiList.SetMaxSelections(newCount);
        multiList.ResetSelected();
        partySkillNames.Clear();
        // Get All IDs From Party As Well?
        partySkillIDs.Clear();
        int partyCount = partyData.ReturnTotalPartyCount();
        for (int i = 0; i < partyCount; i++)
        {
            int partyID = partyData.ReturnIDAtIndex(i);
            // Get all skills from the party (including skills granted by passives / equipment);
            List<string> actorSkills = GetAllActorActives(partyData, partyID, dummyEquip);
            for (int j = 0; j < actorSkills.Count; j++)
            {
                if (actorSkills[j].Length < 1){continue;}
                partySkillNames.Add(actorSkills[j]);
                partySkillIDs.Add(partyID);
            }
        }
        // Set the multiselect list based on all skills from the party.
        multiList.SetSelectables(partySkillNames);
    }
    public void SelectSkill()
    {
        // Get The Index From The MultiSelectList
        int selectedIndex = multiList.GetRecentlySelected();
        // If nothing is selected or something was just deselected, then reset the display.
        if (selectedIndex < 0 || !multiList.SelectedRecently())
        {
            ResetDisplay();
            return;
        }
        // Get The Actor And Skill From The Party, Based On Skill Index
        string skillName = partySkillNames[selectedIndex];
        int partyID = partySkillIDs[selectedIndex];
        UpdateDisplay(skillName, partyData.ReturnActorFromID(partyID));
    }
    public void ConfirmChoices()
    {
        // Copy Is Special.
        if (mod == "Copy")
        {
            // TODO Add A Copy Of The Selected Skills To The Party Books.
            return;
        }
        // RandomBasic Is Special.
        if (mod == "RandomBasic")
        {
            // TODO For Each Selected Skill, Assign An Appropriate Random Mod If Possible.
            return;
        }
        List<int> selectedSkillIndices = multiList.GetAllSelected();
        for (int i = 0; i < selectedSkillIndices.Count; i++)
        {
            // Get The Skill Name.
            string skillName = partySkillNames[selectedSkillIndices[i]];
            // Determine The Skill Mod String.
            string modDetails = skillName + "_" + mod;
            // Get The Party Member.
            int partyID = partySkillIDs[selectedSkillIndices[i]];
            // Apply The Skill Mod To The Party Member.
            partyData.ApplyEffectToPartyID("SkillMod", modDetails, "1", partyID);
        }
    }
    public void SetMod(string newMod)
    {
        mod = newMod;
        modNameText.text = newMod;
        modDetailText.text = activeDetailViewer.GetSkillModDescription(newMod);
    }
    protected void ResetDisplay()
    {
        actorNameText.text = "";
        currentSkillText.text = "";
        moddedSkillText.text = "";
    }
    // Update the UI.
    protected void UpdateDisplay(string skillName, TacticActor skillUser)
    {
        if (skillName == "" || skillUser == null || !multiList.SelectedRecently())
        {
            ResetDisplay();
            return;
        }
        actorNameText.text = skillUser.GetPersonalName();
        // Show the current skill with all actor mods.
        currentSkillText.text = activeDetailViewer.ReturnActiveDescriptionFromName(skillName, skillUser);
        // Show the potential future skill with the new mod.
        moddedSkillText.text = activeDetailViewer.ReturnActiveDescriptionFromNameWithMod(skillName, skillUser, mod);
    }
    // Get ALL Actives Including Those Granted By Equipment
    public List<string> GetAllActorActives(PartyDataManager partyData, int actorID, Equipment dummyEquipment)
    {
        int index = partyData.ReturnIndexAtID(actorID);
        if (index < 0){return new List<string>();}
        TacticActor actor = partyData.ReturnActorAtIndex(index);
        List<string> allActives = actor.GetActiveSkills();
        // Get The Actors Equipment From The PartyDataManager.
        string currentEquipment = partyData.ReturnPartyMemberEquipFromIndex(index);
        (List<string> passiveNames, List<string> passiveStats) = partyData.GetActorPassivesWithEquipmentPassives(actor, dummyEquipment, currentEquipment);
        List<string> allPassives = passiveDetailViewer.ReturnAllPassiveInfo(passiveNames, passiveStats);
        for (int i = 0; i < allPassives.Count; i++)
        {
            string[] blocks = allPassives[i].Split("|");
            if (blocks.Length < 6){continue;}
            if (blocks[4].EndsWith("Skill"))
            {
                allActives.Add(blocks[5]);
            }
        }
        return allActives;
    }
}
