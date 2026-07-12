using System.Collections;
using UnityEngine;

// Syncs the Stone GameObject position/rotation to the physics state each frame.
[DefaultExecutionOrder(10)]
public class StoneVisual : MonoBehaviour
{
    IStoneSimulator _sim;
    Renderer _rend;

    void Start()
    {
        _rend = GetComponent<Renderer>();
        StartCoroutine(WaitForSim());
    }

    IEnumerator WaitForSim()
    {
        while (!ServiceLocator.TryGet<IStoneSimulator>(out _sim))
            yield return null;
    }

    void LateUpdate()
    {
        if (_sim == null) return;
        var state = _sim.CurrentState;

        bool visible = state.Phase != StoneState.StonePhase.Sunk;
        if (_rend) _rend.enabled = visible;

        if (visible)
        {
            transform.position = state.Position;
            // Spin around Z axis proportional to angular velocity
            transform.Rotate(0f, 0f, state.AngularVelocity * Time.deltaTime * 60f, Space.Self);
        }
    }
}
