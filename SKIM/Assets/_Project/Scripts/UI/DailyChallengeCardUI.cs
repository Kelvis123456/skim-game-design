using UnityEngine;
using TMPro;

// Binds a challenge card's labels + progress fill to the live daily challenge —
// used by both the MainPanel summary card and the full ChallengePanel.
public class DailyChallengeCardUI : MonoBehaviour
{
    [SerializeField] TMP_Text _descriptionLabel;
    [SerializeField] TMP_Text _progressLabel;
    [SerializeField] TMP_Text _rewardLabel;
    [SerializeField] TMP_Text _resetLabel;
    [SerializeField] RectTransform _progressFill;

    IProgressionSystem _progression;
    ILocalizationSystem _loc;

    void OnEnable()
    {
        ServiceLocator.TryGet<ILocalizationSystem>(out _loc);
        if (_loc != null) _loc.OnLanguageChanged += Refresh;
        Refresh();
        InvokeRepeating(nameof(Refresh), 1f, 1f);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(Refresh));
        if (_loc != null) _loc.OnLanguageChanged -= Refresh;
    }

    string Get(string key) => _loc != null ? _loc.Get(key) : key;

    void Refresh()
    {
        if (_progression == null && !ServiceLocator.TryGet<IProgressionSystem>(out _progression)) return;

        var c = _progression.CurrentDailyChallenge;
        string unit = c.Type == DailyChallengeType.SkipsSingleThrow ? "" : "m";
        int pct = c.Target > 0f ? Mathf.RoundToInt(Mathf.Clamp01(c.Progress / c.Target) * 100f) : 0;

        if (_descriptionLabel) _descriptionLabel.text = c.Description;
        if (_rewardLabel) _rewardLabel.text = c.Completed ? Get("challenge.completed") : string.Format(Get("challenge.reward"), c.RewardConchas);
        if (_progressLabel) _progressLabel.text = $"{c.Progress:0}{unit} / {c.Target:0}{unit}  ·  {pct}%";
        if (_resetLabel) _resetLabel.text = string.Format(Get("challenge.reset"), c.TimeUntilReset.Hours, c.TimeUntilReset.Minutes);

        if (_progressFill)
        {
            var max = _progressFill.anchorMax;
            max.x = Mathf.Clamp01(c.Progress / Mathf.Max(c.Target, 0.001f));
            _progressFill.anchorMax = max;
        }
    }
}
