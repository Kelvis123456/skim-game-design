// GDD §2.5/§7.3. Rainbow's effect (a ×2 global multiplier, no points) can't be expressed
// as a plain point value like the other three, so this no longer doubles as one — see
// ScoringSystemImpl.RegisterBonusZoneHit for the actual per-category reward.
public enum BonusZoneCategory
{
    Green,
    Blue,
    Gold,
    Rainbow,
}
