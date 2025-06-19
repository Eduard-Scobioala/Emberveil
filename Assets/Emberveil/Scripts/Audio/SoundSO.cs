using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Audio/Sound Event")]
public class SoundSO : ScriptableObject
{
    [Header("Sound Properties")]
    public List<AudioClip> clips = new List<AudioClip>();

    [Range(0f, 1f)]
    public float volume = 0.7f;
    [Range(0f, 2f)]
    public float volumeRandomness = 0.1f;

    [Range(0.1f, 3f)]
    public float pitch = 1f;
    [Range(0f, 1f)]
    public float pitchRandomness = 0.1f;

    public bool loop = false;

    public void Play(AudioSource source)
    {
        if (clips.Count == 0)
        {
            Debug.LogWarning($"Sound Event '{name}' has no audio clips assigned.");
            return;
        }

        // --- Select a random clip from the list ---
        AudioClip clipToPlay = clips[Random.Range(0, clips.Count)];

        // --- Configure the AudioSource ---
        source.clip = clipToPlay;
        source.volume = volume * (1f + Random.Range(-volumeRandomness, volumeRandomness));
        source.pitch = pitch * (1f + Random.Range(-pitchRandomness, pitchRandomness));
        source.loop = loop;

        // --- Play the sound ---
        source.Play();
    }
}
