using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public Inventory inventory;
    public DungeonBag dungeonBag;
    //public List<string> keyValues;
    //public List<StatTextText> currentInventoryStuff;
    public bool inventoryDisplay;
    public bool dungeonDisplay;
    public TMP_Text goldString;
    public TMP_Text inventoryString;
    public TMP_Text dungeonBagString;

    void Start()
    {
        UpdateKeyValues();
    }

    public void UpdateKeyValues()
    {
        goldString.text = inventory.GetGold().ToString();
        if (inventoryDisplay)
        {
            inventoryString.text = inventory.ReturnBagLimitString();
        }
        if (dungeonDisplay)
        {
            dungeonBagString.text = dungeonBag.ReturnBagLimitString();
        }
    }
}
