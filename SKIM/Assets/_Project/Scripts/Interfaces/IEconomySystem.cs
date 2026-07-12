using System;

public interface IEconomySystem
{
    int ConchaBalance { get; }
    void EarnConchas(int amount, string reason);
    bool SpendConchas(int amount, string itemId);
    bool IsCosmeticUnlocked(string cosmeticId);
    string GetEquippedStoneSkin(string stoneId);
    void EquipCosmeticSkin(string stoneId, string cosmeticId);
    void PurchaseConchaBundle(string bundleId);
    event Action<string> OnPurchaseCompleted;
    event Action<string> OnPurchaseFailed;
}
