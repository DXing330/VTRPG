using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StSShopSaveData", menuName = "ScriptableObjects/StS/StSShopSaveData", order = 1)]
public class StSShopSaveData : SavedData
{
    public string delimiter2 = ",";
    // LCM (1*1,2*2,3*3), matching reward skillbook rarity weights.
    protected int SKILLWEIGHTBASE = 36;
    // --- Stock Counts ---
    public int normalBookCount = 4;
    public int rareBookCount = 1;
    public int colorlessBookCount = 2;
    public int consumableCount = 3;
    public int relicCount = 3;
    // --- First-Pass Prices ---
    public int commonBookPrice = 75;
    public int uncommonBookPrice = 100;
    public int rareBookPrice = 150;
    public int colorlessBookPrice = 125;
    public int consumablePrice = 50;
    public int relicPrice = 150;
    public int priestPrice = 150;
    // Saved shop stock. Bought/sold state is scene-local runtime state.
    public List<string> books;
    public List<string> bookPrices;
    public List<string> consumables;
    public List<string> consumablePrices;
    public List<string> relics;
    public List<string> relicPrices;
    public string priestServicePrice;

    // --- Shop Generation ---
    // The state manager should call this before entering the shop scene, then save.
    public void GenerateShop(StSRewardSaveData rewardTracker)
    {
        InitializeLists();
        ClearShopData();
        if (rewardTracker == null){return;}
        GenerateNormalBooks(rewardTracker);
        GenerateRareBooks(rewardTracker);
        GenerateColorlessBooks(rewardTracker);
        GenerateConsumables(rewardTracker);
        GenerateRelics(rewardTracker);
        priestServicePrice = priestPrice.ToString();
    }

    protected void GenerateNormalBooks(StSRewardSaveData rewardTracker)
    {
        if (rewardTracker.skillBookDB == null || rewardTracker.skillBookRarity == null){return;}
        List<string> possibleBooks = rewardTracker.skillBookDB.GetAllKeys();
        List<int> bookRarities = utility.ConvertStringListToIntList(rewardTracker.skillBookRarity.GetAllValues());
        List<int> rarityWeights = ReturnSkillBookRarityWeights();
        for (int i = 0; i < normalBookCount && possibleBooks.Count > 0; i++)
        {
            string bookName = "";
            for (int j = 0; j < possibleBooks.Count * 2; j++)
            {
                int rarity = rewardTracker.DetermineRewardRarity(rarityWeights);
                bookName = rewardTracker.GetRewardOfRarity(rarity, possibleBooks, bookRarities);
                if (bookName != ""){break;}
            }
            if (bookName == ""){bookName = possibleBooks[0];}
            AddShopBook(bookName, ReturnSkillBookPrice(bookName, rewardTracker.skillBookRarity));
            RemoveBookFromPool(bookName, possibleBooks, bookRarities);
        }
    }

    protected void GenerateRareBooks(StSRewardSaveData rewardTracker)
    {
        if (rewardTracker.skillBookDB == null || rewardTracker.skillBookRarity == null){return;}

        List<string> possibleBooks = rewardTracker.skillBookDB.GetAllKeys();
        for (int i = possibleBooks.Count - 1; i >= 0; i--)
        {
            if (books.Contains(possibleBooks[i]) || rewardTracker.skillBookRarity.ReturnValue(possibleBooks[i]) != "3")
            {
                possibleBooks.RemoveAt(i);
            }
        }

        for (int i = 0; i < rareBookCount && possibleBooks.Count > 0; i++)
        {
            int index = rewardTracker.rewardSeed.Range(0, possibleBooks.Count);
            string bookName = possibleBooks[index];
            AddShopBook(bookName, ReturnSkillBookPrice(bookName, rewardTracker.skillBookRarity));
            possibleBooks.RemoveAt(index);
        }
    }

    protected void GenerateColorlessBooks(StSRewardSaveData rewardTracker)
    {
        if (rewardTracker.colorlessSkillBookDB == null){return;}

        List<string> possibleBooks = rewardTracker.colorlessSkillBookDB.GetAllKeys();
        for (int i = 0; i < colorlessBookCount && possibleBooks.Count > 0; i++)
        {
            int index = rewardTracker.rewardSeed.Range(0, possibleBooks.Count);
            string bookName = possibleBooks[index];
            AddShopBook(bookName, ReturnColorlessBookPrice(bookName, rewardTracker));
            possibleBooks.RemoveAt(index);
        }
    }

    protected void GenerateConsumables(StSRewardSaveData rewardTracker)
    {
        if (rewardTracker.itemDB == null){return;}

        List<string> possibleItems = rewardTracker.itemDB.GetAllKeys();
        for (int i = 0; i < consumableCount && possibleItems.Count > 0; i++)
        {
            int index = rewardTracker.rewardSeed.Range(0, possibleItems.Count);
            consumables.Add(possibleItems[index]);
            consumablePrices.Add(consumablePrice.ToString());
            possibleItems.RemoveAt(index);
        }
    }

