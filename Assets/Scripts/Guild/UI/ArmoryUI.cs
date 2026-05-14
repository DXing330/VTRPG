using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArmoryUI : MonoBehaviour
{
    public GeneralUtility utility;
    public Equipment dummyEquip;
    public EquipmentInventory equipmentInventory;
    public PartyDataManager partyData;
    public void SetPartyData(PartyDataManager newPartyData)
    {
        partyData = newPartyData;
    }
    public SelectStatTextList actorStats;
    public SelectStatTextList actorStatuses;
    public SelectStatTextList actorPassives;
    public SelectStatTextList actorActives;
    public SelectStatTextList actorDivineSpells;
    public SelectStatTextList actorEquipment;
    public PopUpMessage equipmentStats;
    public List<string> equipmentSlotNames;
    public void ShowEquipmentStats()
    {
        if (allActors.GetSelected() < 0){return;}
        // Show the popup with the equipment stats.
        string allEquipment = partyData.ReturnPartyMemberEquipFromIndex(allActors.GetSelected());
        string[] dataBlocks = allEquipment.Split("@");
        string equipment = "";
        for (int i = 0; i < dataBlocks.Length; i++)
        {
            dummyEquip.SetAllStats(dataBlocks[i]);
            if (dummyEquip.GetSlot() == equipmentSlotNames[actorEquipment.GetSelected()])
            {
                equipment = dataBlocks[i];
                break;
            }
        }
        if (equipment == ""){return;}
        string message = "Equipment Stats:" + "\n";
        List<string> pandL = dummyEquip.GetPassivesAndLevels();
        for (int i = 0; i < pandL.Count; i++)
        {
            message += pandL[i] + "\n";
        }
        // Get the equipment from the party data.
        equipmentStats.SetMessage(message);
    }
    protected virtual void Start()
    {
        allActors.UpdateTextSize();
        actorStats.UpdateTextSize();
        actorPassives.UpdateTextSize();
        actorEquipment.UpdateTextSize();
        selectEquipment.UpdateTextSize();
    }
    public GameObject moreStatObject;
    public StatTextList moreActorStats;
    public void ViewMoreStats()
    {
        if (allActors.GetSelected() < 0 || selectedActor == null){return;}
        moreStatObject.SetActive(true);
        moreActorStats.SetStatsAndData(selectedActor.GetPublicStatNames(), selectedActor.GetPublicStatInfo());
    }
    public GameObject runeGridObject;
    public EquipmentRunesUI runeGrid;
    public void ViewRunes()
    {
        if (allActors.GetSelected() < 0 || selectedActor == null){return;}
        runeGridObject.SetActive(true);
        runeGrid.UpdateRuneGrid(partyData.ReturnPartyMemberEquipFromIndex(allActors.GetSelected()));
    }
    public void InsertRune(string rune, string equipSlot)
    {
        // Determine the actors equipment.
        string actorEquip = partyData.ReturnPartyMemberEquipFromIndex(allActors.GetSelected());
        // Get the slot.
        List<string> equipBlocks = actorEquip.Split("@").ToList();
        for (int i = 0; i < equipBlocks.Count; i++)
        {
            dummyEquip.SetAllStats(equipBlocks[i]);
            if (dummyEquip.GetSlot() == equipSlot)
            {
                // Insert the rune.
                dummyEquip.AddRune(rune);
                dummyEquip.RefreshStats();
                equipBlocks[i] = dummyEquip.GetStats();
                break;
            }
        }
        // Roll back up.
        actorEquip = String.Join("@", equipBlocks);
        partyData.SetPartyMemberEquipFromIndex(actorEquip, allActors.GetSelected());
        runeGrid.UpdateRuneGrid(partyData.ReturnPartyMemberEquipFromIndex(allActors.GetSelected()));
    }
    public GameObject inventoryObject;
    public InventoryManager inventoryManager;
    public void ViewInventory()
    {
        if (allActors.GetSelected() < 0 || selectedActor == null){return;}
        inventoryObject.SetActive(true);
        inventoryManager.SetPartyData(partyData);
        inventoryManager.SetSelectedID(partyData.ReturnIDAtIndex(allActors.GetSelected()));
        inventoryManager.SetActorItemSlots(partyData.ReturnActorAtIndex(allActors.GetSelected()).GetItemSlots());
    }
    public GameObject selectEquipObject;
    public SelectStatTextList selectEquipment;
    public ActorSpriteHPList allActors;
    public TacticActor selectedActor;
    public SelectStatTextList actorSpriteStats;
    public StatDatabase elementPassives;
    public StatDatabase speciesPassives;
    public string selectedPassive;
    public string selectedPassiveLevel;
    public PassiveDetailViewer passiveViewer;
    public ActiveDescriptionViewer activeViewer;
    public void SelectActive()
    {
        if (allActors.GetSelected() < 0 || selectedActor == null){return;}
        activeViewer.SelectActive(selectedActor);
    }

    public virtual void ResetView()
    {
        actorStats.PublicResetPage();
        actorPassives.PublicResetPage();
        actorEquipment.ResetTextText();
    }

    protected void UpdateActorStats()
    {
        selectedActor.SetInitialStatsFromString(allActors.allActorData[allActors.GetSelected()]);
        actorStats.UpdateActorStatTexts(selectedActor, true);
        actorSpriteStats.UpdateActorSpriteStats(selectedActor);
        actorPassives.UpdateActorPassiveTexts(selectedActor, partyData.ReturnPartyMemberEquipFromIndex(allActors.GetSelected()));
        actorEquipment.UpdateActorEquipmentTexts(partyData.ReturnPartyMemberEquipFromIndex(allActors.GetSelected()));
        actorEquipment.ResetSelected();
        actorStatuses.SetStatsAndData(selectedActor.GetUniqueStatuses(), selectedActor.GetUniqueStatusStacks());
        UpdateActorActives();
        UpdateActorDivineSpells();
    }
    
    protected void UpdateActorActives()
    {
        List<string> allActives = selectedActor.GetActiveSkills();
        // Go through all the passives.
        // For any that add actives at the start of battle, add those actives.
        List<string> allPassives = passiveViewer.ReturnAllPassiveInfo(actorPassives.stats, actorPassives.data);
        for (int i = 0; i < allPassives.Count; i++)
        {
            string[] blocks = allPassives[i].Split("|");
            if (blocks.Length < 4){break;}
            if (blocks[4].Contains("Skill"))
            {
                allActives.Add(blocks[5]);
            }
        }
        actorActives.SetStatsAndData(allActives);
    }

    protected void UpdateActorDivineSpells()
    {
        List<string> allSpells = selectedActor.GetSpells();
        // Add any spells from attributes.
        List<string> attributes = selectedActor.GetAttributes();
        for (int i = 0; i < attributes.Count; i++)
        {
            if (attributes[i] == "Nil"){continue;}
            int attributeCount = selectedActor.AttributeCount(attributes[i]);
            allSpells.Add("Hoti-" + attributes[i]);
            if (attributeCount >= 2)
            {
                allSpells.Add("Bhavati-" + attributes[i]);
            }
        }
        allSpells = allSpells.Distinct().ToList();
        actorDivineSpells.SetStatsAndData(allSpells);
    }

    public virtual void UpdateSelectedActor()
    {
        EndSelectingEquipment();
        passiveViewer.DisablePanel();
        UpdateActorStats();
    }

    public virtual void UpdateSelectedActorWithCurrentHealth()
    {
        UpdateSelectedActor();
        actorStats.UpdateActorStatTexts(selectedActor, true);
    }

    public virtual void ViewPassiveDetails()
    {
        if (allActors.GetSelected() < 0) { return; }
        selectedPassive = actorPassives.statTexts[actorPassives.GetSelected() % actorPassives.statTexts.Count].GetStatText();
        selectedPassiveLevel = actorPassives.statTexts[actorPassives.GetSelected() % actorPassives.statTexts.Count].GetText();
        passiveViewer.UpdatePassiveNames(selectedPassive, selectedPassiveLevel);
    }

    public virtual void ViewCustomPassiveDetails()
    {
        if (allActors.GetSelected() < 0 || selectedActor == null){return;}
        passiveViewer.ViewCustomPassives(selectedActor);
    }

    public virtual void BeginSelectingEquipment()
    {
        if (allActors.GetSelected() < 0){return;}
        if (actorEquipment.GetSelected() < 0){return;}
        switch (actorEquipment.GetSelected())
        {
            case 0:
            if (equipmentInventory.WeaponCount() <= 0){return;}
            selectEquipment.SetTitle("Weapons");
            selectEquipment.SetData(equipmentInventory.GetWeapons());
            break;
            case 1:
            if (equipmentInventory.ArmorCount() <= 0){return;}
            selectEquipment.SetTitle("Armor");
            selectEquipment.SetData(equipmentInventory.GetArmor());
            break;
            case 2:
            if (equipmentInventory.CharmCount() <= 0){return;}
            selectEquipment.SetTitle("Charms");
            selectEquipment.SetData(equipmentInventory.GetCharms());
            break;
            case 3:
            if (equipmentInventory.HelmetCount() <= 0){return;}
            selectEquipment.SetTitle("Helmets");
            selectEquipment.SetData(equipmentInventory.GetHelmets());
            break;
            case 4:
            if (equipmentInventory.BootsCount() <= 0){return;}
            selectEquipment.SetTitle("Boots");
            selectEquipment.SetData(equipmentInventory.GetBoots());
            break;
            case 5:
            if (equipmentInventory.GlovesCount() <= 0){return;}
            selectEquipment.SetTitle("Gloves");
            selectEquipment.SetData(equipmentInventory.GetGloves());
            break;
        }
        selectEquipObject.SetActive(true);
        selectEquipment.UpdateEquipNames();
        selectEquipment.ResetSelected();
    }

    public virtual void EndSelectingEquipment()
    {
        selectEquipment.ResetSelected();
        selectEquipObject.SetActive(false);
        // Disable other panels that might be over the equip panel as well.
        runeGridObject.SetActive(false);
        moreStatObject.SetActive(false);
        inventoryObject.SetActive(false);
        if (allActors.GetSelected() < 0){return;}
        selectedActor.SetInitialStatsFromString(allActors.allActorData[allActors.GetSelected()]);
        actorStats.UpdateActorStatTexts(selectedActor, true);
        actorPassives.UpdateActorPassiveTexts(selectedActor, partyData.ReturnPartyMemberEquipFromIndex(allActors.GetSelected()));
        actorEquipment.UpdateActorEquipmentTexts(partyData.ReturnPartyMemberEquipFromIndex(allActors.GetSelected()));
        UpdateActorActives();
    }

    public virtual void PreviewEquippedPassives()
    {
        selectEquipment.ResetHighlights();
        selectEquipment.HighlightIndex(selectEquipment.GetSelected());
        selectedActor.SetInitialStatsFromString(allActors.allActorData[allActors.GetSelected()]);
        actorPassives.UpdatePotentialPassives(selectedActor, partyData.ReturnPartyMemberEquipFromIndex(allActors.GetSelected()), selectEquipment.data[selectEquipment.GetSelected()]);
    }

    public virtual void ConfirmEquipSelection()
    {
        if (selectEquipment.GetSelected() < 0){return;}
        // take the selected index and pull the equipment from the equipment inventory
        string equipData = equipmentInventory.TakeEquipment(selectEquipment.GetSelected(), actorEquipment.GetSelected());
        // pass the new equip to the party data and equip it to the selected member
        string oldEquip = partyData.EquipToPartyMember(equipData, allActors.GetSelected(), dummyEquip);
        // get the old equip back and pass it to the equip inventory
        equipmentInventory.AddEquipmentByStats(oldEquip);
        // close the screen.
        EndSelectingEquipment();
    }

    public virtual void UnequipSelected()
    {
        if (allActors.GetSelected() < 0){return;}
        if (actorEquipment.GetSelected() < 0){return;}
        string slot = "";
        switch (actorEquipment.GetSelected())
        {
            case 0:
            slot = "Weapon";
            break;
            case 1:
            slot = "Armor";
            break;
            case 2:
            slot = "Charm";
            break;
            case 3:
            slot = "Helmet";
            break;
            case 4:
            slot = "Boots";
            break;
            case 5:
            slot = "Gloves";
            break;
        }
        string oldEquip = partyData.UnequipFromPartyMember(allActors.GetSelected(), slot, dummyEquip);
        equipmentInventory.AddEquipmentByStats(oldEquip);
        actorEquipment.ResetSelected();
        EndSelectingEquipment();
    }
}
