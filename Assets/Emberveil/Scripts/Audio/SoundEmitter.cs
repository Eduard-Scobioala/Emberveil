using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    // Plays a 2D sound effect (UI, etc.)
    public void PlaySFX(SoundSO sound)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(sound);
        }
        else
        {
            Debug.LogWarning("AudioManager instance not found. Cannot play sound.");
        }
    }

    // Plays a 3D sound effect at this object's position (character footsteps, impacts, etc.)
    public void PlaySFXAtThisPosition(SoundSO sound)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFXAtPoint(sound, transform.position);
        }
        else
        {
            Debug.LogWarning("AudioManager instance not found. Cannot play sound.");
        }
    }
}
