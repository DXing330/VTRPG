using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AutoChessBenchSlot : MonoBehaviour
{
    public GeneralUtility utility;
    public SpriteContainer factionSprites;
    public StatDatabase actorData;
    public List<GameObject> factionIconObjects;
    public List<Image> factionIcons;
    public TMP_Text actorNameLevelText;
    public void ResetDisplay()
    {
        utility.DisableGameObjects(factionIconObjects);
        actorNameLevelText.text = "";
    }
    public void UpdateBenchSlot(string newName, int level = 1)
    {
        ResetDisplay();
        actorNameLevelText.text = newName;
        string[] blocks = actorData.ReturnValue(newName).Split("|");
        string[] allFactions = blocks[0].Split(",");
        for (int i = 0; i < allFactions.Length; i++)
        {
            factionIconObjects[i].SetActive(true);
            factionIcons[i].sprite = factionSprites.SpriteDictionary(allFactions[i]);
        }
    }
}
