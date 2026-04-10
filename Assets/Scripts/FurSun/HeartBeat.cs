using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartBeat : MonoBehaviour
{
    public Transform mainPanel;
    public List<RectTransform> leftUpperCircle;
    public List<RectTransform> rightUpperCircle;
    public List<RectTransform> leftSide;
    public List<RectTransform> rightSide;
    public bool expand = true;
    public float baseInterval = 1f;
    public float currentInterval;
    public float change = 0.2f;

    public void Start()
    {
        for (int i = 0; i < leftUpperCircle.Count; i++)
        {
            leftUpperCircle[i].SetParent(mainPanel);
        }
        for (int i = 0; i < rightUpperCircle.Count; i++)
        {
            rightUpperCircle[i].SetParent(mainPanel);
        }
        for (int i = 0; i < leftSide.Count; i++)
        {
            leftSide[i].SetParent(mainPanel);
        }
        for (int i = 0; i < rightSide.Count; i++)
        {
            rightSide[i].SetParent(mainPanel);
        }
        currentInterval = baseInterval;
    }

    public void Update()
    {
        if (currentInterval <= 0)
        {
            expand = !expand;
            currentInterval = baseInterval;
        }
        currentInterval -= Time.deltaTime;
        float nChange = change;
        if (!expand){nChange = -nChange;}
        float spread = Time.deltaTime * (nChange/baseInterval);
        // Left circle is from pi - 0, 180-0 degrees.
        for (int i = 0; i < leftUpperCircle.Count; i++)
        {
            // Radians.
            float nAngle = Mathf.PI - (i*(Mathf.PI/(leftUpperCircle.Count - 1)));
            float xChange = Mathf.Cos(nAngle) * spread;
            float yChange = Mathf.Sin(nAngle) * spread;
            leftUpperCircle[i].pivot = new Vector3(leftUpperCircle[i].pivot.x + xChange, leftUpperCircle[i].pivot.y + yChange, 0);
        }
        // Right circle could be the same.
        // More fun to make it from 0 - pi, 0-180 degrees.
        for (int i = 0; i < rightUpperCircle.Count; i++)
        {
            // Radians.
            float nAngle = 0f + (i*(Mathf.PI/(rightUpperCircle.Count - 1)));
            float xChange = Mathf.Cos(nAngle) * spread;
            float yChange = Mathf.Sin(nAngle) * spread;
            rightUpperCircle[i].pivot = new Vector3(rightUpperCircle[i].pivot.x + xChange, rightUpperCircle[i].pivot.y + yChange, 0);
        }
        // Left side is 225 degrees 5*PI/4.
        for (int i = 0; i < leftSide.Count; i++)
        {
            // Radians.
            float nAngle = 5f*Mathf.PI/4f;
            float xChange = Mathf.Cos(nAngle) * spread;
            float yChange = Mathf.Sin(nAngle) * spread;
            leftSide[i].pivot = new Vector3(leftSide[i].pivot.x + xChange, leftSide[i].pivot.y + yChange, 0);
        }
        // Right side is 315 degrees 7*PI/4.
        for (int i = 0; i < rightSide.Count; i++)
        {
            // Radians.
            float nAngle = 7f*Mathf.PI/4f;
            float xChange = Mathf.Cos(nAngle) * spread;
            float yChange = Mathf.Sin(nAngle) * spread;
            rightSide[i].pivot = new Vector3(rightSide[i].pivot.x + xChange, rightSide[i].pivot.y + yChange, 0);
        }
    }
}
