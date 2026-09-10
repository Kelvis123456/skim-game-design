using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoneSelectorScreen : MonoBehaviour
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

        foreach (var stone in _prog.AvailableStones)
        {
            var row = Instantiate(_rowPrefab, _rowContainer);
            bool unlocked = _prog.IsStoneUnlocked(stone);
            bool equipped = _prog.SelectedStone == stone;

            row.transform.Find("Name")?.GetComponent<TMP_Text>()
               ?.SetText(stone.StoneName);

            var desc = row.transform.Find("Desc")?.GetComponent<TMP_Text>();
            if (desc) desc.text = unlocked
                ? $"Rebote: {stone.ReboundCoefficient:F2}  Spin: {stone.SpinSensitivity:F2}  ·  Récord: {_prog.BestDistanceForStone(stone.StoneName):0.0}m"
                : $"Desbloquea a {stone.UnlockDistanceMeters:N0}m acumulados";

            var lockIcon = row.transform.Find("Lock");
            if (lockIcon) lockIcon.gameObject.SetActive(!unlocked);

            var badge = row.transform.Find("EquippedBadge");
            if (badge) badge.gameObject.SetActive(equipped);

            if (unlocked)
            {
                var btn = row.GetComponent<Button>();
                if (btn)
                {
                    var capturedStone = stone;
                    btn.onClick.AddListener(() =>
                    {
                        _prog.SelectedStone = capturedStone;
                        BuildRows();
                    });
                }
            }
        }
    }
}
