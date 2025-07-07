using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private CurrentScreen currentScreen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentScreen = CurrentScreen.Main;

        LoadPlayerPrefs();
    }

    // Update is called once per frame
    void Update()
    {
        
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
    }

    public void CloseSettingsMenu()
    {
        fade.SetActive(false);
        settingsMenu.SetActive(false);
        currentScreen = CurrentScreen.Main;
    }
    public void OpenControlsMenu()
    {
        settingsMenu.SetActive(false);
        controlsMenu.SetActive(true);
        currentScreen = CurrentScreen.Controls;
    }

    public void CloseControlsMenu()
    {
        settingsMenu.SetActive(true);
        controlsMenu.SetActive(false);
        currentScreen = CurrentScreen.Settings;
    }
    public void OpenAudioMenu()
    {
        settingsMenu.SetActive(false);
        audioMenu.SetActive(true);
        currentScreen = CurrentScreen.Audio;
    }

    public void CloseAudioMenu()
    {
        settingsMenu.SetActive(true);
        audioMenu.SetActive(false);
        currentScreen = CurrentScreen.Settings;
    }
    public void OpenExtrasMenu()
    {
        settingsMenu.SetActive(false);
        extrasMenu.SetActive(true);
        currentScreen = CurrentScreen.Audio;
    }

    public void CloseExtrasMenu()
    {
        settingsMenu.SetActive(true);
        extrasMenu.SetActive(false);
        currentScreen = CurrentScreen.Settings;
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
