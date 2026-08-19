using PrimeTween;
using UnityEngine;

public class ItemView : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] private SpriteRenderer characterRenderer;

    [Header("Tree Visuals")]
    [SerializeField] private SpriteRenderer expressionRenderer;
    [SerializeField] private SpriteRenderer effectRenderer;
    [SerializeField] private TreeVisualDatabase treeVisualDatabase;

    [Header("Shared Bieu Cam")]
    [SerializeField] private Sprite happyExpressionSprite;
    [SerializeField] private Sprite sadExpressionSprite;

    [Header("Shared Effect")]
    [SerializeField] private Sprite goodSmellEffectSprite;
    [SerializeField] private Sprite badSmellEffectSprite;

    private TreeVisualEntry _activeEntry;
    private Transform _visualTransform;
    private Vector3 _normalScale = Vector3.one;
    private Tween _opacityTween;
    private Tween _scaleTween;
    private bool _hasBoosterSortingOverride;
    private int _normalSortingLayerId;
    private int _normalSortingOrder;

    public SpriteRenderer SpriteRenderer => characterRenderer;

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
    }

    public void ShowCorrect()
    {
    }

    public void ShowWrong()
    {
    }

    public void ApplyTreeVisualProfile(string treeKey, string fallbackKey = "")
    {
        EnsureTreeVisualDatabase();

        if (treeVisualDatabase != null)
            _activeEntry = treeVisualDatabase.FindEntry(treeKey) ?? treeVisualDatabase.FindEntry(fallbackKey);
        else
            _activeEntry = null;

        ApplyCurrentProfile();
    }

    public void SetMood(bool isHappy)
    {
        EnsureReferences();

        if (_activeEntry == null)
            return;

        Sprite targetCharacterSprite = isHappy
            ? _activeEntry.happyCharacterSprite
            : _activeEntry.sadCharacterSprite;

        if (characterRenderer != null && targetCharacterSprite != null)
            characterRenderer.sprite = targetCharacterSprite;

        Sprite targetExpressionSprite = isHappy
            ? happyExpressionSprite
            : sadExpressionSprite;

        if (expressionRenderer != null && targetExpressionSprite != null)
            expressionRenderer.sprite = targetExpressionSprite;
    }

    public void SetEffect(PlantSmell smell)
    {
        EnsureReferences();

        if (_activeEntry == null || effectRenderer == null)
            return;

        if (smell == PlantSmell.None)
        {
            effectRenderer.enabled = false;
            return;
        }

        effectRenderer.enabled = true;
        Sprite targetEffectSprite = smell == PlantSmell.Disgust
            ? badSmellEffectSprite
            : goodSmellEffectSprite;

        if (targetEffectSprite != null)
            effectRenderer.sprite = targetEffectSprite;
    }

    public void ApplyCurrentProfile()
    {
        EnsureReferences();

        if (_activeEntry == null)
            return;

        if (characterRenderer != null && _activeEntry.sadCharacterSprite != null)
            characterRenderer.sprite = _activeEntry.sadCharacterSprite;

        if (expressionRenderer != null && sadExpressionSprite != null)
            expressionRenderer.sprite = sadExpressionSprite;

        if (effectRenderer != null)
            effectRenderer.enabled = false;
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

        if (characterRenderer != null)
        {
            Color color = characterRenderer.color;
            color.a = 1f;
            characterRenderer.color = color;
        }
    }

    public void SetBoosterSelectionFocus(bool focused, int sortingLayerId, int sortingOrder)
    {
        EnsureReferences();
        if (characterRenderer == null)
            return;

        if (focused)
        {
            if (!_hasBoosterSortingOverride)
            {
                _normalSortingLayerId = characterRenderer.sortingLayerID;
                _normalSortingOrder = characterRenderer.sortingOrder;
                _hasBoosterSortingOverride = true;
            }

            characterRenderer.sortingLayerID = sortingLayerId;
            characterRenderer.sortingOrder = sortingOrder;
            return;
        }

        if (!_hasBoosterSortingOverride)
            return;

        characterRenderer.sortingLayerID = _normalSortingLayerId;
        characterRenderer.sortingOrder = _normalSortingOrder;
        _hasBoosterSortingOverride = false;
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
        if (characterRenderer == null)
            return;

        if (_opacityTween.isAlive)
            _opacityTween.Stop();

        Color currentColor = characterRenderer.color;
        Color targetColor = currentColor;
        targetColor.a = opacity;

        if (currentColor != targetColor)
            _opacityTween = Tween.Custom(currentColor, targetColor, duration, value => characterRenderer.color = value);
    }

    private void EnsureReferences()
    {
        if (characterRenderer == null)
            characterRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (_visualTransform == null && characterRenderer != null)
        {
            _visualTransform = characterRenderer.transform;
            _normalScale = _visualTransform.localScale;
        }
    }

    private void EnsureTreeVisualDatabase()
    {
        if (treeVisualDatabase == null)
            treeVisualDatabase = Resources.Load<TreeVisualDatabase>("TreeVisualDatabase");
    }
}
