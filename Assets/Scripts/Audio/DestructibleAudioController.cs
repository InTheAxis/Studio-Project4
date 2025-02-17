﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructibleAudioController : MonoBehaviour
{
    private GameObject audioGO;
    private Sound sound;

    protected void Awake()
    {
        // init audio
        sound = new Sound();
        sound.clip = (AudioClip)Resources.Load("destruct1", typeof(AudioClip));
        sound.pitch = 1;
        sound.volume = 0.3f;
    }

    public void DestructAudio()
    {
        audioGO = Instantiate(new GameObject("BuildingAudio"), transform.position, Quaternion.identity);
        audioGO.AddComponent<AudioSource>();
        audioGO.AddComponent<AudioController>();
        audioGO.GetComponent<AudioController>().SetSFX();
        audioGO.GetComponent<AudioController>().Add(sound);
        audioGO.GetComponent<AudioController>().Play("destruct1");
    }
}