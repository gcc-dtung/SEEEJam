using System;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelTransitionManager : MonoBehaviour
{
    [Header("Transition")]
    [SerializeField] private NextLevelTransition nextLevelTransition;

    private bool isTransitioning;

    private void Awake()
    {
        EnsureReferences();

        if (nextLevelTransition != null)
            nextLevelTransition.HideImmediate();
    }

    public void LoadNextLevel()
    {
        PlayLevelTransition(() => LevelManager.Instance.LoadNextLevel());
    }

    public void ReloadCurrentLevel()
    {
        PlayLevelTransition(() => LevelManager.Instance.ReloadLevel());
    }

    public void LoadPreviousLevel()
    {
        PlayLevelTransition(() => LevelManager.Instance.LoadPreviousLevel());
    }

    private async void PlayLevelTransition(Func<bool> loadLevel)
    {
        if (isTransitioning)
            return;

        EnsureReferences();

        if (loadLevel == null)
            return;

        if (nextLevelTransition == null)
        {
            Debug.LogError("[LevelTransitionManager] Next Level Transition must be assigned.", this);
            loadLevel.Invoke();
            return;
        }

        isTransitioning = true;

        try
        {
            await nextLevelTransition.PlayAsync(() =>
            {
                if (!loadLevel.Invoke())
                    Debug.LogWarning("[LevelTransitionManager] Level load request did not load a level.", this);
            });
        }
        finally
        {
            isTransitioning = false;
        }
    }

    private void EnsureReferences()
    {
        if (nextLevelTransition == null)
            nextLevelTransition = FindFirstObjectByType<NextLevelTransition>(FindObjectsInactive.Include);
    }
}
