using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentPassiveViewer : MonoBehaviour
{
    public PassiveDetailViewer detailViewer;
    public SelectStatTextList equipmentSelect;
    public SelectStatTextList passiveSelect;
    public Equipment dummyEquip;
    public List<string> allEquipment;
    public void SetAllEquipment(List<string> newInfo)
    {
        allEquipment = new List<string>(newInfo);
        allEquipmentNames = new List<string>();
        allEquipmentSlots = new List<string>();
        for (int i = 0; i < allEquipment.Count; i++)
        {
            dummyEquip.SetAllStats(allEquipment[i]);
            allEquipmentNames.Add(dummyEquip.GetName());
            allEquipmentSlots.Add(dummyEquip.GetSlot());
        }
        equipmentSelect.SetStatsAndData(allEquipmentNames, allEquipmentSlots);
        List<string> empty = new List<string>();
        passiveSelect.SetStatsAndData(empty, empty);
    }
    public List<string> allEquipmentNames;
    public List<string> allEquipmentSlots;

    public void UpdateEquipment()
    {
        if (equipmentSelect.GetSelected() < 0)
        {
            return;
        }
        dummyEquip.SetAllStats(allEquipment[equipmentSelect.GetSelected()]);
        passiveSelect.SetStatsAndData(dummyEquip.GetPassives(), dummyEquip.GetPassiveLevels());
    }

    public void ViewPassiveDetails()
    {
        if (equipmentSelect.GetSelected() < 0 || passiveSelect.GetSelected() < 0)
        {
            return;
        }
        string selectedPassive = passiveSelect.statTexts[passiveSelect.GetSelected()].GetStatText();
        string selectedPassiveLevel = passiveSelect.statTexts[passiveSelect.GetSelected()].GetText();
        detailViewer.UpdatePassiveNames(selectedPassive, selectedPassiveLevel);
    }
}
