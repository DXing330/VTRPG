using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonDetailsViewer : MonoBehaviour
{
    public int state = 0;
    public List<GameObject> panels;

    public void ChangeState(int newState)
    {
        panels[state].SetActive(false);
        state = newState;
        panels[state].SetActive(true);
    }
}
