using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StSEventScene : MonoBehaviour
{
    public string mainMapScene = "StSMap";
    public GeneralUtility utility;
    public PartyDataManager partyData;
    public StSStateManager stsManager;
    public TMP_Text eventName;
    public TMP_Text eventDescription;
    public Image eventImage;
    public SpriteContainer eventSprites;
    public List<GameObject> choiceObjects;
    public void ResetChoices()
    {
        utility.DisableGameObjects(choiceObjects);
    }

    void Start()
    {
    }

    public void DisplayEvent()
    {
    }

    public void SelectChoice(int index)
    {
    }
}
