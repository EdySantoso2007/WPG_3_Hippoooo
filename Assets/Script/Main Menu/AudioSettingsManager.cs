using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("Audio Mixer Reference")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("UI Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
{
    // 1. Set nilai awal UI Slider ke posisi maksimal (1 = pojok kanan)
    if (bgmSlider != null) 
    {
        bgmSlider.value = 1f;
        SetBGMVolume(1f); // Sinkronkan volume Audio Mixer sejak awal
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
    }

    if (sfxSlider != null) 
    {
        sfxSlider.value = 1f;
        SetSFXVolume(1f); // Sinkronkan volume Audio Mixer sejak awal
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }
}

    public void SetBGMVolume(float value)
    {
        // Nilai slider (0.0001 s/d 1) dikonversi ke skala Decibel (-80dB s/d 0dB)
        float dbValue = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        mainMixer.SetFloat("BGMVolume", dbValue);
    }

    public void SetSFXVolume(float value)
    {
        float dbValue = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        mainMixer.SetFloat("SFXVolume", dbValue);
    }
}