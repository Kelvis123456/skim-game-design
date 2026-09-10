public enum AchievementMetric
{
    TotalDistance,
    BestDistance,
    BestSkipCount,
    DailyChallengesCompleted,
    StonesUnlocked,
    ClimatesUnlocked,
}

[System.Serializable]
public struct AchievementDefinition
{
    public string Id;
    public string Name;
    public string Description;
    public AchievementMetric Metric;
    public float Target;

    public AchievementDefinition(string id, string name, string description, AchievementMetric metric, float target)
    {
        Id = id; Name = name; Description = description; Metric = metric; Target = target;
    }
}

public static class AchievementCatalog
{
    public static readonly AchievementDefinition[] All =
    {
        new("first_throw",  "Primer lanzamiento", "Completa tu primera tirada",
            AchievementMetric.TotalDistance, 0.01f),
        new("dist_100",     "Cien metros", "Acumula 100m de distancia total",
            AchievementMetric.TotalDistance, 100f),
        new("dist_1000",    "Kilómetro completo", "Acumula 1000m de distancia total",
            AchievementMetric.TotalDistance, 1000f),
        new("best_50",      "Media centuria", "Logra 50m en una sola tirada",
            AchievementMetric.BestDistance, 50f),
        new("skips_15",     "Rebotador", "Logra 15 rebotes en una sola tirada",
            AchievementMetric.BestSkipCount, 15f),
        new("challenges_5", "Constante", "Completa 5 desafíos diarios",
            AchievementMetric.DailyChallengesCompleted, 5f),
        new("all_stones",   "Coleccionista", "Desbloquea todas las piedras",
            AchievementMetric.StonesUnlocked, 4f),
        new("all_climates", "Todoterreno", "Desbloquea todos los climas",
            AchievementMetric.ClimatesUnlocked, 5f),
    };
}
