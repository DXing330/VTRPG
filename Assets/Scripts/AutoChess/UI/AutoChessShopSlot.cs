using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shop stores a list of these. Only stores UI objects. Updated by a manager.
public class AutoChessShopSlot : MonoBehaviour
{
    public GeneralUtility utility;
    public List<GameObject> factionIconObjects;
    public List<Image> factionIcons;
    // Later replace with a sprite.
    public TMP_Text actorName;
    // Gold Stars?
    public List<GameObject> rarityObjects;
    public TMP_Text goldCost;
    public void ResetAutoChessShopSlot()
    {
        utility.DisableGameObjects(factionIconObjects);
        utility.DisableGameObjects(rarityObjects);
        goldCost.text = "";
        actorName.text = "";
    }
    public void UpdateAutoChessShopSlot(string newName, string factions, SpriteContainer factionSprites, int actorRarity, int actorCost)
    {
        ResetAutoChessShopSlot();
        actorName.text = newName;
        goldCost.text = actorCost.ToString();
        for (int i = 0; i < actorRarity; i++)
        {
            rarityObjects[i].SetActive(true);
        }
        string[] allFactions = factions.Split(",");
        for (int i = 0; i < allFactions.Length; i++)
        {
            factionIconObjects[i].SetActive(true);
            factionIcons[i].sprite = factionSprites.SpriteDictionary(allFactions[i]);
        }
    }
}
