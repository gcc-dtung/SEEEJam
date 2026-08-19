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

    [Header("Clamp")]
    [Tooltip("Name of the layer containing the ToolTipCollider used to constrain tooltip positions.")]
    [SerializeField] private string clampLayerName = "ToolTipCollider";

    // runtime reference to the collider we'll clamp inside
    private Collider2D _clampCollider;

    [Header("Icons")]
    [Tooltip("Sprite Asset used for inline icons in tooltip text. Put both icons in this single Sprite Asset.")]
    [SerializeField] private TMPro.TMP_SpriteAsset tooltipSpriteAsset;
    [Tooltip("Index of the 'V' sprite inside the sprite asset (0-based).")]
    [SerializeField] private int spriteIndexV = 0;
    [Tooltip("Index of the 'X' sprite inside the sprite asset (0-based).")]
    [SerializeField] private int spriteIndexX = 1;
    [Header("Separate Icons")]
    [Tooltip("Sprite used for satisfied (V) icon.")]
    [SerializeField] private Sprite iconV;
    [Tooltip("Sprite used for unsatisfied (X) icon.")]
    [SerializeField] private Sprite iconX;
    [Tooltip("World scale for icon renderers.")]
    [SerializeField] private float iconScale = 0.01f;
    [Tooltip("Horizontal offset from left edge of text (world units).")]
    [SerializeField] private float iconOffsetX = -0.2f;

    private static TooltipPopup _activeTooltip;

    private Item _ownerItem;
    private Sequence _tooltipSequence;
    private SortingGroup _sortingGroup;
    private Camera _mainCamera;
    private int _shownFrame = -1;
    private bool _isShowing;
    private readonly List<SpriteRenderer> _iconPool = new List<SpriteRenderer>();

    private void Awake()
    {
        _ownerItem = GetComponentInParent<Item>();
        _mainCamera = Camera.main;

        if (tooltipTransform == null)
            tooltipTransform = transform;

        ConfigureSorting();
        HideImmediate();

        // Try to locate a collider on the configured layer
        FindClampCollider();

        // Ensure tooltip visuals do not block input raycasts
        SetIgnoreRaycastLayerRecursive(tooltipTransform != null ? tooltipTransform.gameObject : gameObject);

        // If a TMP sprite asset was provided, assign it to the content text so
        // inline <sprite=..> tags resolve automatically.
        if (tooltipSpriteAsset != null && contentText != null)
            contentText.spriteAsset = tooltipSpriteAsset;
    }

    private void SetIgnoreRaycastLayerRecursive(GameObject go)
    {
        if (go == null)
            return;

        int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreLayer >= 0)
            go.layer = ignoreLayer;

        for (int i = 0; i < go.transform.childCount; i++)
        {
            Transform child = go.transform.GetChild(i);
            if (child != null)
                SetIgnoreRaycastLayerRecursive(child.gameObject);
        }
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
        if (!_isShowing)
            return;

        // Always update tooltip position while visible so first-show is correct
        PositionNearOwner();
        ClampToCamera();

        // Don't process pointer-down hide logic on the same frame the tooltip was shown
        if (_shownFrame == Time.frameCount)
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

        PositionNearOwner();
        ClampToCamera();
        ConfigureSorting();
        PlayTransition(1f, 1f);

        // Update per-line icons (V/X) rendered next to text lines
        UpdateConditionIcons();
    }

    private void PositionNearOwner()
    {
        if (_ownerItem == null || tooltipTransform == null)
            return;

        SpriteRenderer ownerRenderer = null;
        if (_ownerItem != null && _ownerItem.View != null)
            ownerRenderer = _ownerItem.View.SpriteRenderer;

        if (ownerRenderer == null)
            ownerRenderer = _ownerItem.GetComponentInChildren<SpriteRenderer>();

        if (ownerRenderer == null)
            return;

        Bounds b = ownerRenderer.bounds;
        float yOffset = 0.05f; // small gap above the sprite
        Vector3 pos = new Vector3(b.center.x, b.max.y + yOffset, tooltipTransform.position.z);
        tooltipTransform.position = pos;
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
        if (tooltipTransform == null)
            return;

        // Ensure we have a clamp collider reference (it may be created/activated after Awake)
        if (_clampCollider == null)
            FindClampCollider();

        // Tooltip sizes in world units
        float tooltipHalfWidth  = middle.sprite.bounds.size.x * middle.transform.lossyScale.x * 0.5f;
        float bottomH = bottom.sprite.bounds.size.y * bottom.transform.lossyScale.y;
        float middleH = middle.sprite.bounds.size.y * middle.transform.lossyScale.y;
        float topH    = top.sprite.bounds.size.y    * top.transform.lossyScale.y;
        float tooltipTotalHeight = bottomH + middleH + topH;

        const float margin = 0.01f;

        Vector3 pos = tooltipTransform.position;

        if (_clampCollider != null)
        {
            Bounds b = _clampCollider.bounds;

            // Horizontal clamp (pos.x is center-aligned horizontally)
            pos.x = Mathf.Clamp(pos.x,
                b.min.x + tooltipHalfWidth + margin,
                b.max.x - tooltipHalfWidth - margin);

            // Vertical: ensure top and bottom inside bounds
            float tooltipTop = pos.y + tooltipTotalHeight;
            if (tooltipTop > b.max.y - margin)
                pos.y -= tooltipTop - (b.max.y - margin);

            float tooltipBottom = pos.y - (bottomH * 0.5f);
            if (tooltipBottom < b.min.y + margin)
                pos.y += (b.min.y + margin) - tooltipBottom;
        }
        else if (_mainCamera != null)
        {
            // Fallback: clamp to camera
            float depth = Mathf.Abs(_mainCamera.transform.position.z);
            Vector3 bottomLeft  = _mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
            Vector3 topRight    = _mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, depth));

            float camLeft   = bottomLeft.x;
            float camRight  = topRight.x;
            float camTop    = topRight.y;

            pos.x = Mathf.Clamp(pos.x,
                camLeft  + tooltipHalfWidth  + margin,
                camRight - tooltipHalfWidth  - margin);

            float tooltipTop = pos.y + tooltipTotalHeight;
            if (tooltipTop > camTop - margin)
                pos.y -= tooltipTop - (camTop - margin);
        }

        tooltipTransform.position = pos;
    }

    private void FindClampCollider()
    {
        if (string.IsNullOrWhiteSpace(clampLayerName))
            return;

        int layer = LayerMask.NameToLayer(clampLayerName);
        if (layer < 0)
            return;

        Collider2D[] all = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.layer == layer)
            {
                _clampCollider = all[i];
                return;
            }
        }
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

    private string BuildTooltipContent(Item item)
    {
        if (item == null || item.Conditions == null || item.Conditions.Count == 0)
            return "No conditions.";

        List<string> lines = new List<string>();
        foreach (PlantCondition condition in item.Conditions)
        {
            if (condition == null)
                continue;

            lines.Add(condition.GetDescription());
        }

        return lines.Count > 0 ? string.Join("\n", lines) : "No conditions.";
    }

    private void UpdateConditionIcons()
    {
        if (_ownerItem == null || contentText == null || contentText.textInfo == null)
            return;

        contentText.ForceMeshUpdate();
        var info = contentText.textInfo;
        int lineCount = info.lineCount;

        // Ensure pool
        while (_iconPool.Count < lineCount)
        {
            GameObject go = new GameObject("TooltipIcon", typeof(SpriteRenderer));
            go.transform.SetParent(contentText.transform, false);
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            sr.sortingLayerID = _sortingGroup != null ? _sortingGroup.sortingLayerID : 0;
            sr.sortingOrder = _sortingGroup != null ? _sortingGroup.sortingOrder + textSortingOrderOffset + 1 : 1000;
            _iconPool.Add(sr);
        }

        // Compute per-line satisfied state from conditions
        List<bool> satisfiedList = new List<bool>();
        if (_ownerItem != null && _ownerItem.Conditions != null)
        {
            foreach (PlantCondition cond in _ownerItem.Conditions)
                satisfiedList.Add(_ownerItem.CurrentSlot != null && cond != null && cond.CheckCondition(_ownerItem.CurrentSlot));
        }

        for (int i = 0; i < _iconPool.Count; i++)
        {
            SpriteRenderer sr = _iconPool[i];
            if (i >= lineCount || i >= satisfiedList.Count || (iconV == null && iconX == null))
            {
                sr.gameObject.SetActive(false);
                continue;
            }

            bool sat = satisfiedList[i];
            sr.sprite = sat ? iconV : iconX;
            sr.gameObject.SetActive(sr.sprite != null);
            sr.transform.localScale = Vector3.one * iconScale;

            // Position: relative to contentText local space. Use line baseline.
            var line = info.lineInfo[i];
            float localX = -textWidth * 0.5f + iconOffsetX;
            float localY = line.baseline;
            sr.transform.localPosition = new Vector3(localX, localY, -0.05f);
            if (_sortingGroup != null)
            {
                sr.sortingLayerID = _sortingGroup.sortingLayerID;
                sr.sortingOrder = _sortingGroup.sortingOrder + textSortingOrderOffset + 1;
            }
        }
    }
}
