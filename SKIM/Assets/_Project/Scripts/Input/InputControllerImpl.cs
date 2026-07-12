using System;
using System.Collections.Generic;
using UnityEngine;

public class InputControllerImpl : MonoBehaviour, IInputController
{
    public event Action<FlickInput> OnFlickDetected;
    public event Action<FlickInput> OnFlickDrag;
    public bool IsEnabled { get; set; } = true;

    const float MAX_SWIPE_SPEED = 2200f;
    const float MAX_CURVATURE = 60f;
    const int CURVE_SAMPLES = 6;

    Vector2 _startPos;
    float _startTime;
    readonly List<Vector2> _dragPoints = new();
    bool _tracking;

    void Update()
    {
        if (!IsEnabled) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    void HandleTouch()
    {
        if (Input.touchCount == 0) return;
        var touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                BeginSwipe(touch.position);
                break;
            case TouchPhase.Moved:
                SampleDrag(touch.position);
                OnFlickDrag?.Invoke(BuildInput(touch.position));
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                EndSwipe(touch.position);
                break;
        }
    }

    void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0)) BeginSwipe(Input.mousePosition);
        else if (Input.GetMouseButton(0) && _tracking)
        {
            SampleDrag(Input.mousePosition);
            OnFlickDrag?.Invoke(BuildInput(Input.mousePosition));
        }
        else if (Input.GetMouseButtonUp(0) && _tracking) EndSwipe(Input.mousePosition);
    }

    void BeginSwipe(Vector2 pos)
    {
        _startPos = pos;
        _startTime = Time.time;
        _dragPoints.Clear();
        _dragPoints.Add(pos);
        _tracking = true;
    }

    void SampleDrag(Vector2 pos)
    {
        if (_dragPoints.Count < CURVE_SAMPLES) _dragPoints.Add(pos);
        else { _dragPoints.RemoveAt(0); _dragPoints.Add(pos); }
    }

    void EndSwipe(Vector2 endPos)
    {
        _tracking = false;
        float dt = Mathf.Max(Time.time - _startTime, 0.01f);
        OnFlickDetected?.Invoke(BuildInput(endPos));
    }

    FlickInput BuildInput(Vector2 endPos)
    {
        var delta = endPos - _startPos;
        if (delta.sqrMagnitude < 4f) return FlickInput.Default;

        float dt = Mathf.Max(Time.time - _startTime, 0.016f);
        float swipeSpeed = delta.magnitude / dt;
        float force = Mathf.Clamp01(swipeSpeed / MAX_SWIPE_SPEED);
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        float spin = CalculateSpin(endPos);

        return new FlickInput(angle, force, spin);
    }

    float CalculateSpin(Vector2 endPos)
    {
        if (_dragPoints.Count < 3) return 0f;

        var first = _dragPoints[0];
        var line = endPos - first;
        float maxDev = 0f;

        for (int i = 1; i < _dragPoints.Count - 1; i++)
        {
            float t = Vector2.Dot(_dragPoints[i] - first, line) / Mathf.Max(line.sqrMagnitude, 1f);
            var proj = first + t * line;
            float dev = (_dragPoints[i] - proj).x;
            if (Mathf.Abs(dev) > Mathf.Abs(maxDev)) maxDev = dev;
        }

        return Mathf.Clamp(maxDev / MAX_CURVATURE, -1f, 1f);
    }
}
