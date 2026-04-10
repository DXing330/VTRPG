using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StSSettingDisplay : MonoBehaviour
{
    void Start()
    {
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        settings.Load();
        difficultyDisplay.text = settings.GetDifficulty().ToString();
        difficultyDescription.text = difficultyDescriptions.ReturnValueAtIndex(settings.GetDifficulty());
    }

    public StSSettings settings;
    public StatDatabase difficultyDescriptions;
    public TMP_Text difficultyDisplay;
    public TMP_Text difficultyDescription;
    
    public void ChangeDifficulty(bool right = true)
    {
        if (right)
        {
            settings.IncreaseDifficulty();
        }
        else
        {
            settings.DecreaseDifficulty();
        }
        difficultyDisplay.text = settings.GetDifficulty().ToString();
        difficultyDescription.text = difficultyDescriptions.ReturnValueAtIndex(settings.GetDifficulty());
    }
}
