using System;
using UnityEngine;
using UnityEngine.UI;

public enum TransitionSlideDirection
{
    RightToLeft,
    LeftToRight
}

[DisallowMultipleComponent]
public sealed class NextLevelTransition : MonoBehaviour
{
    private const int RequiredBarCount = 7;

    [Header("Manual References")]
    [Tooltip("Drag the full-screen overlay RectTransform here. It can be this component's own RectTransform.")]
    [SerializeField] private RectTransform transitionRoot;

    [Tooltip("Exactly 7 bars, ordered from top to bottom. Set each bar color on its Image component.")]
    [SerializeField] private RectTransform[] bars = new RectTransform[RequiredBarCount];

    [Header("Motion")]
    [SerializeField] private TransitionSlideDirection direction = TransitionSlideDirection.RightToLeft;
    [SerializeField, Min(0.05f)] private float moveDuration = 0.42f;
    [SerializeField, Min(0f)] private float barStagger = 0.055f;
    [SerializeField, Min(1f)] private float travelDistanceMultiplier = 1.1f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector2[] coveredPositions;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        DisableTransitionRootGraphic();
    }

    public void HideImmediate()
    {
        if (transitionRoot != null)
        {
            transitionRoot.gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(false);
    }

    public async Awaitable PlayAsync(Action onCovered)
    {
        if (isPlaying)
            return;

        if (!HasValidReferences())
        {
            onCovered?.Invoke();
            return;
        }

        isPlaying = true;
        transitionRoot.gameObject.SetActive(true);
        DisableTransitionRootGraphic();
        Canvas.ForceUpdateCanvases();
        CacheCoveredPositions();
        SetBarsAtEntrySide();

        try
        {
            await AnimateBarsAsync(isEntering: true);

            onCovered?.Invoke();

            // Keep the old level hidden until the replacement has completed one frame.
            await Awaitable.NextFrameAsync();
            await AnimateBarsAsync(isEntering: false);
        }
        finally
        {
            isPlaying = false;
            transitionRoot.gameObject.SetActive(false);
        }
    }

    private async Awaitable AnimateBarsAsync(bool isEntering)
    {
        float duration = Mathf.Max(0.05f, moveDuration);
        float stagger = Mathf.Max(0f, barStagger);
        int maximumOrder = GetStaggerOrder(0);
        float totalDuration = duration + stagger * maximumOrder;
        float travelDistance = GetTravelDistance();
        float entrySide = direction == TransitionSlideDirection.RightToLeft ? 1f : -1f;
        float exitSide = -entrySide;
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            for (int index = 0; index < bars.Length; index++)
            {
                float delay = GetStaggerOrder(index) * stagger;
                float normalizedTime = Mathf.Clamp01((elapsed - delay) / duration);
                float curvedTime = moveCurve != null
                    ? moveCurve.Evaluate(normalizedTime)
                    : normalizedTime;

                Vector2 coveredPosition = coveredPositions[index];
                float fromX = isEntering
                    ? coveredPosition.x + entrySide * travelDistance
                    : coveredPosition.x;
                float toX = isEntering
                    ? coveredPosition.x
                    : coveredPosition.x + exitSide * travelDistance;

                bars[index].anchoredPosition = new Vector2(
                    Mathf.LerpUnclamped(fromX, toX, curvedTime),
                    coveredPosition.y
                );
            }

            elapsed += Time.unscaledDeltaTime;
            await Awaitable.NextFrameAsync();
        }

        for (int index = 0; index < bars.Length; index++)
        {
            Vector2 coveredPosition = coveredPositions[index];
            float finalX = isEntering
                ? coveredPosition.x
                : coveredPosition.x + exitSide * travelDistance;

            bars[index].anchoredPosition = new Vector2(finalX, coveredPosition.y);
        }
    }

    private void SetBarsAtEntrySide()
    {
        float travelDistance = GetTravelDistance();
        float entrySide = direction == TransitionSlideDirection.RightToLeft ? 1f : -1f;

        for (int index = 0; index < bars.Length; index++)
        {
            Vector2 coveredPosition = coveredPositions[index];
            bars[index].anchoredPosition = new Vector2(
                coveredPosition.x + entrySide * travelDistance,
                coveredPosition.y
            );
        }
    }

    private int GetStaggerOrder(int index)
    {
        int centerIndex = bars.Length / 2;
        return Mathf.Abs(index - centerIndex);
    }

    private float GetTravelDistance()
    {
        float maximumWidth = transitionRoot.rect.width;

        foreach (RectTransform bar in bars)
            maximumWidth = Mathf.Max(maximumWidth, bar.rect.width);

        return maximumWidth * Mathf.Max(1f, travelDistanceMultiplier);
    }

    private void CacheCoveredPositions()
    {
        if (coveredPositions != null && coveredPositions.Length == bars.Length)
            return;

        coveredPositions = new Vector2[bars.Length];

        for (int index = 0; index < bars.Length; index++)
            coveredPositions[index] = bars[index].anchoredPosition;
    }

    private bool HasValidReferences()
    {
        if (transitionRoot == null)
        {
            Debug.LogError("NextLevelTransition needs a Transition Root reference.", this);
            return false;
        }

        if (bars == null || bars.Length != RequiredBarCount)
        {
            Debug.LogError($"NextLevelTransition needs exactly {RequiredBarCount} bars.", this);
            return false;
        }

        for (int index = 0; index < bars.Length; index++)
        {
            if (bars[index] != null)
                continue;

            Debug.LogError($"NextLevelTransition Bar {index} is not assigned.", this);
            return false;
        }

        return true;
    }

    private void DisableTransitionRootGraphic()
    {
        if (transitionRoot == null)
            return;

        if (!transitionRoot.TryGetComponent(out Graphic rootGraphic))
            return;

        rootGraphic.enabled = false;
        rootGraphic.raycastTarget = false;
    }
}
