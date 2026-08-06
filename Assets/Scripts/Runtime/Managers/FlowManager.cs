using System;
using UnityEngine;

[DisallowMultipleComponent]
public class FlowManager : SingletonMonoBehaviour<FlowManager>
{
    [Header("Canvas Transition")]
    [SerializeField] private CanvasTransition canvasTransition;

    [Header("Next Level Transition")]
    [SerializeField] private NextLevelTransition nextLevelTransition;

    private bool isChangingFlow;

    protected override bool PersistAcrossScenes => false;

    protected override void Awake()
    {
        base.Awake();
        EnsureReferences();
        nextLevelTransition?.HideImmediate();
    }

    public void StartGame()
    {
        CanvasManager canvasManager = FindFirstObjectByType<CanvasManager>(FindObjectsInactive.Include);
        if (canvasManager != null)
            canvasManager.ShowGameplayCanvas();
    }

    public void BackToMainMenu()
    {
        CanvasManager canvasManager = FindFirstObjectByType<CanvasManager>(FindObjectsInactive.Include);
        if (canvasManager != null)
            canvasManager.ShowMainMenuCanvas();
    }

    public void NextLevel()
    {
        PlayLevelTransition(() => LevelManager.Instance.LoadNextLevel());
    }

    public void ReplayCurrentLevel()
    {
        PlayLevelTransition(() => LevelManager.Instance.ReloadLevel());
    }

    private async void PlayLevelTransition(Func<bool> loadLevel)
    {
        if (isChangingFlow)
            return;

        EnsureReferences();

        if (loadLevel == null)
            return;

        isChangingFlow = true;

        try
        {
            if (nextLevelTransition != null)
            {
                await nextLevelTransition.PlayAsync(() =>
                {
                    if (!loadLevel.Invoke())
                        Debug.LogWarning("[FlowManager] Level load request did not load a level.", this);
                });
            }
            else
            {
                Debug.LogError("[FlowManager] Next Level Transition must be assigned.", this);
                loadLevel.Invoke();
            }
        }
        finally
        {
            isChangingFlow = false;
        }
    }

    private void EnsureReferences()
    {
        if (canvasTransition == null)
            canvasTransition = FindFirstObjectByType<CanvasTransition>(FindObjectsInactive.Include);

        if (nextLevelTransition == null)
            nextLevelTransition = FindFirstObjectByType<NextLevelTransition>(FindObjectsInactive.Include);
    }
}
