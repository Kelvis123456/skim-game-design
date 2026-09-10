using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsScreen : MonoBehaviour
{
    [SerializeField] Slider _musicSlider;
    [SerializeField] Slider _sfxSlider;
    [SerializeField] Toggle _vibrationToggle;
    [SerializeField] Toggle _reduceEffectsToggle;
    [SerializeField] Toggle _permanentAssistToggle;
    [SerializeField] Button _deleteDataButton;
    [SerializeField] GameObject _deleteConfirmDialog;

    IAudioSystem _audio;
    IProgressionSystem _progression;

    void OnEnable()
    {
        _audio = ServiceLocator.Get<IAudioSystem>();
        _progression = ServiceLocator.Get<IProgressionSystem>();
        if (_musicSlider)
        {
            _musicSlider.value = _audio.MusicVolume;
            _musicSlider.onValueChanged.AddListener(v => _audio.MusicVolume = v);
        }
        if (_sfxSlider)
        {
            _sfxSlider.value = _audio.SFXVolume;
            _sfxSlider.onValueChanged.AddListener(v => _audio.SFXVolume = v);
        }
        if (_vibrationToggle)
        {
            _vibrationToggle.isOn = _progression.VibrationEnabled;
            _vibrationToggle.onValueChanged.AddListener(v => _progression.VibrationEnabled = v);
        }
        if (_reduceEffectsToggle)
        {
            _reduceEffectsToggle.isOn = _progression.ReduceEffects;
            _reduceEffectsToggle.onValueChanged.AddListener(v => _progression.ReduceEffects = v);
        }
        if (_permanentAssistToggle)
        {
            _permanentAssistToggle.isOn = _progression.PermanentAssist;
            _permanentAssistToggle.onValueChanged.AddListener(v => _progression.PermanentAssist = v);
        }
        _deleteDataButton?.onClick.AddListener(() => _deleteConfirmDialog?.SetActive(true));
    }

    void OnDisable()
    {
        _musicSlider?.onValueChanged.RemoveAllListeners();
        _sfxSlider?.onValueChanged.RemoveAllListeners();
        _vibrationToggle?.onValueChanged.RemoveAllListeners();
        _reduceEffectsToggle?.onValueChanged.RemoveAllListeners();
        _permanentAssistToggle?.onValueChanged.RemoveAllListeners();
        _deleteDataButton?.onClick.RemoveAllListeners();
    }

    public void ConfirmDeleteData()
    {
        ServiceLocator.Get<IProgressionSystem>().ResetData();
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}
