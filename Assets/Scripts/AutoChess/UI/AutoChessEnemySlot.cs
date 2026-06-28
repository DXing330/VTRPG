using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AutoChessEnemySlot : MonoBehaviour
{
    public GameObject thisObject;
    public TMP_Text enemyName;
    public TMP_Text enemyQuantity;
    public void ResetSlot()
    {
        enemyName.text = "";
        enemyQuantity.text = "";
        thisObject.SetActive(false);
    }
    public void UpdateSlot(string newName, int newQuantity)
    {
        thisObject.SetActive(true);
        enemyName.text = newName;
        enemyQuantity.text = newQuantity.ToString();
    }
}
