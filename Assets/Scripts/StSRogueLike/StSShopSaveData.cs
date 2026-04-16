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

    public List<string> normalBooks;
    public List<string> normalBookPrices;
    public List<string> normalBookSold;

    public List<string> rareBooks;
    public List<string> rareBookPrices;
    public List<string> rareBookSold;

    public List<string> colorlessBooks;
    public List<string> colorlessBookPrices;
    public List<string> colorlessBookSold;

    public List<string> consumables;
    public List<string> consumablePrices;
    public List<string> consumableSold;

    public List<string> relics;
    public List<string> relicPrices;
    public List<string> relicSold;

    public string priestServicePrice;
    public string priestServiceUsed;

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
        allData += "NormalBooks=" + JoinList(normalBooks) + delimiter;
        allData += "NormalBookPrices=" + JoinList(normalBookPrices) + delimiter;
        allData += "NormalBookSold=" + JoinList(normalBookSold) + delimiter;
        allData += "RareBooks=" + JoinList(rareBooks) + delimiter;
        allData += "RareBookPrices=" + JoinList(rareBookPrices) + delimiter;
        allData += "RareBookSold=" + JoinList(rareBookSold) + delimiter;
        allData += "ColorlessBooks=" + JoinList(colorlessBooks) + delimiter;
        allData += "ColorlessBookPrices=" + JoinList(colorlessBookPrices) + delimiter;
        allData += "ColorlessBookSold=" + JoinList(colorlessBookSold) + delimiter;
        allData += "Consumables=" + JoinList(consumables) + delimiter;
        allData += "ConsumablePrices=" + JoinList(consumablePrices) + delimiter;
        allData += "ConsumableSold=" + JoinList(consumableSold) + delimiter;
        allData += "Relics=" + JoinList(relics) + delimiter;
        allData += "RelicPrices=" + JoinList(relicPrices) + delimiter;
        allData += "RelicSold=" + JoinList(relicSold) + delimiter;
        allData += "PriestServicePrice=" + priestServicePrice + delimiter;
        allData += "PriestServiceUsed=" + priestServiceUsed + delimiter;
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
        normalBooks.Clear();
        normalBookPrices.Clear();
        normalBookSold.Clear();

        rareBooks.Clear();
        rareBookPrices.Clear();
        rareBookSold.Clear();

        colorlessBooks.Clear();
        colorlessBookPrices.Clear();
        colorlessBookSold.Clear();

        consumables.Clear();
        consumablePrices.Clear();
        consumableSold.Clear();

        relics.Clear();
        relicPrices.Clear();
        relicSold.Clear();

        priestServicePrice = "";
        priestServiceUsed = "0";
    }

    protected void InitializeLists()
    {
        if (normalBooks == null){normalBooks = new List<string>();}
        if (normalBookPrices == null){normalBookPrices = new List<string>();}
        if (normalBookSold == null){normalBookSold = new List<string>();}

        if (rareBooks == null){rareBooks = new List<string>();}
        if (rareBookPrices == null){rareBookPrices = new List<string>();}
        if (rareBookSold == null){rareBookSold = new List<string>();}

        if (colorlessBooks == null){colorlessBooks = new List<string>();}
        if (colorlessBookPrices == null){colorlessBookPrices = new List<string>();}
        if (colorlessBookSold == null){colorlessBookSold = new List<string>();}

        if (consumables == null){consumables = new List<string>();}
        if (consumablePrices == null){consumablePrices = new List<string>();}
        if (consumableSold == null){consumableSold = new List<string>();}

        if (relics == null){relics = new List<string>();}
        if (relicPrices == null){relicPrices = new List<string>();}
        if (relicSold == null){relicSold = new List<string>();}
        if (priestServicePrice == null){priestServicePrice = "";}
        if (priestServiceUsed == null){priestServiceUsed = "0";}
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
            case "NormalBooks":
            normalBooks = SplitList(value);
            break;
            case "NormalBookPrices":
            normalBookPrices = SplitList(value);
            break;
            case "NormalBookSold":
            normalBookSold = SplitList(value);
            break;
            case "RareBooks":
            rareBooks = SplitList(value);
            break;
            case "RareBookPrices":
            rareBookPrices = SplitList(value);
            break;
            case "RareBookSold":
            rareBookSold = SplitList(value);
            break;
            case "ColorlessBooks":
            colorlessBooks = SplitList(value);
            break;
            case "ColorlessBookPrices":
            colorlessBookPrices = SplitList(value);
            break;
            case "ColorlessBookSold":
            colorlessBookSold = SplitList(value);
            break;
            case "Consumables":
            consumables = SplitList(value);
            break;
            case "ConsumablePrices":
            consumablePrices = SplitList(value);
            break;
            case "ConsumableSold":
            consumableSold = SplitList(value);
            break;
            case "Relics":
            relics = SplitList(value);
            break;
            case "RelicPrices":
            relicPrices = SplitList(value);
            break;
            case "RelicSold":
            relicSold = SplitList(value);
            break;
            case "PriestServicePrice":
            priestServicePrice = value;
            break;
            case "PriestServiceUsed":
            priestServiceUsed = value;
            break;
        }
    }
}
