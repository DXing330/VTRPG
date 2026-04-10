using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiscTester : MonoBehaviour
{
    public int testDir;

    public string CheckRelativeDirections(int dir1, int dir2)
    {
        int directionDiff = Mathf.Abs(dir1 - dir2);
        switch (directionDiff)
        {
            case 0:
            return "Same";
            case 1:
            return "Back";
            case 2:
            return "Front";
            case 3:
            return "Opposite";
            case 4:
            return "Front";
            case 5:
            return "Back";
        }
        return "None";
    }

    public bool CheckDirectionSpecifics(string conditionSpecifics, string specifics)
    {
        if (conditionSpecifics == "Back" && specifics == "Same") { return true; }
        else if (conditionSpecifics == "Front" && specifics == "Opposite") { return true; }
        else if (conditionSpecifics == "Side")
        {
            if (specifics != "Opposite" && specifics != "Same")
            {
                return true;
            }
        }
        return (conditionSpecifics == specifics);
    }

    public List<string> testDirectionCondition;
    [ContextMenu("Check Direction Conditions")]
    public void CheckDirectionConditions()
    {
        string testResults = "";
        for (int k = 0; k < 6; k++)
        {
            int newTestDir = k;
            testResults += "\nTest Direction: " + newTestDir;
            for (int i = 0; i < 6; i++)
            {
                testResults += "\nTesting Direction: " + i;
                string relativeDir = CheckRelativeDirections(newTestDir, i);
                testResults += ", Relative Direction: " + relativeDir;
                testResults += "\nPassing Conditions = (";
                for (int j = 0; j < testDirectionCondition.Count; j++)
                {
                    if (CheckDirectionSpecifics(testDirectionCondition[j], relativeDir))
                    {
                        testResults += " " + testDirectionCondition[j] + " ";
                    }
                }
                testResults += ")";
            }
        }
        Debug.Log(testResults);
    }

    [ContextMenu("Check Directions")]
    public void CheckDirections()
    {
        for (int i = 0; i < 6; i++)
        {
            Debug.Log(CheckRelativeDirections(testDir, i));
        }
    }
}
