using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MultiLevelStats", menuName = "ScriptableObjects/DataContainers/MultiLevelStats", order = 1)]
public class MultiLevelDatabase : StatDatabase
{
    public string allData;
    public override void SetAllData(string newData){allData = newData;}
    public List<string> multiLevelData;
    public string allDataDelimiter;
    public StatDatabase statDatabase;
    public int statKeyIndex;
    public int statValueIndex;
    public MultiKeyStatDatabase multiKeyDatabase;
    public int multiKeyIndex1;
    public int multiKeyIndex2;
    public int multiKeyIndex3;
    public int multiKeyValueIndex;

    public override void Initialize()
    {
        multiLevelData = allData.Split(allDataDelimiter).ToList();
        statDatabase.SetAllKeys(multiLevelData[statKeyIndex]);
        statDatabase.SetValues(multiLevelData[statValueIndex]);
        statDatabase.Initialize();
        statDatabase.DBSetDirty();
        multiKeyDatabase.SetAllKeys(multiLevelData[multiKeyIndex1]);
        multiKeyDatabase.SetAllSecondKeys(multiLevelData[multiKeyIndex2]);
        multiKeyDatabase.SetValues(multiLevelData[multiKeyValueIndex]);
        multiKeyDatabase.Initialize();
        multiKeyDatabase.DBSetDirty();
    }
}
