using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrackManager : MonoBehaviour
{
    public GeneralUtility utility;
    public CharacterList mainParty;
    public PartyDataManager partyData;
    public PartyData mainPartyData;
    public CharacterList barracksParty;
    public BarracksData barracksData;
    void Start()
    {
        mainPartyData = partyData.mainPartyData;
        barracksData.Load();
        UpdateLists();
    }
    protected void UpdateLists()
    {
        mainParty.UpdateBasedOnPartyData(mainPartyData);
        barracksParty.UpdateBasedOnPartyData(barracksData);
        barracksActors.RefreshData();
        mainActors.RefreshData();
    }
    public TacticActor selectedActor;
    public ActorSpriteHPList mainActors;
    public ActorSpriteHPList barracksActors;
    public SelectStatTextList actorStats;
    public SelectStatTextList actorPassives;
    public PopUpMessage popUp;

    public void UpdateSelectedBarrackActor()
    {
        mainActors.ResetSelected();
        actorStats.ResetSelected();
        actorPassives.ResetSelected();
        selectedActor.SetInitialStatsFromString(barracksActors.allActorData[barracksActors.GetSelected()]);
        actorStats.UpdateActorStatTexts(selectedActor);
        actorPassives.UpdateActorPassiveTexts(selectedActor, barracksData.partyEquipment[barracksActors.GetSelected()]);
    }

    public void UpdateSelectedMainActor()
    {
        barracksActors.ResetSelected();
        actorStats.ResetSelected();
        actorPassives.ResetSelected();
        selectedActor.SetInitialStatsFromString(mainActors.allActorData[mainActors.GetSelected()]);
        actorStats.UpdateActorStatTexts(selectedActor);
        actorPassives.UpdateActorPassiveTexts(selectedActor, partyData.ReturnMainPartyEquipment(mainActors.GetSelected()));
    }

    public void MoveToBarracks()
    {
        if (mainActors.GetSelected() < 0){return;}
        // Arbitrary limit to barracks?
        if (barracksData.PartyCount() > utility.Exponent(partyData.guildCard.GetGuildRank(), 2))
        {
            popUp.SetMessage("You aren't allowed to station any more allies here. Rank up more in order to be given more housing funds.");
            return;
        }
        barracksData.AddFromParty(mainActors.GetSelected(), partyData);
        UpdateLists();
        mainActors.ResetSelected();
        actorStats.ResetSelected();
        actorPassives.ResetSelected();
        barracksActors.RefreshData();
        mainActors.RefreshData();
    }

    public void AddToMainParty()
    {
        if (barracksActors.GetSelected() < 0){return;}
        // Make sure you don't make the party size larger than its allowed to be.
        if (!partyData.OpenSlots())
        {
            popUp.SetMessage("You aren't allowed to increase your party size any further. Rank up more in order to be trusted with more men.");
            return;
        }
        barracksData.AddFromBarracks(barracksActors.GetSelected(), partyData);
        UpdateLists();
        barracksActors.ResetSelected();
        actorStats.ResetSelected();
        actorPassives.ResetSelected();
        barracksActors.RefreshData();
        mainActors.RefreshData();
    }
}
