using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Helper class to map a body part name to its SkinnedMeshRenderer in the Inspector.
[System.Serializable]
public class BodyPart
{
    public string partName; // e.g., "Torso", "Hips", "Head"
    public SkinnedMeshRenderer meshRenderer;
}

public class PlayerEquipmentManager : MonoBehaviour
{
    [Header("Body Part Renderers")]
    [Tooltip("Add all SkinnedMeshRenderers for your character's swappable parts here.")]
    [SerializeField] private List<BodyPart> bodyParts = new ();

    [Header("Default Meshes")]
    [Tooltip("Set the default 'naked' meshes for each body part.")]
    [SerializeField] private List<ArmorMeshData> defaultMeshes = new ();

    [Header("Head & Helmet Setup")]
    [Tooltip("The GameObject for the default head (with hair, etc.). This will be hidden when a helmet is on.")]
    [SerializeField] private GameObject defaultHeadObject;
    [Tooltip("The GameObject that will display helmets. It should have its own SkinnedMeshRenderer.")]
    [SerializeField] private SkinnedMeshRenderer helmetRenderer;

    // Dictionaries for fast lookups at runtime.
    private Dictionary<string, SkinnedMeshRenderer> bodyPartDictionary = new ();
    private Dictionary<string, Mesh> defaultMeshDictionary = new ();

    private void Awake()
    {
        // Populate the body part dictionary for quick access.
        foreach (var part in bodyParts)
        {
            if (part.meshRenderer != null && !string.IsNullOrEmpty(part.partName))
            {
                bodyPartDictionary[part.partName] = part.meshRenderer;
            }
        }

        // Populate the default mesh dictionary.
        foreach (var defaultMeshData in defaultMeshes)
        {
            if (defaultMeshData.mesh != null && !string.IsNullOrEmpty(defaultMeshData.targetBodyPartName))
            {
                defaultMeshDictionary[defaultMeshData.targetBodyPartName] = defaultMeshData.mesh;
            }
        }
    }

    public void EquipArmor(ArmorItem headItem, ArmorItem bodyItem, ArmorItem handItem, ArmorItem legItem)
    {
        // Handle Head/Helmet Swapping
        if (headItem != null)
        {
            // A helmet is equipped.
            defaultHeadObject.SetActive(false);
            helmetRenderer.gameObject.SetActive(true);

            // Find the mesh data intended for the 'Head' part in the ArmorItem.
            ArmorMeshData headMeshData = headItem.armorMeshes.FirstOrDefault(data => data.targetBodyPartName == "Head");
            if (headMeshData != null && headMeshData.mesh != null)
            {
                helmetRenderer.sharedMesh = headMeshData.mesh;
            }
            else
            {
                // If the head item has no mesh for the 'Head', hide the helmet renderer.
                helmetRenderer.sharedMesh = null;
            }
        }
        else
        {
            // No helmet is equipped. Show the default head.
            defaultHeadObject.SetActive(true);
            helmetRenderer.gameObject.SetActive(false);
        }

        // Collect all the mesh data from the equipped armor items.
        List<ArmorMeshData> equippedMeshes = new List<ArmorMeshData>();
        if (bodyItem != null) equippedMeshes.AddRange(bodyItem.armorMeshes);
        if (handItem != null) equippedMeshes.AddRange(handItem.armorMeshes);
        if (legItem != null) equippedMeshes.AddRange(legItem.armorMeshes);

        // Create a dictionary of the equipped meshes for easy lookup.
        var equippedMeshMap = equippedMeshes.ToDictionary(data => data.targetBodyPartName, data => data.mesh);

        // Iterate through all known body parts on the character.
        foreach (var part in bodyParts)
        {
            Mesh meshToApply = null;

            // Check if any equipped armor provides a mesh for this body part.
            if (equippedMeshMap.TryGetValue(part.partName, out Mesh equippedMesh))
            {
                meshToApply = equippedMesh;
            }
            else
            {
                // If no equipped armor provides a mesh, use the default mesh for this part.
                defaultMeshDictionary.TryGetValue(part.partName, out meshToApply);
            }

            // Apply the chosen mesh to the SkinnedMeshRenderer.
            part.meshRenderer.sharedMesh = meshToApply;
        }
    }
}