using System;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public Transform lockOnTransform;
    public event Action OnDeath;

    protected void RaiseDeath()
    {
        OnDeath?.Invoke();
    }
}
