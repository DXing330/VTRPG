using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MedicinePanel : MonoBehaviour
{
    void Start()
    {
        UpdateItems();
    }
    // Only some items can be used while resting.
    public StatDatabase itemTags;
    public string usableTags = "Medicine";
    List<string> currentUsableItems;
    public PartyDataManager partyData;
    public void SetPartyData(PartyDataManager newPartyData)
    {
        partyData = newPartyData;
    }
    public ActorSpriteHPList actorSelect;
    public TacticActor dummyActor;
    public TMP_Text actorHealthText;
    public SelectStatTextList statusList;
    public SelectList medicineSelect;
    public void ResetSelectedMedicine()
    {
        medicineSelect.ResetSelected();
        medicineEffectText.text = "";
    }
    public SkillEffect medicineEffect;
    public StatDatabase activeData;
    public ActiveSkill dummyActive;
    public ActiveDescriptionViewer activeDescriptionViewer;
    public TMP_Text medicineEffectText;

    public void UpdateItems()
    {
        currentUsableItems = new List<string>();
        // Iterate through the party inventory.
        List<string> allItems = partyData.inventory.GetItems();
        for (int i = 0; i < allItems.Count; i ++)
        {
            // Determine which items are usable based on tags.
            if (usableTags.Contains(itemTags.ReturnValue(allItems[i])))
            {
                currentUsableItems.Add(allItems[i]);
            }
        }
        medicineSelect.SetSelectables(currentUsableItems);
        ResetSelectedMedicine();
    }

    public void SelectItem()
    {
        int index = medicineSelect.GetSelected();
        if (index < 0) { return; }
        dummyActive.LoadSkillFromString(activeData.ReturnValue(currentUsableItems[index]), null);
        medicineEffectText.text = activeDescriptionViewer.ReturnActiveDescription(dummyActive);
    }

    public void UseItem()
    {
        if (medicineSelect.GetSelected() < 0 || actorSelect.GetSelected() < 0)
        {
            return;
        }
        medicineEffect.AffectActor(dummyActor, dummyActive.GetEffect(), dummyActive.GetSpecifics(), dummyActive.GetPower());
        partyData.inventory.RemoveItemQuantity(1, currentUsableItems[medicineSelect.GetSelected()]);
        UpdateItems();
        // Update the party data.
        partyData.UpdatePartyMember(dummyActor, actorSelect.GetSelected());
        partyData.SetFullParty();
        int selectedIndex = actorSelect.GetSelected();
        int page = actorSelect.page;
        actorSelect.RefreshData();
        actorSelect.SetPage(page);
        actorSelect.SetSelectedIndex(selectedIndex);
        // Update the actor stats view.
        ViewActorStats();
    }

    public void ViewActorStats()
    {
        if (actorSelect.GetSelected() < 0) { return; }
        dummyActor.SetInitialStatsFromString(actorSelect.allActorData[actorSelect.GetSelected()]);
        actorHealthText.text = dummyActor.GetHealth() + " / " + dummyActor.GetBaseHealth();
        statusList.SetStatsAndData(dummyActor.GetUniqueStatuses(), dummyActor.GetUniqueStatusStacks());
    }
}
