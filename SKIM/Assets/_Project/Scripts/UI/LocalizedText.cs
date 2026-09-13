using System.Collections;
using TMPro;
using UnityEngine;

// Attached to chrome baked at editor time (titles, button labels, section
// headers) that no controller script ever re-sets at runtime — dynamic text
// (row descriptions, HUD readouts, etc.) is localized directly by the
// controller that already owns it instead of going through this component.
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    public string Key;

    TMP_Text _tmp;
    ILocalizationSystem _loc;

    void OnEnable()
    {
        _tmp = GetComponent<TMP_Text>();
        StartCoroutine(WaitForService());
    }

    IEnumerator WaitForService()
    {
        while (!ServiceLocator.TryGet<ILocalizationSystem>(out _loc)) yield return null;
        Apply();
        _loc.OnLanguageChanged += Apply;
    }

    void OnDisable()
    {
        if (_loc != null) _loc.OnLanguageChanged -= Apply;
    }

    void Apply()
    {
        if (_tmp != null && !string.IsNullOrEmpty(Key)) _tmp.text = _loc.Get(Key);
    }
}
