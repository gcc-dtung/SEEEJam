using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private ItemType itemType;
    [SerializeField] private SlotType itemSlotType;
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] private string solutionSlotId;
    [SerializeField] private ItemView itemView;
    
    [SerializeReference]
    [SubclassSelector]
    private List<PlantCondition> conditions = new List<PlantCondition>();

    private ItemSlot currentSlot;
    private bool _ignoreConditionsForRun;
    private BoxCollider2D _interactionCollider;

    private void Awake()
    {
        EnsureItemView();
        AutoFitInteractionCollider();
    }

    private void OnEnable()
    {
        EnsureItemView();
        EventBus.Instance.Subscribe<BoardChangedEvent>(HandleBoardChanged);
    }

    private void OnDisable()
    {
        if (EventBus.TryGetInstance(out EventBus eventBus))
            eventBus.Unsubscribe<BoardChangedEvent>(HandleBoardChanged);
    }
    
    #region Public API
    public SlotType ItemSlotType => itemSlotType;
    public ItemType ItemType => itemType;
    public string ItemId => itemId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName)
        ? TreeNameRegistry.GetDisplayName(itemId)
        : displayName;
    public string SolutionSlotId => solutionSlotId;
    public IReadOnlyList<PlantCondition> Conditions => conditions;
    public bool IgnoresConditionsForRun => _ignoreConditionsForRun;
    public ItemView View
    {
        get
        {
            EnsureItemView();
            return itemView;
        }
    }
    public ItemSlot CurrentSlot => currentSlot;

    public void Configure(
        ItemType newItemType,
        SlotType newItemSlotType,
        string newItemId,
        List<PlantCondition> newConditions,
        string newSolutionSlotId = "",
        string newDisplayName = "")
    {
        itemType = newItemType;
        itemSlotType = newItemSlotType;
        itemId = newItemId;
        displayName = newDisplayName ?? "";
        conditions = newConditions ?? new List<PlantCondition>();
        solutionSlotId = newSolutionSlotId;
        _ignoreConditionsForRun = false;

        UpdateTreeVisuals();
    }

    public bool TryGetEmittedSmell(out PlantSmell emittedSmell)
    {
        if (conditions != null)
        {
            foreach (PlantCondition condition in conditions)
            {
                if (condition is EmitSmellCondition smellCondition &&
                    smellCondition.Smell != PlantSmell.None)
                {
                    emittedSmell = smellCondition.Smell;
                    return true;
                }
            }
        }

        emittedSmell = PlantSmell.None;
        return false;
    }

    private bool CheckCondition(ItemSlot slot)
    {
        if (_ignoreConditionsForRun)
            return true;

        if(conditions!= null)
            foreach(PlantCondition cond in conditions)
                if (!cond.CheckCondition(slot))
                    return false;
        return true;
    }

    public void RefreshConditionVisual(ItemSlot slot)
    {
        currentSlot = slot;
        bool isHappyState = slot != null && slot.Type != SlotType.Wait && CheckCondition(slot);
        View.SetMood(isHappyState);

        if (slot.Type == SlotType.Wait)
        {
            View.ShowNormal();
        }
        else if (CheckCondition(slot))
        {
            View.ShowCorrect();
        }
        else
        {
            View.ShowWrong();
        }
    }

    public void SetCurrentSlot(ItemSlot slot)
    {
        currentSlot = slot;

        if (slot != null)
            RefreshConditionVisual(slot);
    }

    private void HandleBoardChanged(BoardChangedEvent gameEvent)
    {
        if (currentSlot == null) return;

        RefreshConditionVisual(currentSlot);
    }

    public bool IsSatisfiedOnBoard()
    {
        return currentSlot != null && currentSlot.Type != SlotType.Wait && CheckCondition(currentSlot);
    }

    public bool TryIgnoreConditionsForRun()
    {
        if (_ignoreConditionsForRun)
            return false;

        _ignoreConditionsForRun = true;
        if (currentSlot != null)
            RefreshConditionVisual(currentSlot);
        return true;
    }

    private void UpdateTreeVisuals()
    {
        if (View == null)
            return;

        View.ApplyTreeVisualProfile(DisplayName, itemId);
        View.SetEffect(GetEmittedSmell());
    }

    private PlantSmell GetEmittedSmell()
    {
        return TryGetEmittedSmell(out PlantSmell emittedSmell)
            ? emittedSmell
            : PlantSmell.None;
    }

    private void EnsureItemView()
    {
        if (itemView == null)
            itemView = GetComponent<ItemView>();

        if (itemView == null)
            itemView = gameObject.AddComponent<ItemView>();
    }

    private void AutoFitInteractionCollider()
    {
        if (_interactionCollider == null)
            _interactionCollider = GetComponent<BoxCollider2D>();

        if (_interactionCollider == null || itemView == null || itemView.SpriteRenderer == null)
            return;

        Bounds worldBounds = itemView.SpriteRenderer.bounds;
        _interactionCollider.offset = transform.InverseTransformPoint(worldBounds.center);
        _interactionCollider.size = new Vector2(worldBounds.size.x, worldBounds.size.y);
    }
    
    #endregion
}

