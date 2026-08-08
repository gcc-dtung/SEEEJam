using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoosterBarView : MonoBehaviour
{
    [SerializeField] private Button undoButton;
    [SerializeField] private Button removeConditionsButton;
    [SerializeField] private Button hintButton;
    [SerializeField] private TMP_Text removeConditionsCountText;
    [SerializeField] private TMP_Text hintCountText;
    [SerializeField] private Image removeConditionsZeroCountImage;
    [SerializeField] private Image hintZeroCountImage;

    private void Awake()
    {
        EnsureCountTextReferences();
    }

    private void OnEnable()
    {
        EnsureCountTextReferences();

        if (undoButton != null)
            undoButton.onClick.AddListener(HandleUndoClicked);
        if (removeConditionsButton != null)
            removeConditionsButton.onClick.AddListener(HandleRemoveConditionsClicked);
        if (hintButton != null)
            hintButton.onClick.AddListener(HandleHintClicked);

        EventBus.Instance.Subscribe<BoosterInventoryChangedEvent>(HandleBoosterInventoryChanged);
        UpdateAllCounts();
    }

    private void Start()
    {
        UpdateAllCounts();
    }

    private void OnDisable()
    {
        if (undoButton != null)
            undoButton.onClick.RemoveListener(HandleUndoClicked);
        if (removeConditionsButton != null)
            removeConditionsButton.onClick.RemoveListener(HandleRemoveConditionsClicked);
        if (hintButton != null)
            hintButton.onClick.RemoveListener(HandleHintClicked);

        if (EventBus.TryGetInstance(out EventBus eventBus))
            eventBus.Unsubscribe<BoosterInventoryChangedEvent>(HandleBoosterInventoryChanged);
    }

    private void HandleUndoClicked()
    {
        BoosterManager.Instance.UseUndo();
    }

    private void HandleRemoveConditionsClicked()
    {
        BoosterManager.Instance.BeginRemoveConditionsSelection();
    }

    private void HandleHintClicked()
    {
        BoosterManager.Instance.BeginHintSelection();
    }

    private void HandleBoosterInventoryChanged(BoosterInventoryChangedEvent gameEvent)
    {
        UpdateCount(gameEvent.BoosterType, gameEvent.RemainingCount);
    }

    private void UpdateAllCounts()
    {
        BoosterInventoryManager inventoryManager = BoosterInventoryManager.Instance;
        UpdateCount(BoosterType.RemoveConditions, inventoryManager.GetCount(BoosterType.RemoveConditions));
        UpdateCount(BoosterType.Hint, inventoryManager.GetCount(BoosterType.Hint));
    }

    private void UpdateCount(BoosterType boosterType, int count)
    {
        TMP_Text countText = GetCountText(boosterType);
        Image zeroCountImage = GetZeroCountImage(boosterType);
        if (countText == null)
            return;

        if (boosterType == BoosterType.Undo)
        {
            countText.gameObject.SetActive(false);
            return;
        }

        int normalizedCount = Mathf.Max(0, count);
        bool hasCount = normalizedCount > 0;

        countText.gameObject.SetActive(hasCount);

        if (zeroCountImage != null)
            zeroCountImage.gameObject.SetActive(!hasCount);

        if (hasCount)
            countText.text = normalizedCount.ToString();
    }

    private TMP_Text GetCountText(BoosterType boosterType)
    {
        switch (boosterType)
        {
            case BoosterType.RemoveConditions:
                return removeConditionsCountText;
            case BoosterType.Hint:
                return hintCountText;
            default:
                return null;
        }
    }

    private Image GetZeroCountImage(BoosterType boosterType)
    {
        switch (boosterType)
        {
            case BoosterType.RemoveConditions:
                return removeConditionsZeroCountImage;
            case BoosterType.Hint:
                return hintZeroCountImage;
            default:
                return null;
        }
    }

    private void EnsureCountTextReferences()
    {
        if (removeConditionsCountText == null)
            removeConditionsCountText = FindCountText(removeConditionsButton);
        if (hintCountText == null)
            hintCountText = FindCountText(hintButton);
    }

    private static TMP_Text FindCountText(Button button)
    {
        if (button == null)
            return null;

        Transform countTransform = button.transform.Find("Count");
        if (countTransform != null && countTransform.TryGetComponent(out TMP_Text countText))
            return countText;

        TMP_Text[] childTexts = button.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < childTexts.Length; i++)
            if (childTexts[i].name == "Count")
                return childTexts[i];

        return null;
    }
}
