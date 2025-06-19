using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSourcePrefab; // A prefab for our pooled SFX sources

    [Header("Sound Settings")]
    [SerializeField] private int sfxPoolSize = 10;

    private readonly List<AudioSource> sfxPool = new ();
    private int sfxPoolIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize the SFX AudioSource pool
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource source = Instantiate(sfxSourcePrefab, transform);
            source.gameObject.SetActive(false); // Start disabled
            sfxPool.Add(source);
        }
    }

    public void PlayMusic(SoundSO sound)
    {
        if (sound == null)
        {
            Debug.LogWarning("PlayMusic called with a null SoundSO.");
            musicSource.Stop();
            return;
        }
        sound.Play(musicSource);
    }

    public void PlaySFX(SoundSO sound)
    {
        if (sound == null)
        {
            Debug.LogWarning("PlaySFX called with a null SoundSO.");
            return;
        }

        AudioSource source = GetAvailableSfxSource();
        source.transform.position = Vector3.zero; // For 2D sounds
        sound.Play(source);
    }

    public void PlaySFXAtPoint(SoundSO sound, Vector3 position)
    {
        if (sound == null)
        {
            Debug.LogWarning("PlaySFXAtPoint called with a null SoundSO.");
            return;
        }

        AudioSource source = GetAvailableSfxSource();

        // Position it in the world for 3D sound
        source.transform.position = position;

        sound.Play(source);
    }

    private AudioSource GetAvailableSfxSource()
    {
        // Cycle through the pool
        AudioSource source = sfxPool[sfxPoolIndex];
        sfxPoolIndex = (sfxPoolIndex + 1) % sfxPoolSize;

        // If the source is still playing something, stop it first.
        if (source.isPlaying)
        {
            source.Stop();
        }

        // Re-enable it if it was disabled
        if (!source.gameObject.activeSelf)
        {
            source.gameObject.SetActive(true);
        }

        return source;
    }
}
