using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BoxCollider2D itemCollider;
    [SerializeField] private Item currentItem;
    [SerializeField] private ItemSlot startingItemSlot;

    [Header("Drag Settings")]
    [SerializeField] private float dragScaleMultiplier = 1.2f;
    [SerializeField] private float dragOpacity = 0.5f;
    [SerializeField] private float positionTweenDuration = 0.2f;
    [SerializeField] private float viewTweenDuration = 0.2f;
    [SerializeField] private LayerMask slotLayerMask;

    private Vector3 _dragOffset;
    private float _zCoordinate;
    private Vector3 _originalItemPosition;
    private ItemSlot _currentHoverItemSlot;
    private ItemSlot _sourceItemSlot;
    private float _slotZOffset;
    private Tween _positionTween;
    private SlotQueryService _slotQueryService;
    private PlacementService _placementService;

    public Item CurrentItem => currentItem;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        _slotQueryService = new SlotQueryService(slotLayerMask);
        _placementService = new PlacementService();
    }

    private void Start()
    {
        if (startingItemSlot != null)
        {
            transform.position = startingItemSlot.transform.position;
            startingItemSlot.SeCurrentItem(currentItem);
        }

        _originalItemPosition = transform.position;
    }

    private void OnDisable()
    {
        if (_positionTween.isAlive)
            _positionTween.Stop();

        if (itemCollider != null)
            itemCollider.enabled = true;

        transform.position = _originalItemPosition;

        if (currentItem != null)
            currentItem.View.RestoreAppearanceImmediately();
    }

    public bool BeginDrag(PointerEventData eventData, DragItem dragItem)
    {
        if (mainCamera == null || currentItem == null || itemCollider == null)
            return false;

        if (GameManager.Instance.CurrentState != GameState.Playing)
            return false;

        if (_positionTween.isAlive)
        {
            _positionTween.Stop();
            transform.position = _originalItemPosition;
        }

        _zCoordinate = mainCamera.WorldToScreenPoint(transform.position).z;
        Vector3 mouseWorldPosition = GetWorldPosition(eventData.position);
        _dragOffset = transform.position - mouseWorldPosition;

        currentItem.View.SetDragAppearance(dragScaleMultiplier, dragOpacity, viewTweenDuration);
        itemCollider.enabled = false;

        _sourceItemSlot = currentItem.CurrentSlot;
        if (_sourceItemSlot == null)
            _sourceItemSlot = _slotQueryService.GetSlotAt(transform.position);

        EventBus.Instance.Publish(new DragStartedEvent(dragItem));

        SetHoveredSlot(_slotQueryService.GetSlotAt(mouseWorldPosition), dragItem);
        return true;
    }

    public void Drag(PointerEventData eventData, DragItem dragItem)
    {
        if (mainCamera == null || currentItem == null)
            return;

        if (GameManager.Instance.CurrentState != GameState.Playing)
            return;

        Vector3 mouseWorldPosition = GetWorldPosition(eventData.position);
        transform.position = mouseWorldPosition + _dragOffset;
        SetHoveredSlot(_slotQueryService.GetSlotAt(mouseWorldPosition), dragItem);
    }

    public void EndDrag(PointerEventData eventData, DragItem dragItem)
    {
        if (currentItem == null)
            return;

        if (GameManager.Instance.CurrentState != GameState.Playing)
            return;

        if (itemCollider != null)
            itemCollider.enabled = true;

        if (mainCamera != null)
        {
            ItemSlot targetSlot = _slotQueryService.GetSlotAt(GetWorldPosition(eventData.position));
            TryPlaceInSlot(targetSlot);
        }

        currentItem.View.RestoreAppearance(viewTweenDuration);
        TweenToOriginalPosition();

        EventBus.Instance.Publish(new DragEndedEvent(dragItem));
        _currentHoverItemSlot = null;
        _sourceItemSlot = null;
    }

    public void ConfigureStart(ItemSlot newStartingItemSlot, float zOffset = 0f)
    {
        startingItemSlot = newStartingItemSlot;
        _slotZOffset = zOffset;

        if (startingItemSlot == null || currentItem == null)
            return;

        startingItemSlot.SeCurrentItem(currentItem);
        MoveToRestingPosition(
            startingItemSlot.transform.position + new Vector3(0f, 0f, _slotZOffset),
            animated: false);
    }

    private void TryPlaceInSlot(ItemSlot targetSlot)
    {
        if (!_placementService.CanPlace(currentItem, targetSlot))
            return;

        if (_sourceItemSlot == targetSlot)
            return;

        Vector3 sourcePosition = _sourceItemSlot != null
            ? _sourceItemSlot.transform.position + new Vector3(0f, 0f, _slotZOffset)
            : _originalItemPosition;
        Vector3 targetPosition = targetSlot.transform.position + new Vector3(0f, 0f, _slotZOffset);
        MoveItemCommand command = new MoveItemCommand(
            currentItem,
            _sourceItemSlot,
            targetSlot,
            this,
            sourcePosition,
            targetPosition);

        if (MoveManager.Instance.ExecuteMove(command))
            _originalItemPosition = targetPosition;
    }

    private void SetHoveredSlot(ItemSlot hoverSlot, DragItem dragItem)
    {
        if (_currentHoverItemSlot == hoverSlot)
            return;

        _currentHoverItemSlot = hoverSlot;
        EventBus.Instance.Publish(new DragHoverChangedEvent(dragItem, _currentHoverItemSlot));
    }

    private Vector3 GetWorldPosition(Vector2 screenPosition)
    {
        return mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, _zCoordinate));
    }

    private void TweenToOriginalPosition()
    {
        MoveToRestingPosition(_originalItemPosition, animated: true);
    }

    public void MoveToRestingPosition(Vector3 targetPosition, bool animated)
    {
        if (_positionTween.isAlive)
            _positionTween.Stop();

        _originalItemPosition = targetPosition;
        if ((transform.position - targetPosition).sqrMagnitude <= 0.000001f)
        {
            transform.position = targetPosition;
            return;
        }

        if (!animated || positionTweenDuration <= 0f)
        {
            transform.position = targetPosition;
            return;
        }

        _positionTween = Tween.Custom(
            transform.position,
            targetPosition,
            positionTweenDuration,
            value => transform.position = value)
            .OnComplete(() =>
            {
                if (BoardManager.TryGetInstance(out BoardManager boardManager))
                    boardManager.NotifyBoardChanged();
            });
    }
}
