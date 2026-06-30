using PrimeTween;
using UnityEngine;

public class ItemSlot : MonoBehaviour
{
    #region Fields
    [Header("Visual Elements")]
    [SerializeField] private SpriteRenderer hoverIndicatorSprite;
    
    [Header("Animation Settiing")]
    [SerializeField] private float dragStateScale;
    [SerializeField] private float scaleTransitionDuration = 0.2f;
    
    private SlotVisualState _currentVisualState = SlotVisualState.Idle;
    private Tween _scaleTween;
    #endregion

    #region LifeCycle
    private void OnEnable()
    {
        DragItem.OnDragStart.AddListener(HandleDragStart);
        DragItem.OnDragEnd.AddListener(HandleDragEnd);
        DragItem.OnHoverChanged.AddListener(HandleHoverChanged);
    }

    private void Start()
    {
        hoverIndicatorSprite.transform.localScale = Vector3.zero;
    }   

    private void OnDisable()
    {
        DragItem.OnDragStart.RemoveListener(HandleDragStart);
        DragItem.OnDragEnd.RemoveListener(HandleDragEnd);
        DragItem.OnHoverChanged.RemoveListener(HandleHoverChanged);
        if(_scaleTween.isAlive) _scaleTween.Stop();
    }
    #endregion

    #region EventHanlders

    private void HandleDragStart(DragItem item)
    {
        SetVisualState(SlotVisualState.ActiveDrag);
    }

    private void HandleDragEnd(DragItem item)
    {
        SetVisualState(SlotVisualState.Idle);
    }

    private void HandleHoverChanged(DragItem item, ItemSlot currentHoverItemSlot)
    {
        if (currentHoverItemSlot == this)
        {
            SetVisualState(SlotVisualState.Hovered);
        }
        else
        {
            SetVisualState(SlotVisualState.ActiveDrag);
        }
    }

    #endregion
    
    #region PrivateMethods
    private void ApplyVisualState()
    {
        switch (_currentVisualState)
        {
            case SlotVisualState.ActiveDrag:
                TweenHoverIndicatorScale(dragStateScale);
                break;
            case SlotVisualState.Idle:
                TweenHoverIndicatorScale(0f);
                break;
            case SlotVisualState.Hovered:
                TweenHoverIndicatorScale(1);
                break;
        }
    }
    
    private void TweenHoverIndicatorScale(float newSize)
    {
        if(_scaleTween.isAlive)
            _scaleTween.Stop();
        _scaleTween = Tween.Scale(hoverIndicatorSprite.transform, newSize, scaleTransitionDuration);
    }
    #endregion
    
    #region PublicMethods
    public void SetVisualState(SlotVisualState visualState)
    {
        if(_currentVisualState == visualState) return;

        _currentVisualState = visualState;
        ApplyVisualState();
    }
    #endregion
    
}

