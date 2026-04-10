using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

[CreateAssetMenu(fileName = "GroupedSprites", menuName = "ScriptableObjects/DataContainers/GroupedSprites", order = 1)]
public class GroupedSpriteContainer : SpriteContainer
{
    public List<SpriteContainer> groupedSprites;
    public string allData;
    public string allDataDelimiter;
    public override void SetAllData(string newInfo)
    {
        allData = newInfo;
        #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
        #endif
    }

    public override void Initialize()
    {
        string[] blocks = allData.Split(allDataDelimiter);
        for (int i = 0; i < groupedSprites.Count; i++)
        {
            groupedSprites[i].SetAllData(blocks[i]);
            groupedSprites[i].sprites = utility.SortSpritesByNames(groupedSprites[i].sprites);
            groupedSprites[i].Initialize();
        }
    }
}
