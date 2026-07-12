using System.Collections;
using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    TMP_Text _scoreLabel;
    TMP_Text _distanceLabel;
    TMP_Text _multiplierLabel;

    void Start()
    {
        _scoreLabel      = FindLabel("ScoreLabel");
        _distanceLabel   = FindLabel("DistanceLabel");
        _multiplierLabel = FindLabel("MultiplierBadge");

        StartCoroutine(WaitForServices());
    }

    IEnumerator WaitForServices()
    {
        // Wait until Boot scene has registered services (max 5 seconds)
        float waited = 0f;
        while (!ServiceLocator.TryGet<IScoringSystem>(out _) && waited < 5f)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        if (!ServiceLocator.TryGet<IScoringSystem>(out var scoring))
        {
            Debug.LogError("[HUD] IScoringSystem never registered.");
            yield break;
        }
        if (!ServiceLocator.TryGet<IStoneSimulator>(out var sim))
        {
            Debug.LogError("[HUD] IStoneSimulator never registered.");
            yield break;
        }

        scoring.OnMultiplierChanged += mult =>
        {
            if (_multiplierLabel) _multiplierLabel.text = $"×{mult:F1}";
        };

        scoring.OnLaunchCompleted += _ =>
        {
            if (_scoreLabel) _scoreLabel.text = scoring.SessionScore.ToString("N0");
        };

        sim.OnImpact += state =>
        {
            if (_distanceLabel) _distanceLabel.text = $"{state.TotalDistance:F0}m";
        };

        sim.OnSunk += _ =>
        {
            if (_scoreLabel) _scoreLabel.text = scoring.SessionScore.ToString("N0");
        };

        Debug.Log("[HUD] Wired to services successfully.");
    }

    TMP_Text FindLabel(string childName)
    {
        var t = transform.Find(childName);
        return t ? t.GetComponent<TMP_Text>() : null;
    }
}
