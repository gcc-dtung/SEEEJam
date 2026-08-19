using System.Collections;
using PrimeTween;
using UnityEngine;

public class ItemSlotView : MonoBehaviour
{
    private static readonly Color NormalSlotColor = new Color32(0x63, 0x49, 0x2B, 0xFF);
    private static readonly Color BadSmellSlotColor = new Color32(0x2D, 0x6C, 0x3E, 0xFF);
    private static readonly Color GoodSmellSlotColor = new Color32(0xF2, 0xB0, 0xBF, 0xFF);

    [SerializeField] private SpriteRenderer hoverIndicatorSprite;
    [SerializeField] private float activeDragScale = 0.7f;
    [SerializeField] private float hintScale = 1.15f;
    [SerializeField] private float scaleTransitionDuration = 0.2f;

    private Tween _scaleTween;
    private ItemSlot _itemSlot;
    private Coroutine _hintCoroutine;
    private SlotVisualState _visualState;
    private bool _canPlaceItem;
    private bool _isShowingHint;

    private void Awake()
    {
        _itemSlot = GetComponent<ItemSlot>();
        EnsureHoverIndicator();
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<HintRevealedEvent>(HandleHintRevealed);
        EventBus.Instance.Subscribe<BoardChangedEvent>(HandleBoardChanged);
    }

    private void OnDisable()
    {
        if (EventBus.TryGetInstance(out EventBus eventBus))
        {
            eventBus.Unsubscribe<HintRevealedEvent>(HandleHintRevealed);
            eventBus.Unsubscribe<BoardChangedEvent>(HandleBoardChanged);
        }

        _scaleTween.Stop();

        if (_hintCoroutine != null)
            StopCoroutine(_hintCoroutine);

        _hintCoroutine = null;
        _isShowingHint = false;
    }

    public void ShowState(SlotVisualState visualState, bool canPlaceItem)
    {
        _visualState = visualState;
        _canPlaceItem = canPlaceItem;

        ApplyColor(visualState, GetHoveredItemSmell());

        float targetScale = _isShowingHint
            ? hintScale
            : GetTargetScale(_visualState, _canPlaceItem);
        TweenHoverIndicatorScale(targetScale);
    }

    public void HideImmediately()
    {
        EnsureHoverIndicator();
        if (hoverIndicatorSprite != null)
        {
            hoverIndicatorSprite.transform.localScale = Vector3.zero;
            hoverIndicatorSprite.color = NormalSlotColor;
        }
    }

    private float GetTargetScale(SlotVisualState visualState, bool canPlaceItem)
    {
        if (!canPlaceItem)
            return 0f;

        switch (visualState)
        {
            case SlotVisualState.ActiveDrag:
                return activeDragScale;
            case SlotVisualState.Hovered:
                return 1f;
            default:
                return 0f;
        }
    }

    private void HandleHintRevealed(HintRevealedEvent gameEvent)
    {
        if (gameEvent.TargetSlot != _itemSlot)
            return;

        if (_hintCoroutine != null)
            StopCoroutine(_hintCoroutine);

        _hintCoroutine = StartCoroutine(ShowHint(gameEvent.Duration));
    }

    private void HandleBoardChanged(BoardChangedEvent gameEvent)
    {
        HideHint();
    }

    private IEnumerator ShowHint(float duration)
    {
        _isShowingHint = true;
        TweenHoverIndicatorScale(hintScale);
        yield return new WaitForSeconds(Mathf.Max(0.1f, duration));
        HideHint();
    }

    private void HideHint()
    {
        if (!_isShowingHint && _hintCoroutine == null)
            return;

        if (_hintCoroutine != null)
            StopCoroutine(_hintCoroutine);

        _isShowingHint = false;
        _hintCoroutine = null;
        TweenHoverIndicatorScale(GetTargetScale(_visualState, _canPlaceItem));
    }

    private void TweenHoverIndicatorScale(float targetScale)
    {
        EnsureHoverIndicator();
        if (hoverIndicatorSprite == null)
            return;

        if (_scaleTween.isAlive)
            _scaleTween.Stop();

        if (hoverIndicatorSprite.transform.localScale != targetScale * Vector3.one)
            _scaleTween = Tween.Scale(hoverIndicatorSprite.transform, targetScale, scaleTransitionDuration);
    }

    private void ApplyColor(SlotVisualState visualState, PlantSmell hoveredItemSmell)
    {
        EnsureHoverIndicator();
        if (hoverIndicatorSprite == null)
            return;

        Color targetColor = NormalSlotColor;
        if (visualState == SlotVisualState.Hovered)
            targetColor = GetSmellColor(hoveredItemSmell);

        hoverIndicatorSprite.color = targetColor;
    }

    private PlantSmell GetHoveredItemSmell()
    {
        if (_itemSlot == null || _itemSlot.HoverItem == null)
            return PlantSmell.None;

        return _itemSlot.HoverItem.TryGetEmittedSmell(out PlantSmell hoveredSmell)
            ? hoveredSmell
            : PlantSmell.None;
    }

    private static Color GetSmellColor(PlantSmell smell)
    {
        switch (smell)
        {
            case PlantSmell.Disgust:
                return BadSmellSlotColor;
            case PlantSmell.Perfume:
                return GoodSmellSlotColor;
            default:
                return NormalSlotColor;
        }
    }

    private void EnsureHoverIndicator()
    {
        if (hoverIndicatorSprite == null)
            hoverIndicatorSprite = GetComponentInChildren<SpriteRenderer>();
    }
}
