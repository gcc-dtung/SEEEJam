using System;
using UnityEngine;
using UnityEngine.UI;

public class BoosterPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image nameImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image coinImage;
    
    [SerializeField] private CanvasGroup root;
    
    [Header("Hint Data")]
    [SerializeField] private BoosterPanelData hintData;
    
    [Header("Remove Data")]
    [SerializeField] private BoosterPanelData removeData;
    
    private BoosterPanelData currentData;

    private void Start()
    {
        root.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<OutOfBoosterRequestedEvent>(HandleOutOfBoosterRequested);
    }

    private void OnDisable()
    {
        if (EventBus.TryGetInstance(out EventBus eventBus))
            eventBus.Unsubscribe<OutOfBoosterRequestedEvent>(HandleOutOfBoosterRequested);
    }

    private void ShowOutOfBoosterPanel(BoosterPanelData data)
    {
        if(data.iconName) nameImage.sprite = data.iconName;
        if(data.iconContent) iconImage.sprite = data.iconContent;
        if(data.iconCoin) coinImage.sprite = data.iconCoin;
        
        root.gameObject.SetActive(true);
    }

    public void ShowHint()
    {
        currentData = hintData;
        ShowOutOfBoosterPanel(hintData);
    }

    public void ShowRemove()
    {
        currentData = removeData;
        ShowOutOfBoosterPanel(removeData);
    }

    public void HideOutOfBoosterPanel()
    {
        if (AudioManager.TryGetInstance(out AudioManager audioManager))
            audioManager.PlaySoundEffect(AudioManager.SfxPressAnyButton);

        root.gameObject.SetActive(false);
    }

    public void BuyBooster()
    {
        if (AudioManager.TryGetInstance(out AudioManager audioManager))
            audioManager.PlaySoundEffect(AudioManager.SfxPressAnyButton);

        int neededCoin = currentData.coins;
        if (neededCoin <= 0)
        {
            Debug.LogWarning("[BoosterPanel] Booster price is not configured: " + currentData.boosterType);
            return;
        }

        if (EconomyManager.Instance.TrySpendCoins(neededCoin))
        {
            BoosterInventoryManager.Instance.Add(currentData.boosterType, 1);
            root.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Not enough coins");
        }
    }

    private void HandleOutOfBoosterRequested(OutOfBoosterRequestedEvent gameEvent)
    {
        switch (gameEvent.BoosterType)
        {
            case BoosterType.Hint:
                ShowHint();
                break;
            case BoosterType.RemoveConditions:
                ShowRemove();
                break;
        }
    }
}

[System.Serializable]
public struct BoosterPanelData
{
    public BoosterType boosterType;
    public Sprite iconName;
    public Sprite iconContent;
    public Sprite iconCoin;
    [Min(1)]
    public int coins;
}
