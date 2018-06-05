﻿using UnityEngine;
using UnityEngine.UI;
public class ShipIcon : MonoBehaviour
{
    private GameController gameController;
    private SpriteRenderer sprite;
    private SpriteRenderer shadow;

    public void Start()
    {
        gameController = GameObject.Find("GameController").GetComponent<GameController>();
        sprite = GetComponent<SpriteRenderer>();

    }

    void LateUpdate()
    {
        var target = Camera.main.transform.position;
        target.x = transform.position.x;
        transform.LookAt(target);
        var distanceToCamera = Vector3.Distance(transform.position, target) / 60f;
        transform.localScale = new Vector3(distanceToCamera, distanceToCamera, distanceToCamera);
        if (GetComponentInParent<Ownable>().GetOwner() == gameController.GetCurrentPlayer())
        {
            sprite.color = Color.white;

        } else sprite.color = new Color(0.75f, 0, 0);

        
    }
}