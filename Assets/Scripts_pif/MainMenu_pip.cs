using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenu_pip : MonoBehaviour
{
    [SerializeField]
    private GameObject fade;
    [SerializeField]
    private GameObject settingsMenu;
    [SerializeField]
    private GameObject controlsMenu;
    [SerializeField]
    private GameObject audioMenu;
    [SerializeField]
    private GameObject extrasMenu;
    [SerializeField]
    private AudioMixer mixer;
    [SerializeField]
    private Slider bgmVolSlider;
    [SerializeField]
    private Slider sfxVolSlider;
    [SerializeField]
    private Button startGameButton;
    [SerializeField]
    private Button controlsButton;
    [SerializeField]
    private Button controlsBackButton;
    [SerializeField]
    private Slider BGMSlider;
    [SerializeField]
    private Button extrasBackButton;
    [SerializeField]
    private bool isTitleScreen = true;
    private GameObject lastSelected;


    private CurrentScreen currentScreen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentScreen = CurrentScreen.Main;

        LoadPlayerPrefs();
        
        // Set initial selection based on whether this is the title screen
        if (!isTitleScreen)
        {
            controlsButton.Select();
        }
    }

    // Update is called once per frame
    void Update()
    {
        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current != null)
        {
            lastSelected = current;
        }
        else if (lastSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(lastSelected);
        }
    }

    private void LoadPlayerPrefs()
    {
        bgmVolSlider.value = PlayerPrefs.GetFloat("BGMVol", 0.7f);
        SetBGMVolume(bgmVolSlider.value);
        sfxVolSlider.value = PlayerPrefs.GetFloat("SFXVol", 0.7f);
        SetSFXVolume(sfxVolSlider.value);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void BackButton()
    {
        switch (currentScreen)
        {
            case CurrentScreen.Main:
                break;
            case CurrentScreen.Settings:
                CloseSettingsMenu();
                break;
            case CurrentScreen.Controls:
                CloseControlsMenu();
                break;
            case CurrentScreen.Audio:
                CloseAudioMenu();
                break;
            case CurrentScreen.Extras:
                CloseAudioMenu();
                break;
        }
    }

    public void OpenSettingsMenu()
    {
        fade.SetActive(true);
        settingsMenu.SetActive(true);
        currentScreen = CurrentScreen.Settings;
        controlsButton.Select();
    }

    public void CloseSettingsMenu()
    {
        // Do nothing if this is not the title screen
        if (!isTitleScreen)
            return;
            
        fade.SetActive(false);
        settingsMenu.SetActive(false);
        currentScreen = CurrentScreen.Main;
        startGameButton.Select();
    }
    public void OpenControlsMenu()
    {
        settingsMenu.SetActive(false);
        controlsMenu.SetActive(true);
        currentScreen = CurrentScreen.Controls;
        controlsBackButton.Select();
    }

    public void CloseControlsMenu()
    {
        settingsMenu.SetActive(true);
        controlsMenu.SetActive(false);
        currentScreen = CurrentScreen.Settings;
        controlsButton.Select();
    }
    public void OpenAudioMenu()
    {
        settingsMenu.SetActive(false);
        audioMenu.SetActive(true);
        currentScreen = CurrentScreen.Audio;
        BGMSlider.Select();
    }

    public void CloseAudioMenu()
    {
        settingsMenu.SetActive(true);
        audioMenu.SetActive(false);
        currentScreen = CurrentScreen.Settings;
        controlsButton.Select();
    }
    public void OpenExtrasMenu()
    {
        settingsMenu.SetActive(false);
        extrasMenu.SetActive(true);
        currentScreen = CurrentScreen.Audio;
        extrasBackButton.Select();
    }

    public void CloseExtrasMenu()
    {
        settingsMenu.SetActive(true);
        extrasMenu.SetActive(false);
        currentScreen = CurrentScreen.Settings;
        controlsButton.Select();
    }

    #region Audio

    public void SetBGMVolume(float volume)
    {
        float dB = linearToDB(volume);

        mixer.SetFloat("bgm", dB);
        PlayerPrefs.SetFloat("BGMVol", volume);
    }

    public void SetSFXVolume(float volume)
    {
        float dB = linearToDB(volume);

        mixer.SetFloat("sfx", dB);
        PlayerPrefs.SetFloat("SFXVol", volume);
    }

    private float linearToDB(float volume)
    {
        float decibels = -144f;
        if (volume != 0)
        {
            decibels = 20 * Mathf.Log10(volume);
        }
        return decibels;
    }

    private float DBToLinear(float dB)
    {
        float vol = Mathf.Pow(10f, dB / 20f);
        return vol;
    }
    #endregion

    private enum CurrentScreen
    {
        Main,
        Settings,
        Controls,
        Audio,
        Extras
    }
}
