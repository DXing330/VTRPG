using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureChestManager : MonoBehaviour
{
    public GeneralUtility utility;
    public StatDatabase chestRewards;
    public StatDatabase runeNames;
    public EquipmentGenerator equipmentGenerator;
    public PartyDataManager partyData;
    public int gold;
    public List<string> rarityStrings;
    public List<int> rarityInts;
    public List<string> equipmentFound;
    public List<string> GetEquipmentFound()
    {
        return equipmentFound;
    }
    public List<string> itemsFound;
    public List<string> GetItemsFound()
    {
        return itemsFound;
    }
    public List<int> quantitiesFound;
    public List<string> GetQuantitiesFound()
    {
        return utility.ConvertIntListToStringList(quantitiesFound);
    }
    public void ResetFound()
    {
        gold = 0;
        equipmentFound.Clear();
        itemsFound.Clear();
        quantitiesFound.Clear();
    }
    public void GainEquipment(string newInfo)
    {
        equipmentFound.Add(newInfo);
    }
    public void GainItem(string newInfo, int quantity)
    {
        int indexOf = itemsFound.IndexOf(newInfo);
        if (indexOf < 0)
        {
            itemsFound.Add(newInfo);
            quantitiesFound.Add(quantity);
            return;
        }
        quantitiesFound[indexOf] += quantity;
    }

    public void OpenAllChests()
    {
        ResetFound();
        List<string> chests = partyData.dungeonBag.ReturnAllChests();
        for (int i = 0; i < chests.Count; i++)
        {
            OpenChest(chests[i]);
        }
    }

    public void OpenChest(string chestType)
    {
        string[] possibleRewards = chestRewards.ReturnValue(chestType).Split("|");
        string chosenReward = possibleRewards[Random.Range(0, possibleRewards.Length)];
        GainReward(chosenReward);
    }

    [ContextMenu("Mass Generate Runes")]
    public void MassGenerateRune()
    {
        string runes = "";
        for (int i = 0; i < 99; i++)
        {
            runes += GenerateRune() + " ";
        }
        Debug.Log(runes);
    }

    public string GenerateRune()
    {
        return runeNames.ReturnRandomKey();
    }

    protected void GainReward(string rewardAndQuantity)
    {
        string[] details = rewardAndQuantity.Split("*");
        int quantity = int.Parse(details[1]);
        if (rarityStrings.Contains(details[0]))
        {
            for (int i = 0; i < quantity; i++)
            {
                partyData.equipmentInventory.AddEquipmentByStats(GenerateEquipment(details[0]));
            }
        }
        else if (details[0] == "Gold")
        {
            gold += quantity;
            partyData.inventory.GainGold(quantity);
        }
        else if (details[0] == "Rune")
        {
            for (int i = 0; i < quantity; i++)
            {
                string rune = GenerateRune();
                GainItem(rune, 1);
                partyData.inventory.AddItemQuantity(rune);
            }
        }
        else
        {
            GainItem(details[0], quantity);
            partyData.inventory.AddItemQuantity(details[0], quantity);
        }
    }

    protected string GenerateEquipment(string rarity)
    {
        string equipData = equipmentGenerator.GenerateEquipmentOfRarity(rarity);
        string[] dBlocks = equipData.Split("|");
        GainEquipment(dBlocks[0]);
        return equipData;
    }
}
