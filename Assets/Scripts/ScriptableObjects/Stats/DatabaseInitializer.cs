using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DatabaseInitializer : MonoBehaviour
{
    public string allData;
    public string allDataDelimiter = "}";
    public void SetAllData(string newInfo)
    {
        allData = newInfo;
    }
    public bool stats;
    public string allStatData;
    public bool sprites;
    public string allSpriteData;
    public GroupedStatDatabase masterDatabase;
    public GroupedSpriteContainer masterSprites;
    public List<StatDatabase> statData;
    public List<SpriteContainer> spriteContainers;
    public List<SpecificStatDatabase> specificStats;

    public void Initialize()
    {
        for (int i = 0; i < statData.Count; i++)
        {
            statData[i].Initialize();
            statData[i].DBSetDirty();
        }
    }
    [ContextMenu("InitializeStatAndSpriteData")]
    public void InitializeStatAndSpriteData()
    {
        string[] blocks = allData.Split(allDataDelimiter);
        int blockIndex = 0;
        if (stats)
        {
            allStatData = blocks[blockIndex];
            masterDatabase.SetAllData(allStatData);
            masterDatabase.Initialize();
            for (int i = 0; i < specificStats.Count; i++)
            {
                specificStats[i].Initialize();
            }
            blockIndex++;
        }
        if (sprites)
        {
            allSpriteData = blocks[blockIndex];
            masterSprites.SetAllData(allSpriteData);
            masterSprites.Initialize();
        }
    }
}
