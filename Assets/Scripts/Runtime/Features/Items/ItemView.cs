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
    private PlantVisual _plantVisual;
    private Vector3 _normalScale = Vector3.one;
    private Tween _opacityTween;
    private Tween _scaleTween;
    private bool _hasBoosterSortingOverride;
    private int _normalSortingLayerId;
    private int _normalSortingOrder;

    public SpriteRenderer SpriteRenderer
    {
        get
        {
            EnsureReferences();
            return characterRenderer;
        }
    }

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
        if (_plantVisual != null)
        {
            SetColor(Color.white);
            return;
        }
    }

    public void ShowWrong()
    {
        if (_plantVisual != null)
        {
            SetColor(Color.white);
            return;
        }
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
            if (expressionRenderer != null)
            {
                expressionRenderer.sortingLayerID = sortingLayerId;
                expressionRenderer.sortingOrder = sortingOrder + 1;
            }
            if (effectRenderer != null)
            {
                effectRenderer.sortingLayerID = sortingLayerId;
                effectRenderer.sortingOrder = sortingOrder + 2;
            }
            _plantVisual?.SyncSortingWithSkin();
            return;
        }

        if (!_hasBoosterSortingOverride)
            return;

        characterRenderer.sortingLayerID = _normalSortingLayerId;
        characterRenderer.sortingOrder = _normalSortingOrder;
        if (expressionRenderer != null)
        {
            expressionRenderer.sortingLayerID = _normalSortingLayerId;
            expressionRenderer.sortingOrder = _normalSortingOrder + 1;
        }
        if (effectRenderer != null)
        {
            effectRenderer.sortingLayerID = _normalSortingLayerId;
            effectRenderer.sortingOrder = _normalSortingOrder + 2;
        }
        _plantVisual?.SyncSortingWithSkin();
        _hasBoosterSortingOverride = false;
    }

    private void SetColor(Color color)
    {
        EnsureReferences();
        if (characterRenderer == null)
            return;

        color.a = characterRenderer.color.a;
        characterRenderer.color = color;
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

        if (_plantVisual == null)
            _plantVisual = GetComponent<PlantVisual>();
    }

    private void EnsureTreeVisualDatabase()
    {
        if (treeVisualDatabase == null)
            treeVisualDatabase = Resources.Load<TreeVisualDatabase>("TreeVisualDatabase");
    }
}
