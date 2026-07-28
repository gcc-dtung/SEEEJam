using TMPro;
using UnityEngine;

public class HudViewUpPanelView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moveCounterText;
    [SerializeField] private TextMeshProUGUI levelNameText;

    private void Awake()
    {
        EnsureTextReferences();
    }

    private void OnEnable()
    {
        EnsureTextReferences();
        EventBus.Instance.Subscribe<MovesChangedEvent>(HandleMovesChanged);
        EventBus.Instance.Subscribe<LevelChangedEvent>(HandleLevelChanged);
        
        if (moveCounterText != null)
            moveCounterText.text = LevelManager.Instance.RemainingMoves.ToString();
        if (levelNameText != null)
            levelNameText.text = LevelManager.Instance.CurrentLevelName;
    }

    private void OnDisable()
    {
        if (!EventBus.TryGetInstance(out EventBus eventBus))
            return;

        eventBus.Unsubscribe<MovesChangedEvent>(HandleMovesChanged);
        eventBus.Unsubscribe<LevelChangedEvent>(HandleLevelChanged);
    }

    private void HandleMovesChanged(MovesChangedEvent gameEvent)
    {
        if (moveCounterText != null)
            moveCounterText.text = gameEvent.RemainingMoves.ToString();
    }

    private void HandleLevelChanged(LevelChangedEvent gameEvent)
    {
        if (levelNameText != null)
            levelNameText.text = gameEvent.LevelName;
    }

    private void EnsureTextReferences()
    {
        if (moveCounterText == null)
            moveCounterText = FindText("Move Count");
        if (levelNameText == null)
            levelNameText = FindText("Level Count");
    }

    private TextMeshProUGUI FindText(string childName)
    {
        TextMeshProUGUI[] childTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < childTexts.Length; i++)
            if (childTexts[i].name == childName)
                return childTexts[i];

        return null;
    }
}
