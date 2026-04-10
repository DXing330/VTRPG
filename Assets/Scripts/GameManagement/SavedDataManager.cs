using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavedDataManager : MonoBehaviour
{
    public List<SavedData> allData;
    public CharacterList partyList;
    public SavedData partyData;

    public void Save()
    {
        for (int i = 0; i < allData.Count; i++)
        {
            allData[i].Save();
        }
    }

    protected void SavePartyData()
    {
        
    }
}
