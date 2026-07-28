using UnityEngine;

public class BoosterSelectionOverlay : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float darkness = 0.68f;
    [SerializeField] private int overlaySortingOrder = 30000;

    private SpriteRenderer _overlayRenderer;
    private Texture2D _overlayTexture;
    private Sprite _overlaySprite;

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<BoosterSelectionChangedEvent>(HandleSelectionChanged);
    }

    private void OnDisable()
    {
        if (EventBus.TryGetInstance(out EventBus eventBus))
            eventBus.Unsubscribe<BoosterSelectionChangedEvent>(HandleSelectionChanged);

        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (_overlaySprite != null)
            Destroy(_overlaySprite);

        if (_overlayTexture != null)
            Destroy(_overlayTexture);
    }

    private void HandleSelectionChanged(BoosterSelectionChangedEvent gameEvent)
    {
        SetVisible(gameEvent.SelectionMode != BoosterSelectionMode.None);
    }

    private void SetVisible(bool visible)
    {
        if (visible)
        {
            EnsureOverlay();
            FitOverlayToCamera();
        }

        if (_overlayRenderer != null)
            _overlayRenderer.gameObject.SetActive(visible);

        if (!BoardManager.TryGetInstance(out BoardManager boardManager))
            return;

        foreach (Item item in boardManager.LevelItems)
        {
            if (item != null)
                item.View.SetBoosterSelectionFocus(visible, 0, overlaySortingOrder + 1);
        }
    }

    private void EnsureOverlay()
    {
        if (_overlayRenderer != null)
            return;

        GameObject overlayObject = new GameObject("Booster Selection Overlay");
        overlayObject.transform.SetParent(transform, false);
        _overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
        _overlayRenderer.sortingLayerID = 0;
        _overlayRenderer.sortingOrder = overlaySortingOrder;
        _overlayRenderer.color = new Color(0f, 0f, 0f, darkness);

        _overlayTexture = new Texture2D(1, 1);
        _overlayTexture.name = "Booster Selection Overlay Texture";
        _overlayTexture.SetPixel(0, 0, Color.white);
        _overlayTexture.Apply();

        _overlaySprite = Sprite.Create(
            _overlayTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        _overlaySprite.name = "Booster Selection Overlay Sprite";
        _overlayRenderer.sprite = _overlaySprite;
    }

    private void FitOverlayToCamera()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null || !targetCamera.orthographic)
        {
            Debug.LogWarning("[BoosterSelectionOverlay] An orthographic main camera is required.");
            return;
        }

        float height = targetCamera.orthographicSize * 2f;
        float width = height * targetCamera.aspect;
        _overlayRenderer.transform.position = new Vector3(
            targetCamera.transform.position.x,
            targetCamera.transform.position.y,
            0f);
        _overlayRenderer.transform.localScale = new Vector3(width, height, 1f);
    }
}
