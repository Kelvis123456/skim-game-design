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
    [SerializeField] Button _cancelDeleteButton;
    [SerializeField] Button _confirmDeleteButton;
    [SerializeField] Button _langEsButton;
    [SerializeField] Button _langEnButton;

    static readonly Color TEAL = new(0f, 0.769f, 0.8f);
    static readonly Color BORDER_DIM = new(0.118f, 0.227f, 0.373f, 0.6f);

    IAudioSystem _audio;
    IProgressionSystem _progression;
    ILocalizationSystem _loc;

    void OnEnable()
    {
        _audio = ServiceLocator.Get<IAudioSystem>();
        _progression = ServiceLocator.Get<IProgressionSystem>();
        ServiceLocator.TryGet<ILocalizationSystem>(out _loc);
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
        _cancelDeleteButton?.onClick.AddListener(() => _deleteConfirmDialog?.SetActive(false));
        _confirmDeleteButton?.onClick.AddListener(ConfirmDeleteData);

        _langEsButton?.onClick.AddListener(() => _loc?.SetLanguage("es"));
        _langEnButton?.onClick.AddListener(() => _loc?.SetLanguage("en"));
        if (_loc != null) _loc.OnLanguageChanged += RefreshLanguageButtons;
        RefreshLanguageButtons();
    }

    void OnDisable()
    {
        _musicSlider?.onValueChanged.RemoveAllListeners();
        _sfxSlider?.onValueChanged.RemoveAllListeners();
        _vibrationToggle?.onValueChanged.RemoveAllListeners();
        _reduceEffectsToggle?.onValueChanged.RemoveAllListeners();
        _permanentAssistToggle?.onValueChanged.RemoveAllListeners();
        _deleteDataButton?.onClick.RemoveAllListeners();
        _cancelDeleteButton?.onClick.RemoveAllListeners();
        _confirmDeleteButton?.onClick.RemoveAllListeners();
        _langEsButton?.onClick.RemoveAllListeners();
        _langEnButton?.onClick.RemoveAllListeners();
        if (_loc != null) _loc.OnLanguageChanged -= RefreshLanguageButtons;
    }

    // Tints whichever language is active teal (matching the toggle-on / LANZAR
    // pill treatment elsewhere) so it reads as a selected state, not just two
    // buttons that happen to say Español/English.
    void RefreshLanguageButtons()
    {
        if (_loc == null) return;
        var esImg = _langEsButton?.GetComponent<Image>();
        var enImg = _langEnButton?.GetComponent<Image>();
        if (esImg) esImg.color = _loc.Language == "es" ? TEAL : BORDER_DIM;
        if (enImg) enImg.color = _loc.Language == "en" ? TEAL : BORDER_DIM;
    }

    public void ConfirmDeleteData()
    {
        ServiceLocator.Get<IProgressionSystem>().ResetData();
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}
