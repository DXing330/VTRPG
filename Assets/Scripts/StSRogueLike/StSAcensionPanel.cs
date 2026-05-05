using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StSAcensionPanel : MonoBehaviour
{
    public StSRunModifiersSaveData savedModifiers;
    public List<GameObject> panelOneObjects;
    public List<SpinnerMenu> statSpinners;
    public List<GameObject> panelTwoObjects;
    public void LoadModifiers()
    {
        savedModifiers.Load();
        List<int> statMods = savedModifiers.ReturnStatMods();
        // Update The Displays Based On The Modifiers.
        for (int i = 0; i < Mathf.Min(statMods.Count, statSpinners.Count); i++)
        {
            statSpinners[i].SetSelectedText(statMods[i].ToString());
        }
    }
    public void SaveModifiers()
    {
        // Update The Saved Modifiers Based On The Displays.
        List<int> statMods = new List<int>();
        for (int i = 0; i < statSpinners.Count; i++)
        {
            statMods.Add(int.Parse(statSpinners[i].GetSelected()));
        }
        savedModifiers.SetStatMods(statMods);
        savedModifiers.Save();
    }
    public void ResetModifiers()
    {
        savedModifiers.NewGame();
    }
}
