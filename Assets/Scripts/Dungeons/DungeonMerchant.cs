using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DungeonMerchant : MonoBehaviour
{
    public GameObject merchantPanel;
    public PartyDataManager partyData;
    public DungeonMap map;
    public Dungeon dungeon;
    public SelectStatTextList currentStock;
    public int totalBill;
    public List<string> takenItems;
    public TMP_Text currentGold;
    public TMP_Text currentInventory;
    public TMP_Text currentBillText;
    public GameObject cantPayPanel;
    public GameObject confirmStealPanel;

    void Start()
    {
        Reset();
    }

    protected void Reset()
    {
        totalBill = 0;
        takenItems.Clear();
        UpdateDisplay();
    }

    public void ActivateMerchant()
    {
        merchantPanel.SetActive(true);
        Reset();
    }

    protected void UpdateDisplay()
    {
        currentStock.SetStatsAndData(dungeon.GetMerchantItems(), dungeon.GetMerchantPriceString());
        UpdateShopping();
    }

    protected void UpdateShopping()
    {
        currentGold.text = partyData.inventory.GetGold().ToString();
        currentInventory.text = partyData.dungeonBag.ReturnBagLimitString();
        currentBillText.text = totalBill.ToString();
    }

    public void TakeItem()
    {
        if (currentStock.GetSelected() < 0){return;}
        // Check inventory.
        if (partyData.dungeonBag.BagFull()){return;}
        // Take the item.
        takenItems.Add(currentStock.GetSelectedStat());
        partyData.dungeonBag.GainItem(currentStock.GetSelectedStat());
        // Increase the price.
        totalBill += int.Parse(currentStock.GetSelectedData());
        UpdateShopping();
    }

    public void ReturnItems()
    {
        totalBill = 0;
        for (int i = 0; i < takenItems.Count; i++)
        {
            partyData.dungeonBag.UseItem(takenItems[i]);
        }
        takenItems.Clear();
        UpdateShopping();
    }

    public void Leave()
    {
        if (totalBill <= 0)
        {
            merchantPanel.SetActive(false);
        }
        else
        {
            confirmStealPanel.SetActive(true);
        }
    }

    public void Pay()
    {
        // If you can't pay then activate the can't pay panel.
        if (partyData.inventory.GetGold() < totalBill)
        {
            cantPayPanel.SetActive(true);
            return;
        }
        partyData.inventory.SpendGold(totalBill);
        totalBill = 0;
        takenItems.Clear();
        merchantPanel.SetActive(false);
    }

    public void ConfirmSteal()
    {
        dungeon.RobMerchant();
        merchantPanel.SetActive(false);
    }
}
