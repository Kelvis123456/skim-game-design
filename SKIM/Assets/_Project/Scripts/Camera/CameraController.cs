using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] Transform _stoneTransform;
    [SerializeField] float _followDamping = 0.15f;
    [SerializeField] float _fixedY = 4f;
    [SerializeField] float _fixedZ = -8f;

    IStoneSimulator _sim;
    float _originX;
    Vector3 _vel;

    void Start()
    {
        _originX = transform.position.x;
        transform.position = new Vector3(_originX, _fixedY, _fixedZ);

        // Fallback: find stone by name if serialized reference was lost
        if (_stoneTransform == null)
        {
            var go = GameObject.Find("Stone");
            if (go) _stoneTransform = go.transform;
        }

        if (ServiceLocator.TryGet<IStoneSimulator>(out _sim))
            _sim.OnSunk += _ => StartCoroutine(ReturnToOrigin());
    }

    void LateUpdate()
    {
        if (_stoneTransform == null) return;
        if (ServiceLocator.TryGet<IStoneSimulator>(out var sim) && sim.CurrentState.IsActive)
        {
            var target = new Vector3(_stoneTransform.position.x, _fixedY, _fixedZ);
            transform.position = Vector3.SmoothDamp(transform.position, target, ref _vel, _followDamping);
        }
    }

    IEnumerator ReturnToOrigin()
    {
        var start = transform.position;
        var end = new Vector3(_originX, _fixedY, _fixedZ);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 1.5f;
            transform.position = Vector3.Lerp(start, end, t * t * (3f - 2f * t));
            yield return null;
        }
        transform.position = end;
    }
}
