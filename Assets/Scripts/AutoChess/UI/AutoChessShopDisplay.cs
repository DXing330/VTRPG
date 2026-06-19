using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessShopDisplay : MonoBehaviour
{
    public GeneralUtility utility;
    public SpriteContainer iconSprites;
    public List<GameObject> shopObjects;
    public List<AutoChessShopSlot> shopSlots;
    public void ResetDisplay()
    {
        utility.DisableGameObjects(shopObjects);
    }
    public void UpdateAutoChessShopUI(AutoChessShopManager manager)
    {
        ResetDisplay();
        for (int i = 0; i < Mathf.Min(manager.shopActors.Count, shopObjects.Count); i++)
        {
            shopObjects[i].SetActive(true);
            AutoActorRollUpData actor = manager.shopActors[i];
            string actorName = actor.GetName();
            string factions = manager.ReturnActorFactions(actor);
            int rarity = manager.ReturnActorRarity(actor);
            int cost = manager.ReturnActorCost(actor);
            shopSlots[i].UpdateAutoChessShopSlot(actorName, factions, iconSprites, rarity, cost);
        }
    }
}
