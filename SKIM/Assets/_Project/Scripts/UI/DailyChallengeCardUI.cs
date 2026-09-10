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

    void OnEnable()
    {
        Refresh();
        InvokeRepeating(nameof(Refresh), 1f, 1f);
    }

    void OnDisable() => CancelInvoke(nameof(Refresh));

    void Refresh()
    {
        if (_progression == null && !ServiceLocator.TryGet<IProgressionSystem>(out _progression)) return;

        var c = _progression.CurrentDailyChallenge;
        string unit = c.Type == DailyChallengeType.SkipsSingleThrow ? "" : "m";
        int pct = c.Target > 0f ? Mathf.RoundToInt(Mathf.Clamp01(c.Progress / c.Target) * 100f) : 0;

        if (_descriptionLabel) _descriptionLabel.text = c.Description;
        if (_rewardLabel) _rewardLabel.text = c.Completed ? "¡Completado!" : $"+{c.RewardConchas} conchas";
        if (_progressLabel) _progressLabel.text = $"{c.Progress:0}{unit} / {c.Target:0}{unit}  ·  {pct}%";
        if (_resetLabel) _resetLabel.text = $"Renueva en {c.TimeUntilReset.Hours}h {c.TimeUntilReset.Minutes}m";

        if (_progressFill)
        {
            var max = _progressFill.anchorMax;
            max.x = Mathf.Clamp01(c.Progress / Mathf.Max(c.Target, 0.001f));
            _progressFill.anchorMax = max;
        }
    }
}
