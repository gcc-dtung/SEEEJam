using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class TooltipPopup : MonoBehaviour
{
    [Header("Background Sprites")]
    [SerializeField] private SpriteRenderer top;
    [SerializeField] private SpriteRenderer middle;
    [SerializeField] private SpriteRenderer bottom;

    [Header("Text")]
    [SerializeField] private TextMeshPro nameText;
    [SerializeField] private TextMeshPro contentText;

    [Header("Layout")]
    [SerializeField] private float textWidth = 2f;
    [SerializeField] private float verticalPadding = 0.15f;
    [SerializeField] private int textSortingOrderOffset = 1;

    [Header("Tween")]
    [SerializeField] private Transform tooltipTransform;
    [SerializeField] private float durationTween = 0.2f;

    private static TooltipPopup _activeTooltip;

    private Item _ownerItem;
    private Sequence _tooltipSequence;
    private SortingGroup _sortingGroup;
    private Camera _mainCamera;
    private int _shownFrame = -1;
    private bool _isShowing;

    private void Awake()
    {
        _ownerItem = GetComponentInParent<Item>();
        _mainCamera = Camera.main;

        if (tooltipTransform == null)
            tooltipTransform = transform;

        ConfigureSorting();
        HideImmediate();
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<ItemPointerStateChangedEvent>(HandlePointerStateChanged);
        EventBus.Instance.Subscribe<DragStartedEvent>(HandleDragStarted);
        EventBus.Instance.Subscribe<GameStateChangedEvent>(HandleGameStateChanged);
    }

    private void OnDisable()
    {
        if (EventBus.TryGetInstance(out EventBus eventBus))
        {
            eventBus.Unsubscribe<ItemPointerStateChangedEvent>(HandlePointerStateChanged);
            eventBus.Unsubscribe<DragStartedEvent>(HandleDragStarted);
            eventBus.Unsubscribe<GameStateChangedEvent>(HandleGameStateChanged);
        }

        if (_activeTooltip == this)
            _activeTooltip = null;

        if (_tooltipSequence.isAlive)
            _tooltipSequence.Stop();
    }

    private void Update()
    {
        if (!_isShowing || _shownFrame == Time.frameCount)
            return;

        if (!TryGetPointerDownPosition(out Vector2 screenPosition))
            return;

        if (!IsPointerOverOwner(screenPosition))
            Hide();
    }

    private void HandlePointerStateChanged(ItemPointerStateChangedEvent gameEvent)
    {
        if (gameEvent.Item == null)
            return;

        if (gameEvent.State != ItemPointerState.OnPress)
            return;

        if (gameEvent.Item == _ownerItem)
            Show(_ownerItem.DisplayName, BuildTooltipContent(_ownerItem));
        else
            Hide();
    }

    private void HandleDragStarted(DragStartedEvent gameEvent)
    {
        if (gameEvent.DragItem != null && gameEvent.DragItem.CurrentDragItem == _ownerItem)
            HideImmediate();
    }

    private void HandleGameStateChanged(GameStateChangedEvent gameEvent)
    {
        if (gameEvent.CurrentState != GameState.Playing)
            Hide();
    }

    public void Show(string itemName, string content)
    {
        if (_ownerItem == null || tooltipTransform == null)
            return;

        if (_activeTooltip != null && _activeTooltip != this)
            _activeTooltip.Hide();

        _activeTooltip = this;
        _isShowing = true;
        _shownFrame = Time.frameCount;
        SetTooltipVisible(true);

        nameText.text = itemName;
        contentText.text = string.IsNullOrWhiteSpace(content) ? "No conditions." : content;

        contentText.textWrappingMode = TextWrappingModes.Normal;
        contentText.overflowMode = TextOverflowModes.Overflow;
        contentText.rectTransform.sizeDelta = new Vector2(textWidth, 100f);
        contentText.ForceMeshUpdate();

        float textHeight = contentText.GetPreferredValues(contentText.text, textWidth, Mathf.Infinity).y;
        float middleHeight = textHeight + verticalPadding * 2f;

        ResizeInternal(middleHeight);

        float bottomHeight = bottom.sprite.bounds.size.y * bottom.transform.localScale.y;
        float topHeight = top.sprite.bounds.size.y * top.transform.localScale.y;
        float middleCenterY = bottomHeight * 0.5f + middleHeight * 0.5f;

        contentText.alignment = TextAlignmentOptions.TopLeft;
        contentText.rectTransform.pivot = new Vector2(0f, 1f);
        contentText.rectTransform.sizeDelta = new Vector2(textWidth, textHeight);
        contentText.transform.localPosition = new Vector3(
            -textWidth * 0.5f,
            middleCenterY + middleHeight * 0.5f - verticalPadding,
            -0.1f);

        float topCenterY = bottomHeight * 0.5f + middleHeight + topHeight * 0.5f;
        nameText.transform.localPosition = new Vector3(0f, topCenterY, -0.1f);

        ClampToCamera();
        ConfigureSorting();
        PlayTransition(1f, 1f);
    }

    public void Hide()
    {
        if (!_isShowing || tooltipTransform == null)
            return;

        _isShowing = false;
        if (_activeTooltip == this)
            _activeTooltip = null;

        PlayTransition(0f, 0f, () => SetTooltipVisible(false));
    }

    public void HideImmediate()
    {
        if (_tooltipSequence.isAlive)
            _tooltipSequence.Stop();

        _isShowing = false;
        if (_activeTooltip == this)
            _activeTooltip = null;

        if (top != null)
            ChangeColor(new Color(top.color.r, top.color.g, top.color.b, 0f));

        if (tooltipTransform != null)
        {
            tooltipTransform.localScale = Vector3.zero;
            SetTooltipVisible(false);
        }
    }

    private void ConfigureSorting()
    {
        if (tooltipTransform == null)
            return;

        _sortingGroup = tooltipTransform.GetComponent<SortingGroup>();
        if (_sortingGroup == null)
            _sortingGroup = tooltipTransform.gameObject.AddComponent<SortingGroup>();

        SpriteRenderer itemRenderer = _ownerItem != null
            ? _ownerItem.GetComponentInChildren<SpriteRenderer>()
            : null;

        if (itemRenderer == null)
        {
            _sortingGroup.sortingOrder = 30000;
            return;
        }

        _sortingGroup.sortingLayerID = itemRenderer.sortingLayerID;
        _sortingGroup.sortingOrder = itemRenderer.sortingOrder + 50;

        if (nameText != null)
            nameText.sortingOrder = _sortingGroup.sortingOrder + textSortingOrderOffset;

        if (contentText != null)
            contentText.sortingOrder = _sortingGroup.sortingOrder + textSortingOrderOffset;
    }

    private void PlayTransition(float toScale, float toAlpha, System.Action onComplete = null)
    {
        if (_tooltipSequence.isAlive)
            _tooltipSequence.Stop();

        Sequence sequence = Sequence.Create();
        bool hasAnyTween = false;

        if (!Mathf.Approximately(tooltipTransform.localScale.x, toScale))
        {
            Tween scaleTween = Tween.Scale(tooltipTransform, toScale, durationTween);
            sequence = hasAnyTween ? sequence.Group(scaleTween) : sequence.Chain(scaleTween);
            hasAnyTween = true;
        }

        if (top != null && !Mathf.Approximately(top.color.a, toAlpha))
        {
            Color oldColor = top.color;
            Color newColor = oldColor;
            newColor.a = toAlpha;

            Tween alphaTween = Tween.Custom(oldColor, newColor, durationTween / 2f, ChangeColor);
            sequence = hasAnyTween ? sequence.Group(alphaTween) : sequence.Chain(alphaTween);
            hasAnyTween = true;
        }

        if (!hasAnyTween)
        {
            onComplete?.Invoke();
            return;
        }

        _tooltipSequence = sequence;
        if (onComplete != null)
            _tooltipSequence.OnComplete(onComplete);
    }

    private void ClampToCamera()
    {
        if (_mainCamera == null || tooltipTransform == null)
            return;

        // Tooltip sprites are centered on tooltipTransform.position.x
        float tooltipHalfWidth  = middle.sprite.bounds.size.x * middle.transform.lossyScale.x * 0.5f;

        // Total tooltip height so we can clamp the top edge too
        float bottomH = bottom.sprite.bounds.size.y * bottom.transform.lossyScale.y;
        float middleH = middle.sprite.bounds.size.y * middle.transform.lossyScale.y;
        float topH    = top.sprite.bounds.size.y    * top.transform.lossyScale.y;
        float tooltipTotalHeight = bottomH + middleH + topH;

        // Convert camera viewport corners to world space (z = 0 for 2D)
        float depth = Mathf.Abs(_mainCamera.transform.position.z);
        Vector3 bottomLeft  = _mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        Vector3 topRight    = _mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, depth));

        float camLeft   = bottomLeft.x;
        float camRight  = topRight.x;
        float camTop    = topRight.y;

        const float margin = 0.05f;

        Vector3 pos = tooltipTransform.position;

        // Horizontal: clamp so neither left nor right edge goes outside
        pos.x = Mathf.Clamp(pos.x,
            camLeft  + tooltipHalfWidth  + margin,
            camRight - tooltipHalfWidth  - margin);

        // Vertical: if the tooltip top goes above the camera, push it down
        float tooltipTop = pos.y + tooltipTotalHeight;
        if (tooltipTop > camTop - margin)
            pos.y -= tooltipTop - (camTop - margin);

        tooltipTransform.position = pos;
    }

    private void ResizeInternal(float desiredMiddleHeight)
    {
        float bottomHeight = bottom.sprite.bounds.size.y * bottom.transform.localScale.y;
        float middleBaseHeight = middle.sprite.bounds.size.y;
        float topHeight = top.sprite.bounds.size.y * top.transform.localScale.y;

        float scaleY = desiredMiddleHeight / middleBaseHeight;
        middle.transform.localScale = new Vector3(
            middle.transform.localScale.x,
            scaleY,
            middle.transform.localScale.z);

        float actualMiddleHeight = middleBaseHeight * scaleY;

        bottom.transform.localPosition = Vector3.zero;
        middle.transform.localPosition = new Vector3(
            0f,
            bottomHeight * 0.5f + actualMiddleHeight * 0.5f,
            0f);

        top.transform.localPosition = new Vector3(
            0f,
            bottomHeight * 0.5f + actualMiddleHeight + topHeight * 0.5f,
            0f);
    }

    private void ChangeColor(Color color)
    {
        if (top != null)
            top.color = color;

        if (middle != null)
            middle.color = color;

        if (bottom != null)
            bottom.color = color;

        if (nameText != null)
        {
            Color textColor = nameText.color;
            textColor.a = color.a;
            nameText.color = textColor;
        }

        if (contentText != null)
        {
            Color textColor = contentText.color;
            textColor.a = color.a;
            contentText.color = textColor;
        }
    }

    private void SetTooltipVisible(bool visible)
    {
        if (tooltipTransform == null || tooltipTransform == transform)
            return;

        tooltipTransform.gameObject.SetActive(visible);
    }

    private bool IsPointerOverOwner(Vector2 screenPosition)
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null || _ownerItem == null)
            return false;

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider != null && hitCollider.GetComponentInParent<Item>() == _ownerItem)
                return true;
        }

        return false;
    }

    private static bool TryGetPointerDownPosition(out Vector2 screenPosition)
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null)
        {
            foreach (UnityEngine.InputSystem.Controls.TouchControl touch in Touchscreen.current.touches)
            {
                if (!touch.press.wasPressedThisFrame)
                    continue;

                screenPosition = touch.position.ReadValue();
                return true;
            }
        }

        screenPosition = default;
        return false;
    }

    private static string BuildTooltipContent(Item item)
    {
        if (item == null || item.Conditions == null || item.Conditions.Count == 0)
            return "No conditions.";

        List<string> lines = new List<string>();
        foreach (PlantCondition condition in item.Conditions)
        {
            if (condition != null)
                lines.Add(condition.GetDescription());
        }

        return lines.Count > 0 ? string.Join("\n", lines) : "No conditions.";
    }
}
