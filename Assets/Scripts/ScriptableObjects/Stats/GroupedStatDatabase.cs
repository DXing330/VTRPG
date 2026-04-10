using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GroupedStats", menuName = "ScriptableObjects/DataContainers/GroupedStats", order = 1)]
public class GroupedStatDatabase : StatDatabase
{
    public List<StatDatabase> groupedDBs;
    public string allData;
    public override void SetAllData(string newData)
    {
        allData = newData;
    }
    public string allDataDelimiter;

    public override void Initialize()
    {
        string[] blocks = allData.Split(allDataDelimiter);
        for (int i = 0; i < groupedDBs.Count; i++)
        {
            groupedDBs[i].SetAllData(blocks[i]);
            groupedDBs[i].Initialize();
            groupedDBs[i].DBSetDirty();
        }
    }
}
