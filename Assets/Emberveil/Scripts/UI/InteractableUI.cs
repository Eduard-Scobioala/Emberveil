using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractableUI : MonoBehaviour
{
    public GameObject interactionPopUp;
    public GameObject itemPopUp;

    public TMP_Text interactableInfoText;
    public TMP_Text itemInfoText;
    public Image itemImage;

    public void EnableInteractionPopUpGameObject(bool isEnabled)
    {
        interactionPopUp.SetActive(isEnabled);
    }

    public void EnableItemPopUpGameObject(bool isEnabled)
    {
        itemPopUp.SetActive(isEnabled);
    }
}
