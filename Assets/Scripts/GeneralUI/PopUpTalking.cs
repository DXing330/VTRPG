using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopUpTalking : MonoBehaviour
{
    public GameObject popUp;
    public float textSpeed;
    public bool talking = true;
    public SpriteContainer speakerSprites;
    public string testWords;
    public string words;
    public string currentWords;
    public string spriteName;
    public TMP_Text speakerNameTag;
    public TMP_Text speakersWords;
    public GameObject speakerImageObject;
    public Image speakerImage;

    [ContextMenu("Test Talking")]
    public void TestTalking()
    {
        StartTalking(testWords, "Helena", "Noble");
    }

    public void StartTalking(string newWords, string speakerName = "", string spriteName = "")
    {
        popUp.SetActive(true);
        talking = true;
        speakerNameTag.text  = speakerName;
        words = newWords;
        currentWords = "";
        speakersWords.text = currentWords;
        if (spriteName == "")
        {
            speakerImageObject.SetActive(false);
        }
        else
        {
            speakerImageObject.SetActive(true);
            speakerImage.sprite = speakerSprites.SpriteDictionary(spriteName);
        }
        StartCoroutine(Talking());
    }

    public void ClickPopUp()
    {
        if (talking)
        {
            talking = false;
            StopAllCoroutines();
            speakersWords.text = words;
        }
        else
        {
            popUp.SetActive(false);
        }
    }

    IEnumerator Talking()
    {
        currentWords = "";
        for (int i = 0; i < words.Length; i++)
        {
            currentWords += words[i];
            speakersWords.text = currentWords;
            yield return new WaitForSeconds(textSpeed);
        }
        talking = false;
    }
}
