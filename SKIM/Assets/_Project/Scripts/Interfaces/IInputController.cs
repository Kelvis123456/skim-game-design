using System;
using UnityEngine;

public interface IInputController
{
    event Action<FlickInput> OnFlickDetected;
    event Action<FlickInput> OnFlickDrag;
    bool IsEnabled { get; set; }
}
