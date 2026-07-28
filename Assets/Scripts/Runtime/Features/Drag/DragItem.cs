using UnityEngine.EventSystems;
using UnityEngine;

[RequireComponent(typeof(DragController))]
public class DragItem : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private DragController dragController;
    private bool _boosterSelectionPointer;
    private ItemPointerState _state = ItemPointerState.Idle;

    public Item CurrentDragItem => dragController != null ? dragController.CurrentItem : null;
    public ItemPointerState State => _state;

    private void Awake()
    {
        if (dragController == null)
            dragController = GetComponent<DragController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _boosterSelectionPointer = BoosterManager.Instance.TryHandleItemSelection(CurrentDragItem);
        if (_boosterSelectionPointer)
            return;

        if (GameManager.Instance.CurrentState != GameState.Playing)
            return;

        SetState(ItemPointerState.OnPress, eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_boosterSelectionPointer)
            return;

        if (GameManager.Instance.CurrentState != GameState.Playing)
            return;

        if (_state != ItemPointerState.OnPress)
            return;

        if (!dragController.BeginDrag(eventData, this))
        {
            SetState(ItemPointerState.Idle, eventData);
            return;
        }

        SetState(ItemPointerState.StartDrag, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_boosterSelectionPointer)
            return;

        if (GameManager.Instance.CurrentState != GameState.Playing)
            return;

        if (_state != ItemPointerState.StartDrag && _state != ItemPointerState.Drag)
            return;

        dragController.Drag(eventData, this);
        SetState(ItemPointerState.Drag, eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_boosterSelectionPointer)
        {
            _boosterSelectionPointer = false;
            return;
        }

        if (GameManager.Instance.CurrentState != GameState.Playing)
            return;

        if (_state == ItemPointerState.StartDrag || _state == ItemPointerState.Drag)
        {
            dragController.EndDrag(eventData, this);
            SetState(ItemPointerState.EndDrag, eventData);
        }
        else if (_state == ItemPointerState.OnPress)
        {
            SetState(ItemPointerState.EndPress, eventData);
        }

        SetState(ItemPointerState.Idle, eventData);
    }

    public void ConfigureStart(ItemSlot newStartingItemSlot, float zOffset = 0f)
    {
        dragController.ConfigureStart(newStartingItemSlot, zOffset);
    }

    private void SetState(ItemPointerState state, PointerEventData eventData)
    {
        _state = state;
        EventBus.Instance.Publish(new ItemPointerStateChangedEvent(
            this,
            CurrentDragItem,
            _state,
            eventData.position));
    }
}

