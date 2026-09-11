using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Binds one challenge card — the compact one on the main menu, or the full
// screen — to the real progress tracked by IProgressionSystem. Both instances
// point at the same underlying data, so they always agree.
public class DailyChallengeUI : MonoBehaviour
{
    [SerializeField] TMP_Text _descriptionLabel;
    [SerializeField] TMP_Text _progressLabel;
    [SerializeField] TMP_Text _rewardLabel;
    [SerializeField] TMP_Text _countdownLabel;
    [SerializeField] Image _progressFill;

    void OnEnable() => Refresh();

    void Refresh()
    {
        if (!ServiceLocator.TryGet<IProgressionSystem>(out var prog)) return;

        var challenge = prog.CurrentDailyChallenge;
        float progress = prog.DailyChallengeProgress;
        float pct = challenge.Target > 0f ? Mathf.Clamp01(progress / challenge.Target) : 0f;
        bool done = prog.DailyChallengeCompleted;

        if (_descriptionLabel) _descriptionLabel.text = challenge.Description;

        if (_progressLabel)
            _progressLabel.text = done
                ? "¡Completado!"
                : $"{progress:F0}m / {challenge.Target:F0}m  ·  {pct:P0}";

        if (_rewardLabel) _rewardLabel.text = done ? "Cobrado" : $"+{challenge.ConchaReward} conchas";
        if (_progressFill) _progressFill.fillAmount = pct;

        if (_countdownLabel) _countdownLabel.text = $"Renueva en {TimeUntilNextUtcDay()}";
    }

    static string TimeUntilNextUtcDay()
    {
        var now = System.DateTimeOffset.UtcNow;
        var tomorrow = now.UtcDateTime.Date.AddDays(1);
        var remaining = tomorrow - now.UtcDateTime;
        return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
    }
}
