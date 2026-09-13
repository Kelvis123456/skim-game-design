using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClimateSelectorScreen : MonoBehaviour
{
    [SerializeField] Transform _rowContainer;
    [SerializeField] GameObject _rowPrefab;
    [SerializeField] TMP_Text _unlockedCountLabel;

    IProgressionSystem _prog;
    ILocalizationSystem _loc;

    void OnEnable()
    {
        _prog = ServiceLocator.Get<IProgressionSystem>();
        ServiceLocator.TryGet<ILocalizationSystem>(out _loc);
        if (_loc != null) _loc.OnLanguageChanged += BuildRows;
        BuildRows();
    }

    void OnDisable()
    {
        if (_loc != null) _loc.OnLanguageChanged -= BuildRows;
    }

    string Get(string key) => _loc != null ? _loc.Get(key) : key;

    void BuildRows()
    {
        foreach (Transform t in _rowContainer) Destroy(t.gameObject);

        int unlocked = 0;
        foreach (var climate in _prog.AvailableClimates)
        {
            bool isUnlocked = _prog.IsClimateUnlocked(climate);
            bool isActive = _prog.SelectedClimate == climate;
            if (isUnlocked) unlocked++;

            var row = Instantiate(_rowPrefab, _rowContainer);

            row.transform.Find("Name")?.GetComponent<TMP_Text>()?.SetText(Get("climate.name." + climate.ClimateName));

            var thumb = row.transform.Find("Thumbnail")?.GetComponent<Image>();
            if (thumb)
            {
                var color = climate.WaterSurfaceColor;
                thumb.color = isUnlocked ? color : Dim(color);
            }

            var desc = row.transform.Find("Desc")?.GetComponent<TMP_Text>();
            if (desc) desc.text = isUnlocked
                ? string.Format(Get("climate.desc_unlocked"), climate.Harmonics.Length,
                    climate.ClimateMultiplier.ToString("F1"), _prog.BestDistanceForClimate(climate.ClimateName).ToString("0.0"))
                : string.Format(Get("climate.desc_locked"), climate.UnlockDistanceMeters.ToString("N0") + "m");

            var lockIcon = row.transform.Find("Lock");
            if (lockIcon) lockIcon.gameObject.SetActive(!isUnlocked);

            var activeBorder = row.transform.Find("ActiveBorder");
            if (activeBorder) activeBorder.gameObject.SetActive(isActive);

            if (isUnlocked)
            {
                var btn = row.GetComponent<Button>();
                if (btn)
                {
                    var cap = climate;
                    btn.onClick.AddListener(() =>
                    {
                        _prog.SelectedClimate = cap;
                        ServiceLocator.Get<IOceanSystem>().SetClimate(cap);
                        BuildRows();
                    });
                }
            }
        }

        if (_unlockedCountLabel)
            _unlockedCountLabel.text = string.Format(Get("climate.unlocked_count"), unlocked, _prog.AvailableClimates.Count);
    }

    // Mutes a swatch toward a fixed mid-gray for the locked state — see the
    // matching helper in StoneSelectorScreen for why this isn't a plain darken.
    static Color Dim(Color c)
    {
        var muted = Color.Lerp(c, new Color(0.42f, 0.46f, 0.52f), 0.6f);
        return new Color(muted.r, muted.g, muted.b, 0.9f);
    }
}
