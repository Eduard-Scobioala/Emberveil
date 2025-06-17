using UnityEngine;
using System;

// This component should be placed on any GameObject in the scene that needs a unique, persistent ID.
[ExecuteInEditMode]
public class SavableEntity : MonoBehaviour
{
    [SerializeField] private string uniqueIdentifier = "";

    public string GetUniqueIdentifier() => uniqueIdentifier;

    private void GenerateID()
    {
        uniqueIdentifier = Guid.NewGuid().ToString();
    }

    // This ensures every SavableEntity placed in the scene gets an ID automatically.
#if UNITY_EDITOR
    private void Update()
    {
        if (Application.isPlaying) return;

        // Ensure the scene path is valid (prevents errors with prefabs)
        if (string.IsNullOrEmpty(gameObject.scene.path)) return;

        // If the ID is empty, generate one.
        if (string.IsNullOrEmpty(uniqueIdentifier))
        {
            GenerateID();
            // Mark the object as "dirty" so Unity knows to save the change.
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}
