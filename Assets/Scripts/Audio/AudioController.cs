﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Audio;

public class AudioController : MonoBehaviour
{
    [SerializeField]
    protected List<Sound> sounds = new List<Sound>();

    protected AudioSource[] source;
    private bool mixerSet = false;

    public void SetSFX()
    {
        foreach (AudioSource audio in source)
        {
            audio.outputAudioMixerGroup = AudioInstance.instance.sfxMixer;
        }
        mixerSet = true;
    }

    public void SetMusic()
    {
        foreach (AudioSource audio in source)
        {
            audio.outputAudioMixerGroup = AudioInstance.instance.musicMixer;
        }
        mixerSet = true;
    }

    public enum AudioState
    {
        Play,
        Pause,
        Resume,
        Stop,
    }

    public void Add(Sound sound)
    {
        sounds.Add(sound);
    }

    protected virtual void Awake()
    {
        source = GetComponents<AudioSource>();
        if (source.Length == 0)
            Debug.LogError("No audio source found on: " + gameObject.name);

        if (sounds.Count > 0)
        {
            foreach (Sound s in sounds)
            {
                //s.source = gameObject.AddComponent<AudioSource>();
                //s.source.clip = s.clip;
                //s.source.volume = s.volume;
                //s.source.pitch = s.pitch;
                //s.source.loop = s.loop;

                //if (s.sfx)
                //    sfxSounds.Add(s);
                //else
                //    musicSounds.Add(s);

                if (s.playOnAwake)
                    Play(s.clip.name);
            }
        }
    }

    private void Update()
    {
        if (!mixerSet)
            Debug.LogError("Mixer not set: " + gameObject.name);
        SetSFX();
    }

    public void Stop(int index = 0)
    {
        source[index].Stop();
    }

    public void SetAudio(string name, AudioState state, int index = 0)
    {
        Sound s = sounds.Find(sound => sound.clip?.name == name);
        if (s == null)
        {
            Debug.LogError("[Audio] " + name + " sound does not exist!");
            return;
        }

        if (index >= source.Length)
        {
            Debug.LogError("index out of range (Index=" + index + ")");
            return;
        }

        source[index].clip = s.clip;
        source[index].clip = s.clip;
        source[index].volume = s.volume;
        source[index].pitch = s.pitch;
        source[index].loop = s.loop;
        switch (state)
        {
            case AudioState.Play:
                source[index].Play();
                break;

            case AudioState.Pause:
                source[index].Pause();
                break;

            case AudioState.Resume:
                source[index].UnPause();
                break;

            case AudioState.Stop:
                source[index].Stop();
                break;
        }
    }

    public void SetVol(float vol, int index = 0)
    {
        source[index].volume = vol;
    }

    public float GetVol(int index = 0)
    {
        return source[index].volume;
    }

    public void Play(string name, int index = 0)
    {
        SetAudio(name, AudioState.Play, index);
    }

    public void PlayIndex(int audioIndex, int index = 0)
    {
        SetAudio(sounds[audioIndex].clip.name, AudioState.Play, index);
    }
}