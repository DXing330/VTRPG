using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmorySimulator : ArmoryUI
{
    protected override void Start()
    {
        ResetArmory();
    }
    // All equipment are available instead of just what's in the inventory.
    public StatDatabase allEquipment;
    // No party data, just the character list that is being used.
    public CharacterList actorData;
    [ContextMenu("Test Reset Armory")]
    public void ResetArmory()
    {
        // Refresh all equipment.
        equipmentInventory.SortEquipment();
    }
    public GameObject statsObjects;
    public void DisableStatsView()
    {
        statsObjects.SetActive(false);
    }
    // Same as the armory, except no party data to draw data from, just the stat databases.
    public override void UpdateSelectedActor()
    {
        EndSelectingEquipment();
        statsObjects.SetActive(true);
        passiveViewer.DisablePanel();
        selectedActor.SetInitialStatsFromString(allActors.allActorData[allActors.GetSelected()]);
        actorStats.UpdateActorStatTexts(selectedActor);
        actorSpriteStats.UpdateActorSpriteStats(selectedActor);
        actorPassives.UpdateActorPassiveTexts(selectedActor, actorData.ReturnPartyMemberEquipFromIndex(allActors.GetSelected()));
        actorEquipment.UpdateActorEquipmentTexts(actorData.ReturnPartyMemberEquipFromIndex(allActors.GetSelected()));
        actorEquipment.ResetSelected();
        actorStatuses.SetStatsAndData(selectedActor.GetUniqueStatuses(), selectedActor.GetUniqueStatusStacks());
        actorActives.SetStatsAndData(selectedActor.GetActiveSkills());
    }

    public override void EndSelectingEquipment()
    {
        selectEquipment.ResetSelected();
        selectEquipObject.SetActive(false);
        if (allActors.GetSelected() < 0) { return; }
        selectedActor.SetInitialStatsFromString(allActors.allActorData[allActors.GetSelected()]);
        actorStats.UpdateActorStatTexts(selectedActor);
        actorPassives.UpdateActorPassiveTexts(selectedActor, actorData.ReturnPartyMemberEquipFromIndex(allActors.GetSelected()));
        actorEquipment.UpdateActorEquipmentTexts(actorData.ReturnPartyMemberEquipFromIndex(allActors.GetSelected()));
    }

    public override void UnequipSelected()
    {
        if (allActors.GetSelected() < 0) { return; }
        if (actorEquipment.GetSelected() < 0) { return; }
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
        }
        string oldEquip = actorData.UnequipFromMember(allActors.GetSelected(), slot, dummyEquip);
        actorEquipment.ResetSelected();
        EndSelectingEquipment();
    }

    public override void PreviewEquippedPassives()
    {
        selectEquipment.ResetHighlights();
        selectEquipment.HighlightIndex(selectEquipment.GetSelected());
        selectedActor.SetInitialStatsFromString(allActors.allActorData[allActors.GetSelected()]);
        actorPassives.UpdatePotentialPassives(selectedActor, actorData.ReturnPartyMemberEquipFromIndex(allActors.GetSelected()), selectEquipment.data[selectEquipment.GetSelected()]);
    }

    public override void ConfirmEquipSelection()
    {
        if (selectEquipment.GetSelected() < 0){return;}
        // Don't remove equipment since its just a simulation.
        // You have infinite of all equipment.
        string equipData = equipmentInventory.TakeEquipment(selectEquipment.GetSelected(), actorEquipment.GetSelected(), false);
        // pass the new equip to the party data and equip it to the selected member
        string oldEquip = actorData.EquipToMember(equipData, allActors.GetSelected(), dummyEquip);
        EndSelectingEquipment();
    }
}
