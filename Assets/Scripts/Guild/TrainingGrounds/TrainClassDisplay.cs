using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrainClassDisplay : MonoBehaviour
{
    public GameObject displayPanel;
    public SpriteContainer actorSprites;
    public bool trainer = true;
    public TMP_Text actorName;
    public Image actorSprite;
    public TMP_Text mainClass;
    public TMP_Text mainClassLevel;

    public void DisableDisplay()
    {
        displayPanel.SetActive(false);
    }

    public void UpdateDisplay(TacticActor actor, List<string> trainableClasses)
    {
        displayPanel.SetActive(true);
        actorSprite.sprite = actorSprites.SpriteDictionary(actor.GetSpriteName());
        actorName.text = actor.GetPersonalName();
        if (trainer)
        {
            mainClass.text = actor.GetSpriteName();
            mainClassLevel.text = actor.GetLevelFromPassive(actor.GetSpriteName()).ToString();
        }
        else
        {
            mainClassLevel.text = actor.GetTotalPassiveLevelsOfPassiveGroup(trainableClasses).ToString();
        }
    }
}
