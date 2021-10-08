using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{

    public GameObject soundPrefab;

    public static AudioManager s;

    public AudioClip bossMusic, defaultMusic;

    AudioSource musicSource;

    private void Awake()
    {
        s = this;
        musicSource = GetComponentInChildren<AudioSource>();
    }

    public void PlaySound(AudioClip sound, Vector2 pos)
    {
        if (sound == null)
            return;

        try
        {
            Vector3 soundPos = new Vector3(0, 0, -10) + (Vector3)pos;

            GameObject spawnedSound = Instantiate(soundPrefab, soundPos, Quaternion.identity, transform);
            AudioSource audioSource = spawnedSound.GetComponent<AudioSource>();
            audioSource.clip = sound;
            audioSource.Play();
        }
        catch
        {
        }
        
    }

    public void PlaySound(AudioClip sound)
    {
        PlaySound(sound, Vector2.zero);
    }

    public void PlayBossMusic()
    {
        if (musicSource.clip != bossMusic)
        {
            musicSource.clip = bossMusic;
            musicSource.Play();
        }
    }

    public void PlayDefaultMusic()
    {
        if (musicSource.clip != default)
        {
            musicSource.clip = defaultMusic;
            musicSource.Play();
        }
    }
}
