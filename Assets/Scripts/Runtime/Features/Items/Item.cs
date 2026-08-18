using System.Collections.Generic;
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
