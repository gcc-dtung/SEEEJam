using UnityEngine;
using UnityEngine.EventSystems;
using PrimeTween;
using UnityEngine.Events;

public class DragItem : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    #region Event
    public static UnityEvent<DragItem>  OnDragStart = new UnityEvent<DragItem>();
    public static UnityEvent<DragItem> OnDragEnd = new UnityEvent<DragItem>();
    public static UnityEvent<DragItem, ItemSlot> OnHoverChanged = new UnityEvent<DragItem, ItemSlot>();
    #endregion
    
    #region Field
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private SpriteRenderer itemViewRenderer;
    [SerializeField] private BoxCollider2D itemCollider;
    
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
    
    private Tween _positionTween;
    private Tween _opacityTween;
    private Tween _scaleTween;
    #endregion

    #region LifeCycle
    private void Awake()
    {
        if(mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Start()
    {
        _originalItemPosition = transform.position;
    }
    
    private void OnDisable()
    {
        _positionTween.Stop();
        _opacityTween.Stop();
        _scaleTween.Stop();
        
        itemCollider.enabled = true;
        transform.position = _originalItemPosition;
        transform.localScale = Vector3.one;
        
        if(itemViewRenderer != null)
        {
            Color resetColor = itemViewRenderer.color;
            resetColor.a = 1f;
            itemViewRenderer.color = resetColor;
        }
    }
    #endregion

    #region PointerCycle
    public void OnPointerDown(PointerEventData eventData)
    {
        if(_positionTween.isAlive)
            _positionTween.Stop();
        
        _zCoordinate = mainCamera.WorldToScreenPoint(transform.position).z;
        Vector3 mouseWorldPos = GetMousePos(eventData.position);
        _dragOffset = transform.position - mouseWorldPos;
        
        ChangeViewItem(dragScaleMultiplier, dragOpacity);
        itemCollider.enabled = false;
        
        OnDragStart?.Invoke(this);
        
        ItemSlot startingItemSlot = GetSlotAtPosition(mouseWorldPos);
        if (startingItemSlot != null)
        {
            _currentHoverItemSlot = startingItemSlot;
            OnHoverChanged?.Invoke(this, _currentHoverItemSlot);
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        Vector3 mouseWorldPos = GetMousePos(eventData.position);
        transform.position = mouseWorldPos + _dragOffset;
        
        ItemSlot hoverItemSlot = GetSlotAtPosition(mouseWorldPos);

        if (_currentHoverItemSlot != hoverItemSlot)
        {
            _currentHoverItemSlot = hoverItemSlot;
            OnHoverChanged?.Invoke(this, _currentHoverItemSlot);
        }

        
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        Collider2D hit = Physics2D.OverlapPoint(GetMousePos(eventData.position), slotLayerMask);
        if (hit != null)
        {
            ItemSlot itemSlot = hit.GetComponent<ItemSlot>();
            if (itemSlot != null)
            {
                _originalItemPosition = itemSlot.transform.position;
            }
        }
        ChangeViewItem(1f, 1f);
        TweenToPosition(transform.position, _originalItemPosition, positionTweenDuration);
        itemCollider.enabled = true;
        
        OnDragEnd?.Invoke(this);
        _currentHoverItemSlot = null;
    }
    #endregion

    #region PrivateMethods
    private Vector3 GetMousePos(Vector3 pos)
    {
        return mainCamera.ScreenToWorldPoint(new Vector3(pos.x, pos.y, _zCoordinate));
    }

    private void ChangeViewItem(float viewScale, float viewOpacity)
    {
        if(_scaleTween.isAlive)
            _scaleTween.Stop();
        Tween.Scale(transform, viewScale, viewTweenDuration);
        Color currentColor = itemViewRenderer.color;
        Color newColor = itemViewRenderer.color;
        newColor.a = viewOpacity;
        if(_opacityTween.isAlive)
            _opacityTween.Stop();
        Tween.Custom(currentColor, newColor, duration: viewTweenDuration, onValueChange: newVal => itemViewRenderer.color = newVal);
    }

    private void TweenToPosition(Vector3 from, Vector3 to, float duration)
    {
        if(_positionTween.isAlive)
            _positionTween.Stop();
        _positionTween = Tween.Custom(from, to, duration: duration, onValueChange : newVal => transform.position = newVal);
    }
    

    private ItemSlot GetSlotAtPosition(Vector3 worldPosition)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPosition, slotLayerMask);
        if (hit != null)
        {
            return  hit.GetComponent<ItemSlot>();
        }

        return null;
    }
    
    #endregion
    
}

