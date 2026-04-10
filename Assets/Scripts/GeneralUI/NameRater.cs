using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NameRater : MonoBehaviour
{
    public TMP_Text newNameText;
    public string newNameString;
    public int characterLimit = 13;
    public bool upperCase = true;
    public TMP_Text caseText;
    public string upperCaseString = "BIG LETTERS";
    public string lowerCaseString = "small letters";
    public void ToggleCase()
    {
        upperCase = !upperCase;
        if (upperCase)
        {
            caseText.text = upperCaseString;
        }
        else
        {
            caseText.text = lowerCaseString;
        }
    }

    public void ResetNewName()
    {
        newNameString = "";
        newNameText.text = newNameString;
    }

    public void AddCharacterToName(string character)
    {
        if (newNameString.Length >= characterLimit) { return; }
        if (upperCase) { character = character.ToUpper(); }
        else { character = character.ToLower(); }
        newNameString += character;
        newNameText.text = newNameString;
    }

    public void RemoveCharacterFromName()
    {
        if (newNameString.Length <= 0){ return; }
        newNameString = newNameString.Remove(newNameString.Length - 1, 1);
        newNameText.text = newNameString;
    }

    public string ConfirmName()
    {
        return newNameString;
    }

    public string ReturnNameWithFirstCharUpperCase()
    {
        string name = newNameString.ToLowerInvariant();
        if (!string.IsNullOrEmpty(name))
        {
            string fName = char.ToUpper(name[0]) + name.Substring(1);
            return fName;
        }
        return name;
    }
}
