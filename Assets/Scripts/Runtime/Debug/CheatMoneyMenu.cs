using UnityEngine;

public class CheatMoneyMenu : MonoBehaviour
{
    [SerializeField, Min(1)] private int amountPerClick = 100;

    public int AmountPerClick => Mathf.Max(1, amountPerClick);

    public void IncreaseMoney()
    {
        if (!EconomyManager.TryGetInstance(out EconomyManager economyManager))
            return;

        economyManager.AddCoins(AmountPerClick);
    }

    public void DecreaseMoney()
    {
        if (!EconomyManager.TryGetInstance(out EconomyManager economyManager))
            return;

        economyManager.TrySpendCoins(AmountPerClick);
    }
}