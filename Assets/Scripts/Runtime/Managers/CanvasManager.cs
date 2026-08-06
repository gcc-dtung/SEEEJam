using System.Collections;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    private enum CanvasScreen
    {
        MainMenu,
        Gameplay
    }

    [Header("Canvases")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject gameplayCanvas;

    [Header("Transition")]
    [SerializeField] private CanvasTransition transitionCanvas;

    private CanvasScreen _currentScreen;
    private bool _isTransitioning;

    private void Awake()
    {
        EnsureReferences();
        if (transitionCanvas != null)
            transitionCanvas.HideImmediate();

        SetActiveScreen(CanvasScreen.MainMenu);
    }

    public void ShowMainMenuCanvas()
    {
        SwitchToScreen(CanvasScreen.MainMenu);
    }

    public void ShowGameplayCanvas()
    {
        SwitchToScreen(CanvasScreen.Gameplay);
    }

    private void SwitchToScreen(CanvasScreen targetScreen)
    {
        EnsureReferences();

        if (_isTransitioning)
            return;

        if (mainMenuCanvas == null || gameplayCanvas == null)
        {
            Debug.LogError("[CanvasManager] Main Menu Canvas and Gameplay Canvas must be assigned.", this);
            return;
        }

        if (_currentScreen == targetScreen)
        {
            SetActiveScreen(targetScreen);
            return;
        }

        _isTransitioning = true;
        StartCoroutine(SwitchToScreenRoutine(targetScreen));
    }

    private IEnumerator SwitchToScreenRoutine(CanvasScreen targetScreen)
    {
        if (transitionCanvas == null)
        {
            Debug.LogError("[CanvasManager] Transition Canvas must be assigned before switching screens.", this);
            _isTransitioning = false;
            yield break;
        }

        yield return transitionCanvas.PlayCoverRoutine();
        if (!transitionCanvas.IsPlaying)
        {
            Debug.LogError("[CanvasManager] Transition cover could not start, so the canvas was not switched.", this);
            _isTransitioning = false;
            yield break;
        }

        SetActiveScreen(targetScreen);
        yield return null;
        yield return transitionCanvas.PlayRevealRoutine();
        _isTransitioning = false;
    }

    private void SetActiveScreen(CanvasScreen screen)
    {
        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(screen == CanvasScreen.MainMenu);

        if (gameplayCanvas != null)
            gameplayCanvas.SetActive(screen == CanvasScreen.Gameplay);

        _currentScreen = screen;
    }

    private void EnsureReferences()
    {
        if (mainMenuCanvas == null)
            mainMenuCanvas = FindCanvasObject("MainMenu Canvas");

        if (gameplayCanvas == null)
            gameplayCanvas = FindCanvasObject("Gameplay Canvas");

        if (transitionCanvas == null)
            transitionCanvas = FindFirstObjectByType<CanvasTransition>(FindObjectsInactive.Include);
    }

    private static GameObject FindCanvasObject(string canvasName)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
            if (canvases[i].name == canvasName)
                return canvases[i].gameObject;

        return null;
    }
}
