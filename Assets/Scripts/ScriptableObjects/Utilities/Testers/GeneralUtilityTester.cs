using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralUtilityTester : MonoBehaviour
{
    public GeneralUtility testUtility;
    public List<string> intStrings;
    public int intStringLength;

    [ContextMenu("Generate")]
    public void GenerateRandomIntStringList()
    {
        intStrings.Clear();
        for (int i = 0; i < intStringLength; i++)
        {
            intStrings.Add(Random.Range(0,intStringLength).ToString());
        }
    }

    [ContextMenu("Test Sort")]
    public void TestQuickSort()
    {
        testUtility.QuickSortIntStringList(intStrings, 0, intStringLength - 1);
    }
}
