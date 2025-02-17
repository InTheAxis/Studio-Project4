﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerInfo
{
    /* UI */
    public Transform self = null;
    public Transform barHolder = null;
    public TextMeshProUGUI tmName = null;
    public TextMeshProUGUI tmNumberCounter = null;

    public string name = "";
    public int number = 0;
    public int health = -1;
    public bool invulnerable = false;

    public PlayerInfo(Transform self)
    {
        this.self = self;
        tmName = self.Find("Name").GetComponent<TextMeshProUGUI>();
        name = tmName.text;
        tmNumberCounter = self.Find("Number").Find("Counter").GetComponent<TextMeshProUGUI>();
        number = int.Parse(tmNumberCounter.text);
        barHolder = self.Find("Healthbars");
    }
    public PlayerInfo(Transform self, string name)
    {
        this.self = self;
        tmName = self.Find("Name").GetComponent<TextMeshProUGUI>();
        this.name = name;
        tmName.text = name;
        tmNumberCounter = self.Find("Number").Find("Counter").GetComponent<TextMeshProUGUI>();
        number = int.Parse(tmNumberCounter.text);
        barHolder = self.Find("Healthbars");
    }

    public void setHealth(int health, bool invulnerable)
    {
        this.health = health;
        this.invulnerable = invulnerable;

        if (invulnerable)
        {
            for (int i = 0; i < 3; ++i)
                barHolder.GetChild(i).gameObject.SetActive(false);
            barHolder.GetChild(3).gameObject.SetActive(true);
        }
        else
        {
            for(int i = 0; i < 3; ++i)
            {
                if(i < health)
                    barHolder.GetChild(i).gameObject.SetActive(true);
                else
                    barHolder.GetChild(i).gameObject.SetActive(false);
            }
            barHolder.GetChild(3).gameObject.SetActive(false);
        }
    }

    public void setName(string name)
    {
        this.name = name;
        tmName.text = name;
    }

    public void setNumber(int number)
    {
        this.number = number;
        tmNumberCounter.text = number.ToString();
    }

    public void setAll(int number, string name, int health, bool invulnerable)
    {
        setNumber(number);
        setName(name);
        setHealth(health, invulnerable);
    }

    public void setActive(bool isActive)
    {
        self.gameObject.SetActive(isActive);
    }
}
