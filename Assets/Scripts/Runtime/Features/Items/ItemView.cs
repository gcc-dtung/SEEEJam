using PrimeTween;
using UnityEngine;

public class ItemView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Transform _visualTransform;
    private PlantVisual _plantVisual;
    private Vector3 _normalScale = Vector3.one;
    private Tween _opacityTween;
    private Tween _scaleTween;
    private bool _hasBoosterSortingOverride;
    private int _normalSortingLayerId;
    private int _normalSortingOrder;

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnDisable()
    {
        _opacityTween.Stop();
        _scaleTween.Stop();
        RestoreAppearanceImmediately();
    }

    public void ShowNormal()
    {
        SetColor(Color.white);
    }

    public void ShowCorrect()
    {
        SetColor(Color.blue);
    }

    public void ShowWrong()
    {
        SetColor(Color.red);
    }

    public void SetDragAppearance(float scaleMultiplier, float opacity, float duration)
    {
        EnsureReferences();
        TweenScale(_normalScale * scaleMultiplier, duration);
        TweenOpacity(opacity, duration);
    }

    public void RestoreAppearance(float duration)
    {
        EnsureReferences();
        TweenScale(_normalScale, duration);
        TweenOpacity(1f, duration);
    }

    public void RestoreAppearanceImmediately()
    {
        EnsureReferences();

        if (_visualTransform != null)
            _visualTransform.localScale = _normalScale;

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }
    }

    public void SetBoosterSelectionFocus(bool focused, int sortingLayerId, int sortingOrder)
    {
        EnsureReferences();
        if (spriteRenderer == null)
            return;

        if (focused)
        {
            if (!_hasBoosterSortingOverride)
            {
                _normalSortingLayerId = spriteRenderer.sortingLayerID;
                _normalSortingOrder = spriteRenderer.sortingOrder;
                _hasBoosterSortingOverride = true;
            }

            spriteRenderer.sortingLayerID = sortingLayerId;
            spriteRenderer.sortingOrder = sortingOrder;
            _plantVisual?.SyncSortingWithSkin();
            return;
        }

        if (!_hasBoosterSortingOverride)
            return;

        spriteRenderer.sortingLayerID = _normalSortingLayerId;
        spriteRenderer.sortingOrder = _normalSortingOrder;
        _plantVisual?.SyncSortingWithSkin();
        _hasBoosterSortingOverride = false;
    }

    private void SetColor(Color color)
    {
        EnsureReferences();
        if (spriteRenderer == null)
            return;

        color.a = spriteRenderer.color.a;
        spriteRenderer.color = color;
    }

    private void TweenScale(Vector3 targetScale, float duration)
    {
        if (_visualTransform == null)
            return;

        if (_scaleTween.isAlive)
            _scaleTween.Stop();

        if (_visualTransform.localScale != targetScale)
            _scaleTween = Tween.Scale(_visualTransform, targetScale, duration);
    }

    private void TweenOpacity(float opacity, float duration)
    {
        if (spriteRenderer == null)
            return;

        if (_opacityTween.isAlive)
            _opacityTween.Stop();

        Color currentColor = spriteRenderer.color;
        Color targetColor = currentColor;
        targetColor.a = opacity;

        if (currentColor != targetColor)
            _opacityTween = Tween.Custom(currentColor, targetColor, duration, value => spriteRenderer.color = value);
    }

    private void EnsureReferences()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (_visualTransform == null && spriteRenderer != null)
        {
            _visualTransform = spriteRenderer.transform;
            _normalScale = _visualTransform.localScale;
        }

        if (_plantVisual == null)
            _plantVisual = GetComponent<PlantVisual>();
    }
}
