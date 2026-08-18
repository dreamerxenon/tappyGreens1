using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Made by Rajendra Abhinaya, 2023

namespace DiabolicalGames
{
    public class Despawn : MonoBehaviour
    {
        private int despawnPercentage;
        private float despawnTime;
        private float distanceFromPlayer;
        private GameObject player;
        private AudioClip clip;
        private float volume;
        private float variation;
        private float volumeVariation;
        private float pitchVariation;
        private AudioSource audioSource;

        //Used to receive the variables' values from the parent object
        public void SetVariables(int despawnPercentage, float despawnTime, float distanceFromPlayer, AudioClip clip, float volumeVariation, float pitchVariation)
        {
            this.despawnPercentage = despawnPercentage;
            this.despawnTime = despawnTime;
            this.distanceFromPlayer = distanceFromPlayer;
            this.clip = clip;
            this.volumeVariation = volumeVariation;
            this.pitchVariation = pitchVariation;

            // Play immediately once we have everything we need, instead of
            // relying on Start()'s implicit "runs after SetActive(true)" timing.
            PlayBreakSound();
        }

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void PlayBreakSound()
        {
            if (audioSource == null || clip == null) return;

            audioSource.pitch = 1f + Random.Range(-pitchVariation / 2f, pitchVariation / 2f);

            // Default to full volume (1f) if the player has never opened the
            // settings menu / the pref hasn't been written yet on this device -
            // previously this defaulted to 0 and played the clip silently.
            volume = PlayerPrefs.GetFloat("effectsVolume", 1f);

            float finalVolume = Mathf.Clamp01(volume + Random.Range(-volumeVariation, volumeVariation));

            audioSource.PlayOneShot(clip, finalVolume);
        }

        //Starts the selected despawn mode's coroutine function




    }
}