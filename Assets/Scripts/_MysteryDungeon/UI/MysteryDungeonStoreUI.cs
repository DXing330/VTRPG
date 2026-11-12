using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// 11/10/26 Implemented As Planned.
// 11/11/26 Key Functions Tested.
public class MysteryDungeonStoreUI : MonoBehaviour
{
    public GeneralUtility utility;
    public MysteryDungeonManager generalManager;
    public PartyDataManager partyData;
    public MysteryDungeonBattleMap map;
    public GameObject thisObject;
    public void ShowMDStoreUI()
    {
        thisObject.SetActive(true);
        UpdateMDStoreUI();
    }
    public void HideMDStoreUI(){thisObject.SetActive(false);}
    public StatDatabase itemData;
    public MysteryDungeonItemDetailSwitch itemDetails;
    public TMP_Text itemText;
    public TMP_Text goldText;
    public TMP_Text bagText;
    public TMP_Text billText;
    protected int currentBill;
    public TMP_Text cartText;
    public SelectStatTextList storeItems;
    public SelectStatTextList partyItems;
    public SelectStatTextList soldItems;
    // Includes A Yes And A No Button.
    // Yes Leaves The Store And Deals With The Consequences
    // No Continues Shopping.
    public PopUpMessage checkOutPopUp;
    public PopUpMessage buyPopUp;
    public PopUpMessage buyBackPopUp;
    public bool AvailableBagSpace()
    {
        return partyData.dungeonBag.GetItems().Count + map.GetUnpaidItems().Count < partyData.dungeonBag.GetMaxBagSpace();
    }
    protected List<string> GetPricesOfItems(List<string> items, bool buy = true)
    {
        List<string> prices = new List<string>();
        for (int i = 0; i < items.Count; i++)
        {
            string[] data = itemData.ReturnValue(items[i]).Split("|");
            if (items[i].StartsWith("TM"))
            {
                data = itemData.ReturnValue("TM").Split("|");
            }
            // If The DB Is Bad Then Crash?
            if (data.Length < 7)
            {
                prices.Add("1");
                continue;
            }
            int dataIndex = buy ? 5 : 6;
            prices.Add(data[dataIndex]);
        }
        return prices;
    }
    [ContextMenu("Update Store UI")]
    public void UpdateMDStoreUI()
    {
        goldText.text = partyData.inventory.GetGold().ToString();
        bagText.text = partyData.dungeonBag.ReturnBagLimitString();
        currentBill = utility.SumStringList(GetPricesOfItems(map.GetUnpaidItems())) - utility.SumStringList(GetPricesOfItems(map.GetSoldItems(), false));
        billText.text = (currentBill).ToString();
        cartText.text = String.Join(", ", map.GetUnpaidItems());
        List<string> prices = new List<string>();
        List<string> items = partyData.dungeonBag.GetItems();
        prices = GetPricesOfItems(items, false);
        partyItems.SetStatsAndData(items, prices);
        items = map.GetStoreItems();
        prices = GetPricesOfItems(items);
        storeItems.SetStatsAndData(items, prices);
        items = map.GetSoldItems();
        prices = GetPricesOfItems(items, false);
        soldItems.SetStatsAndData(items, prices);
        itemText.text = "";
    }
    public void ClearCartButton()
    {
        map.ClearCart();
        UpdateMDStoreUI();
    }
    // Already Confirmed Enough Bag Space.
    protected void GainItemsFromStore()
    {
        List<string> items = map.GetUnpaidItems();
        for (int i = 0; i < items.Count; i++)
        {
            partyData.dungeonBag.GainItem(items[i]);
        }
        map.ClearUnpaidItems();
    }
    protected void RobStore()
    {
        GainItemsFromStore();
        map.ClearSoldItems();
        map.RobStore();
    }
    // Only Called After A Successful Checkout.
    protected void SettleBill()
    {
        if (currentBill == 0){return;}
        if (currentBill < 0)
        {
            partyData.inventory.GainGold(-currentBill);
        }
        else if (currentBill > 0)
        {
            partyData.inventory.LoseGold(currentBill);
        }
    }
    protected void Checkout()
    {
        GainItemsFromStore();
        SettleBill();
        map.ClearSoldItems();
    }
    public void CheckoutButton()
    {
        bool hasSoldItems = map.GetSoldItems().Count > 0;
        bool hasBoughtItems = map.GetUnpaidItems().Count > 0;
        if (!hasBoughtItems && !hasSoldItems)
        {
            HideMDStoreUI();
            return;
        }
        if (!hasBoughtItems && hasSoldItems)
        {
            Checkout();
            HideMDStoreUI();
            return;
        }
        if (hasBoughtItems && currentBill <= partyData.inventory.GetGold())
        {
            Checkout();
            HideMDStoreUI();
            return;
        }
        if (hasBoughtItems && currentBill > partyData.inventory.GetGold())
        {
            checkOutPopUp.SetMessage("You can't afford this, are you trying to scam me?");
            return;
        }
    }
    protected void LeaveStore()
    {
        bool hasSoldItems = map.GetSoldItems().Count > 0;
        bool hasBoughtItems = map.GetUnpaidItems().Count > 0;
        // Like Leaving Money On The Table.
        if (!hasBoughtItems && hasSoldItems)
        {
            map.ClearSoldItems();
        }
        if (hasBoughtItems && currentBill > 0)
        {
            RobStore();
        }
        // Overpaying For Items.
        if (hasBoughtItems && currentBill < 0)
        {
            GainItemsFromStore();
            map.ClearSoldItems();
        }
        HideMDStoreUI();
    }
    public void LeaveButton()
    {
        bool hasSoldItems = map.GetSoldItems().Count > 0;
        bool hasBoughtItems = map.GetUnpaidItems().Count > 0;
        // Cart Empty + No Sold Items -> Leave.
        if (!hasBoughtItems && !hasSoldItems)
        {
            HideMDStoreUI();
            return;
        }
        // Cart Empty + Sold Items -> Confirm Leave.
        if (!hasBoughtItems && hasSoldItems)
        {
            checkOutPopUp.SetMessage("You're really just giving me these things?");
            return;
        }
        // Cart Not Empty + Positive Bill -> Confirm Robbing.
        if (hasBoughtItems && currentBill > 0)
        {
            checkOutPopUp.SetMessage("Are you trying to leave without paying?");
            return;
        }
        // Cart Not Empty + 0 Bill -> Leave.
        if (hasBoughtItems && currentBill == 0)
        {
            Checkout();
            HideMDStoreUI();
            return;
        }
        // Cart Not Empty + Negative Bill -> Confirm Leave Without Claiming Money.
        if (hasBoughtItems && currentBill < 0)
        {
            checkOutPopUp.SetMessage("Are you sure you're happy just leaving now?");
            return;
        }
    }
    public void SelectItem()
    {
        itemText.text = itemDetails.ReturnItemDescriptionFromName(storeItems.GetSelectedStat());
    }
    public void BuyItem()
    {
        if (!AvailableBagSpace())
        {
            buyPopUp.SetMessage("You won't be able to carry so much. Why don't you try selling some items?");
            return;
        }
        string itemName = storeItems.GetSelectedStat();
        map.AddToCart(itemName);
        UpdateMDStoreUI();
    }
    public void SelectInventoryItem()
    {
        itemText.text = itemDetails.ReturnItemDescriptionFromName(partyItems.GetSelectedStat());
    }
    public void SellItem()
    {
        string itemName = partyItems.GetSelectedStat();
        partyData.dungeonBag.UseItem(itemName);
        map.SellItem(itemName);
        UpdateMDStoreUI();
    }
    public void SelectBuyBackItem()
    {
        itemText.text = itemDetails.ReturnItemDescriptionFromName(soldItems.GetSelectedStat());
    }
    public void BuyBackItem()
    {
        if (!AvailableBagSpace())
        {
            buyBackPopUp.SetMessage("You won't be able to carry so much. Why don't you try selling some items?");
            return;
        }
        string itemName = soldItems.GetSelectedStat();
        partyData.dungeonBag.GainItem(itemName);
        map.BuyBackItem(itemName);
        UpdateMDStoreUI();
    }
}