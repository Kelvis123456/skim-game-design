using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Fades in the SKIM wordmark, hops the stone icon twice (same squash motion as a
// real skip), then loads Boot. Tap/click anywhere skips ahead.
public class SplashController : MonoBehaviour
{
    [SerializeField] CanvasGroup _canvasGroup;
    [SerializeField] RectTransform _stone;
    [SerializeField] RectTransform _ring;
    [SerializeField] Image _ringImage;

    bool _skip;

    void Start()
    {
        _canvasGroup.alpha = 0f;
        StartCoroutine(PlaySequence());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) _skip = true;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) _skip = true;
    }

    IEnumerator PlaySequence()
    {
        yield return Fade(0f, 1f, 0.4f);

        for (int hop = 0; hop < 2 && !_skip; hop++)
            yield return Hop();

        float waited = 0f;
        while (waited < 0.6f && !_skip) { waited += Time.deltaTime; yield return null; }

        yield return Fade(1f, 0f, 0.3f);
        SceneManager.LoadScene("Boot");
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        _canvasGroup.alpha = to;
    }

    IEnumerator Hop()
    {
        const float dur = 0.35f;
        float t = 0f;
        var basePos = _stone.anchoredPosition;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            float height = Mathf.Sin(p * Mathf.PI) * 40f;
            _stone.anchoredPosition = basePos + new Vector2(0f, height);

            float squish = 1f - Mathf.Sin(p * Mathf.PI) * 0.15f;
            _stone.localScale = new Vector3(1f / squish, squish, 1f);

            if (_ringImage != null)
            {
                var c = _ringImage.color;
                _ringImage.color = new Color(c.r, c.g, c.b, (1f - p) * 0.6f);
                _ring.localScale = Vector3.one * (0.6f + p * 0.8f);
            }
            yield return null;
        }
        _stone.anchoredPosition = basePos;
        _stone.localScale = Vector3.one;
    }
}
