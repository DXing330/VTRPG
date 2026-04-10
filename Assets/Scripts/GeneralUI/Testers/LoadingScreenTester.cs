using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LoadingScreenTester : MonoBehaviour
{
    public LoadingScreen loadingScreen;
    public TMP_Text testText;

    public void TestLoadingScreen()
    {
        StartCoroutine(TestingCoroutine());
    }

    IEnumerator TestingCoroutine()
    {
        for (int i = 0; i < 3; i++)
        {
            if (i == 0)
            {
                loadingScreen.StartLoadingScreen();
            }
            if (i == 1)
            {
                testText.text = Random.Range(0, 100).ToString();
            }
            if (i == 2)
            {
                loadingScreen.FinishLoadingScreen();
            }
            yield return new WaitForSeconds(loadingScreen.totalFadeTime);
        }
    }
}
