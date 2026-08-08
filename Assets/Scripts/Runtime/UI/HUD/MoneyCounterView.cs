using TMPro;
using UnityEngine;

public class MoneyCounterView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyCounterText;

    private void Awake()
    {
        EnsureTextReferences();
    }

    private void OnEnable()
    {
        EnsureTextReferences();

        if (EventBus.TryGetInstance(out EventBus eventBus))
            eventBus.Subscribe<EconomyChangedEvent>(HandleEconomyChanged);

        if (EconomyManager.TryGetInstance(out EconomyManager economyManager))
            UpdateDisplay(economyManager.GetCoins());
    }

    private void OnDisable()
    {
        if (EventBus.TryGetInstance(out EventBus eventBus))
            eventBus.Unsubscribe<EconomyChangedEvent>(HandleEconomyChanged);
    }

    private void HandleEconomyChanged(EconomyChangedEvent gameEvent)
    {
        UpdateDisplay(gameEvent.CurrentCoins);
    }

    private void UpdateDisplay(int amount)
    {
        if (moneyCounterText == null)
            return;

        moneyCounterText.text = amount.ToString();
    }

    private void EnsureTextReferences()
    {
        if (moneyCounterText != null)
            return;

        moneyCounterText = FindText("Money Count");
        if (moneyCounterText == null)
            moneyCounterText = CreateMoneyText();
    }

    private TextMeshProUGUI FindText(string childName)
    {
        TextMeshProUGUI[] childTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < childTexts.Length; i++)
            if (childTexts[i].name == childName)
                return childTexts[i];

        return null;
    }

    private TextMeshProUGUI CreateMoneyText()
    {
        GameObject textObject = new GameObject("Money Count");
        textObject.transform.SetParent(transform, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(220f, 40f);
        rectTransform.anchoredPosition = new Vector2(0f, 0f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = "0";
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }
}