    protected void GenerateRelics(StSRewardSaveData rewardTracker)
    {
        if (rewardTracker.availableShopRelics == null){return;}
        List<string> possibleRelics = new List<string>(rewardTracker.availableShopRelics);
        for (int i = 0; i < relicCount && possibleRelics.Count > 0; i++)
        {
            int index = rewardTracker.rewardSeed.Range(0, possibleRelics.Count);
            relics.Add(possibleRelics[index]);
            relicPrices.Add(relicPrice.ToString());
            possibleRelics.RemoveAt(index);
        }
    }

    protected List<int> ReturnSkillBookRarityWeights()
    {
        List<int> rarityWeights = new List<int>();
        rarityWeights.Add(SKILLWEIGHTBASE / 1);
        rarityWeights.Add(SKILLWEIGHTBASE / 4);
        rarityWeights.Add(SKILLWEIGHTBASE / 9);
        return rarityWeights;
    }

    protected void AddShopBook(string bookName, int price)
    {
        books.Add(bookName);
        bookPrices.Add(price.ToString());
    }

    protected void RemoveBookFromPool(string bookName, List<string> possibleBooks, List<int> bookRarities)
    {
        int index = possibleBooks.IndexOf(bookName);
        if (index < 0){return;}
        possibleBooks.RemoveAt(index);
        if (index < bookRarities.Count)
        {
            bookRarities.RemoveAt(index);
        }
    }

    protected int ReturnSkillBookPrice(string bookName, StatDatabase rarityData)
    {
        if (rarityData == null){return commonBookPrice;}
        switch (rarityData.ReturnValue(bookName))
        {
            case "3":
            return rareBookPrice;
            case "2":
            return uncommonBookPrice;
        }
        return commonBookPrice;
    }

    protected int ReturnColorlessBookPrice(string bookName, StSRewardSaveData rewardTracker)
    {
        if (rewardTracker.colorlessSkillBookRarity == null){return colorlessBookPrice;}
        string rarity = rewardTracker.colorlessSkillBookRarity.ReturnValue(bookName);
        if (rarity == ""){return colorlessBookPrice;}
        return ReturnSkillBookPrice(bookName, rewardTracker.colorlessSkillBookRarity);
    }

    public override void NewGame()
    {
        InitializeLists();
        ClearShopData();
        Save();
    }

    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "Books=" + JoinList(books) + delimiter;
        allData += "BookPrices=" + JoinList(bookPrices) + delimiter;
        allData += "Consumables=" + JoinList(consumables) + delimiter;
        allData += "ConsumablePrices=" + JoinList(consumablePrices) + delimiter;
        allData += "Relics=" + JoinList(relics) + delimiter;
        allData += "RelicPrices=" + JoinList(relicPrices) + delimiter;
        allData += "PriestServicePrice=" + priestServicePrice + delimiter;
        File.WriteAllText(dataPath, allData);
    }

    public override void Load()
    {
        InitializeLists();
        dataPath = Application.persistentDataPath + "/" + filename;
        if (!File.Exists(dataPath))
        {
            NewGame();
            return;
        }
        allData = File.ReadAllText(dataPath);
        dataList = allData.Split(delimiter).ToList();
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
    }

    public void ClearShopData()
    {
        books.Clear();
        bookPrices.Clear();

        consumables.Clear();
        consumablePrices.Clear();

        relics.Clear();
        relicPrices.Clear();

        priestServicePrice = "";
    }

    protected void InitializeLists()
    {
        if (books == null){books = new List<string>();}
        if (bookPrices == null){bookPrices = new List<string>();}

        if (consumables == null){consumables = new List<string>();}
        if (consumablePrices == null){consumablePrices = new List<string>();}

        if (relics == null){relics = new List<string>();}
        if (relicPrices == null){relicPrices = new List<string>();}
        if (priestServicePrice == null){priestServicePrice = "";}
    }

    protected string JoinList(List<string> values)
    {
        if (values == null || values.Count == 0){return "";}
        return String.Join(delimiter2, values);
    }

    protected List<string> SplitList(string value)
    {
        if (value == ""){return new List<string>();}
        return value.Split(delimiter2).ToList();
    }

    public void LoadStat(string stat)
    {
        string[] statData = stat.Split("=");
        if (statData.Length < 2){return;}
        string key = statData[0];
        string value = statData[1];
        switch (key)
        {
            case "Books":
            books = SplitList(value);
            break;
            case "BookPrices":
            bookPrices = SplitList(value);
            break;
            case "Consumables":
            consumables = SplitList(value);
            break;
            case "ConsumablePrices":
            consumablePrices = SplitList(value);
            break;
            case "Relics":
            relics = SplitList(value);
            break;
            case "RelicPrices":
            relicPrices = SplitList(value);
            break;
            case "PriestServicePrice":
            priestServicePrice = value;
            break;
        }
    }
}
