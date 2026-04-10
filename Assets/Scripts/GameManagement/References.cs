using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class References : MonoBehaviour
{
    // Used to prevent resources from being unloaded since they are not referenced.
    public Dungeon dungeonReference;
    public CharacterList partyReference;
    public List<SavedData> savedDataReferences;
    public void NewGame()
    {
        for (int i = 0; i < savedDataReferences.Count; i++)
        {
            savedDataReferences[i].NewGame();
        }
    }

    public void Save()
    {
        for (int i = 0; i < savedDataReferences.Count; i++)
        {
            savedDataReferences[i].Save();
        }
    }

    public void Load()
    {
        for (int i = 0; i < savedDataReferences.Count; i++)
        {
            savedDataReferences[i].Load();
        }
    }
}
