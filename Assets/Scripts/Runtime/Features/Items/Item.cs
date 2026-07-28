using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private ItemType itemType;
    [SerializeField] private SlotType itemSlotType;
    [SerializeField] private string itemId;
    [SerializeField] private string solutionSlotId;
    [SerializeField] private PlantSmell smell;
    [SerializeField] private ItemView itemView;
    
    [SerializeReference]
    [SubclassSelector]
    private List<PlantCondition> conditions = new List<PlantCondition>();

    private ItemSlot currentSlot;
    private bool _ignoreConditionsForRun;

    private void Awake()
    {
        EnsureItemView();
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
    public string SolutionSlotId => solutionSlotId;
    public PlantSmell Smell => smell;
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
        PlantSmell newSmell,
        List<PlantCondition> newConditions,
        string newSolutionSlotId = "")
    {
        itemType = newItemType;
        itemSlotType = newItemSlotType;
        itemId = newItemId;
        smell = newSmell;
        conditions = newConditions ?? new List<PlantCondition>();
        solutionSlotId = newSolutionSlotId;
        _ignoreConditionsForRun = false;
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
    
    #endregion
}
