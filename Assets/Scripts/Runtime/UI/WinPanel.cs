using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WinPanel : MonoBehaviour
{
    [Header("Panel Roots")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameObject losePanelRoot;

    [Header("Buttons")]
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button homeButton;

    [Header("Win Animation")]
    [SerializeField] private RectTransform[] winAnimatedElements;

    [Header("Lose Animation")]
    [SerializeField] private RectTransform[] loseAnimatedElements;

    [Header("Tween Settings")]
    [SerializeField, Min(0.05f)] private float elementShowDuration = 0.22f;
    [SerializeField, Min(0f)] private float elementStagger = 0.08f;
    [SerializeField, Min(0f)] private float hiddenScale = 0.75f;

    private readonly List<Tween> _runningTweens = new List<Tween>();
    private Coroutine _showRoutine;

    private void Awake()
    {
        EnsureReferences();
        HideImmediate();
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<GameStateChangedEvent>(HandleGameStateChanged);

        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(HandleNextLevelClicked);

        if (restartButton != null)
            restartButton.onClick.AddListener(HandleRestartClicked);

        if (homeButton != null)
            homeButton.onClick.AddListener(HandleHomeClicked);
    }

    private void OnDisable()
    {
        if (EventBus.TryGetInstance(out EventBus eventBus))
            eventBus.Unsubscribe<GameStateChangedEvent>(HandleGameStateChanged);

        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveListener(HandleNextLevelClicked);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(HandleRestartClicked);

        if (homeButton != null)
            homeButton.onClick.RemoveListener(HandleHomeClicked);

        StopAnimations();
    }

    public void Show()
    {
        ShowWin();
    }

    public void ShowWin()
    {
        ShowPanel(panelRoot, losePanelRoot, winAnimatedElements);
    }

    public void ShowLose()
    {
        ShowPanel(losePanelRoot, panelRoot, loseAnimatedElements);
    }

    public void HideImmediate()
    {
        StopAnimations();
        SetPanelVisible(panelRoot, false);
        SetPanelVisible(losePanelRoot, false);
    }

    private void HandleGameStateChanged(GameStateChangedEvent gameEvent)
    {
        if (gameEvent.CurrentState == GameState.Won)
            ShowWin();
        else if (gameEvent.CurrentState == GameState.Lost)
            ShowLose();
        else if (gameEvent.CurrentState == GameState.Playing ||
                 gameEvent.CurrentState == GameState.LoadingLevel ||
                 gameEvent.CurrentState == GameState.Boot)
            HideImmediate();
    }

    private void HandleNextLevelClicked()
    {
        HideImmediate();
        FlowManager.Instance.NextLevel();
    }

    private void HandleRestartClicked()
    {
        HideImmediate();
        FlowManager.Instance.ReplayCurrentLevel();
    }

    private void HandleHomeClicked()
    {
        HideImmediate();
        FlowManager.Instance.BackToMainMenu();
    }

    private void ShowPanel(GameObject showRoot, GameObject hideRoot, RectTransform[] animatedElements)
    {
        EnsureReferences();
        StopAnimations();

        if (hideRoot != null)
            hideRoot.SetActive(false);

        if (showRoot == null)
        {
            SetFallbackVisible(true);
            return;
        }

        showRoot.SetActive(true);

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(AnimateElementsIn(animatedElements));
    }

    private IEnumerator AnimateElementsIn(RectTransform[] elements)
    {
        if (elements == null || elements.Length == 0)
            yield break;

        for (int i = 0; i < elements.Length; i++)
        {
            RectTransform target = elements[i];
            if (target == null)
                continue;

            CanvasGroup group = EnsureCanvasGroup(target.gameObject);
            if (group != null)
                group.alpha = 0f;

            target.localScale = Vector3.one * hiddenScale;
        }

        for (int i = 0; i < elements.Length; i++)
        {
            RectTransform target = elements[i];
            if (target == null)
                continue;

            CanvasGroup group = EnsureCanvasGroup(target.gameObject);
            _runningTweens.Add(Tween.Scale(target, Vector3.one, elementShowDuration));

            if (group != null)
            {
                _runningTweens.Add(Tween.Custom(0f, 1f, elementShowDuration, value =>
                {
                    if (group != null)
                        group.alpha = value;
                }));
            }

            if (elementStagger > 0f)
                yield return new WaitForSecondsRealtime(elementStagger);
        }
    }

    private void StopAnimations()
    {
        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        for (int i = 0; i < _runningTweens.Count; i++)
            _runningTweens[i].Stop();

        _runningTweens.Clear();
    }

    private void SetPanelVisible(GameObject panel, bool isVisible)
    {
        if (panel != null)
        {
            panel.SetActive(isVisible);
            return;
        }

        SetFallbackVisible(isVisible);
    }

    private void SetFallbackVisible(bool isVisible)
    {
        EnsureReferences();

        if (panelRoot != null)
            panelRoot.SetActive(isVisible);

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = isVisible ? 1f : 0f;
        canvasGroup.interactable = isVisible;
        canvasGroup.blocksRaycasts = isVisible;
    }

    private void EnsureReferences()
    {
        Button[] childButtons = GetComponentsInChildren<Button>(true);
        foreach (Button button in childButtons)
        {
            if (button == null)
                continue;

            string lowerName = button.name.ToLowerInvariant();
            if (nextLevelButton == null && lowerName.Contains("next"))
                nextLevelButton = button;
            else if (restartButton == null && (lowerName.Contains("restart") || lowerName.Contains("retry")))
                restartButton = button;
            else if (homeButton == null && lowerName.Contains("home"))
                homeButton = button;
        }

        if (panelRoot == null)
            panelRoot = gameObject;
    }

    private static CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        if (!target.TryGetComponent(out CanvasGroup canvasGroup))
            canvasGroup = target.AddComponent<CanvasGroup>();

        return canvasGroup;
    }
}
