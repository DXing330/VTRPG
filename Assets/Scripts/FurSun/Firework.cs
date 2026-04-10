using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// More like a bubble.
public class Firework : MonoBehaviour
{
    // Starts at a random x, goes up a random y based on screen width.
    public GameObject initialFirework;
    public RectTransform fPosition;
    public bool up = true;
    // Shoot out in six directions and fizzle out.
    // Fizzles means that the size decreases.
    public List<GameObject> fireworkSpread;
    public float explodeDistance;
    // secondsTilMaxHeight is a Random.Range(1.0f, 3.0f);
    public float minSecondsTilMaxHeight = 2.0f;
    public float maxSecondsTilMaxHeight = 4.0f;
    public float secondsTilMaxHeight;
    public float yChange;
    // Random.Range(0.5f, 1.0f);
    public float maxHeight;
    // Random.Range(0.0f, 1.0f);
    public float xValue;

    public void Explode()
    {

    }

    public void Start()
    {
        DetermineStartingX();
        DetermineMaxY();
        DetermineExplodeDistance();
        DetermineSeconds();
        if (up){fPosition.pivot = new Vector3(xValue, 0, 0);}
        else{fPosition.pivot = new Vector3(xValue, 1, 0);}
    }

    public void Update()
    {
        if (fPosition.pivot.y >= maxHeight && up)
        {
            Destroy(initialFirework);
            return;
        }
        else if (fPosition.pivot.y <= 0 && !up)
        {
            Destroy(initialFirework);
            return;
        }
        yChange = Time.deltaTime * (maxHeight/secondsTilMaxHeight);
        float xChange = explodeDistance * Mathf.Sin(2f*Mathf.PI*fPosition.pivot.y/maxHeight);
        if (up)
        {
            fPosition.pivot = new Vector3(xValue+xChange, fPosition.pivot.y + yChange, 0);
        }
        else
        {
            fPosition.pivot = new Vector3(xValue+xChange, fPosition.pivot.y - yChange, 0);
        }
    }

    protected void DetermineSeconds()
    {
        secondsTilMaxHeight = Random.Range(minSecondsTilMaxHeight, maxSecondsTilMaxHeight);
    }

    protected void DetermineStartingX()
    {
        xValue = Random.Range(0.0f, 1.0f);
    }

    protected void DetermineMaxY()
    {
        maxHeight = Random.Range(0.5f, 1.0f);
    }

    protected void DetermineExplodeDistance()
    {
        explodeDistance = Random.Range(0.1f, 0.2f);
    }
}