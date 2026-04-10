using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavedState : SavedData
{
    public string delimiterTwo = "|";
    public SceneTracker sceneTracker;
    public string previousScene;
    public virtual void UpdatePreviousScene() { previousScene = sceneTracker.GetPreviousScene(); }
    public virtual void SetPreviousScene(string sceneName) { previousScene = sceneName; }
    public string GetPreviousScene(){ return previousScene; }
}
