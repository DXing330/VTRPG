using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempPartyMemberManager : MonoBehaviour
{
    public PartyDataManager partyDataManager;
    public string tempMember;

    public void AddTempMember()
    {
        partyDataManager.AddTempPartyMember(tempMember);
    } 
}
