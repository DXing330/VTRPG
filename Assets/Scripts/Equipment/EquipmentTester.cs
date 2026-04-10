using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentTester : MonoBehaviour
{
    public Equipment dummyEquip;
    public EquipmentInventory dummyInventory;
    public StatDatabase equipData;

    [ContextMenu("Gain Equipment")]
    public void GainEquipment(string equipName)
    {
        dummyInventory.AddEquipmentByName(equipName);
    }
    public EquipmentPassiveViewer dummyEquipViewer;
    public List<string> testEquipment;
    [ContextMenu("Test Setting Equipment")]
    public void TestSetEquipment()
    {
        dummyEquipViewer.SetAllEquipment(testEquipment);
    }
}
