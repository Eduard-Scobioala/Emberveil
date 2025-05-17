using UnityEngine;
using UnityEngine.UI;

public class StatUIBar : MonoBehaviour
{
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public void SetMaxSliderValue(float maxHealth)
    {
        slider.maxValue = maxHealth;
        slider.value = maxHealth;
    }

    public void SetCurrentStatValue(float currentStatValue)
    {
        slider.value = currentStatValue;
    }
}
