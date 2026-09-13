using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementsScreen : MonoBehaviour
{
    [SerializeField] Transform _rowContainer;
    [SerializeField] GameObject _rowPrefab;

    IProgressionSystem _prog;

    void OnEnable()
    {
        _prog = ServiceLocator.Get<IProgressionSystem>();
        BuildRows();
    }

    void BuildRows()
    {
        foreach (Transform t in _rowContainer) Destroy(t.gameObject);

        foreach (var def in AchievementCatalog.All)
        {
            var row = Instantiate(_rowPrefab, _rowContainer);
            bool unlocked = _prog.IsAchievementUnlocked(def.Id);

            row.transform.Find("Name")?.GetComponent<TMP_Text>()?.SetText(def.Name);

            var thumb = row.transform.Find("Thumbnail")?.GetComponent<Image>();
            if (thumb)
            {
                var color = PaletteFor(def.Metric);
                thumb.color = unlocked ? color : Color.Lerp(color, new Color(0.42f, 0.46f, 0.52f), 0.6f);
            }

            var desc = row.transform.Find("Desc")?.GetComponent<TMP_Text>();
            if (desc) desc.text = def.Description;

            var badge = row.transform.Find("EquippedBadge");
            if (badge)
            {
                badge.gameObject.SetActive(unlocked);
                var badgeText = badge.GetComponent<TMP_Text>();
                if (badgeText) badgeText.text = "DESBLOQUEADO";
            }

            var lockIcon = row.transform.Find("Lock");
            if (lockIcon) lockIcon.gameObject.SetActive(!unlocked);
        }
    }

    // No natural "color" for an achievement — group by metric instead, so the
    // Thumbnail swatch RowTemplate now adds to every row means something here
    // too, rather than being a uniform gray circle.
    static Color PaletteFor(AchievementMetric metric) => metric switch
    {
        AchievementMetric.TotalDistance             => new Color(0f, 0.77f, 0.8f),   // teal, matches distance readouts
        AchievementMetric.BestDistance               => new Color(0.96f, 0.82f, 0.25f), // gold, matches record badge
        AchievementMetric.BestSkipCount               => new Color(0.55f, 0.71f, 0.83f),
        AchievementMetric.DailyChallengesCompleted   => new Color(0.85f, 0.4f, 0.95f),
        AchievementMetric.StonesUnlocked             => new Color(0.722f, 0.773f, 0.816f),
        AchievementMetric.ClimatesUnlocked           => new Color(0.2f, 0.55f, 0.95f),
        _                                             => new Color(0.722f, 0.773f, 0.816f),
    };
}
