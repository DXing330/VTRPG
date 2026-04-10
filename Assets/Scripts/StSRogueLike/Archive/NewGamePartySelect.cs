using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NewGamePartySelect : MonoBehaviour
{
    public GeneralUtility utility;
    public PartyDataManager partyData;
    public StatDatabase newGameParties;
    public int newPartyIndex = 0;
    public void ChangeIndex(bool right = true)
    {
        newPartyIndex = utility.ChangeIndex(newPartyIndex, right, newGameParties.keys.Count-1);
        UpdateSelectedParty();
    }
    public TMP_Text partyName;
    public ActorSpriteHPList newPartyActors;

    public void StartSelectingNewParty()
    {
        newPartyIndex = 0;
        UpdateSelectedParty();
    }
    public void UpdateSelectedParty()
    {
        partyData.ForceNewGameData(newGameParties.ReturnValueAtIndex(newPartyIndex));
        partyData.SetFullParty();
        partyName.text = newGameParties.ReturnKeyAtIndex(newPartyIndex);
        newPartyActors.RefreshData();
    }
}
