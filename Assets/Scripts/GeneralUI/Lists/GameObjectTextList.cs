using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectTextList : MonoBehaviour
{
    public GameObject thisList;
    public void Enable()
    {
        thisList.SetActive(true);
    }
    public void Disable() { thisList.SetActive(false); }
    public int page = 0;
    public GeneralUtility utility;
    public List<GameObject> objects;
    public int GetListLength(){return objects.Count;}
    public List<string> data;
    public void SetData(List<string> newData)
    {
        data = new List<string>(newData);
    }
    [ContextMenu("Right")]
    public void ChangeRight(){ChangePage();}
    [ContextMenu("Left")]
    public void ChangeLeft(){ChangePage(false);}
    public virtual void ChangePage(bool right = true)
    {
        page = utility.ChangePage(page, right, objects, data);
        UpdateCurrentPage();
    }
    protected virtual void UpdateCurrentPage()
    {
        ResetPage();
        List<int> newPageIndices = utility.GetCurrentPageIndices(page, objects, data);
        for (int i = 0; i < newPageIndices.Count; i++)
        {
            objects[i].SetActive(true);
        }
    }
    protected virtual void ResetPage()
    {
        utility.DisableGameObjects(objects);
    }
}
