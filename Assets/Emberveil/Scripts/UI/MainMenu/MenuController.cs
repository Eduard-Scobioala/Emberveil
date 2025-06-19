using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuController : MonoBehaviour
{
    [Header("Levels To Load")]
    public string _newGameLevel;
    private string levelToLoad;

    [Header ("Volume Setting")]
    [SerializeField] private TMP_Text volumeTextValue = null;
    [SerializeField] private Slider volumeSlider = null;
    [SerializeField] private float defaultVolume = 0.5f;

    [SerializeField] private GameObject noSavedGameDialog = null;
    [SerializeField] private GameObject comfirmationPrompt = null;
    [SerializeField] private float comfirmationPromptShowTime = 1f;

    public void StartNewGame()
    {
        // Delete the old save file.
        SaveLoadManager.Instance.DeleteSaveFile();

        // Starting a NEW game, so it shouldn't auto-load.
        SaveLoadManager.Instance.ShouldSaveOnStart = false;

        SceneManager.LoadScene(_newGameLevel);
    }

    public void LoadGame()
    {
        if (SaveLoadManager.Instance.HasSaveFile())
        {
            // We want to load the game when the scene starts.
            SaveLoadManager.Instance.ShouldSaveOnStart = true;

            // Load the first level. The GameLoader will handle the rest.
            SceneManager.LoadScene(_newGameLevel);
        }
        else
        {
            noSavedGameDialog.SetActive(true);
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        volumeTextValue.text = FloatToVolumeString(volume);
    }

    public void ResetButton(string menuType) {

        if (menuType == "Audio") {
            AudioListener.volume = defaultVolume;
            volumeSlider.value = defaultVolume;
            volumeTextValue.text = FloatToVolumeString(defaultVolume);
            VolumeApply();
        }
    }

    public void VolumeApply()
    {
        PlayerPrefs.SetFloat("masterVolume", AudioListener.volume);
        StartCoroutine(ConfirmationBox());
    }

    public IEnumerator ConfirmationBox()
    {
        comfirmationPrompt.SetActive(true);
        yield return new WaitForSeconds(comfirmationPromptShowTime);
        comfirmationPrompt.SetActive(false);
    }

    private string FloatToVolumeString(float volume) {
        return (volume * 100).ToString("0");
    }
}