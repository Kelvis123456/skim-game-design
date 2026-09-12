using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Unity's default Button transition only tints color, which barely reads on
// a touchscreen (no hover state, and the press is gone before it fades in).
// This adds an actual scale punch so taps feel like they landed.
[RequireComponent(typeof(RectTransform))]
public class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    const float PRESSED_SCALE = 0.94f;
    const float ANIM_DURATION = 0.08f;

    RectTransform _rect;
    Coroutine _routine;

    void Awake() => _rect = (RectTransform)transform;

    public void OnPointerDown(PointerEventData eventData) => AnimateTo(PRESSED_SCALE);
    public void OnPointerUp(PointerEventData eventData) => AnimateTo(1f);
    public void OnPointerExit(PointerEventData eventData) => AnimateTo(1f);

    void AnimateTo(float target)
    {
        if (!isActiveAndEnabled) return;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Animate(target));
    }

    IEnumerator Animate(float target)
    {
        float start = _rect.localScale.x;
        float t = 0f;
        while (t < ANIM_DURATION)
        {
            t += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(start, target, t / ANIM_DURATION);
            _rect.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        _rect.localScale = new Vector3(target, target, 1f);
        _routine = null;
    }

    void OnDisable()
    {
        if (_rect != null) _rect.localScale = Vector3.one;
    }
}
