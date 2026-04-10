using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StSTreasureScene : MonoBehaviour
{
    public PartyDataManager partyData;
    public StatDatabase rareEquipmentData;
    // If you beat a boss then you are guaranteed a very rare equipment.
    public int equipMinRarity = 0;
    // You usually get three random equipment.
    public int equipCount = 3;
    // Get a random amount of gold.
    public int minGold;
    public int maxGold;
    // How do we know if it is a treasure room or a boss treasure?
    public SavedData stsState;
    //stsState.ReturnCurrentTile()
    public Equipment dummyEquip;
    public GameObject equipmentPassiveViewerObject;
    public EquipmentPassiveViewer equipmentPassiveViewer;

    public string GetRandomEquipment(int mininmumRarity = -1)
    {
        string rEquip = rareEquipmentData.ReturnRandomValue();
        dummyEquip.SetAllStats(rEquip);
        if (dummyEquip.GetRarity() < mininmumRarity)
        {
            return GetRandomEquipment(mininmumRarity);
        }
        return rEquip;
    }

    public void OpenTreasure()
    {
        List<string> foundEquipment = new List<string>();
        for (int i = 0; i < equipCount; i++)
        {
            foundEquipment.Add(GetRandomEquipment());
            partyData.equipmentInventory.AddEquipmentByStats(foundEquipment[i]);
        }
        // Get some amount of gold.
        partyData.inventory.GainGold(Random.Range(minGold, maxGold));
        equipmentPassiveViewerObject.SetActive(true);
        equipmentPassiveViewer.SetAllEquipment(foundEquipment);
    }
}
