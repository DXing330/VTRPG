using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Controls The UI for Selecting Skill Mods From Events/Relics
public class SkillModSelectMenu : MonoBehaviour
{
    // GAME LOGIC
    public PartyDataManager partyData;
    public SelectMultiList multiList;
    public ActiveSkill active;
    public ActiveDescriptionViewer activeDetailViewer;
    public string mod;
    public List<string> partySkillNames;
    public List<string> partySkillIDs;
    // UI
    public TMP_Text actorNameText;
    public TMP_Text modNameText;
    public TMP_Text modDetailText;
    public TMP_Text currentSkillText;
    public TMP_Text moddedSkillText;
    public void InitializeMenu(PartyDataManager newParty, string newMod, int newCount = 1)
    {
        partyData = newParty;
        mod = newMod;
        multiList.SetMaxSelections(newCount);
        multiList.ResetSelected();
        partySkillNames.Clear();
        partySkillIDs.Clear();
        int partyCount = partyData.ReturnTotalPartyCount();
        for (int i = 0; i < partyCount; i++)
        {
            TacticActor newActor = partyData.ReturnActorAtIndex(i);
            // TODO Load Some Things So We Get Equip Skills/Temp Skills/Class Skill/Etc.
            List<string> actorSkills = new List<string>(newActor.GetActiveSkills());
        }
        // Reset the multiselectlist.
        // Get all skills from the party (including skills granted by actives?)
        // Maybe Get All IDs From Party As Well?
        // Set the multiselect list based on all skills from the party.
        // Set the amount of skills that can be selected.
    }
    public void SelectSkill()
    {
        // Get The Index From The MultiSelectList
        // Get The Actor And Skill From The Party, Based On Skill Index
    }
    // Update the UI.
    protected void UpdateDisplay(string skillName, TacticActor skillUser)
    {

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
        /*List<string> allPassives = passiveViewer.ReturnAllPassiveInfo(passiveNames, passiveStats);
        for (int i = 0; i < allPassives.Count; i++)
        {
            string[] blocks = allPassives[i].Split("|");
            if (blocks.Length < 6){continue;}
            if (blocks[4].Contains("Skill"))
            {
                allActives.Add(blocks[5]);
            }
        }*/
        return allActives;
    }
}
