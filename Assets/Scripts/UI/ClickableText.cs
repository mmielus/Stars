﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;
using UnityEngine.UI;
using UnityEngine.EventSystems;
//used to give a player feedback that the text is clickable
public class ClickableText : MonoBehaviour 
    , IPointerEnterHandler
    , IPointerExitHandler
    , IPointerUpHandler
{
    private AudioSource sound;
    private Shadow shadow;
    // Use this for initialization
    void Start () {
        sound = gameObject.GetComponent<AudioSource>();
        shadow = gameObject.GetComponent<Shadow>();

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        GetComponent<Transform>().localScale = new Vector3(1.05f, 1.1f, 1.05f);
        shadow.effectDistance = new Vector2(0,0);
   
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetComponent<Transform>().localScale = new Vector3(1, 1, 1);
        shadow.effectDistance = new Vector2(3, -3);

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        sound.Play();
    }
}
