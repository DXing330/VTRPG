using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TrainingGroundUI : MonoBehaviour
{
    public GeneralUtility utility;
    public PartyDataManager partyData;
    public ActorSpriteHPList allActors;
    public TacticActor selectedActor;
    public StatDatabase featData;
    public SelectStatTextList actorStats;
    public SelectStatTextList actorPassives;
    public PassiveDetailViewer detailViewer;
    public PopUpMessage errorPopUp;
    public PassiveDetailViewer featDetailViewer;
    public SelectStatTextList featSelect;
    public void UpdateAvailableFeats()
    {
        List<string> featNames = featData.GetAllKeys();
        List<string> featLevels = featData.GetAllValues();
        selectedActor.RemoveMaxLevelPassives(featNames, featLevels);
        featSelect.ResetSelected();
        featSelect.SetStatsAndData(featNames, featLevels);
    }
    public TMP_Text partyGold;
    public void UpdatePartyGold()
    {
        partyGold.text = partyData.inventory.GetGold().ToString();
    }
    public TMP_Text newFeatPrice;
    public void UpdateFeatPrice()
    {
        int featCount = 1 + selectedActor.GetTotalPassiveLevelsOfPassiveGroup(featData.GetAllKeys());
        newFeatPrice.text = (featCount * pricePerFeat).ToString();
    }
    public int pricePerFeat = 50;

    protected void UpdateActorStats()
    {
        selectedActor.SetInitialStatsFromString(partyData.ReturnPartyMemberStatsAtIndex(allActors.GetSelected()));
        actorStats.UpdateActorStatTexts(selectedActor);
        actorPassives.UpdateActorPassiveTexts(selectedActor);
    }

    public virtual void UpdateSelectedActor()
    {
        detailViewer.DisablePanel();
        featDetailViewer.DisablePanel();
        UpdateActorStats();
        UpdatePartyGold();
        UpdateFeatPrice();
        UpdateAvailableFeats();
    }

    public virtual void ViewPassiveDetails()
    {
        if (allActors.GetSelected() < 0) { return; }
        string selectedPassive = actorPassives.statTexts[actorPassives.GetSelected()].GetStatText();
        string selectedPassiveLevel = actorPassives.statTexts[actorPassives.GetSelected()].GetText();
        detailViewer.UpdatePassiveNames(selectedPassive, selectedPassiveLevel);
    }

    public void ViewFeatDetails()
    {
        if (allActors.GetSelected() < 0) { return; }
        // If not enough gold then give error message.
        if (!partyData.inventory.EnoughGold(int.Parse(newFeatPrice.text)))
        {
            errorPopUp.SetMessage("Not enough gold to afford this training.");
            return;
        }
        featDetailViewer.UpdatePassiveNames(featSelect.GetSelectedStat(), featSelect.GetSelectedData());
    }

    public void LearnFeat()
    {
        string newFeat = featSelect.GetSelectedStat();
        int selectedIndex = allActors.GetSelected();
        selectedActor.SetInitialStatsFromString(partyData.ReturnPartyMemberStatsAtIndex(selectedIndex));
        selectedActor.AddPassiveSkill(newFeat, "1");
        partyData.UpdatePartyMember(selectedActor, selectedIndex);
        partyData.inventory.SpendGold(int.Parse(newFeatPrice.text));
        partyData.Save();
        UpdateSelectedActor();
    }
}
