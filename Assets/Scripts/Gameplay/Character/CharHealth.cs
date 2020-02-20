﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class CharHealth : MonoBehaviour, IPunObservable
{
    public CharTPController charControl;
    public int maxHp = 3;
    public float autoRespawnTime = 3; //set to negative if dont want auto
    public float respawnInvulTime = 1; //default invul time when respawn
    
    public bool dead { private set; get; }
    
    private int hp;
    private bool invulnerable;
    private IEnumerator invulCorr;
    private IEnumerator respawnCorr;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(hp);
            stream.SendNext(invulnerable);
            stream.SendNext(dead);
        }
        else 
        {
            hp =            (int)stream.ReceiveNext();
            invulnerable =  (bool)stream.ReceiveNext();
            bool nextDead = (bool)stream.ReceiveNext();
            if (dead && !nextDead)
            {
                Respawn();
                dead = nextDead;
            }
            else if (!dead && nextDead)
            {
                Die();
                dead = nextDead;
            }
        }
    }


    private void Start()
    {
        invulCorr = respawnCorr = null;
        Respawn();
    }

    private void Update()
    {
        //TEMP ,DO THIS ELSEWHERE
        if (Input.GetKeyDown(KeyCode.R))
            Respawn(1);
    }

    public void Respawn(float invulTime)
    {
        if (respawnCorr != null)
            StopCoroutine(respawnCorr);
        respawnCorr = null;
        hp = maxHp;
        dead = false;
        charControl.disableMovement = dead;
        SetInvulnerableTime(invulTime);
    }
    public void Respawn()
    {
        Respawn(respawnInvulTime);
    }

    public void TakeDmg(int dmg)
    {
        if (dead || invulnerable)
            return;

        hp -= dmg;

        if (hp <= 0)
        {
            hp = 0;
            dead = true;
            Die();
        }

        Debug.Log("Took dmg, hp is " + hp);
    }

    private void Die()
    {
        charControl.disableMovement = dead;

        if (autoRespawnTime > 0)
        {
            if (respawnCorr != null)
                StopCoroutine(respawnCorr);
            respawnCorr = AutoRespawn(autoRespawnTime);
            StartCoroutine(respawnCorr);
        }

        Debug.Log("Dieded");
    } 

    public void SetInvulnerableTime(float dura) 
    {
        if (invulCorr != null)
            StopCoroutine(invulCorr);
        invulCorr = InvulTimeCor(dura);
        StartCoroutine(invulCorr);
    }

    public void SetInvulnerable(bool b)
    {
        invulnerable = b;
    }


    private IEnumerator InvulTimeCor(float dura)
    {
        invulnerable = true;
        yield return new WaitForSeconds(dura);
        invulnerable = false;
    }

    private IEnumerator AutoRespawn(float dura)
    {
        yield return new WaitForSeconds(dura);
        Respawn();
    }
}