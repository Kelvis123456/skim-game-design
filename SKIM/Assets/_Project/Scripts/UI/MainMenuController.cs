using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] TMP_Text _recordLabel;
    [SerializeField] TMP_Text _climateLabel;

    [Header("Panels")]
    [SerializeField] GameObject _hudRoot;
    [SerializeField] GameObject _mainPanel;
    [SerializeField] GameObject _stoneSelectorPanel;
    [SerializeField] GameObject _climateSelectorPanel;
    [SerializeField] GameObject _settingsPanel;
    [SerializeField] GameObject _dailyChallengePanel;
    [SerializeField] GameObject _achievementsPanel;

    [Header("Buttons")]
    [SerializeField] Button _launchButton;
    [SerializeField] Button _tabStones;
    [SerializeField] Button _tabClimates;
    [SerializeField] Button _tabSettings;
    [SerializeField] Button _tabDailyChallenge;
    [SerializeField] Button _tabAchievements;

    IProgressionSystem _prog;
    IOceanSystem _ocean;

    void Start()
    {
        _prog  = ServiceLocator.Get<IProgressionSystem>();
        _ocean = ServiceLocator.Get<IOceanSystem>();

        // The menu owns the screen until the player taps LANZAR, so a stray drag
        // behind it can't fire a launch.
        ServiceLocator.Get<IInputController>().IsEnabled = false;
        if (_hudRoot != null) _hudRoot.SetActive(false);

        RefreshStats();

        _launchButton?.onClick.AddListener(StartGame);
        _tabStones?.onClick.AddListener(() => ShowPanel(_stoneSelectorPanel));
        _tabClimates?.onClick.AddListener(() => ShowPanel(_climateSelectorPanel));
        _tabSettings?.onClick.AddListener(() => ShowPanel(_settingsPanel));
        _tabDailyChallenge?.onClick.AddListener(() => ShowPanel(_dailyChallengePanel));
        _tabAchievements?.onClick.AddListener(() => ShowPanel(_achievementsPanel));

        ShowPanel(_mainPanel);
    }

    void RefreshStats()
    {
        if (_recordLabel)
            _recordLabel.text = $"RÉCORD: {_prog.AllTimeRecord:F1}m";
        if (_climateLabel && _ocean?.CurrentClimate != null)
            _climateLabel.text = _ocean.CurrentClimate.ClimateName.ToUpper();
    }

    public void ShowMainPanel() => ShowPanel(_mainPanel);

    void ShowPanel(GameObject target)
    {
        foreach (var p in new[] { _mainPanel, _stoneSelectorPanel, _climateSelectorPanel,
                                   _settingsPanel, _dailyChallengePanel, _achievementsPanel })
            if (p != null) p.SetActive(p == target);
    }

    void StartGame()
    {
        if (_mainPanel != null) StartCoroutine(PopOut(_mainPanel.transform, () =>
        {
            gameObject.SetActive(false);
            if (_hudRoot != null) _hudRoot.SetActive(true);
            ServiceLocator.Get<IInputController>().IsEnabled = true;
        }));
    }

    IEnumerator PopOut(Transform t, System.Action onDone)
    {
        float elapsed = 0f; float dur = 0.2f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.SmoothStep(1f, 0f, elapsed / dur);
            yield return null;
        }
        t.localScale = Vector3.zero;
        onDone?.Invoke();
    }
}
