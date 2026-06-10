using System;
using UnityEngine;

/// <summary>
/// Premium currency wallet. Contract rewards are the primary source in Alpha.
/// </summary>
public class DiamondWallet : MonoBehaviour
{
    public int Balance { get; private set; }

    public event Action<int> OnDiamondsChanged;

    public void Init()
    {
        Balance = 0;
        OnDiamondsChanged?.Invoke(Balance);
    }

    public void LoadFromSave(int diamonds)
    {
        Balance = Math.Max(0, diamonds);
        OnDiamondsChanged?.Invoke(Balance);
    }

    public int GetSaveData() => Balance;

    public void Add(int amount)
    {
        if (amount <= 0) return;
        Balance += amount;
        OnDiamondsChanged?.Invoke(Balance);
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (Balance < amount) return false;
        Balance -= amount;
        OnDiamondsChanged?.Invoke(Balance);
        return true;
    }
}
