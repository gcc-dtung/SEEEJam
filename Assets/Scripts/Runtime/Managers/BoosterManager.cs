using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum BoosterSelectionMode
{
    None,
    RemoveConditions,
    Hint
}

public class BoosterManager : SingletonMonoBehaviour<BoosterManager>
{
    [SerializeField] private float hintDuration = 2.5f;

    private Camera _mainCamera;

    public BoosterSelectionMode SelectionMode { get; private set; }
    public bool IsSelectingItem => SelectionMode != BoosterSelectionMode.None;
    public bool CanUseUndo => MoveManager.Instance.CanUndoLastMove;

    protected override void Awake()
    {
        base.Awake();
        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        EnsureSelectionOverlay();
        EventBus.Instance.Subscribe<GameStateChangedEvent>(HandleGameStateChanged);
    }

    private void OnDisable()
    {
        if (EventBus.TryGetInstance(out EventBus eventBus))
            eventBus.Unsubscribe<GameStateChangedEvent>(HandleGameStateChanged);
    }

    private void Update()
    {
        if (!IsSelectingItem || GameManager.Instance.CurrentState != GameState.Playing)
            return;

        if (!TryGetPointerDownPosition(out Vector2 screenPosition, out int pointerId))
            return;

        if (IsPointerOverUi(pointerId) || IsPointerOverItem(screenPosition))
            return;

        CancelSelection();
    }

    public bool UseUndo()
    {
        CancelSelection();
        if (!MoveManager.Instance.UndoLastMove())
        {
            Debug.LogWarning(
                "[BoosterManager] Undo rejected because there is no valid move to undo. " +
                "CanUndo=" + MoveManager.Instance.CanUndoLastMove +
                ", GameState=" + GameManager.Instance.CurrentState);
            return false;
        }

        return true;
    }

    public bool BeginRemoveConditionsSelection()
    {
        return BeginSelection(BoosterSelectionMode.RemoveConditions);
    }

    public bool BeginHintSelection()
    {
        return BeginSelection(BoosterSelectionMode.Hint);
    }

    public void CancelSelection()
    {
        if (SelectionMode == BoosterSelectionMode.None)
            return;

        SelectionMode = BoosterSelectionMode.None;
        EventBus.Instance.Publish(new BoosterSelectionChangedEvent(SelectionMode));
    }

    public void ResetForLevel()
    {
        CancelSelection();
    }

    public bool TryHandleItemSelection(Item item)
    {
        if (!IsSelectingItem)
            return false;

        if (GameManager.Instance.CurrentState != GameState.Playing)
        {
            CancelSelection();
            return true;
        }

        if (item == null)
            return true;

        BoosterType boosterType = GetBoosterType(SelectionMode);
        if (!BoosterInventoryManager.Instance.HasBooster(boosterType))
        {
            CancelSelection();
            return true;
        }

        bool didApply;
        switch (SelectionMode)
        {
            case BoosterSelectionMode.RemoveConditions:
                didApply = item.TryIgnoreConditionsForRun();
                if (didApply)
                    BoardManager.Instance.NotifyRulesChanged();
                break;

            case BoosterSelectionMode.Hint:
                didApply = TryRevealHint(item);
                break;

            default:
                didApply = false;
                break;
        }

        if (didApply)
        {
            ConsumeSuccessfulBooster(boosterType);
            CancelSelection();
        }

        return true;
    }

    private bool BeginSelection(BoosterSelectionMode mode)
    {
        if (GameManager.Instance.CurrentState != GameState.Playing)
            return false;

        if (!BoosterInventoryManager.Instance.HasBooster(GetBoosterType(mode)))
            return false;

        SelectionMode = mode;
        EventBus.Instance.Publish(new BoosterSelectionChangedEvent(SelectionMode));
        return true;
    }

    private bool TryRevealHint(Item item)
    {
        if (string.IsNullOrWhiteSpace(item.SolutionSlotId) ||
            !BoardManager.Instance.TryGetBoardSlot(item.SolutionSlotId, out ItemSlot targetSlot))
        {
            Debug.LogWarning("[BoosterManager] No valid solution slot is configured for item: " + item.ItemId);
            return false;
        }

        if (targetSlot.Type != item.ItemSlotType)
        {
            Debug.LogWarning("[BoosterManager] Solution slot type does not match item: " + item.ItemId);
            return false;
        }

        EventBus.Instance.Publish(new HintRevealedEvent(item, targetSlot, hintDuration));
        return true;
    }

    private static bool TryGetPointerDownPosition(out Vector2 screenPosition, out int pointerId)
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            pointerId = -1;
            return true;
        }

        if (Touchscreen.current != null)
        {
            foreach (UnityEngine.InputSystem.Controls.TouchControl touch in Touchscreen.current.touches)
            {
                if (!touch.press.wasPressedThisFrame)
                    continue;

                screenPosition = touch.position.ReadValue();
                pointerId = touch.touchId.ReadValue();
                return true;
            }
        }

        screenPosition = default;
        pointerId = -1;
        return false;
    }

    private static bool IsPointerOverUi(int pointerId)
    {
        if (EventSystem.current == null)
            return false;

        return pointerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(pointerId)
            : EventSystem.current.IsPointerOverGameObject();
    }

    private bool IsPointerOverItem(Vector2 screenPosition)
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null)
            return false;

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider != null && hitCollider.GetComponentInParent<Item>() != null)
                return true;
        }

        return false;
    }

    private void HandleGameStateChanged(GameStateChangedEvent gameEvent)
    {
        if (gameEvent.CurrentState != GameState.Playing)
            CancelSelection();
    }

    private void EnsureSelectionOverlay()
    {
        if (GetComponent<BoosterSelectionOverlay>() == null)
            gameObject.AddComponent<BoosterSelectionOverlay>();
    }

    private static BoosterType GetBoosterType(BoosterSelectionMode mode)
    {
        switch (mode)
        {
            case BoosterSelectionMode.RemoveConditions:
                return BoosterType.RemoveConditions;
            case BoosterSelectionMode.Hint:
                return BoosterType.Hint;
            default:
                return BoosterType.Undo;
        }
    }

    private static void ConsumeSuccessfulBooster(BoosterType boosterType)
    {
        if (!BoosterInventoryManager.Instance.TryConsume(boosterType))
            Debug.LogError("[BoosterManager] Booster succeeded but inventory could not be consumed: " + boosterType);
    }
}
