using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AutoActorDisplay : MonoBehaviour
{
    public GeneralUtility utility;
    public StatDatabase actorData;
    public StatDatabase actorRarity;
    public StatDatabase activeData;
    public SpriteContainer factionSprites;
    public TMP_Text actorName;
    public TMP_Text traitText;
    public TMP_Text skillText;
    public List<GameObject> rarityObjects;
    public List<GameObject> factionIconObjects;
    public List<Image> factionIcons;
    public TMP_Text healthText;
    public TMP_Text attackText;
    public TMP_Text defenseText;
    public TMP_Text energyText;
    // TODO Add A Display For Attack Range + Shape
    public Image attackShapeImage;
    public TMP_Text attackRangeText;
    // TODO Add A Display For Respawn Timing.
    public TMP_Text respawnText;
    public void ResetDisplay()
    {
        utility.DisableGameObjects(factionIconObjects);
        utility.DisableGameObjects(rarityObjects);
        actorName.text = "";
        traitText.text = "";
        skillText.text = "";
        healthText.text = "";
        attackText.text = "";
        defenseText.text = "";
        energyText.text = "";
        attackRangeText.text = "";
        respawnText.text = "";
    }
    public void DisplayActor(string newName)
    {
        ResetDisplay();
        actorName.text = newName;
        int rarity = utility.SafeParseInt(actorRarity.ReturnValue(newName), 1);
        for (int i = 0; i < rarity; i++)
        {
            rarityObjects[i].SetActive(true);
        }
        string[] blocks = actorData.ReturnValue(newName).Split("|");
        string[] allFactions = blocks[0].Split(",");
        for (int i = 0; i < allFactions.Length; i++)
        {
            factionIconObjects[i].SetActive(true);
            factionIcons[i].sprite = factionSprites.SpriteDictionary(allFactions[i]);
        }
        // TODO Display Trait Based On 1,2,3
        // TODO Display Skill Based On 4
        energyText.text = blocks[5] + "(" + blocks[6] + ")";
        healthText.text = blocks[7];
        attackText.text = blocks[8];
        defenseText.text = blocks[9];
        attackRangeText.text = blocks[10];
        // TODO Update Attack Range Image Based On Attack Range Type.
        respawnText.text = blocks[13];
    }
    public void DisplayActor(AutoActorRollUpData actor)
    {
        DisplayActor(actor.GetName());
    }
}
