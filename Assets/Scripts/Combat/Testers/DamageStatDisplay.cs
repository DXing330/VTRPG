using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DamageStatDisplay : MonoBehaviour
{
    public GameObject displayObject;
    public Image actorImage;
    public TMP_Text actorName;
    public TMP_Text damageDealtText;
    public TMP_Text damageTakenText;
    public GameObject damageDealtBar;
    public GameObject damageTakenBar;
    public float minY = 0.01f;
    public void UpdateDisplay(Sprite actorSprite, string personalName, int damageDealt, int damageTaken, float damageDealtProportion, float damageTakenProportion)
    {
        displayObject.SetActive(true);
        actorImage.sprite = actorSprite;
        actorName.text = personalName;
        damageDealtText.text = damageDealt.ToString();
        damageTakenText.text = damageTaken.ToString();
        damageDealtBar.transform.localScale = new Vector3(0.4f, Mathf.Max(damageDealtProportion, minY), 0f);
        damageTakenBar.transform.localScale = new Vector3(0.4f, Mathf.Max(damageTakenProportion, minY), 0f);
    }
}
