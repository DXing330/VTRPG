using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handles The State Of All Cards.
public class GHDeck : MonoBehaviour
{
    public GHSavedDeck savedDeck;
    [ContextMenu("InitializeStarterDeck")]
    public void InitializeStarterDeck()
    {
        savedDeck.NewGame();
        savedDeck.LoadToDeck(this);
    }
    public List<GHCard> start = new List<GHCard>();
    public List<GHCard> deck = new List<GHCard>();
    public List<GHCard> hand = new List<GHCard>();
    public List<GHCard> discardPile = new List<GHCard>();
    public List<GHCard> exhaustPile = new List<GHCard>();
    // After Playing, Before Putting In Discard.
    public void RemoveCard(GHCard card)
    {
        deck.Remove(card);
        hand.Remove(card);
    }
    public void Start()
    {
        savedDeck.LoadToDeck(this);
        InitializeDeck();
    }
    public void InitializeDeck()
    {
        deck = new List<GHCard>(start);
        hand.Clear();
        discardPile.Clear();
        exhaustPile.Clear();
        ShuffleDeck();
    }
    public void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            GHCard temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }
    public void StartTurn()
    {
        DrawCards(5);
    }
    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GHCard card = DrawCard();
            if (card == null) break;
            hand.Add(card);
        }
    }
    public GHCard DrawCard()
    {
        if (deck.Count == 0){ReshuffleDiscard();}
        if (deck.Count == 0){return null;}
        GHCard card = deck[0];
        deck.RemoveAt(0);
        return card;
    }
    public void PlayCard(GHCard card)
    {
        if (card.cardType == GHCardType.Power)
        {
            RemoveCard(card);
            return;
        }
        if (card.exhaust > 0)
        {
            ExhaustCard(card);
        }
        else
        {
            DiscardCard(card);
        }
    }
    public void ReshuffleDiscard()
    {
        if (discardPile.Count == 0) return;
        deck.AddRange(discardPile);
        discardPile.Clear();
        ShuffleDeck();
    }
    public void DiscardCard(GHCard card)
    {
        RemoveCard(card);
        discardPile.Add(card);
    }
    public void ExhaustCard(GHCard card)
    {
        RemoveCard(card);
        exhaustPile.Add(card);
    }
    public void DiscardHand()
    {
        for (int i = hand.Count - 1; i >= 0; i--)
        {
            GHCard card = hand[i];
            hand.RemoveAt(i);
            if (card.ethereal > 0)
            {
                exhaustPile.Add(card);
            }
            else {discardPile.Add(card);}
        }
    }
    public void EndTurn()
    {
        DiscardHand();
    }
}