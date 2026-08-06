using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WinPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button homeButton;

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

        if (homeButton != null)
            homeButton.onClick.AddListener(HandleHomeClicked);
    }

    private void OnDisable()
    {
        if (EventBus.TryGetInstance(out EventBus eventBus))
            eventBus.Unsubscribe<GameStateChangedEvent>(HandleGameStateChanged);

        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveListener(HandleNextLevelClicked);

        if (homeButton != null)
            homeButton.onClick.RemoveListener(HandleHomeClicked);
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void HideImmediate()
    {
        SetVisible(false);
    }

    private void HandleGameStateChanged(GameStateChangedEvent gameEvent)
    {
        if (gameEvent.CurrentState == GameState.Won)
            Show();
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

    private void HandleHomeClicked()
    {
        HideImmediate();
        FlowManager.Instance.BackToMainMenu();
    }

    private void SetVisible(bool isVisible)
    {
        EnsureReferences();

        if (panelRoot != null)
        {
            panelRoot.SetActive(isVisible);
            return;
        }

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
            else if (homeButton == null && lowerName.Contains("home"))
                homeButton = button;
        }
    }
}
