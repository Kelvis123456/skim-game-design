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

    const float PANEL_TRANSITION_DURATION = 0.12f;

    GameObject _currentPanel;
    Coroutine _panelTransition;

    // Panels used to SetActive-swap instantly, which read as a hard cut between
    // screens. Cross-fading them (via the CanvasGroup Panel() now adds to each)
    // is cheap and makes tab switches feel like part of the same UI instead of
    // a different screen popping in.
    void ShowPanel(GameObject target)
    {
        if (target == _currentPanel) return;
        if (_panelTransition != null) StopCoroutine(_panelTransition);
        _panelTransition = StartCoroutine(TransitionPanel(_currentPanel, target));
        _currentPanel = target;
    }

    IEnumerator TransitionPanel(GameObject outgoing, GameObject target)
    {
        var outgoingGroup = outgoing != null ? outgoing.GetComponent<CanvasGroup>() : null;
        CanvasGroup incomingGroup = null;

        if (target != null)
        {
            target.SetActive(true);
            incomingGroup = target.GetComponent<CanvasGroup>();
            if (incomingGroup != null)
            {
                incomingGroup.alpha = 0f;
                incomingGroup.blocksRaycasts = false;
            }
        }

        float t = 0f;
        while (t < PANEL_TRANSITION_DURATION)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / PANEL_TRANSITION_DURATION);
            if (outgoingGroup != null) outgoingGroup.alpha = 1f - p;
            if (incomingGroup != null) incomingGroup.alpha = p;
            yield return null;
        }

        if (incomingGroup != null)
        {
            incomingGroup.alpha = 1f;
            incomingGroup.blocksRaycasts = true;
        }

        // Belt-and-suspenders: also fully deactivate every panel except the
        // target, same as the old instant-swap did, so a panel that was never
        // routed through this transition (e.g. before the first ShowPanel call)
        // can't linger active underneath.
        foreach (var p in new[] { _mainPanel, _stoneSelectorPanel, _climateSelectorPanel,
                                   _settingsPanel, _dailyChallengePanel, _achievementsPanel })
            if (p != null && p != target) p.SetActive(false);

        _panelTransition = null;
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
