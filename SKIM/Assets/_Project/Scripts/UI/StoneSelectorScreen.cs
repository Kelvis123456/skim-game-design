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

            var thumb = row.transform.Find("Thumbnail")?.GetComponent<Image>();
            if (thumb)
            {
                var color = PaletteFor(stone.StoneName);
                thumb.color = unlocked ? color : Dim(color);
            }

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

    // Mirrors the per-stone palette StoneVisual applies in gameplay, so the
    // selector swatch matches what the stone actually looks like in flight.
    static Color PaletteFor(string stoneName) => stoneName switch
    {
        "Esquisto" => new Color(0.290f, 0.416f, 0.353f),
        "Basalto"  => new Color(0.118f, 0.157f, 0.188f),
        "Cuarzo"   => new Color(0.753f, 0.847f, 0.941f),
        _          => new Color(0.722f, 0.773f, 0.816f), // Guijarro
    };

    // Mutes a swatch toward a fixed mid-gray for the locked state. Scaling
    // brightness down instead would make the already-dark Basalto vanish
    // against the card background, so this blends toward a value that reads
    // against the card at either end of the palette.
    static Color Dim(Color c)
    {
        var muted = Color.Lerp(c, new Color(0.42f, 0.46f, 0.52f), 0.6f);
        return new Color(muted.r, muted.g, muted.b, 0.9f);
    }
}
