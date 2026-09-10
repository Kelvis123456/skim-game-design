using System.Collections;
using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    TMP_Text _scoreLabel;
    TMP_Text _distanceLabel;
    TMP_Text _multiplierLabel;
    TMP_Text _achievementToast;
    GameObject _tutorialHintPanel;

    void Start()
    {
        _scoreLabel      = FindLabel("ScoreLabel");
        _distanceLabel   = FindLabel("DistanceLabel");
        _multiplierLabel = FindLabel("MultiplierBadge");
        _achievementToast = CreateAchievementToast();
        _tutorialHintPanel = CreateTutorialHint();

        StartCoroutine(WaitForServices());
    }

    // Nothing on screen explained the drag gesture or the "no losing, just beat your
    // record" loop until this — shown once, ever, and dismissed on the first drag.
    GameObject CreateTutorialHint()
    {
        var bg = new GameObject("TutorialHint");
        bg.transform.SetParent(transform, false);
        var bgImg = bg.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.04f, 0.09f, 0.16f, 0.88f);
        var bgRect = bgImg.rectTransform;
        bgRect.anchorMin = bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = new Vector2(0f, -350f);
        bgRect.sizeDelta = new Vector2(860f, 220f);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(bg.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Arrastrá y soltá en cualquier parte de la pantalla para lanzar la piedra.\n" +
                   "No hay que perder — cada tirada es un intento. ¡Superá tu récord!";
        tmp.fontSize = 30f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var rect = tmp.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(40f, 20f);
        rect.offsetMax = new Vector2(-40f, -20f);

        bg.SetActive(false);
        return bg;
    }

    TMP_Text CreateAchievementToast()
    {
        var go = new GameObject("AchievementToast");
        go.transform.SetParent(transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 32f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.96f, 0.82f, 0.25f);
        var rect = tmp.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 250f);
        rect.sizeDelta = new Vector2(800f, 80f);
        go.SetActive(false);
        return tmp;
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

        IProgressionSystem prog = null;
        if (ServiceLocator.TryGet<IProgressionSystem>(out prog))
            prog.OnAchievementUnlocked += def => StartCoroutine(ShowAchievementToast(def));

        if (prog != null && !prog.HasCompletedTutorial && _tutorialHintPanel != null)
        {
            _tutorialHintPanel.SetActive(true);
            if (ServiceLocator.TryGet<IInputController>(out var input))
                input.OnFlickDrag += _ => DismissTutorialHint(prog);
        }

        Debug.Log("[HUD] Wired to services successfully.");
    }

    void DismissTutorialHint(IProgressionSystem prog)
    {
        if (_tutorialHintPanel == null || !_tutorialHintPanel.activeSelf) return;
        _tutorialHintPanel.SetActive(false);
        prog.HasCompletedTutorial = true;
        prog.Save();
    }

    IEnumerator ShowAchievementToast(AchievementDefinition def)
    {
        if (_achievementToast == null) yield break;

        _achievementToast.gameObject.SetActive(true);
        _achievementToast.text = $"¡Logro desbloqueado! {def.Name}";
        _achievementToast.alpha = 1f;

        yield return new WaitForSeconds(2.5f);

        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            _achievementToast.alpha = Mathf.Lerp(1f, 0f, t / 0.5f);
            yield return null;
        }
        _achievementToast.gameObject.SetActive(false);
    }

    TMP_Text FindLabel(string childName)
    {
        var t = transform.Find(childName);
        return t ? t.GetComponent<TMP_Text>() : null;
    }
}
