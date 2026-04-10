using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuildUIManager : MonoBehaviour
{
    public List<GameObject> statPanels;
    public int statState = 0;
    public void UpdateStatState(int newState)
    {
        if (statState == newState){return;}
        statPanels[statState].SetActive(false);
        statPanels[newState].SetActive(true);
        statState = newState;
    }
}
