using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleFormation : MonoBehaviour
{
    public GameObject go;
    public RectTransform mainUnit;
    public Transform mainPanel;
    public void SetMainPanel(Transform newMain)
    {
        mainPanel = newMain;
    }
    public List<GameObject> subGos;
    public List<RectTransform> subUnits;
    public float spreadDistance;
    public float minSpread = 0.2f;
    public float maxSpread = 0.4f;
    public float minSecondsTilFin = 2.0f;
    public float maxSecondsTilFin = 4.0f;
    public float secondsTilFin;
    public float oSecondsTilFin;
    public float xValue;
    public float yValue;

    public void Start()
    {
        GetStartPoint();
        GetSpread();
        GetFinTime();
        for (int i = 0; i < subUnits.Count; i++)
        {
            subUnits[i].SetParent(mainPanel);
        }
    }

    public void Update()
    {
        if (secondsTilFin <= 0)
        {
            for (int i = 0; i < subGos.Count; i++)
            {
                Destroy(subGos[i]);
            }
            Destroy(go);
            return;
        }
        secondsTilFin -= Time.deltaTime;
        float angle = 2f*Mathf.PI/(subUnits.Count);
        float spread = Time.deltaTime * (spreadDistance/oSecondsTilFin);
        for (int i = 0; i < subUnits.Count; i++)
        {
            // Radians.
            float nAngle = i*angle;
            float xChange = Mathf.Cos(nAngle) * spread;
            float yChange = Mathf.Sin(nAngle) * spread;
            subUnits[i].pivot = new Vector3(subUnits[i].pivot.x + xChange, subUnits[i].pivot.y + yChange, 0);
        }
    }

    protected void GetStartPoint()
    {
        xValue = Random.Range(0f, 1f);
        yValue = Random.Range(0f, 1f);
        mainUnit.pivot = new Vector3(xValue, yValue, 0);
    }

    protected void GetSpread()
    {
        spreadDistance = Random.Range(minSpread, maxSpread);
    }

    protected void GetFinTime()
    {
        secondsTilFin = Random.Range(minSecondsTilFin,maxSecondsTilFin);
        oSecondsTilFin = secondsTilFin;
    }
}
