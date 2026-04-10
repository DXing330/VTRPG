using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StSRewardTester : MonoBehaviour
{
    public StSRewardSaveData testRewardData;

    [ContextMenu("Test Generate Skills")]
    public void TestGenerateSkills()
    {
        List<string> test = testRewardData.GenerateSkillBookChoices(4);
        for (int i = 0; i < test.Count; i++)
        {
            Debug.Log(test[i]);
        }
    }
    [ContextMenu("Test Generate Rare Skills")]
    public void TestGenerateRareSkills()
    {
        List<string> test = testRewardData.GenerateSkillBookChoices(3, true);
        for (int i = 0; i < test.Count; i++)
        {
            Debug.Log(test[i]);
        }
    }
}
