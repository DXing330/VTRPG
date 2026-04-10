using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActorSpriteAndHP : ActorSprite
{
    public TMP_Text actorHealth;
    public TMP_Text actorAttack;
    public TMP_Text acctorDefense;
    public GameObject healthBar;
    public float healthBarYScale = 0.7f;


    public override void ShowActorInfo(TacticActor actor)
    {
        ShowActorSprite(actor);
        actorSprite.sprite = actorSprites.GetSprite(actor.GetSpriteName());
        int cHP = actor.GetHealth();
        int mHP = actor.GetBaseHealth();
        actorHealth.text = cHP+" / "+mHP;
        healthBar.transform.localScale = new Vector3(Mathf.Min(1, (float)cHP/(float)mHP),healthBarYScale,0);
        actorAttack.text = actor.GetBaseAttack().ToString();
        acctorDefense.text = actor.GetBaseDefense().ToString();
    }
}
