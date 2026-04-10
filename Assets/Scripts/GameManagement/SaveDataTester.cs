using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveDataTester : MonoBehaviour
{
    public SavedData savedData;
    public TMP_Text text;
    bool showText = false;

    public void UpdateText()
    {
        if (!showText){return;}
        if (savedData.dataList.Count == 0){return;}
        text.text = savedData.dataList[0];
    }

    public void Load()
    {
        savedData.Load();
        UpdateText();
    }

    public void Save()
    {
        savedData.Save();
    }

    public void TestData()
    {
        savedData.dataList[0] = Random.Range(0, 10).ToString();
        UpdateText();
    }
}
