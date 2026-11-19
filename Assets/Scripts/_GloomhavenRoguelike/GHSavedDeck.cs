using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GHSavedDeck", menuName = "ScriptableObjects/Gloomhaven/GHSavedDeck", order = 1)]
public class GHSavedDeck : SavedData
{
    public StatDatabase GHCardData;
    public GHCard ReturnCardByName(string cardName)
    {
        string cardData = GHCardData.ReturnValue(cardName);
        if (cardData == ""){return null;}
        GHCard newCard = new();
        newCard.LoadBaseCardData(cardData.Split("|"));
        return newCard;
    }
    public override void NewGame()
    {
        string allCardData = "";
        List<string> starterDeck = new();
        for (int i = 0; i < 3; i++)
        {
            starterDeck.Add("Strike");
            starterDeck.Add("Defend");
            starterDeck.Add("Move");
        }
        starterDeck.Add("Bash");
        for (int i = 0; i < starterDeck.Count; i++)
        {
            allCardData += ReturnCardByName(starterDeck[i]).ReturnCardData();
            if (i < starterDeck.Count - 1){allCardData += delimiter;}
        }
        dataPath = GetSavePath();
        File.WriteAllText(dataPath, allCardData);
    }
    public void SaveDeck(GHDeck deck)
    {
        string allCardData = "";
        for (int i = 0; i < deck.start.Count; i++)
        {
            allCardData += deck.start[i].ReturnCardData();
            if (i < deck.start.Count - 1){allCardData += delimiter;}
        }
        dataPath = GetSavePath();
        File.WriteAllText(dataPath, allCardData);
    }
    public void LoadToDeck(GHDeck deck)
    {
        dataPath = GetSavePath();
        if (!File.Exists(dataPath))
        {
            NewGame();
            LoadToDeck(deck);
            return;
        }
        string allCardData = File.ReadAllText(dataPath);
        deck.start.Clear();
        string[] cardData = allCardData.Split(new string[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string cardInfo in cardData)
        {
            GHCard card = new GHCard();
            card.LoadCardData(cardInfo);
            deck.start.Add(card);
        }
        deck.InitializeDeck();
    }
}