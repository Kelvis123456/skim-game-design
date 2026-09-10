using UnityEngine;
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
}
