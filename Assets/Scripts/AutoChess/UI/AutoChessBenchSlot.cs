using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AutoChessBenchSlot : MonoBehaviour
{
    public GeneralUtility utility;
    public SpriteContainer masterSprites;
    public SpriteContainer factionSprites;
    public List<GameObject> factionIconObjects;
    public List<Image> factionIcons;
    public GameObject actorImageObject;
    public Image actorImage;
    public List<GameObject> equipmentIconObjects;
    public List<Image> equipmentIcons;
    public void ResetDisplay()
    {
        utility.DisableGameObjects(factionIconObjects);
        utility.DisableGameObjects(equipmentIconObjects);
        actorImageObject.SetActive(false);
    }
    public void UpdateBenchSlot(AutoActorRollUpData actor)
    {
        ResetDisplay();
        string newName = actor.GetName();
        int level = actor.GetLevel();
        actorImageObject.SetActive(true);
        masterSprites.ApplyToImage(actorImage, newName);
        List<string> allFactions = actor.GetFactions();
        for (int i = 0; i < allFactions.Count; i++)
        {
            factionIconObjects[i].SetActive(true);
            factionIcons[i].sprite = factionSprites.SpriteDictionary(allFactions[i]);
        }
        List<string> equipNames = actor.GetEquipmentNames();
        for (int i = 0; i < equipNames.Count; i++)
        {
            if (equipNames[i].Length <= 0){continue;}
            equipmentIconObjects[i].SetActive(true);
            masterSprites.ApplyToImage(equipmentIcons[i], equipNames[i]);
        }
    }
}
