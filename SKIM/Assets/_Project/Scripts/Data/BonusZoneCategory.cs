// GDD §2.5/§7.3 — point values double as the enum's underlying value so scoring
// can cast straight to an int without a lookup table.
public enum BonusZoneCategory
{
    Green = 200,
    Blue = 500,
    Gold = 900,
    Rainbow = 1500,
}
