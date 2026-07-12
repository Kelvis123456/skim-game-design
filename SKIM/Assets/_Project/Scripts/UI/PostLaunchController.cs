using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PostLaunchController : MonoBehaviour
{
    TMP_Text _distanceLabel;
    TMP_Text _skipsLabel;
    TMP_Text _scoreLabel;
    TMP_Text _recordBadge;
    Button   _retryButton;
    CanvasGroup _group;

    void Start()
    {
        _distanceLabel = FindLabel("DistResult");
        _skipsLabel    = FindLabel("SkipsResult");
        _scoreLabel    = FindLabel("ScoreResult");
        _recordBadge   = FindLabel("RecordBadge");
        _retryButton   = GetComponentInChildren<Button>(includeInactive: true);
        _group         = GetComponent<CanvasGroup>();

        Hide();

        if (ServiceLocator.TryGet<IScoringSystem>(out var scoring))
        {
            scoring.OnLaunchCompleted += ShowResult;
            Debug.Log("[PostLaunch] Subscribed to OnLaunchCompleted.");
        }
        else
            Debug.LogError("[PostLaunch] IScoringSystem not ready in Start()");

        _retryButton?.onClick.AddListener(Retry);
    }

    void Hide()
    {
        if (_group) { _group.alpha = 0f; _group.interactable = false; _group.blocksRaycasts = false; }
    }

    void Show()
    {
        if (_group) { _group.alpha = 1f; _group.interactable = true; _group.blocksRaycasts = true; }
    }

    TMP_Text FindLabel(string childName)
    {
        var t = transform.Find(childName);
        return t ? t.GetComponent<TMP_Text>() : null;
    }

    void ShowResult(LaunchResult result)
    {
        Debug.Log($"[PostLaunch] ShowResult — {result.Distance:F1}m | {result.SkipCount} skips | {result.Score} pts");

        if (_distanceLabel) _distanceLabel.text = $"{result.Distance:F1}m";
        if (_skipsLabel)    _skipsLabel.text    = $"{result.SkipCount} skips";
        if (_scoreLabel)    _scoreLabel.text    = result.Score.ToString("N0");

        bool showRecord = result.IsNewSessionRecord || result.IsNewAllTimeRecord;
        if (_recordBadge)
        {
            _recordBadge.gameObject.SetActive(showRecord);
            if (showRecord)
                _recordBadge.text = result.IsNewAllTimeRecord ? "¡NUEVO RÉCORD!" : "¡MEJOR DE SESIÓN!";
        }

        transform.localScale = Vector3.zero;
        Show();
        StartCoroutine(PopIn());
    }

    void Retry()
    {
        StartCoroutine(PopOut(() =>
        {
            Hide();
            if (ServiceLocator.TryGet<IVFXSystem>(out var vfx))      vfx.ClearSessionRings();
            if (ServiceLocator.TryGet<IScoringSystem>(out var scor))  scor.ResetForNewLaunch();
            if (ServiceLocator.TryGet<IInputController>(out var inp)) inp.IsEnabled = true;
        }));
    }

    IEnumerator PopIn()
    {
        float e = 0f;
        while (e < 0.22f)
        {
            e += Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, e / 0.22f);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    IEnumerator PopOut(System.Action onDone)
    {
        float e = 0f;
        while (e < 0.15f)
        {
            e += Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.SmoothStep(1f, 0f, e / 0.15f);
            yield return null;
        }
        transform.localScale = Vector3.zero;
        onDone?.Invoke();
    }
}
