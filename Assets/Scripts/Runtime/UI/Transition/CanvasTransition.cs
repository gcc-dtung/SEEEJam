using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CanvasTransition : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private RectTransform squareCover;
    [SerializeField] private Image circleOverlay;

    [Header("Material")]
    [SerializeField] private Material circleCutoutMaterial;

    [Header("Square")]
    [SerializeField] private float squareDuration = 0.35f;
    [SerializeField] private Vector2 squareStartPos = new Vector2(1500f, -700f);
    [SerializeField] private Vector2 squareEndPos = Vector2.zero;
    [SerializeField] private AnimationCurve squareCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Circle Reveal")]
    [SerializeField] private float circleDuration = 0.8f;
    [SerializeField] private AnimationCurve radiusCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.55f, 0.85f),
        new Keyframe(0.7f, 0.68f),
        new Keyframe(1f, 1.8f)
    );

    private Material circleMaterial;
    private Material circleSourceMaterial;
    private bool isPlaying;

    private static readonly int RadiusId = Shader.PropertyToID(Constants.ShaderPropertyNames.Radius);

    public bool IsPlaying => isPlaying;

    public float CoverDuration => Mathf.Max(0.01f, squareDuration);
    public float RevealDuration => Mathf.Max(0.01f, circleDuration);
    public float TotalDuration => Mathf.Max(0.01f, squareDuration + circleDuration);

    private void Awake()
    {
        EnsureCircleMaterial();
        HideVisuals();
    }

    private void OnDestroy()
    {
        if (circleMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(circleMaterial);
        else
            DestroyImmediate(circleMaterial);
    }

    public async Awaitable PlayAsync(Action onCovered)
    {
        if (!BeginCover())
            return;

        await PlaySquareInAsync();

        onCovered?.Invoke();
        await PlayRevealAsync();
    }

    public async Awaitable PlayCoverAsync()
    {
        if (!BeginCover())
            return;

        await PlaySquareInAsync();
    }

    public IEnumerator PlayCoverRoutine()
    {
        if (!BeginCover())
            yield break;

        yield return PlaySquareInRoutine();
    }

    public async Awaitable PlayRevealAsync()
    {
        if (!isPlaying)
            return;

        squareCover.gameObject.SetActive(false);
        circleOverlay.gameObject.SetActive(true);

        await PlayCircleRevealAsync();

        Hide();
    }

    public IEnumerator PlayRevealRoutine()
    {
        if (!isPlaying)
            yield break;

        squareCover.gameObject.SetActive(false);
        circleOverlay.gameObject.SetActive(true);

        yield return PlayCircleRevealRoutine();

        Hide();
    }

    private bool BeginCover()
    {
        if (isPlaying)
            return false;

        EnsureCircleMaterial();
        gameObject.SetActive(true);
        BringToFront();

        circleOverlay.gameObject.SetActive(false);
        squareCover.gameObject.SetActive(true);

        squareCover.anchoredPosition = squareStartPos;
        if (!SetCircleRadius(0f))
        {
            gameObject.SetActive(false);
            return false;
        }

        isPlaying = true;
        return true;
    }

    private async Awaitable PlaySquareInAsync()
    {
        float time = 0f;

        while (time < CoverDuration)
        {
            float t = time / CoverDuration;
            float curvedT = squareCurve.Evaluate(t);

            squareCover.anchoredPosition = Vector2.LerpUnclamped(
                squareStartPos,
                squareEndPos,
                curvedT
            );

            time += Time.unscaledDeltaTime;
            await Awaitable.NextFrameAsync();
        }

        squareCover.anchoredPosition = squareEndPos;
    }

    private IEnumerator PlaySquareInRoutine()
    {
        float time = 0f;

        while (time < CoverDuration)
        {
            float t = time / CoverDuration;
            float curvedT = squareCurve.Evaluate(t);

            squareCover.anchoredPosition = Vector2.LerpUnclamped(
                squareStartPos,
                squareEndPos,
                curvedT
            );

            time += Time.unscaledDeltaTime;
            yield return null;
        }

        squareCover.anchoredPosition = squareEndPos;
    }

    private async Awaitable PlayCircleRevealAsync()
    {
        float time = 0f;

        while (time < RevealDuration)
        {
            float t = time / RevealDuration;
            float radius = radiusCurve.Evaluate(t);

            SetCircleRadius(radius);

            time += Time.unscaledDeltaTime;
            await Awaitable.NextFrameAsync();
        }

        SetCircleRadius(radiusCurve.Evaluate(1f));
    }

    private IEnumerator PlayCircleRevealRoutine()
    {
        float time = 0f;

        while (time < RevealDuration)
        {
            float t = time / RevealDuration;
            float radius = radiusCurve.Evaluate(t);

            SetCircleRadius(radius);

            time += Time.unscaledDeltaTime;
            yield return null;
        }

        SetCircleRadius(radiusCurve.Evaluate(1f));
    }

    public void Preview(float normalizedTime)
    {
        EnsureCircleMaterial();

        normalizedTime = Mathf.Clamp01(normalizedTime);
        gameObject.SetActive(true);

        float totalDuration = squareDuration + circleDuration;
        float squareRatio = totalDuration <= 0f ? 0f : squareDuration / totalDuration;

        if (normalizedTime <= squareRatio)
        {
            float squareT = squareRatio <= 0f ? 1f : normalizedTime / squareRatio;
            float curvedT = squareCurve.Evaluate(squareT);

            squareCover.gameObject.SetActive(true);
            circleOverlay.gameObject.SetActive(false);
            squareCover.anchoredPosition = Vector2.LerpUnclamped(squareStartPos, squareEndPos, curvedT);
            SetCircleRadius(0f);
            return;
        }

        float circleT = Mathf.InverseLerp(squareRatio, 1f, normalizedTime);

        squareCover.gameObject.SetActive(false);
        circleOverlay.gameObject.SetActive(true);
        squareCover.anchoredPosition = squareEndPos;
        SetCircleRadius(radiusCurve.Evaluate(circleT));
    }

    public void ResetPreview()
    {
        EnsureCircleMaterial();

        squareCover.gameObject.SetActive(true);
        circleOverlay.gameObject.SetActive(false);
        squareCover.anchoredPosition = squareStartPos;
        SetCircleRadius(0f);
        gameObject.SetActive(false);
    }

    private bool SetCircleRadius(float radius)
    {
        EnsureCircleMaterial();

        if (circleMaterial == null)
            return false;

        circleMaterial.SetFloat(RadiusId, radius);
        return true;
    }

    private void EnsureCircleMaterial()
    {
        if (!circleOverlay)
            return;

        Material sourceMaterial = circleCutoutMaterial ? circleCutoutMaterial : circleOverlay.material;
        if (!sourceMaterial)
        {
            Debug.LogError(
                "CanvasTransition is missing the circle cutout material. Assign Mat_CircleCutout to Circle Cutout Material.",
                this);
            return;
        }

        if (!sourceMaterial.HasProperty(RadiusId))
        {
            Debug.LogError(
                "CanvasTransition needs a circle cutout material with a _Radius property. " +
                "Assign Mat_CircleCutout to Circle Cutout Material or to the Circle Overlay Image.",
                this);
            return;
        }

        if (circleMaterial && circleSourceMaterial == sourceMaterial && circleMaterial.HasProperty(RadiusId))
            return;

        if (circleMaterial)
        {
            if (Application.isPlaying)
                Destroy(circleMaterial);
            else
                DestroyImmediate(circleMaterial);
        }

        circleMaterial = Instantiate(sourceMaterial);
        circleSourceMaterial = sourceMaterial;
        circleMaterial.name = $"{sourceMaterial.name} (Canvas Transition Instance)";
        circleMaterial.hideFlags = HideFlags.DontSave;
        circleOverlay.material = circleMaterial;
    }

    private void BringToFront()
    {
        transform.SetAsLastSibling();

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            return;

        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
    }

    private void Hide()
    {
        isPlaying = false;
        HideVisuals();
        gameObject.SetActive(false);
    }

    public void HideImmediate()
    {
        Hide();
    }

    private void HideVisuals()
    {
        SetCircleRadius(0f);

        if (squareCover != null)
            squareCover.gameObject.SetActive(false);

        if (circleOverlay != null)
            circleOverlay.gameObject.SetActive(false);
    }
}
