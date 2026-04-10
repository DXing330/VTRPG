using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiLevelDBInitializer : MonoBehaviour
{
    public string allData;
    public string delimiter;
    public List<DatabaseInitializer> allDBInit;
    public List<SpriteContainer> spriteContainers;
    [ContextMenu("Set All Data")]
    public void SetAllData()
    {
        string[] splitData = allData.Split(delimiter);
        for (int i = 0; i < allDBInit.Count; i++)
        {
            allDBInit[i].SetAllData(splitData[i]);
            allDBInit[i].InitializeStatAndSpriteData();
        }
    }

    public void Initialize()
    {
        for (int i = 0; i < allDBInit.Count; i++)
        {
            allDBInit[i].Initialize();
        }
        for (int i = 0; i < spriteContainers.Count; i++)
        {
            spriteContainers[i].Initialize();
        }
    }
}
