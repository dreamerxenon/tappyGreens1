using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Made by Rajendra Abhinaya, 2023

namespace DiabolicalGames{
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
    public void SetVariables(int despawnPercentage, float despawnTime, float distanceFromPlayer,  AudioClip clip,  float volumeVariation, float pitchVariation){
        this.despawnPercentage = despawnPercentage;
        this.despawnTime = despawnTime;
        this.distanceFromPlayer = distanceFromPlayer;
        this.clip = clip;
        this.volumeVariation = volumeVariation;
        this.pitchVariation = pitchVariation;
    }

    void Start(){
        //Plays a random audio clip from the list of audio clips set in the object
        audioSource = GetComponent<AudioSource>();
        audioSource.pitch = 1f + Random.Range(-pitchVariation/2, pitchVariation/2);
        volume = PlayerPrefs.GetFloat("effectsVolume");
        audioSource.PlayOneShot(clip, volume + Random.Range(-volumeVariation, volumeVariation));
    }

    //Starts the selected despawn mode's coroutine function
  

    

   
}
}