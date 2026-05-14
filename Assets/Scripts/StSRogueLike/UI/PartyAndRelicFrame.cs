using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartyAndRelicFrame : MonoBehaviour
{
    public PartyDataManager partyData;
    public SceneMover sceneMover;
    public ArmoryUI armory;
    public GameObject armoryObject;
    public void ActivateArmory()
    {
        armory.SetPartyData(partyData);
        armoryObject.SetActive(true);
    }
    public MedicinePanel medicinePanel;
    public GameObject medicineObject;
    public void UseItems()
    {
        medicinePanel.SetPartyData(partyData);
        medicineObject.SetActive(true);
    }
    public UseSkillBookUI useSkillBook;
    public GameObject useSBObject;
    public void ActivateUseSkillBook()
    {
        useSkillBook.SetPartyData(partyData);
        useSBObject.SetActive(true);
    }
}
