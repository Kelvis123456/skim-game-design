using System;
using UnityEngine;

public class EconomySystemImpl : MonoBehaviour, IEconomySystem
{
    public int ConchaBalance { get; private set; }

    public event Action<string> OnPurchaseCompleted;
    public event Action<string> OnPurchaseFailed;

    SaveData _saveRef;

    public void Init(SaveData save)
    {
        _saveRef = save;
        ConchaBalance = save.ConchaBalance;
    }

    public void EarnConchas(int amount, string reason)
    {
        ConchaBalance += amount;
        if (_saveRef != null) _saveRef.ConchaBalance = ConchaBalance;
    }

    public bool SpendConchas(int amount, string itemId)
    {
        if (ConchaBalance < amount) return false;
        ConchaBalance -= amount;
        if (_saveRef != null) _saveRef.ConchaBalance = ConchaBalance;
        return true;
    }

    public bool IsCosmeticUnlocked(string id) =>
        _saveRef?.UnlockedCosmeticIds?.Contains(id) ?? false;

    public string GetEquippedStoneSkin(string stoneId)
    {
        if (_saveRef?.EquippedSkins == null) return null;
        return _saveRef.EquippedSkins.TryGetValue(stoneId, out var s) ? s : null;
    }

    public void EquipCosmeticSkin(string stoneId, string cosmeticId)
    {
        if (!IsCosmeticUnlocked(cosmeticId)) return;
        if (_saveRef != null)
        {
            _saveRef.EquippedSkins ??= new();
            _saveRef.EquippedSkins[stoneId] = cosmeticId;
        }
    }

    public void PurchaseConchaBundle(string bundleId)
    {
        // Unity IAP integration hook — implement in Milestone 5
        // For now: simulate success in editor
#if UNITY_EDITOR
        int amount = bundleId switch
        {
            "concha_100" => 100,
            "concha_300" => 300,
            "concha_750" => 750,
            "concha_2000" => 2000,
            _ => 0
        };
        if (amount > 0) { EarnConchas(amount, "iap_" + bundleId); OnPurchaseCompleted?.Invoke(bundleId); }
        else OnPurchaseFailed?.Invoke(bundleId);
#endif
    }
}
