using UnityEngine;

[CreateAssetMenu(menuName = "SKIM/Stone Data", fileName = "StoneData")]
public class StoneData : ScriptableObject
{
    [Header("Identity")]
    public string StoneName;
    public Sprite Icon;
    public GameObject Prefab;
    public int UnlockDistanceMeters;

    [Header("Physics")]
    [Range(0.5f, 1.0f)] public float ReboundCoefficient = 0.72f;
    [Range(0.4f, 0.9f)] public float ElasticityCoefficient = 0.65f;
    [Range(0.1f, 1.5f)] public float SpinSensitivity = 0.30f;
    [Range(0.05f, 0.30f)] public float Radius = 0.08f;
    [Range(0.1f, 1.0f)] public float Mass = 0.15f;
}
