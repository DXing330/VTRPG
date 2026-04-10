using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenamePartyMemberUI : MonoBehaviour
{
    public GeneralUtility utility;
    public PartyDataManager partyData;
    public ActorSpriteHPList actorList;
    public NameRater nameRater;

    void Start()
    {
        
    }

    public void RenamePartyMember()
    {
        int selected = actorList.GetSelected();
        string newName = nameRater.ConfirmName();
        if (newName == "") { return; }
        if (utility.CountCharactersInString(newName) == newName.Length) { return; }
        partyData.RenamePartyMember(newName, selected);
        actorList.RefreshData();
    }
}
