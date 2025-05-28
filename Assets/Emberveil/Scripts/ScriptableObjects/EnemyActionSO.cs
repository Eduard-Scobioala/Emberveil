using UnityEngine;

public abstract class EnemyActionSO : ScriptableObject
{
    [Header("Base Action Info")]
    public string actionName = "Unnamed Action";
    public string animationName;
    public float recoveryTime = 1.0f;
    public float staminaCost = 0f;

    // TODO:
    // public AudioClip[] actionSounds;
    // public GameObject[] actionVFX;
}
