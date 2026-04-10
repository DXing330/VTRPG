using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    void Start()
    {
        itemSelectList.SetSelectables(inventory.GetItems());
    }
    // Need to know which items can be assigned to actors.
    public StatDatabase activeData;
    public PartyDataManager partyData;
    public void SetPartyData(PartyDataManager newParty)
    {
        partyData = newParty;
    }
    public Inventory inventory;
    public ItemDetailViewer itemDetails;
    public PopUpMessage popUp;
    public int selectedActorID;
    public void SetSelectedID(int newID)
    {
        selectedActorID = newID;
    }
    // Keep track of how many items can be assigned to each party member here.
    public int selectedActorItemSlots;
    public void SetActorItemSlots(int newValue)
    {
        selectedActorItemSlots = newValue;
    }
    public int selectedItemIndex;
    public void UnassignItem()
    {
        if (itemSelectList.GetSelected() < 0){return;}
        inventory.UnassignItem(selectedItemIndex);
        UpdateSelectedItem();
    }
    public void AssignToActor()
    {
        // TODO get item slots from actor.
        if (itemSelectList.GetSelected() < 0){return;}
        int assignedCount = inventory.AssignedToIDCount(selectedActorID);
        if (assignedCount >= selectedActorItemSlots)
        {
            popUp.SetMessage("Cannot hold any more items at this time.");
            return;
        }
        inventory.AssignToActor(selectedItemIndex, selectedActorID);
        UpdateSelectedItem();
    }
    public SelectList itemSelectList;
    public void SelectItem()
    {
        if (itemSelectList.GetSelected() < 0){return;}
        selectedItemIndex = itemSelectList.GetSelected();
        UpdateSelectedItem();
    }
    public TMP_Text assignedToName;
    public void UpdateSelectedItem()
    {
        if (itemSelectList.GetSelected() < 0){return;}
        assignedToName.text = "";
        itemDetails.ShowInfo(itemSelectList.GetSelectedString());
        string ID = inventory.GetAssignedActorIDFromIndex(itemSelectList.GetSelected());
        if (ID == ""){return;}
        TacticActor assignedActor = partyData.ReturnActorFromID(int.Parse(ID));
        if (assignedActor == null){return;}
        assignedToName.text = assignedActor.GetPersonalName();
    }
}