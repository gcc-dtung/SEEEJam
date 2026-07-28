using System;
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

    private static readonly int RadiusId = Shader.PropertyToID("_Radius");

    public float TotalDuration => Mathf.Max(0.01f, squareDuration + circleDuration);

    private void Awake()
    {
        EnsureCircleMaterial();
        //
        // gameObject.SetActive(false);
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
        if (isPlaying)
            return;

        isPlaying = true;
        EnsureCircleMaterial();
        gameObject.SetActive(true);

        circleOverlay.gameObject.SetActive(false);
        squareCover.gameObject.SetActive(true);

        squareCover.anchoredPosition = squareStartPos;
        if (!SetCircleRadius(0f))
        {
            isPlaying = false;
            return;
        }

        await PlaySquareInAsync();

        onCovered?.Invoke();
        squareCover.gameObject.SetActive(false);
        circleOverlay.gameObject.SetActive(true);

        await PlayCircleRevealAsync();

        gameObject.SetActive(false);
        isPlaying = false;
    }

    private async Awaitable PlaySquareInAsync()
    {
        float time = 0f;

        while (time < squareDuration)
        {
            float t = time / squareDuration;
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

    private async Awaitable PlayCircleRevealAsync()
    {
        float time = 0f;

        while (time < circleDuration)
        {
            float t = time / circleDuration;
            float radius = radiusCurve.Evaluate(t);

        SetCircleRadius(radius);

            time += Time.unscaledDeltaTime;
            await Awaitable.NextFrameAsync();
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
}
