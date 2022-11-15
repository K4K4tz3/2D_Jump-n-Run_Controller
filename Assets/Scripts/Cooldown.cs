using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Cooldown : MonoBehaviour
{
    // Component's
    private Image image;
    private GameObject player;

    private float startTime;
    private float cooldownTime;
    private (string name, bool state) attack = ("attackCooldown", true);

    private void Awake()
    {
        image = GetComponent<Image>();
        player = GameObject.FindGameObjectWithTag("Player");
    }
    private void Start()
    {
        player.SendMessage("SetCooldown", attack);
    }
    private void FixedUpdate()
    {
        FillImage();
    }
    private void SetCooldownTime(float cT)
    {
        cooldownTime = cT;
    }
    private void ResetCooldown()
    {
        image.fillAmount = 0;
    }
    private void FillImage()
    {
        if(image.fillAmount < 1)
        {
            image.fillAmount += ( 1 / cooldownTime) * Time.deltaTime;
        }
        if (image.fillAmount > 0.999f) player.SendMessage("SetCooldown", attack);
    }
}
