using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GHCardType
{
    Attack,
    Skill,
    Power,
    Curse,
    Status,
    Quest,
    None
}
public enum GHCardRarity
{
    Starter,
    Common,
    Uncommon,
    Rare,
    None
}
[System.Serializable]
public class GHCard
{
    protected string cardDelimiter = "GHCardDelim";
    public string cardName = "TestDummy";
    // Uses Actions.
    public int cost = 0;
    public GHCardRarity cardRarity = GHCardRarity.None;
    public GHCardType cardType = GHCardType.None;
    // Effects Apply In Order Into A Queue.
    public List<string> effects = new();
    public List<string> specifics = new();
    // Some Attack/Debuff Have Shape + Span.
    public string targetShape = "None";
    public int targetSpan = 0;
    public int exhaust = 0;
    public int ethereal = 0;
    public string ReturnCardData()
    {
        string cardData = "";
        cardData += "Name=" + cardName + cardDelimiter;
        cardData += "Cost=" + cost + cardDelimiter;
        cardData += "Rarity=" + cardRarity.ToString() + cardDelimiter;
        cardData += "Type=" + cardType.ToString() + cardDelimiter;
        cardData += "Effects=" + string.Join(",", effects) + cardDelimiter;
        cardData += "Specifics=" + string.Join(",", specifics) + cardDelimiter;
        cardData += "Shape=" + targetShape + cardDelimiter;
        cardData += "Span=" + targetSpan + cardDelimiter;
        cardData += "Exhaust=" + exhaust + cardDelimiter;
        cardData += "Ethereal=" + ethereal + cardDelimiter;
        return cardData;
    }
    public void LoadBaseCardData(string[] newInfo)
    {
        if (newInfo.Length < 10){return;}
        cardName = newInfo[0];
        int.TryParse(newInfo[1], out cost);
        if (Enum.TryParse(newInfo[2], out GHCardRarity loadedRarity))
        {
            cardRarity = loadedRarity;
        }
        if (Enum.TryParse(newInfo[3], out GHCardType loadedType))
        {
            cardType = loadedType;
        }
        effects = string.IsNullOrEmpty(newInfo[4]) ? new List<string>() : new List<string>(newInfo[4].Split(','));
        specifics = string.IsNullOrEmpty(newInfo[5]) ? new List<string>() : new List<string>(newInfo[5].Split(','));
        targetShape = newInfo[6];
        int.TryParse(newInfo[7], out targetSpan);
        int.TryParse(newInfo[8], out exhaust);
        int.TryParse(newInfo[9], out ethereal);
    }
    public void LoadCardData(string newInfo, string delimiter = "")
    {
        if (delimiter == ""){delimiter = cardDelimiter;}
        string[] cardData = newInfo.Split(new string[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string data in cardData)
        {
            string[] keyValue = data.Split(new char[] { '=' }, 2);
            if (keyValue.Length < 2)
                continue;
            string key = keyValue[0];
            string value = keyValue[1];
            switch (key)
            {
                case "Name":
                    cardName = value;
                    break;
                case "Cost":
                    int.TryParse(value, out cost);
                    break;
                case "Rarity":
                    if (Enum.TryParse(value, out GHCardRarity loadedRarity))
                        cardRarity = loadedRarity;
                    break;
                case "Type":
                    if (Enum.TryParse(value, out GHCardType loadedType))
                        cardType = loadedType;
                    break;
                case "Effects":
                    effects = string.IsNullOrEmpty(value) ? new List<string>() : new List<string>(value.Split(','));
                    break;
                case "Specifics":
                    specifics = string.IsNullOrEmpty(value) ? new List<string>() : new List<string>(value.Split(','));
                    break;
                case "Shape":
                    targetShape = value;
                    break;
                case "Span":
                    int.TryParse(value, out targetSpan);
                    break;
                case "Exhaust":
                    int.TryParse(value, out exhaust);
                    break;
                case "Ethereal":
                    int.TryParse(value, out ethereal);
                    break;
            }
        }
    }
}