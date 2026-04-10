using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CombatLogSingle : MonoBehaviour
{
    public TMP_Text roundText;
    public TMP_Text turnText;
    public TMP_Text logText;
    public void UpdateLog(string round, string turn, string text)
    {
        roundText.text = round;
        turnText.text = turn;
        logText.text = text;
    }
}
