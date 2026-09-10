using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClimateSelectorScreen : MonoBehaviour
{
    [SerializeField] Transform _rowContainer;
    [SerializeField] GameObject _rowPrefab;
    [SerializeField] TMP_Text _unlockedCountLabel;

    IProgressionSystem _prog;

    void OnEnable()
    {
        _prog = ServiceLocator.Get<IProgressionSystem>();
        BuildRows();
    }

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

            row.transform.Find("Name")?.GetComponent<TMP_Text>()?.SetText(climate.ClimateName);

            var desc = row.transform.Find("Desc")?.GetComponent<TMP_Text>();
            if (desc) desc.text = isUnlocked
                ? $"{climate.Harmonics.Length} olas  ×{climate.ClimateMultiplier:F1} score  ·  Récord: {_prog.BestDistanceForClimate(climate.ClimateName):0.0}m"
                : $"Desbloquea a {climate.UnlockDistanceMeters:N0}m";

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
            _unlockedCountLabel.text = $"{unlocked}/{_prog.AvailableClimates.Count} niveles";
    }
}