public class ItemView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Tree Visuals")]
    [SerializeField] private SpriteRenderer expressionRenderer;
    [SerializeField] private SpriteRenderer effectRenderer;
    [SerializeField] private TreeVisualDatabase treeVisualDatabase;

    [Header("Shared Bieu Cam")]
    [SerializeField] private Sprite happyExpressionSprite;
    [SerializeField] private Sprite sadExpressionSprite;

    [Header("Shared Effect")]
    [SerializeField] private Sprite goodSmellEffectSprite;
    [SerializeField] private Sprite badSmellEffectSprite;

    private TreeVisualEntry _activeEntry;

    public SpriteRenderer SpriteRenderer => spriteRenderer;

    private Transform _visualTransform;
    private Vector3 _normalScale = Vector3.one;
    private Tween _opacityTween;
    private Tween _scaleTween;
    private bool _hasBoosterSortingOverride;
    private int _normalSortingLayerId;
    private int _normalSortingOrder;

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnDisable()
    {
        _opacityTween.Stop();
        _scaleTween.Stop();
        RestoreAppearanceImmediately();
    }

    public void ShowNormal()
    {
        SetColor(Color.white);
    }

    public void ShowCorrect()
    {
        SetColor(Color.blue);
    }

    public void ShowWrong()
    {
        SetColor(Color.red);
    }

    public void ApplyTreeVisualProfile(string treeKey, string fallbackKey = "")
    {
        if (treeVisualDatabase != null)
            _activeEntry = treeVisualDatabase.FindEntry(treeKey) ?? treeVisualDatabase.FindEntry(fallbackKey);
        else
            _activeEntry = null;

        ApplyCurrentProfile();
    }

    public void SetMood(bool isHappy)
    {
        EnsureReferences();

        if (_activeEntry == null)
            return;

        Sprite targetCharacterSprite = isHappy
            ? _activeEntry.happyCharacterSprite
            : _activeEntry.sadCharacterSprite;

        if (spriteRenderer != null && targetCharacterSprite != null)
            spriteRenderer.sprite = targetCharacterSprite;

        Sprite targetExpressionSprite = isHappy
            ? happyExpressionSprite
            : sadExpressionSprite;

        if (expressionRenderer != null && targetExpressionSprite != null)
            expressionRenderer.sprite = targetExpressionSprite;
    }

    public void SetEffect(PlantSmell smell)
    {
        EnsureReferences();

        if (_activeEntry == null || effectRenderer == null)
            return;

        if (smell == PlantSmell.None)
        {
            effectRenderer.enabled = false;
            return;
        }

        effectRenderer.enabled = true;
        Sprite targetEffectSprite = smell == PlantSmell.Disgust
            ? badSmellEffectSprite
            : goodSmellEffectSprite;

        if (targetEffectSprite != null)
            effectRenderer.sprite = targetEffectSprite;
    }

    public void ApplyCurrentProfile()
    {
        EnsureReferences();

        if (_activeEntry == null)
            return;

        if (spriteRenderer != null && _activeEntry.sadCharacterSprite != null)
            spriteRenderer.sprite = _activeEntry.sadCharacterSprite;

        if (expressionRenderer != null && sadExpressionSprite != null)
            expressionRenderer.sprite = sadExpressionSprite;

        if (effectRenderer != null)
            effectRenderer.enabled = false;
    }

    public void SetDragAppearance(float scaleMultiplier, float opacity, float duration)
    {
        EnsureReferences();
        TweenScale(_normalScale * scaleMultiplier, duration);
        TweenOpacity(opacity, duration);
    }

    public void RestoreAppearance(float duration)
    {
        EnsureReferences();
        TweenScale(_normalScale, duration);
        TweenOpacity(1f, duration);
    }

    public void RestoreAppearanceImmediately()
    {
        EnsureReferences();

        if (_visualTransform != null)
            _visualTransform.localScale = _normalScale;

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }
    }

    public void SetBoosterSelectionFocus(bool focused, int sortingLayerId, int sortingOrder)
    {
        EnsureReferences();
        if (spriteRenderer == null)
            return;

        if (focused)
        {
            if (!_hasBoosterSortingOverride)
            {
                _normalSortingLayerId = spriteRenderer.sortingLayerID;
                _normalSortingOrder = spriteRenderer.sortingOrder;
                _hasBoosterSortingOverride = true;
            }

            spriteRenderer.sortingLayerID = sortingLayerId;
            spriteRenderer.sortingOrder = sortingOrder;
            return;
        }

        if (!_hasBoosterSortingOverride)
            return;

        spriteRenderer.sortingLayerID = _normalSortingLayerId;
        spriteRenderer.sortingOrder = _normalSortingOrder;
        _hasBoosterSortingOverride = false;
    }

    private void SetColor(Color color)
    {
        EnsureReferences();
        if (spriteRenderer == null)
            return;

        color.a = spriteRenderer.color.a;
        spriteRenderer.color = color;
    }

    private void TweenScale(Vector3 targetScale, float duration)
    {
        if (_visualTransform == null)
            return;

        if (_scaleTween.isAlive)
            _scaleTween.Stop();

        if (_visualTransform.localScale != targetScale)
            _scaleTween = Tween.Scale(_visualTransform, targetScale, duration);
    }

    private void TweenOpacity(float opacity, float duration)
    {
        if (spriteRenderer == null)
            return;

        if (_opacityTween.isAlive)
            _opacityTween.Stop();

        Color currentColor = spriteRenderer.color;
        Color targetColor = currentColor;
        targetColor.a = opacity;

        if (currentColor != targetColor)
            _opacityTween = Tween.Custom(currentColor, targetColor, duration, value => spriteRenderer.color = value);
    }

    private void EnsureReferences()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (_visualTransform == null && spriteRenderer != null)
        {
            _visualTransform = spriteRenderer.transform;
            _normalScale = _visualTransform.localScale;
        }
    }
}
