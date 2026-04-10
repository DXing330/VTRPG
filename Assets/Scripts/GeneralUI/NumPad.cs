using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NumPad : MonoBehaviour
{
    public string cNumber;
    public int GetNumber(){return int.Parse(cNumber);}
    public TMP_Text numDisplay;
    public void UpdateDisplay()
    {
        numDisplay.text = cNumber;
    }
    public int maxLength = 6;
    public void Reset()
    {
        cNumber = "";
        UpdateDisplay();
    }
    public void PressNumber(int newNumber)
    {
        if (newNumber < 0 || newNumber > 9){return;}
        if (cNumber.Length >= maxLength){return;}
        cNumber += newNumber.ToString();
        UpdateDisplay();
    }
}
