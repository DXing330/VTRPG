using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveDataReset : MonoBehaviour
{
    public List<SavedData> data;

    [ContextMenu("ResetAll")]
    public void ResetAll()
    {
        for (int i = 0; i < data.Count; i++)
        {
            data[i].NewGame();
            data[i].Load();
        }
    }
}
