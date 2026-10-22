using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    public bool starting = true;
    public GameObject thisObject;
    public void EnableLoading()
    {
        thisObject.SetActive(true);
    }
    public Image loadingScreen;
    public List<Color> fadeColors;
    public int fadeFrames = 6;
    public float totalFadeTime = 1.0f;
    protected virtual void Start()
    {
        if (starting)
        {
            StartCoroutine(FadeFromBlack());
        }
    }
    public void SetTransparent()
    {
        int i = 0;
        loadingScreen.color = new Color(fadeColors[i].r,fadeColors[i].g,fadeColors[i].b,fadeColors[i].a);
    }
    IEnumerator FadeToBlack()
    {
        thisObject.SetActive(true);
        float fadeTime = totalFadeTime / fadeFrames;
        for (int i = 0; i < fadeFrames; i++)
        {
            loadingScreen.color = new Color(fadeColors[i].r,fadeColors[i].g,fadeColors[i].b,fadeColors[i].a);
            yield return new WaitForSeconds(fadeTime);
        }
        thisObject.SetActive(false);
    }
    IEnumerator FadeFromBlack()
    {
        thisObject.SetActive(true);
        float fadeTime = totalFadeTime / fadeFrames;
        for (int i = fadeFrames - 1; i >= 0; i--)
        {
            loadingScreen.color = new Color(fadeColors[i].r,fadeColors[i].g,fadeColors[i].b,fadeColors[i].a);
            yield return new WaitForSeconds(fadeTime);
        }
        thisObject.SetActive(false);
    }
    public void StartLoadingScreen()
    {
        StartCoroutine(FadeToBlack());
    }
    public void FinishLoadingScreen()
    {
        StartCoroutine(FadeFromBlack());
    }
}
